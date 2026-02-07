using Serilog;
using Serilog.Extensions.Logging;

// Configure Serilog with the console sink.
// Enrich.FromLogContext() is required for ILogger.BeginScope properties to appear in log output.
Log.Logger = new LoggerConfiguration()
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

// This endpoint demonstrates BeginScope with audit logging properties.
// BeginScope attaches structured properties to every log event within the scope.
// Passing a Dictionary<string, object> is the recommended approach — it produces
// clean top-level properties without a redundant "Scope" wrapper.
// Prefixing a dictionary key with @ triggers Serilog's destructuring for that value.
app.MapGet("/sensor", (ILogger<Program> logger) =>
{
    var reading = new SensorReading(
        SensorId: "temp-sensor-01",
        Location: new GeoLocation(Latitude: 51.5074, Longitude: -0.1278),
        TemperatureC: 18.3,
        RecordedAt: DateTime.UtcNow);

    var audit = new AuditInfo(
        OperatorId: "operator-7",
        Action: "SensorReadingQuery",
        Facility: "North Wing Lab");

    using (logger.BeginScope(new Dictionary<string, object>
    {
        // Scalar value — attached as-is to every log event in this scope.
        ["RequestId"] = Guid.NewGuid(),

        // Destructured value — the @ prefix tells Serilog to serialize the
        // AuditInfo object into its individual properties rather than calling ToString().
        ["@Audit"] = audit
    }))
    {
        // Both log events below will carry RequestId and the destructured Audit properties.
        logger.LogInformation("Sensor reading received: {@Reading}", reading);
        logger.LogInformation("Sensor data processed successfully");
    }

    return reading;
});

app.Run();

record WeatherForecast(string City, double TemperatureC, string Summary);

record GeoLocation(double Latitude, double Longitude);

record SensorReading(string SensorId, GeoLocation Location, double TemperatureC, DateTime RecordedAt);

record AuditInfo(string OperatorId, string Action, string Facility);
