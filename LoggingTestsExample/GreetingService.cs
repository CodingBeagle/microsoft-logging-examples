using Microsoft.Extensions.Logging;

namespace LoggingTestsExample;

/// <summary>
/// A simple service that logs a greeting. Used to demonstrate basic FakeLogger testing.
/// </summary>
public class GreetingService(ILogger<GreetingService> logger)
{
    public string Greet(string name)
    {
        logger.LogInformation("Greeting {Name}", name);
        return $"Hello, {name}!";
    }
}
