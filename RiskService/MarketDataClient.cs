using System.Threading.Tasks;
using System.Net.Http.Json;
using RiskService.Exceptions;

namespace RiskService
{
    public class MarketDataClient
    {
        private readonly HttpClient _httpClient;

        public MarketDataClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<PriceQuote>> GetPricesAsync(IEnumerable<string> symbols)
        {
            var joinedParams = string.Join(",", symbols);
            var encodedParams = Uri.EscapeDataString(joinedParams);
            var url = $"/prices?symbols={encodedParams}";

            try
            {
                var data = await _httpClient.GetFromJsonAsync<List<PriceQuote>>(url)
                    ?? throw new MarketDataUnavailableException("Market data service could not provide prices.");

                return data;
            }
            catch (OperationCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                throw new MarketDataTimeoutException("Request to Market Data Service timed out.", ex);
            }
            catch (HttpRequestException ex)
            {
                throw new MarketDataUnavailableException("Market data service could not provide prices.", ex);
            }
        }
    }
}


