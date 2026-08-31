using NuGet.Frameworks;
using RiskService.Exceptions;
using System.Net;
using System.Text;

namespace RiskService.Tests
{
    public class MarketDataClientTests
    {
        [Fact]
        public async Task GetPricesAsync_WhenServiceReturnsPrices_ReturnsPriceQuotes()
        {
            // Arrange
            var fakeHandler = new FakeHttpMessageHandler((request, cancellationToken) =>
            {
                var json = """
                    [
                        {
                            "symbol": "AAPL",
                            "price": 230
                        }
                    ]
                    """;

                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        json,
                        Encoding.UTF8,
                        "application/json")
                };

                return Task.FromResult(response);
            });

            var httpClient = new HttpClient(fakeHandler)
            {
                BaseAddress = new Uri("http://localhost:5101")
            };

            var marketDataClient = new MarketDataClient(httpClient);

            // Act
            var result = await marketDataClient.GetPricesAsync(["AAPL"]);

            // Assert
            Assert.Single(result);
            Assert.Equal("AAPL", result[0].Symbol);
            Assert.Equal(230, result[0].Price);
        }

        [Fact]
        public async Task GetPricesAsync_WhenServiceReturnsNull_ThrowsMarketDataUnavailException()
        {
            //Arrange
            var fakeHandler = new FakeHttpMessageHandler((request, cancellationToken) =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        "null",
                        Encoding.UTF8,
                        "application/json")
                };

                return Task.FromResult(response);
            });

            var httpClient = new HttpClient(fakeHandler)
            {
                BaseAddress = new Uri("http://localhost:5101")
            };

            var marketDataClient = new MarketDataClient(httpClient);

            //Act + Assert
            var exception = await Assert.ThrowsAsync<MarketDataUnavailableException>(
                () => marketDataClient.GetPricesAsync(["AAPL"]));

            Assert.Null(exception.InnerException);
        }

        [Fact]
        public async Task GetPricesAsync_WhenRequestFails_ThrowsMarketDataUnavailException()
        {
            var fakeHandler = new FakeHttpMessageHandler((request, cancellationToken) =>
            {
                throw new HttpRequestException("Connection Refused.");
            });

            var httpClient = new HttpClient(fakeHandler)
            {
                BaseAddress = new Uri("http://localhost:5101")
            };

            var marketDataClient = new MarketDataClient(httpClient);

            // Act + Assert
            var exception = await Assert.ThrowsAsync<MarketDataUnavailableException>(
                () => marketDataClient.GetPricesAsync(["AAPL"]));

            Assert.IsType<HttpRequestException>(exception.InnerException);
        }

        [Fact]
        public async Task GetPricesAsync_WhenRequestTimesOut_ThrowsMarketDataTimeoutException()
        {
            var fakeHandler = new FakeHttpMessageHandler((request, cancellationtoken) =>
            {
                throw new OperationCanceledException("The request was canceled.",
                    new TimeoutException("The request timed out."));
            });

            var httpClient = new HttpClient(fakeHandler)
            {
                BaseAddress = new Uri("http://localhost:5101")
            };

            var marketDataClient = new MarketDataClient(httpClient);

            //Act + Assert
            var exception = await Assert.ThrowsAsync<MarketDataTimeoutException>(
                () => marketDataClient.GetPricesAsync(["AAPL"]));

            Assert.IsType<OperationCanceledException>(exception.InnerException);
            Assert.IsType<TimeoutException>(exception.InnerException.InnerException);
        }

        [Fact]
        public async Task GetPricesAsync_WhenRequestIsCanceled_DoesNotTreatCancellationAsTimeout()
        {
            // Arrange
            var fakeHandler = new FakeHttpMessageHandler((request, cancellationToken) =>
            {
                throw new OperationCanceledException("Request was canceled.");
            });

            var httpClient = new HttpClient(fakeHandler)
            {
                BaseAddress = new Uri("http://localhost:5101")
            };

            var marketDataClient = new MarketDataClient(httpClient);

            // Act + Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => marketDataClient.GetPricesAsync(["AAPL"]));
        }
    }
}