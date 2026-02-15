using CustomConsoleProviderExample;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Clear the default providers and register our custom colored console provider.
// The parameterless overload reads configuration from appsettings.json under
// "Logging:ColorConsole" thanks to [ProviderAlias] and RegisterProviderOptions.
builder.Logging.ClearProviders();
builder.Logging.AddColorConsole();

// Set minimum level to Debug so that Debug-level messages are visible.
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// Alternative: configure entirely in code using the Action<T> overload:
// builder.Logging.AddColorConsole(options =>
// {
//     options.LogLevelToColorMap[LogLevel.Warning] = ConsoleColor.Magenta;
// });

var app = builder.Build();

// Logs a simple informational message. Appears in green (the default color
// for the Information level).
app.MapGet("/", (ILogger<Program> logger) =>
{
    logger.LogInformation("Request received for {Path}", "/");
    return "Hello from the custom colored console logger!";
});

// Logs at multiple levels to demonstrate color differentiation.
// Each level appears in a different color on the console.
app.MapGet("/weather", (ILogger<Program> logger) =>
{
    var forecast = new WeatherForecast("London", 38.5, "Scorching");

    logger.LogDebug("Looking up forecast data for {City}", forecast.City);
    logger.LogInformation("Forecast for {City}: {TemperatureC}C, {Summary}",
        forecast.City, forecast.TemperatureC, forecast.Summary);

    const double threshold = 35.0;
    if (forecast.TemperatureC > threshold)
    {
        logger.LogWarning("Temperature {TemperatureC}C exceeds threshold {ThresholdC}C in {City}",
            forecast.TemperatureC, threshold, forecast.City);
    }

    return forecast;
});

// Demonstrates error-level logging with an exception.
// The exception details are written on a separate line after the message.
app.MapGet("/sensor", (ILogger<Program> logger) =>
{
    var reading = new SensorReading("temp-sensor-01", 18.3, DateTime.UtcNow);

    logger.LogInformation("Sensor {SensorId} reported {TemperatureC}C at {RecordedAt}",
        reading.SensorId, reading.TemperatureC, reading.RecordedAt);

    try
    {
        throw new InvalidOperationException("Calibration data expired for sensor temp-sensor-01");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to process reading from sensor {SensorId}", reading.SensorId);
    }

    logger.LogInformation("Sensor pipeline completed");

    return reading;
});

app.Run();

record WeatherForecast(string City, double TemperatureC, string Summary);

record SensorReading(string SensorId, double TemperatureC, DateTime RecordedAt);
