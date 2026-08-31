using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RiskService.Exceptions;

namespace RiskService.ExceptionHandlers
{
    public class MarketDataTimeoutHandler : IExceptionHandler
    {
        private readonly ILogger<MarketDataTimeoutHandler> _logger;

        public MarketDataTimeoutHandler(ILogger<MarketDataTimeoutHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            if (exception is not MarketDataTimeoutException)
            {
                return false;
            }

            var problemDetails = new ProblemDetails()
            {
                Status = StatusCodes.Status504GatewayTimeout,
                Title = "Request to Market Data Service timed out.",
                Detail = "Current risk data could not be calculated, request to Market Data Service timed out. Please try again later."
            };

            _logger.LogError(exception, "Request to Market data service timed out while processing {RequestPath}", httpContext.Request.Path);

            httpContext.Response.StatusCode = StatusCodes.Status504GatewayTimeout;

            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken: cancellationToken);

            return true;
        }
    }
}
