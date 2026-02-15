# SourceGeneratedLoggingExample

A minimal ASP.NET Core API that uses the [`[LoggerMessage]`](https://learn.microsoft.com/en-us/dotnet/core/extensions/logger-message-generator) source generator to define high-performance log methods, with Serilog as the underlying logging provider.

## What this demonstrates

- Defining source-generated log methods using `[LoggerMessage]` on `static partial` extension methods
- Strongly-typed scalar parameters in log message templates (no boxing of value types)
- Event IDs for categorizing log events
- Multiple log levels (`Debug`, `Information`, `Warning`, `Error`)
- Exception logging via the special `Exception` parameter
- Dynamic log levels (omitting `Level` from the attribute and accepting a `LogLevel` parameter at the call site)
- Parameterless log methods (zero-allocation fast path)

## Packages used

- [`Serilog`](https://www.nuget.org/packages/Serilog) -- core library
- [`Serilog.Extensions.Logging`](https://www.nuget.org/packages/Serilog.Extensions.Logging) -- provides `AddSerilog()` to register Serilog as a `Microsoft.Extensions.Logging` provider
- [`Serilog.Sinks.Console`](https://www.nuget.org/packages/Serilog.Sinks.Console) -- writes log events to the console

The `[LoggerMessage]` attribute itself requires no additional packages — it ships with `Microsoft.Extensions.Logging.Abstractions`, which is included transitively via the Web SDK.

## Running

```bash
dotnet run
```

Then make requests to the endpoints:

```bash
curl http://localhost:5000/
curl http://localhost:5000/weather
curl http://localhost:5000/sensor
```

## Endpoints

### `GET /`

Logs a message with scalar structured properties (`{Path}`, `{UserId}`). Functionally identical to `SimpleLoggingExample`'s `GET /`, but uses a source-generated method that avoids template re-parsing and value-type boxing.

### `GET /weather`

Creates a `WeatherForecast` and logs at multiple levels:

- `Debug` — generating forecast data
- `Information` — forecast result with decomposed scalar properties
- `Warning` — temperature exceeds a threshold (conditional)

Demonstrates how complex objects must be decomposed into individual scalar parameters, since `[LoggerMessage]` does not support Serilog's `@` destructuring operator.

### `GET /sensor`

Creates a `SensorReading` and demonstrates three features:

- **Dynamic log level** — the log level is chosen at runtime based on the temperature value, using a `[LoggerMessage]` method that accepts `LogLevel` as a parameter
- **Exception logging** — the `Exception` parameter is attached to the log event but is not part of the message template; Serilog renders the full exception details automatically
- **Parameterless method** — a log call with no parameters beyond the logger, representing the absolute zero-allocation path

## How the source generator works

The `[LoggerMessage]` attribute instructs the compiler to generate the method body at build time. The generated code:

1. Parses the message template once and caches the parsed structure
2. Guards every log call with `logger.IsEnabled(level)` — when the level is disabled, the method returns immediately with zero allocations
3. Passes parameters as strongly-typed values, avoiding the `params object[]` allocation and value-type boxing that `ILogger.LogInformation()` incurs

To inspect the generated code, add `<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>` to the `<PropertyGroup>` in the `.csproj` and look in `obj/Debug/net10.0/generated/`.

## Key differences from SimpleLoggingExample

| | SimpleLoggingExample | SourceGeneratedLoggingExample |
|---|---|---|
| **Log call style** | `logger.LogInformation("...", args)` | `logger.MethodName(args)` (extension method) |
| **Template parsing** | Re-parsed on every call | Parsed once at compile time |
| **Value-type boxing** | `int`, `double`, etc. boxed into `object[]` | Passed as strongly-typed parameters |
| **`@` destructuring** | Supported — `{@Forecast}` serializes object properties | Not supported — pass individual scalar properties instead |
| **Event IDs** | Not used | Each method has a unique `EventId` |
| **Exception handling** | Via overloads (`LogError(ex, "...")`) | Via a dedicated `Exception` parameter recognized by the generator |
| **`BeginScope`** | Demonstrated | Fully compatible but omitted to keep focus on `[LoggerMessage]` |
