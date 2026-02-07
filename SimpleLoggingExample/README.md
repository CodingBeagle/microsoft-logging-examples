# SimpleLoggingExample

A minimal ASP.NET Core API that uses Microsoft's `ILogger<T>` interface with Serilog as the underlying logging provider, configured with the console sink.

## What this demonstrates

- Configuring Serilog in code and registering it as a provider via `AddSerilog()` (from `Serilog.Extensions.Logging`)
- Logging structured scalar values through `ILogger<T>` (e.g. `{UserId}`, `{Path}`)
- The difference between default rendering and Serilog's `@` destructuring operator when logging complex objects
- Nested object destructuring
- `ILogger.BeginScope` with a dictionary to attach audit logging properties (including destructured objects) to all log events within a scope

## Packages used

- [`Serilog`](https://www.nuget.org/packages/Serilog) -- core library
- [`Serilog.Extensions.Logging`](https://www.nuget.org/packages/Serilog.Extensions.Logging) -- provides `AddSerilog()` to register Serilog as a `Microsoft.Extensions.Logging` provider
- [`Serilog.Sinks.Console`](https://www.nuget.org/packages/Serilog.Sinks.Console) -- writes log events to the console

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

Logs a message with scalar structured properties (`{Path}`, `{UserId}`).

### `GET /weather`

Logs the same `WeatherForecast` object two ways to contrast the output:

- `{Forecast}` -- default rendering, calls `ToString()` on the object
- `{@Forecast}` -- destructuring, Serilog serializes each property as a structured field

### `GET /sensor`

Uses `ILogger.BeginScope` to attach audit context to all log events within the scope. Demonstrates:

- Passing a `Dictionary<string, object>` to `BeginScope` (the recommended approach — produces clean top-level properties without a redundant `Scope` wrapper)
- A scalar scope property (`RequestId`)
- A destructured scope property (`@Audit`) — the `@` prefix works in dictionary keys the same way it does in message templates
- Multiple log statements within the scope, all inheriting the scope properties
- Nested object destructuring on the `SensorReading` in the message template

## `BeginScope` and `Enrich.FromLogContext()`

`ILogger.BeginScope` pushes properties onto Serilog's `LogContext`. For these properties to actually appear in log output, the Serilog configuration **must** include `.Enrich.FromLogContext()`.

There are two ways to call `BeginScope`:

1. **Dictionary / `IEnumerable<KeyValuePair<string, object>>`** (recommended) — each key-value pair becomes a clean top-level property on the log event. Prefixing a key with `@` triggers destructuring.
2. **String template** (e.g. `BeginScope("Processing {OrderId}", orderId)`) — this works but adds a redundant `Scope` property containing the formatted string alongside the individual properties, [duplicating data in the output](https://github.com/serilog/serilog-extensions-logging/issues/65).

## How `@` destructuring works through `ILogger`

Serilog's `@` destructuring operator is not natively part of Microsoft's logging abstractions. It works because `Serilog.Extensions.Logging` inspects message template property names: when a name is prefixed with `@`, the package strips the prefix and passes the value to Serilog with the destructuring flag enabled.

This was [initially broken](https://github.com/serilog/serilog-extensions-logging/issues/4) in early versions of the package and fixed in 2015.
