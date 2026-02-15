using Microsoft.Extensions.Logging;

namespace CustomConsoleProviderExample;

/// <summary>
/// Configuration for the colored console logger.
/// Maps each <see cref="LogLevel"/> to a <see cref="ConsoleColor"/>.
/// When bound to appsettings.json via <c>IOptionsMonitor&lt;T&gt;</c>,
/// changes are picked up at runtime without restarting the application.
/// </summary>
public sealed class ColorConsoleLoggerConfiguration
{
    public Dictionary<LogLevel, ConsoleColor> LogLevelToColorMap { get; set; } = new()
    {
        [LogLevel.Trace] = ConsoleColor.Gray,
        [LogLevel.Debug] = ConsoleColor.Gray,
        [LogLevel.Information] = ConsoleColor.Green,
        [LogLevel.Warning] = ConsoleColor.Yellow,
        [LogLevel.Error] = ConsoleColor.Red,
        [LogLevel.Critical] = ConsoleColor.DarkRed,
    };
}
