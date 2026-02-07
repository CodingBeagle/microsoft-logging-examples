using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Serilog;
using System;

// Configure Serilog with the console sink
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Clear the default logging providers (Console, Debug, EventSource)
// and add Serilog as the logging provider.
builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

var app = builder.Build();

// A simple endpoint that logs scalar values as structured properties.
app.MapGet("/", (ILogger<Program> logger) =>
{
    logger.LogInformation("Request received for {Path} by user {UserId}", "/", 42);
    return "Hello, World!";
});

// This endpoint demonstrates the difference between default rendering and
// Serilog's destructuring operator (@) when logging complex objects through
// Microsoft's ILogger interface.
app.MapGet("/weather", (ILogger<Program> logger) =>
{
    var forecast = new WeatherForecast("London", 22.5, "Sunny");

    // Default: Serilog calls ToString() on the object.
    // Output will contain the record's ToString() representation as a single string value.
    logger.LogInformation("Without destructuring: {Forecast}", forecast);

    // Destructuring (@): Serilog serializes the object into its individual properties.
    // Output will contain each property (City, TemperatureC, Summary) as a structured field.
    logger.LogInformation("With destructuring: {@Forecast}", forecast);

    return forecast;
});

// This endpoint shows destructuring with a nested object, demonstrating that
// Serilog recursively destructures the entire object graph.
app.MapGet("/sensor", (ILogger<Program> logger) =>
{
    var reading = new SensorReading(
        SensorId: "temp-sensor-01",
        Location: new GeoLocation(Latitude: 51.5074, Longitude: -0.1278),
        TemperatureC: 18.3,
        RecordedAt: DateTime.UtcNow);

    logger.LogInformation("Sensor reading received: {@Reading}", reading);

    return reading;
});

app.Run();

record WeatherForecast(string City, double TemperatureC, string Summary);

record GeoLocation(double Latitude, double Longitude);

record SensorReading(string SensorId, GeoLocation Location, double TemperatureC, DateTime RecordedAt);
