public class Position
{
    public string Portfolio { get; set; }
    public string Symbol { get; set; }
    public int Quantity { get; set; }
}

public class PriceQuote
{
    public string Symbol { get; set; }
    public decimal Price { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}

public class PositionRisk
{
    public string Portfolio { get; set; }
    public string Symbol { get; set; }
    public int Quantity { get; set; }

    public decimal? Price { get; set; }
    public decimal? Exposure { get; set; }

    public DateTimeOffset? PriceAsOf { get; set; }

    public string DataStatus { get; set; }
}

public class PortfolioRisk
{
    public string Portfolio { get; set; }

    public decimal GrossExposure { get; set; }
    public decimal NetExposure { get; set; }

    public bool IsComplete { get; set; }
    public bool HasStaleData { get; set; }

    public List<string> MissingSymbols { get; set; }
}

public class HistoricalPortfolioRisk
{
    public Guid RunId { get; set; }
    public DateTime SnapshotTime { get; set; }
    public string Portfolio { get; set; }
    public decimal GrossExposure { get; set; }
    public decimal NetExposure { get; set; }
    public bool IsComplete { get; set; }
    public bool HasStaleData { get; set; }

}