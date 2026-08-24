using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;

namespace RiskService
{
    public class RiskRepository
    {
        private readonly string _connectionString;

        public RiskRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        private DataTable BuildSnapshotRowsDataTable(Guid runId, List<PositionRisk> positionRisks)
        {
            var table = new DataTable();

            table.Columns.Add("RunId", typeof(Guid));
            table.Columns.Add("Portfolio", typeof(string));
            table.Columns.Add("Symbol", typeof(string));
            table.Columns.Add("Quantity", typeof(int));
            table.Columns.Add("Price", typeof(decimal));
            table.Columns.Add("Exposure", typeof(decimal));
            table.Columns.Add("PriceAsOf", typeof(DateTime));
            table.Columns.Add("DataStatus", typeof(string));

            foreach (var positionRisk in positionRisks)
            {
                table.Rows.Add(
                    runId,
                    positionRisk.Portfolio,
                    positionRisk.Symbol,
                    positionRisk.Quantity,
                    positionRisk.Price ?? (object)DBNull.Value,
                    positionRisk.Exposure ?? (object)DBNull.Value,
                    positionRisk.PriceAsOf?.UtcDateTime ?? (object)DBNull.Value,
                    positionRisk.DataStatus
                );
            }

            return table;
        }

        public async Task<List<Position>> GetPositionsAsync()
        {
            using var sqlConn = new SqlConnection(_connectionString);

            string sql = @"
                SELECT Portfolio, Symbol, SUM(Quantity) as Quantity
                FROM Positions
                GROUP BY Portfolio, Symbol";

            await sqlConn.OpenAsync();

            var aggPositions = await sqlConn.QueryAsync<Position>(sql);

            return aggPositions.ToList();
        }

        public async Task<Guid> SaveRiskSnapshotAsync(List<PositionRisk> positionRisks)
        {
            var runId = Guid.NewGuid();
            var snapshotTime = DateTimeOffset.UtcNow;
            string overallDataStatus;

            if (positionRisks.Any(x => x.DataStatus == "Missing"))
            {
                overallDataStatus = "Missing";
            }
            else if (positionRisks.Any(x => x.DataStatus == "Stale"))
            {
                overallDataStatus = "Stale";
            }
            else
            {
                overallDataStatus = "Current";
            }

            var posRiskDT = BuildSnapshotRowsDataTable(runId, positionRisks);

            using var sqlConn = new SqlConnection(_connectionString);
            await sqlConn.OpenAsync();
            using var transaction = sqlConn.BeginTransaction();

            try
            {
                string sql = @"
                    INSERT INTO dbo.RiskSnapshotRuns 
                        (RunId, SnapshotTime, MarketDataStatus) 
                    VALUES 
                        (@RunId, @SnapshotTime, @MarketDataStatus)";

                await sqlConn.ExecuteAsync(sql, 
                    new 
                    { 
                        RunId = runId, 
                        SnapshotTime = snapshotTime.UtcDateTime, 
                        MarketDataStatus = overallDataStatus 
                    }, transaction);
                
                using var bulkCopy = new SqlBulkCopy(sqlConn, SqlBulkCopyOptions.Default, transaction);

                bulkCopy.DestinationTableName = "dbo.RiskSnapshotRows";

                bulkCopy.ColumnMappings.Add("RunId", "RunId");
                bulkCopy.ColumnMappings.Add("Portfolio", "Portfolio");
                bulkCopy.ColumnMappings.Add("Symbol", "Symbol");
                bulkCopy.ColumnMappings.Add("Quantity", "Quantity");
                bulkCopy.ColumnMappings.Add("Price", "Price");
                bulkCopy.ColumnMappings.Add("Exposure", "Exposure");
                bulkCopy.ColumnMappings.Add("PriceAsOf", "PriceAsOf");
                bulkCopy.ColumnMappings.Add("DataStatus", "DataStatus");

                await bulkCopy.WriteToServerAsync(posRiskDT);

                await transaction.CommitAsync();

                return runId;
            }
            catch
            {
                try
                {
                    await transaction.RollbackAsync();
                } 
                catch (Exception rollbackEx)
                {
                    Console.WriteLine($"Rollback failed: {rollbackEx.Message}");
                }

                throw;
            }
        }

        public async Task<List<HistoricalPortfolioRisk>> GetHistoricalPortfolioRisksAsync()
        {
            using var sqlConn = new SqlConnection(_connectionString);

            await sqlConn.OpenAsync();

            var results = await sqlConn.QueryAsync<HistoricalPortfolioRisk>(
                "dbo.sp_GetHistoricalPortfolioRisk", 
                commandType: CommandType.StoredProcedure);

            return results.ToList();
        }
    }
}