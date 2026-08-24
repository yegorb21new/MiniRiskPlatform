var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var prices = new Dictionary<string, PriceQuote>(
    StringComparer.OrdinalIgnoreCase)
{
    ["AAPL"] = new(
        "AAPL",
        230.00m,
        DateTimeOffset.UtcNow
    ),
    ["MSFT"] = new(
        "MSFT",
        510.00m,
        DateTimeOffset.UtcNow
    ),
    ["NVDA"] = new(
        "NVDA",
        185.00m,
        DateTimeOffset.UtcNow.AddMinutes(-30)
    ),
    ["GOOG"] = new(
        "GOOG",
        170.00m,
        DateTimeOffset.UtcNow
    )
    // TSLA intentionally omitted
};

app.MapGet("/prices", (string? symbols) =>
{
    if (string.IsNullOrWhiteSpace(symbols))
        return Results.Ok(prices.Values);

    var requestedSymbols = symbols
        .Split(',', StringSplitOptions.RemoveEmptyEntries)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    var result = prices.Values
        .Where(price => requestedSymbols.Contains(price.Symbol));

    return Results.Ok(result);
});

app.Run();

public record PriceQuote(
    string Symbol,
    decimal Price,
    DateTimeOffset Timestamp
);