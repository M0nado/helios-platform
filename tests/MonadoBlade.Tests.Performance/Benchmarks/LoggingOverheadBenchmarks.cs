namespace MonadoBlade.Tests.Performance.Benchmarks;

using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

/// <summary>
/// Benchmarks for logging overhead.
/// Measures performance impact of different logging configurations and patterns.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, targetCount: 5)]
public class LoggingOverheadBenchmarks
{
    private ILogger _logger = null!;
    private ILogger _nullLogger = null!;

    [GlobalSetup]
    public void Setup()
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        _logger = loggerFactory.CreateLogger("BenchmarkLogger");

        // Null logger for comparison
        _nullLogger = new NullLogger();
    }

    [Benchmark(Description = "Simple log message")]
    public void SimpleLogMessage()
    {
        _logger.LogInformation("Simple log message");
    }

    [Benchmark(Description = "Log message with single parameter")]
    public void LogWithSingleParameter()
    {
        _logger.LogInformation("User {userId} logged in", 12345);
    }

    [Benchmark(Description = "Log message with multiple parameters")]
    public void LogWithMultipleParameters()
    {
        _logger.LogInformation("User {userId} performed action {action} at {timestamp}", 
            12345, "Login", DateTime.UtcNow);
    }

    [Benchmark(Description = "Log message with object parameter")]
    public void LogWithObjectParameter()
    {
        var user = new { Id = 12345, Name = "Test User", Email = "test@example.com" };
        _logger.LogInformation("User logged in: {user}", user);
    }

    [Benchmark(Description = "Log with IsEnabled check")]
    public void LogWithIsEnabledCheck()
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Message with check");
        }
    }

    [Benchmark(Description = "Log without IsEnabled check")]
    public void LogWithoutIsEnabledCheck()
    {
        _logger.LogInformation("Message without check");
    }

    [Benchmark(Description = "Null logger benchmark")]
    public void NullLoggerBenchmark()
    {
        _nullLogger.LogInformation("Null logger message");
    }

    [Benchmark(Description = "Disabled log level")]
    public void DisabledLogLevel()
    {
        _logger.LogDebug("Debug message (disabled)");
    }

    [Benchmark(Description = "Exception logging")]
    public void ExceptionLogging()
    {
        var ex = new InvalidOperationException("Test exception");
        _logger.LogError(ex, "An error occurred");
    }

    [Benchmark(Description = "Structured logging")]
    public void StructuredLogging()
    {
        using (_logger.BeginScope("Request {RequestId}", Guid.NewGuid()))
        {
            _logger.LogInformation("Processing request");
        }
    }

    [Benchmark(Description = "High frequency logging")]
    public void HighFrequencyLogging()
    {
        for (int i = 0; i < 100; i++)
        {
            _logger.LogDebug("Message {index}", i);
        }
    }

    private class NullLogger : ILogger
    {
        public IDisposable BeginScope<TState>(TState state) => null!;
        public bool IsEnabled(LogLevel logLevel) => false;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }
}
