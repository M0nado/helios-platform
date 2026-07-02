namespace MonadoBlade.Tests.Unit.Async;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

// Test-specific implementation of ILogger for isolation
public interface ILogger
{
    void LogInformation(string message, params object?[] args);
    void LogWarning(string message, params object?[] args);
    void LogError(Exception ex, string message, params object?[] args);
}

public class MockLogger : ILogger
{
    public List<string> LoggedMessages { get; } = new List<string>();

    public void LogInformation(string message, params object?[] args)
    {
        LoggedMessages.Add($"[INFO] {string.Format(message, args)}");
    }

    public void LogWarning(string message, params object?[] args)
    {
        LoggedMessages.Add($"[WARN] {string.Format(message, args)}");
    }

    public void LogError(Exception ex, string message, params object?[] args)
    {
        LoggedMessages.Add($"[ERROR] {string.Format(message, args)}: {ex.Message}");
    }
}

/// <summary>
/// Comprehensive test suite for AsyncPipeline infrastructure.
/// Tests async/await patterns, concurrency control, and error handling.
/// </summary>
public class AsyncInfrastructureTests
{
    private readonly MockLogger _logger;

    public AsyncInfrastructureTests()
    {
        _logger = new MockLogger();
    }

    [Fact]
    public async Task AsyncThrottler_ShouldLimitConcurrency()
    {
        // Arrange
        var throttler = new MonadoBlade.Core.Async.AsyncThrottler(2);
        var concurrentCount = 0;
        var maxConcurrentObserved = 0;
        var lockObj = new object();

        var tasks = new List<Task>();
        for (int i = 0; i < 5; i++)
        {
            var task = throttler.ExecuteAsync(async ct =>
            {
                lock (lockObj)
                {
                    concurrentCount++;
                    if (concurrentCount > maxConcurrentObserved)
                        maxConcurrentObserved = concurrentCount;
                }

                try
                {
                    await Task.Delay(50, ct);
                }
                finally
                {
                    lock (lockObj)
                    {
                        concurrentCount--;
                    }
                }
            });

            tasks.Add(task);
        }

        // Act
        await Task.WhenAll(tasks);

        // Assert
        Assert.True(maxConcurrentObserved <= 2, $"Max concurrent was {maxConcurrentObserved}, expected <= 2");
    }

    [Fact]
    public async Task AsyncLazy_ShouldComputeOnlyOnce()
    {
        // Arrange
        var computeCount = 0;
        var asyncLazy = new MonadoBlade.Core.Async.AsyncLazy<int>(async () =>
        {
            await Task.Delay(10);
            computeCount++;
            return 42;
        });

        // Act
        var result1 = await asyncLazy.Value;
        var result2 = await asyncLazy.Value;
        var result3 = await asyncLazy.Value;

        // Assert
        Assert.Equal(1, computeCount);
        Assert.Equal(42, result1);
        Assert.Equal(42, result2);
        Assert.Equal(42, result3);
        Assert.True(asyncLazy.IsValueCreated);
    }

    [Fact]
    public async Task ExponentialBackoff_ShouldDelayProportionally()
    {
        // Arrange
        var retryPolicy = new MonadoBlade.Core.Async.AsyncPipelineRetryPolicy
        {
            MaxRetries = 3,
            InitialDelay = TimeSpan.FromMilliseconds(10),
            BackoffMultiplier = 2.0
        };

        // Act & Assert
        var delay1 = retryPolicy.GetDelayForAttempt(1);
        var delay2 = retryPolicy.GetDelayForAttempt(2);
        var delay3 = retryPolicy.GetDelayForAttempt(3);

        Assert.Equal(10, delay1.TotalMilliseconds);
        Assert.Equal(20, delay2.TotalMilliseconds);
        Assert.Equal(40, delay3.TotalMilliseconds);
    }

    [Fact]
    public async Task ExponentialBackoff_ShouldNotExceedMaxDelay()
    {
        // Arrange
        var retryPolicy = new MonadoBlade.Core.Async.AsyncPipelineRetryPolicy
        {
            MaxRetries = 10,
            InitialDelay = TimeSpan.FromMilliseconds(1),
            BackoffMultiplier = 2.0,
            MaxDelay = TimeSpan.FromMilliseconds(100)
        };

        // Act
        var delayValues = new List<double>();
        for (int i = 1; i <= 10; i++)
        {
            delayValues.Add(retryPolicy.GetDelayForAttempt(i).TotalMilliseconds);
        }

        // Assert
        Assert.All(delayValues, delay => Assert.True(delay <= 100, $"Delay {delay} exceeds max 100"));
    }

    [Fact]
    public async Task AsyncThrottler_AvailableSlotsTracking()
    {
        // Arrange
        var throttler = new MonadoBlade.Core.Async.AsyncThrottler(3);
        
        // Act & Assert - initially all slots available
        Assert.Equal(3, throttler.AvailableSlots);

        // Run 2 operations concurrently
        var task1 = throttler.ExecuteAsync(async ct => await Task.Delay(50, ct));
        var task2 = throttler.ExecuteAsync(async ct => await Task.Delay(50, ct));
        
        await Task.Delay(10); // Let them start

        // Should have 1 slot remaining
        var availableWhenRunning = throttler.AvailableSlots;
        Assert.True(availableWhenRunning <= 1);

        await Task.WhenAll(task1, task2);

        // All slots should be available again
        Assert.Equal(3, throttler.AvailableSlots);
    }

    [Fact]
    public async Task AsyncBatchProcessor_BasicFunctionality()
    {
        // Arrange
        var batchCount = 0;
        var processor = new MonadoBlade.Core.Async.AsyncBatchProcessor<int, int>(
            async (batch, ct) =>
            {
                batchCount++;
                await Task.Delay(10, ct);
                return batch.Select(x => x * 2).ToArray();
            },
            batchSize: 3);

        // Act
        var result1 = await processor.AddAsync(1);
        var result2 = await processor.AddAsync(2);
        await processor.FlushAsync();

        // Assert
        Assert.True(batchCount >= 1);
    }

    [Fact]
    public async Task AsyncPipelineRetryPolicy_ConfigurationValidation()
    {
        // Arrange & Act
        var policy = new MonadoBlade.Core.Async.AsyncPipelineRetryPolicy
        {
            MaxRetries = 5,
            InitialDelay = TimeSpan.FromMilliseconds(50),
            BackoffMultiplier = 1.5,
            MaxDelay = TimeSpan.FromSeconds(5)
        };

        // Assert
        Assert.Equal(5, policy.MaxRetries);
        Assert.Equal(50, policy.InitialDelay.TotalMilliseconds);
        Assert.Equal(1.5, policy.BackoffMultiplier);
        Assert.Equal(5000, policy.MaxDelay.TotalMilliseconds);
    }

    [Fact]
    public async Task ConcurrentAsyncOperations_PerformanceBaseline()
    {
        // Arrange
        var throttler = new MonadoBlade.Core.Async.AsyncThrottler(4);
        var stopwatch = Stopwatch.StartNew();
        var operations = Enumerable.Range(0, 10).Select(async i =>
        {
            await throttler.ExecuteAsync(async ct =>
            {
                await Task.Delay(20, ct);
            });
        }).ToList();

        // Act
        await Task.WhenAll(operations);
        stopwatch.Stop();

        // Assert - 10 operations with concurrency of 4 and 20ms each
        // Should complete in ~60ms (3 batches of 4, 1 batch of 2, minus overhead)
        Assert.True(stopwatch.ElapsedMilliseconds >= 40);
        Assert.True(stopwatch.ElapsedMilliseconds < 300);
    }
}
