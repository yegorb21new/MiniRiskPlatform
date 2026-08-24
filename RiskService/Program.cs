using Microsoft.Data.SqlClient;
using RiskService;
using System.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddScoped<RiskRepository>(sp => new RiskRepository(connectionString));
builder.Services.AddHttpClient<MarketDataClient>(client =>
{
    client.BaseAddress = new Uri("http://localhost:5101");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});
builder.Services.AddScoped<RiskCalculationService>();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<MarketDataExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.MapGet("/positions", async (RiskRepository repository) =>
{
    var positions = await repository.GetPositionsAsync();
    return Results.Ok(positions);
});

app.MapGet("/risk/positions", async (RiskCalculationService riskCalcService) =>
{
    var positionRisks = await riskCalcService.GetCurrentPositionRiskAsync();
    return Results.Ok(positionRisks);
});

app.MapGet("/risk/portfolios", async (RiskCalculationService riskCalcService) => {
    var portfolioRisks = await riskCalcService.GetCurrentPortfolioRiskAsync();
    return Results.Ok(portfolioRisks);
});

app.MapGet("/risk/history", async (RiskRepository repository) =>
{
    var history = await repository.GetHistoricalPortfolioRisksAsync();
    return Results.Ok(history);
});

app.MapPost("/risk/snapshot", async (
    RiskCalculationService riskCalcService, 
    RiskRepository repository) =>
{
    var positionRisks = await riskCalcService.GetCurrentPositionRiskAsync();
    var id = await repository.SaveRiskSnapshotAsync(positionRisks);
    return Results.Ok(id);
});

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
