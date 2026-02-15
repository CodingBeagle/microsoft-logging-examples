using Microsoft.Extensions.Logging;
using System;

namespace SourceGeneratedLoggingExample;

// All log methods are defined as source-generated partial methods in a single
// static class. The [LoggerMessage] attribute causes the compiler to generate
// the implementation at build time, producing code that:
//   1. Parses the message template once and caches the result
//   2. Guards each call with an IsEnabled check to avoid allocations when the
//      log level is disabled
//   3. Passes parameters as strongly-typed values, avoiding boxing of value types
//
// Compare this to the direct ILogger.LogInformation() calls in SimpleLoggingExample,
// which parse the message template on every invocation and box value-type parameters.

public static partial class Log
{
    // --- Scalar properties (parallel to SimpleLoggingExample's GET /) ---

    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Information,
        Message = "Request received for {Path} by user {UserId}")]
    public static partial void RequestReceived(this ILogger logger, string path, int userId);

    // --- Different log levels ---

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Debug,
        Message = "Generating forecast data for {City}")]
    public static partial void GeneratingForecast(this ILogger logger, string city);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Information,
        Message = "Forecast generated for {City}: {TemperatureC}C, {Summary}")]
    public static partial void ForecastGenerated(
        this ILogger logger, string city, double temperatureC, string summary);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Warning,
        Message = "Temperature {TemperatureC}C exceeds threshold {ThresholdC}C for {City}")]
    public static partial void TemperatureExceedsThreshold(
        this ILogger logger, double temperatureC, double thresholdC, string city);

    // --- Exception logging ---

    [LoggerMessage(
        EventId = 2000,
        Level = LogLevel.Error,
        Message = "Failed to process sensor reading from {SensorId}")]
    public static partial void SensorReadingFailed(
        this ILogger logger, Exception exception, string sensorId);

    // --- Dynamic log level (Level omitted from attribute) ---

    [LoggerMessage(
        EventId = 3000,
        Message = "Sensor {SensorId} reported temperature {TemperatureC}C at {RecordedAt}")]
    public static partial void SensorReadingReceived(
        this ILogger logger, LogLevel level, string sensorId, double temperatureC, DateTime recordedAt);

    // --- Parameterless (zero-allocation fast path) ---

    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Information,
        Message = "Sensor data processed successfully")]
    public static partial void SensorDataProcessed(this ILogger logger);
}
