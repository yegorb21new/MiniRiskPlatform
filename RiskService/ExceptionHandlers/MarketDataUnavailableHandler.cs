using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RiskService.Exceptions;

namespace RiskService.ExceptionHandlers
{
    public class MarketDataUnavailableHandler : IExceptionHandler
    {
        private readonly ILogger<MarketDataUnavailableHandler> _logger;

        public MarketDataUnavailableHandler(ILogger<MarketDataUnavailableHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            if (exception is not MarketDataUnavailableException)
            {
                return false;
            }

            var problemDetails = new ProblemDetails()
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Market data service unavailable",
                Detail = "Current risk data could not be calculated. Please try again later."
            };

            _logger.LogError(exception, "Market data service was unavailable while processing {RequestPath}", httpContext.Request.Path);

            httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;

            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken: cancellationToken);

            return true;
        }
    }
}
