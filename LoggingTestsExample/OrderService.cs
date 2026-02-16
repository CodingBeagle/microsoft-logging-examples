using Microsoft.Extensions.Logging;

namespace LoggingTestsExample;

/// <summary>
/// A service that logs at multiple levels and uses structured logging.
/// Used to demonstrate intermediate-level FakeLogger testing.
/// </summary>
public class OrderService(ILogger<OrderService> logger)
{
    public bool ProcessOrder(string orderId, decimal amount)
    {
        logger.LogInformation("Processing order {OrderId} for amount {Amount}", orderId, amount);

        if (amount <= 0)
        {
            logger.LogWarning("Order {OrderId} rejected: invalid amount {Amount}", orderId, amount);
            return false;
        }

        if (amount > 10_000)
        {
            logger.LogWarning("Order {OrderId} flagged for review: high value {Amount}", orderId, amount);
        }

        logger.LogInformation("Order {OrderId} completed successfully", orderId);
        return true;
    }
}
