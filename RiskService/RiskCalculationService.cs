using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace RiskService
{
    public class RiskCalculationService
    {
        private readonly RiskRepository _riskRepository;
        private readonly MarketDataClient _marketDataClient;

        public RiskCalculationService(RiskRepository riskRepository, MarketDataClient marketDataClient)
        {
            _riskRepository = riskRepository;
            _marketDataClient = marketDataClient;
        }

        public async Task<List<PositionRisk>> GetCurrentPositionRiskAsync()
        {
            var positions = await _riskRepository.GetPositionsAsync();
            var neededSymbols = new List<string>();
            neededSymbols = positions
                .Select(p => p.Symbol)
                .Distinct()
                .ToList();

            var symbolQuotes = await _marketDataClient.GetPricesAsync(neededSymbols);
            var quoteBySymbol = symbolQuotes.ToDictionary(q => q.Symbol);
            var positionRisks = new List<PositionRisk>();

            foreach (var position in positions)
            {
                var currPosRisk = new PositionRisk
                {
                    Portfolio = position.Portfolio,
                    Symbol = position.Symbol,
                    Quantity = position.Quantity
                };

                if (!quoteBySymbol.ContainsKey(position.Symbol))
                {
                    currPosRisk.Price = null;
                    currPosRisk.Exposure = null;
                    currPosRisk.PriceAsOf = null;
                    currPosRisk.DataStatus = "Missing";
                }
                else
                {
                    var currSymbol = quoteBySymbol[position.Symbol];
                    currPosRisk.PriceAsOf = currSymbol.Timestamp;
                    var isQuoteStale = (DateTimeOffset.UtcNow - currSymbol.Timestamp).Duration() > TimeSpan.FromMinutes(5);

                    currPosRisk.DataStatus = isQuoteStale ? "Stale" : "Current";

                    currPosRisk.Price = currSymbol.Price;
                    currPosRisk.Exposure = position.Quantity * currSymbol.Price;
                }

                positionRisks.Add(currPosRisk);
            }

            return positionRisks;
        }

        public async Task<List<PortfolioRisk>> GetCurrentPortfolioRiskAsync()
        {
            var positionRisks = await GetCurrentPositionRiskAsync();

            var portfolioRisks = positionRisks
                .GroupBy(x => x.Portfolio)
                .Select(group => new PortfolioRisk()
                {
                    Portfolio = group.Key,
                    GrossExposure = group.Sum(p => Math.Abs(p.Exposure ?? 0m)),
                    NetExposure = group.Sum(p => p.Exposure ?? 0m),
                    IsComplete = group.All(p => p.Exposure is not null),
                    HasStaleData = group.Any(p => p.DataStatus == "Stale"),
                    MissingSymbols = group.Where(p => p.DataStatus == "Missing")
                    .Select(p => p.Symbol).ToList()
                })
                .ToList();

            return portfolioRisks;
        }

    }
}
