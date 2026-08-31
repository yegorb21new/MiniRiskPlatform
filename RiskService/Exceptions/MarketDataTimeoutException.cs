namespace RiskService.Exceptions
{
    public class MarketDataTimeoutException : Exception
    {
        public MarketDataTimeoutException(string message) : base(message) { }
        public MarketDataTimeoutException(string message, Exception innerException) : base(message, innerException) { }
    }
}
