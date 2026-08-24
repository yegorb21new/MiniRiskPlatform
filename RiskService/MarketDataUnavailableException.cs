namespace RiskService
{
    public class MarketDataUnavailableException : System.Exception
    {
        public MarketDataUnavailableException(string message) : base(message) { }
        public MarketDataUnavailableException(string message, Exception innerException) : base(message, innerException) { }
    }
}
