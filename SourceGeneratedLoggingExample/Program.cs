using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Serilog;
using SourceGeneratedLoggingExample;
using System;

// Configure Serilog with the console sink.
// MinimumLevel.Debug() is set so that the Debug-level log message is visible in output.
// Enrich.FromLogContext() is required for ILogger.BeginScope properties to appear in log output.
Serilog.Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Clear the default logging providers (Console, Debug, EventSource)
// and add Serilog as the logging provider.
builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

var app = builder.Build();

// A simple endpoint that logs scalar values as structured properties.
// Compare with SimpleLoggingExample's GET / — the behavior is identical, but
// the source-generated method avoids re-parsing the template on every call
// and avoids boxing the int parameter.
app.MapGet("/", (ILogger<Program> logger) =>
{
    logger.RequestReceived("/", 42);
    return "Hello, World!";
});

// This endpoint demonstrates logging with multiple log levels and the fact
// that [LoggerMessage] parameters are strongly typed — each property in the
// message template maps to a method parameter rather than an object[] varargs.
//
// Important difference from SimpleLoggingExample's GET /weather:
// The [LoggerMessage] source generator does not support Serilog's @ destructuring
// operator. To log a complex object's individual properties, you must pass them
// as separate scalar parameters. This trades convenience for performance and
// type safety.
app.MapGet("/weather", (ILogger<Program> logger) =>
{
    var forecast = new WeatherForecast("London", 38.5, "Scorching");

    logger.GeneratingForecast(forecast.City);
    logger.ForecastGenerated(forecast.City, forecast.TemperatureC, forecast.Summary);

    // Conditional logic based on data — demonstrates using different
    // pre-defined log methods at different levels.
    const double threshold = 35.0;
    if (forecast.TemperatureC > threshold)
    {
        logger.TemperatureExceedsThreshold(forecast.TemperatureC, threshold, forecast.City);
    }

    return forecast;
});

// This endpoint demonstrates exception logging and dynamic log levels.
// The source generator treats the first Exception parameter specially — it is
// attached to the log event (and rendered by Serilog) but is not a template hole.
// Dynamic log level (where Level is omitted from the attribute and a LogLevel
// parameter is accepted instead) lets the caller choose the level at runtime.
app.MapGet("/sensor", (ILogger<Program> logger) =>
{
    var reading = new SensorReading(
        SensorId: "temp-sensor-01",
        TemperatureC: 18.3,
        RecordedAt: DateTime.UtcNow);

    // Dynamic log level: the caller decides whether this is Information or Warning.
    var level = reading.TemperatureC > 30 ? LogLevel.Warning : LogLevel.Information;
    logger.SensorReadingReceived(level, reading.SensorId, reading.TemperatureC, reading.RecordedAt);

    // Demonstrate exception logging — the Exception parameter is not part of the
    // message template but is automatically attached to the log event.
    try
    {
        throw new InvalidOperationException("Calibration data expired for sensor temp-sensor-01");
    }
    catch (Exception ex)
    {
        logger.SensorReadingFailed(ex, reading.SensorId);
    }

    // Parameterless log method — the absolute zero-allocation path.
    logger.SensorDataProcessed();

    return reading;
});

app.Run();

record WeatherForecast(string City, double TemperatureC, string Summary);

record SensorReading(string SensorId, double TemperatureC, DateTime RecordedAt);
