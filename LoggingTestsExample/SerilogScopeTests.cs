using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace LoggingTestsExample;

/// <summary>
/// Tests that verify Serilog-specific behaviour when properties are attached via
/// <see cref="ILogger.BeginScope{TState}"/>. These tests assert against Serilog's
/// own <see cref="LogEvent"/> objects (captured by an in-memory sink) because the
/// behaviour under test — destructuring via the <c>@</c> prefix and the absence of
/// a <c>Scope</c> property when using <c>Dictionary&lt;string, object&gt;</c> — is
/// specific to how Serilog interprets scope state, not to Microsoft's abstraction.
/// </summary>
public class SerilogScopeTests
{
    private record AuditInfo(string OperatorId, string Action, string Facility);

    /// <summary>
    /// An in-memory <see cref="ILogEventSink"/> that stores every emitted
    /// <see cref="LogEvent"/> so tests can inspect Serilog's final output.
    /// </summary>
    private class LogEventCollectorSink : ILogEventSink
    {
        private readonly List<LogEvent> _events = new();
        public IReadOnlyList<LogEvent> Events => _events;
        public void Emit(LogEvent logEvent) => _events.Add(logEvent);
    }

    /// <summary>
    /// Verifies that prefixing a dictionary key with <c>@</c> in
    /// <see cref="ILogger.BeginScope{TState}"/> causes Serilog to destructure the
    /// value into a <see cref="StructureValue"/> whose individual properties can be
    /// inspected, rather than calling <c>ToString()</c> on the object.
    /// </summary>
    [Fact]
    public void BeginScope_WithDestructuringPrefix_ProperlyDestructuresScopedObject()
    {
        // Arrange
        var sink = new LogEventCollectorSink();
        var serilogLogger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Sink(sink)
            .CreateLogger();

        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(serilogLogger, dispose: true);
        });

        using var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILogger<SerilogScopeTests>>();

        var audit = new AuditInfo("operator-7", "SensorReadingQuery", "North Wing Lab");

        // Act — the @ prefix on the dictionary key tells Serilog to destructure the value
        // into its individual properties rather than calling ToString().
        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["@Audit"] = audit
        }))
        {
            logger.LogInformation("Test message within scope");
        }

        // Assert — a single log event was captured.
        var logEvent = Assert.Single(sink.Events);

        // The property should be named "Audit" (the @ prefix is consumed by Serilog,
        // it does not appear in the final property name).
        Assert.True(logEvent.Properties.ContainsKey("Audit"),
            "Expected an 'Audit' property on the log event from the scoped dictionary.");

        // Because @ was used, the value must be a StructureValue (destructured object)
        // rather than a ScalarValue (which would just contain the ToString() result).
        var auditProperty = logEvent.Properties["Audit"];
        Assert.IsType<StructureValue>(auditProperty);

        var structure = (StructureValue)auditProperty;
        var properties = structure.Properties
            .ToDictionary(p => p.Name, p => p.Value.ToString().Trim('"'));

        Assert.Equal("operator-7", properties["OperatorId"]);
        Assert.Equal("SensorReadingQuery", properties["Action"]);
        Assert.Equal("North Wing Lab", properties["Facility"]);
    }

    /// <summary>
    /// Verifies that passing a <c>Dictionary&lt;string, object&gt;</c> to
    /// <see cref="ILogger.BeginScope{TState}"/> results in log events that carry the
    /// dictionary entries as individual top-level properties and do <em>not</em> include
    /// a redundant <c>Scope</c> property.
    ///
    /// This behaviour is documented in the serilog-extensions-logging README:
    /// when scope state implements <c>IEnumerable&lt;KeyValuePair&lt;string, object&gt;&gt;</c>,
    /// each pair is pushed onto the log context directly. The <c>Scope</c> property is only
    /// added when the string-template overload of <c>BeginScope</c> is used instead.
    /// </summary>
    [Fact]
    public void BeginScope_WithDictionary_DoesNotAddScopeProperty()
    {
        // Arrange
        var sink = new LogEventCollectorSink();
        var serilogLogger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Sink(sink)
            .CreateLogger();

        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(serilogLogger, dispose: true);
        });

        using var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILogger<SerilogScopeTests>>();

        // Act — pass a Dictionary<string, object>, which is the recommended approach.
        // Each key-value pair is pushed individually onto the Serilog LogContext.
        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["TransactionId"] = "TXN-42",
            ["CustomerId"] = "CUST-7"
        }))
        {
            logger.LogInformation("Payment initiated");
        }

        // Assert
        var logEvent = Assert.Single(sink.Events);

        // The dictionary entries should appear as individual top-level properties.
        Assert.True(logEvent.Properties.ContainsKey("TransactionId"));
        Assert.True(logEvent.Properties.ContainsKey("CustomerId"));

        var transactionId = logEvent.Properties["TransactionId"] as ScalarValue;
        var customerId = logEvent.Properties["CustomerId"] as ScalarValue;

        Assert.NotNull(transactionId);
        Assert.NotNull(customerId);
        Assert.Equal("TXN-42", transactionId.Value);
        Assert.Equal("CUST-7", customerId.Value);

        // There must be no "Scope" property — this is the key advantage of the
        // Dictionary<string, object> approach over the string-template overload.
        // When using the string-template overload (e.g. logger.BeginScope("Processing {Key}", value))
        // a "Scope" property containing the formatted message is added to the log event,
        // which can create unnecessary duplication.
        Assert.False(logEvent.Properties.ContainsKey("Scope"),
            "Expected no 'Scope' property when using Dictionary<string, object> with BeginScope. " +
            "The 'Scope' property is only added when the string-template overload is used.");
    }
}
