# CustomConsoleProviderExample

A minimal ASP.NET Core API that implements a custom logging provider using only `Microsoft.Extensions.Logging` — no third-party logging libraries.

## What this demonstrates

- Implementing `ILogger` to write colored output to the console
- Implementing `ILoggerProvider` with `ConcurrentDictionary` logger caching
- Using `[ProviderAlias("ColorConsole")]` for clean configuration section naming
- Using `IOptionsMonitor<T>` to support live configuration reloading
- Creating `AddColorConsole()` extension methods on `ILoggingBuilder` (following the same pattern as the built-in `AddConsole()`, `AddDebug()`, etc.)
- Binding provider configuration from `appsettings.json` via `LoggerProviderOptions.RegisterProviderOptions`

## Packages used

None. The `Microsoft.NET.Sdk.Web` SDK includes all required logging packages transitively:

- `Microsoft.Extensions.Logging`
- `Microsoft.Extensions.Logging.Abstractions`
- `Microsoft.Extensions.Logging.Configuration`
- `Microsoft.Extensions.Options`

## Running

```bash
dotnet run --project CustomConsoleProviderExample
```

Then make requests to the endpoints:

```bash
curl http://localhost:51704/
curl http://localhost:51704/weather
curl http://localhost:51704/sensor
```

## Endpoints

### `GET /`

Logs a simple informational message. Appears in green on the console.

### `GET /weather`

Logs at three levels to demonstrate color differentiation:

- **Debug** (gray) — looking up forecast data
- **Information** (green) — forecast result
- **Warning** (yellow) — temperature exceeds threshold

### `GET /sensor`

Logs sensor data and demonstrates error-level logging:

- **Information** (green) — sensor reading received
- **Error** (red) — simulated failure with full exception output
- **Information** (green) — pipeline completed

## Custom provider architecture

```
ILoggingBuilder.AddColorConsole()
        │
        ▼
ColorConsoleLoggerProvider : ILoggerProvider
  - ConcurrentDictionary<string, ColorConsoleLogger>
  - IOptionsMonitor<ColorConsoleLoggerConfiguration>
        │
        ▼
ColorConsoleLogger : ILogger
  - Reads config via Func<ColorConsoleLoggerConfiguration>
  - Sets Console.ForegroundColor per log level
        │
        ▼
ColorConsoleLoggerConfiguration
  - Dictionary<LogLevel, ConsoleColor> LogLevelToColorMap
```

## Key differences from SimpleLoggingExample and SourceGeneratedLoggingExample

| | SimpleLogging / SourceGenerated | CustomConsoleProvider |
|---|---|---|
| **Logging provider** | Serilog (third-party) | Custom `ILoggerProvider` (built from scratch) |
| **NuGet packages** | 3 Serilog packages | None (SDK-included only) |
| **Configuration** | Serilog's fluent API | `IOptionsMonitor<T>` with `appsettings.json` binding |
| **Focus** | Using `ILogger<T>` for structured logging | Building the provider behind `ILogger<T>` |
