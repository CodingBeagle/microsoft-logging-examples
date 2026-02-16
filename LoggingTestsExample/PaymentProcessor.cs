using Microsoft.Extensions.Logging;

namespace LoggingTestsExample;

/// <summary>
/// A more complex service that uses structured logging with BeginScope to attach
/// contextual information to log entries. Used to demonstrate advanced FakeLogger
/// testing with dependency injection and scope assertions.
/// </summary>
public class PaymentProcessor(ILogger<PaymentProcessor> logger)
{
    public bool ProcessPayment(string transactionId, string customerId, decimal amount, string currency)
    {
        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["TransactionId"] = transactionId,
            ["CustomerId"] = customerId
        }))
        {
            logger.LogInformation("Payment initiated for {Amount} {Currency}", amount, currency);

            if (amount <= 0)
            {
                logger.LogError("Payment failed: amount must be positive");
                return false;
            }

            logger.LogInformation("Payment of {Amount} {Currency} processed successfully", amount, currency);
        }

        return true;
    }
}
