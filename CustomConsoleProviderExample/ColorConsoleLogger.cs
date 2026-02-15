using Microsoft.Extensions.Logging;

namespace CustomConsoleProviderExample;

/// <summary>
/// A custom <see cref="ILogger"/> that writes log messages to the console
/// with colors determined by the log level. Configuration is read via a
/// delegate so the provider can swap it at runtime without recreating loggers.
/// </summary>
public sealed class ColorConsoleLogger(
    string categoryName,
    Func<ColorConsoleLoggerConfiguration> getCurrentConfig) : ILogger
{
    /// <summary>
    /// Creates a logging scope. Returning <c>null</c> opts out of scope support.
    /// A production logger would push/pop scope state onto an
    /// <c>AsyncLocal&lt;T&gt;</c> stack and return an <see cref="IDisposable"/>
    /// that pops it on dispose.
    /// </summary>
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    /// <summary>
    /// A log level is considered enabled if it has a color mapping in the
    /// configuration. Removing a level from the map effectively disables it.
    /// </summary>
    public bool IsEnabled(LogLevel logLevel) =>
        logLevel != LogLevel.None && getCurrentConfig().LogLevelToColorMap.ContainsKey(logLevel);

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var config = getCurrentConfig();
        var originalColor = Console.ForegroundColor;

        if (config.LogLevelToColorMap.TryGetValue(logLevel, out var color))
            Console.ForegroundColor = color;

        Console.Write($"[{logLevel,-12}] ");
        Console.Write($"{categoryName}: ");
        Console.WriteLine(formatter(state, exception));

        if (exception is not null)
            Console.WriteLine(exception);

        Console.ForegroundColor = originalColor;
    }
}
