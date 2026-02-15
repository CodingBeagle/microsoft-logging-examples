using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using System;

namespace CustomConsoleProviderExample;

/// <summary>
/// Extension methods for registering the ColorConsole logging provider.
/// Follows the same pattern as the built-in <c>AddConsole()</c>,
/// <c>AddDebug()</c>, etc.
/// </summary>
public static class ColorConsoleLoggerExtensions
{
    /// <summary>
    /// Registers the <see cref="ColorConsoleLoggerProvider"/> with the
    /// logging framework. Configuration is read from the
    /// <c>"Logging:ColorConsole"</c> section of appsettings.json (the
    /// section name comes from the <c>[ProviderAlias]</c> attribute).
    /// </summary>
    public static ILoggingBuilder AddColorConsole(this ILoggingBuilder builder)
    {
        builder.AddConfiguration();

        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<ILoggerProvider, ColorConsoleLoggerProvider>());

        LoggerProviderOptions.RegisterProviderOptions
            <ColorConsoleLoggerConfiguration, ColorConsoleLoggerProvider>(builder.Services);

        return builder;
    }

    /// <summary>
    /// Registers the <see cref="ColorConsoleLoggerProvider"/> and applies
    /// inline configuration via an <see cref="Action{T}"/> delegate.
    /// </summary>
    public static ILoggingBuilder AddColorConsole(
        this ILoggingBuilder builder,
        Action<ColorConsoleLoggerConfiguration> configure)
    {
        builder.AddColorConsole();
        builder.Services.Configure(configure);
        return builder;
    }
}
