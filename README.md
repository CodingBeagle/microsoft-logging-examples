# Microsoft Logging Examples

Minimal usage examples of Microsoft's logging framework (`Microsoft.Extensions.Logging`) with [Serilog](https://serilog.net/) as a provider, in C# with .NET 10.

Each project in this repository is a standalone example that demonstrates a specific aspect of the logging framework.

## Examples

| Project | Description |
|---------|-------------|
| [SimpleLoggingExample](SimpleLoggingExample/) | Minimal ASP.NET Core API using Serilog's console sink with structured logging and the `@` destructuring operator |

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
