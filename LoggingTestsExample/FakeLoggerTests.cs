using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace LoggingTestsExample;

public class FakeLoggerTests
{
    /// <summary>
    /// The simplest way to use FakeLogger: create one directly, inject it into a service,
    /// invoke the method, and assert that the expected log message was written.
    /// </summary>
    [Fact]
    public void SimpleTest_DirectFakeLogger_CapturesLogMessage()
    {
        // Arrange — create a FakeLogger<T> and inject it into the service.
        var logger = new FakeLogger<GreetingService>();
        var service = new GreetingService(logger);

        // Act
        var result = service.Greet("Alice");

        // Assert — the service returned the expected greeting.
        Assert.Equal("Hello, Alice!", result);

        // Assert — exactly one log record was captured.
        Assert.Equal(1, logger.Collector.Count);

        // Assert — the captured record has the expected level and formatted message.
        var record = logger.LatestRecord;
        Assert.Equal(LogLevel.Information, record.Level);
        Assert.Equal("Greeting Alice", record.Message);
    }

    /// <summary>
    /// A mid-complexity test: the service under test writes multiple log entries at
    /// different levels depending on input. We assert on the count of records, their
    /// levels, and the structured state captured alongside each message.
    /// </summary>
    [Theory]
    [InlineData("ORD-100", 49.99, 2)]   // Normal order — two Information logs.
    [InlineData("ORD-200", -5, 2)]       // Invalid amount — one Information + one Warning.
    [InlineData("ORD-300", 15_000, 3)]   // High-value order — two Information + one Warning.
    public void IntermediateTest_MultipleLogLevels_AndStructuredState(
        string orderId, decimal amount, int expectedLogCount)
    {
        // Arrange
        var logger = new FakeLogger<OrderService>();
        var service = new OrderService(logger);

        // Act
        service.ProcessOrder(orderId, amount);

        // Assert — the expected number of log records were captured.
        var records = logger.Collector.GetSnapshot();
        Assert.Equal(expectedLogCount, records.Count);

        // Assert — the first record is always the "Processing order" Information log.
        var first = records[0];
        Assert.Equal(LogLevel.Information, first.Level);
        Assert.Contains(first.StructuredState, kv => kv.Key == "OrderId" && kv.Value == orderId);

        // Assert — for invalid amounts the second record is a Warning about rejection.
        if (amount <= 0)
        {
            Assert.Equal(LogLevel.Warning, records[1].Level);
            Assert.Contains("rejected", records[1].Message);
        }

        // Assert — for high-value orders a Warning about review is present.
        if (amount > 10_000)
        {
            Assert.Contains(records, r =>
                r.Level == LogLevel.Warning &&
                r.Message.Contains("flagged for review"));
        }
    }

    /// <summary>
    /// A more advanced test demonstrating:
    ///   1. Registering fake logging via DI using AddFakeLogging().
    ///   2. Resolving the service through the container so it receives an ILogger<T> backed
    ///      by the fake infrastructure.
    ///   3. Asserting on structured state, log scopes, and multiple log levels.
    /// </summary>
    [Fact]
    public void AdvancedTest_DependencyInjection_WithScopesAndStructuredLogging()
    {
        // Arrange — build a DI container with fake logging and our service.
        // AddFakeLogging() registers the logging infrastructure and a FakeLoggerProvider
        // so that any ILogger<T> resolved from the container captures log records in memory.
        var services = new ServiceCollection();
        services.AddFakeLogging();
        services.AddTransient<PaymentProcessor>();

        using var provider = services.BuildServiceProvider();

        var processor = provider.GetRequiredService<PaymentProcessor>();

        // Act — process a valid payment that will produce scoped log entries.
        var success = processor.ProcessPayment(
            transactionId: "TXN-42",
            customerId: "CUST-7",
            amount: 250.00m,
            currency: "USD");

        // Assert — the payment was processed successfully.
        Assert.True(success);

        // Retrieve the collector from the DI container.
        var collector = provider.GetFakeLogCollector();
        var records = collector.GetSnapshot();

        // Assert — two Information records were produced (initiated + completed).
        Assert.Equal(2, records.Count);
        Assert.All(records, r => Assert.Equal(LogLevel.Information, r.Level));

        // Assert — the first record contains the structured Amount and Currency values.
        var initiated = records[0];
        Assert.Contains(initiated.StructuredState, kv => kv.Key == "Amount" && kv.Value == "250.00");
        Assert.Contains(initiated.StructuredState, kv => kv.Key == "Currency" && kv.Value == "USD");

        // Assert — both records were written inside a scope that carried TransactionId and CustomerId.
        foreach (var record in records)
        {
            Assert.NotEmpty(record.Scopes);

            // Each scope entry is an IReadOnlyList<KeyValuePair<string, string>>.
            // Flatten all scope key-value pairs so we can assert on them.
            var scopeValues = record.Scopes
                .SelectMany(s => s)
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            Assert.Equal("TXN-42", scopeValues["TransactionId"]);
            Assert.Equal("CUST-7", scopeValues["CustomerId"]);
        }

        // Assert — now test the error path: invalid amount.
        var failed = processor.ProcessPayment(
            transactionId: "TXN-99",
            customerId: "CUST-3",
            amount: -10m,
            currency: "EUR");

        Assert.False(failed);

        // Re-snapshot — should now have 4 records total (2 from first call + 2 from second).
        var allRecords = collector.GetSnapshot();
        Assert.Equal(4, allRecords.Count);

        // The last record from the failed payment should be an Error.
        var errorRecord = allRecords[3];
        Assert.Equal(LogLevel.Error, errorRecord.Level);
        Assert.Contains("amount must be positive", errorRecord.Message);
    }
}
