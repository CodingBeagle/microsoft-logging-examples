using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Testing;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace LoggingTestsExample;

/// <summary>
/// Demonstrates how FakeLogger acts as a "seam" to verify log statements emitted by code
/// that runs through a Serilog pipeline inside a real ASP.NET Core TestServer.
///
/// The key insight is that Microsoft's ILogger abstraction fans out to *every* registered
/// provider simultaneously. By configuring both Serilog (with a real sink) and FakeLogging
/// as providers on the same host, the same log events flow through both pipelines.
/// Tests assert against the FakeLogger's captured records while Serilog keeps handling
/// output exactly as it would in production — no mocking, no monkey-patching.
/// </summary>
public class SerilogFakeLoggerIntegrationTests
{
    [Fact]
    public async Task GetRoot_LogsRequestDetails_VerifiedThroughFakeLoggerAlongsideSerilog()
    {
        // Arrange — build a TestServer that registers both Serilog and FakeLogging as
        // concurrent providers. The Serilog configuration is identical to what you would
        // use in production (console sink, log context enrichment); FakeLogging is added
        // purely as a test-time seam with no effect on the real logging behaviour.
        var builder = new WebHostBuilder()
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();

                // Register Serilog with a console sink — the "production" pipeline.
                // Any sink can be used here (file, Seq, in-memory, …). The point is
                // that this configuration does not need to change for tests at all.
                var serilogLogger = new LoggerConfiguration()
                    .Enrich.FromLogContext()
                    .WriteTo.Console()
                    .CreateLogger();

                logging.AddSerilog(serilogLogger, dispose: true);

                // Register FakeLogging as an *additional* provider alongside Serilog.
                // This is the seam: every ILogger<T>.Log* call fans out to both
                // providers, so FakeLogger receives the same structured events that
                // Serilog already processed and forwarded to the console sink.
                logging.Services.AddFakeLogging();
            })
            .Configure(app =>
            {
                // A minimal endpoint that mirrors the "/" route in SimpleLoggingExample.
                // It logs a structured message with two named properties.
                app.Run(async context =>
                {
                    var logger = context.RequestServices
                        .GetRequiredService<ILogger<SerilogFakeLoggerIntegrationTests>>();

                    // This single LogInformation call is received by both Serilog (which
                    // writes it to the console) AND the FakeLogger provider (which stores
                    // it in memory for assertion below).
                    logger.LogInformation(
                        "Request received for {Path} by user {UserId}",
                        context.Request.Path.Value,
                        42);

                    await context.Response.WriteAsync("Hello!");
                });
            });

        await using var server = new TestServer(builder);
        using var client = server.CreateClient();

        // Act — issue a GET / request, which triggers the log statement above.
        var response = await client.GetAsync("/");
        response.EnsureSuccessStatusCode();

        // Assert — retrieve the FakeLogCollector from the server's DI container.
        // It captured every log event produced during the request, in parallel with
        // Serilog writing those same events to the console.
        var collector = server.Services.GetFakeLogCollector();
        var records = collector.GetSnapshot();

        // Exactly one Information record matching our log template should be present.
        var requestLog = records.Single(r =>
            r.Level == LogLevel.Information &&
            r.Message.Contains("Request received for"));

        Assert.Equal(LogLevel.Information, requestLog.Level);

        // FakeLogger retains the original structured key-value pairs from the log
        // template, enabling precise assertions on individual parameters rather than
        // relying on the fully-rendered message string alone.
        Assert.NotNull(requestLog.StructuredState);
        Assert.Contains(requestLog.StructuredState, kv => kv.Key == "Path"   && kv.Value == "/");
        Assert.Contains(requestLog.StructuredState, kv => kv.Key == "UserId" && kv.Value == "42");
    }
}
