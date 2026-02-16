# Microsoft Logging Examples

Minimal usage examples of Microsoft's logging framework (`Microsoft.Extensions.Logging`) in C# with .NET 10, including both [Serilog](https://serilog.net/)-based and custom provider implementations.

Each project in this repository is a standalone example that demonstrates a specific aspect of the logging framework.

## Examples

| Project | Description |
|---------|-------------|
| [SimpleLoggingExample](SimpleLoggingExample/) | Minimal ASP.NET Core API using Serilog's console sink with structured logging and the `@` destructuring operator |
| [SourceGeneratedLoggingExample](SourceGeneratedLoggingExample/) | High-performance logging using `[LoggerMessage]` source-generated methods with Serilog |
| [CustomConsoleProviderExample](CustomConsoleProviderExample/) | Custom `ILoggerProvider` implementation: a colored console logger with `IOptionsMonitor<T>` live configuration — no third-party packages |
| [LoggingTestsExample](LoggingTestsExample/) | Unit testing logging behaviour with `FakeLogger` and `FakeLoggerFactory` from `Microsoft.Extensions.Diagnostics.Testing` — no mocking frameworks required |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## Getting started

```bash
dotnet restore
dotnet build
```

Then run any individual example:

```bash
dotnet run --project SimpleLoggingExample
```
