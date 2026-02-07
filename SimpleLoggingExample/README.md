# SimpleLoggingExample

A minimal ASP.NET Core API that uses Microsoft's `ILogger<T>` interface with Serilog as the underlying logging provider, configured with the console sink.

## What this demonstrates

- Configuring Serilog in code and registering it as a provider via `AddSerilog()` (from `Serilog.Extensions.Logging`)
- Logging structured scalar values through `ILogger<T>` (e.g. `{UserId}`, `{Path}`)
- The difference between default rendering and Serilog's `@` destructuring operator when logging complex objects
- Nested object destructuring

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

Logs a `SensorReading` that contains a nested `GeoLocation` object, demonstrating that the `@` operator recursively destructures the entire object graph.

## How `@` destructuring works through `ILogger`

Serilog's `@` destructuring operator is not natively part of Microsoft's logging abstractions. It works because `Serilog.Extensions.Logging` inspects message template property names: when a name is prefixed with `@`, the package strips the prefix and passes the value to Serilog with the destructuring flag enabled.

This was [initially broken](https://github.com/serilog/serilog-extensions-logging/issues/4) in early versions of the package and fixed in 2015.
