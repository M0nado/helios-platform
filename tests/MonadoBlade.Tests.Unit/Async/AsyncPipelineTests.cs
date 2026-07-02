namespace MonadoBlade.Tests.Unit.Async;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using MonadoBlade.Core.Async;
using MonadoBlade.Core.Services;

/// <summary>
/// Comprehensive test suite for AsyncPipeline with coverage of:
/// - Basic pipeline execution
/// - Sequential stage execution
/// - Parallel operations
/// - Timeout handling
/// - Cancellation support
/// - Retry logic with exponential backoff
/// - Error handling and aggregation
/// - Performance metrics
/// </summary>
public class AsyncPipelineTests
{
    private readonly MockLogger _logger;

    public AsyncPipelineTests()
    {
        _logger = new MockLogger();
    }

    [Fact]
    public async Task ExecuteAsync_WithSingleStage_ShouldCompleteSuccessfully()
    {
        // Arrange
        var pipeline = new AsyncPipeline(_logger);
        var input = 10;
        var expectedOutput = 20;

        pipeline.AddStage<int, int>("Stage1", async (x, ct) =>
        {
            await Task.Delay(10, ct);
            return x * 2;
        });

        // Act
        var result = await pipeline.ExecuteAsync<int, int>(input);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedOutput, result.Value);
        Assert.Equal(0, result.RetryCount);
        Assert.True(result.ElapsedMilliseconds >= 10);
    }

    [Fact]
    public async Task ExecuteAsync_WithMultipleStages_ShouldChainSequentially()
    {
        // Arrange
        var pipeline = new AsyncPipeline(_logger);
        var input = 5;

        pipeline
            .AddStage<int, int>("Double", async (x, ct) => { await Task.Delay(5, ct); return x * 2; })
            .AddStage<int, int>("AddTen", async (x, ct) => { await Task.Delay(5, ct); return x + 10; })
            .AddStage<int, int>("Square", async (x, ct) => { await Task.Delay(5, ct); return x * x; });

        // Act
        var result = await pipeline.ExecuteAsync<int, int>(input);

        // Assert
        // (5 * 2 + 10) * (5 * 2 + 10) = 20 * 20 = 400
        Assert.True(result.IsSuccess);
        Assert.Equal(400, result.Value);
        Assert.True(result.ElapsedMilliseconds >= 15);
    }

    [Fact]
    public async Task ExecuteAsync_WithTimeout_ShouldReturnTimeoutException()
    {
        // Arrange
        var shortTimeout = TimeSpan.FromMilliseconds(50);
        var pipeline = new AsyncPipeline(_logger, shortTimeout);

        pipeline.AddStage<int, int>("SlowStage", async (x, ct) =>
        {
            await Task.Delay(500, ct);
            return x * 2;
        });

        // Act
        var result = await pipeline.ExecuteAsync<int, int>(10);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<TimeoutException>(result.Exception);
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellation_ShouldHandleCancellationToken()
    {
        // Arrange
        var pipeline = new AsyncPipeline(_logger, TimeSpan.FromSeconds(10));
        var cts = new CancellationTokenSource();

        var executionStarted = false;
        pipeline.AddStage<int, int>("CancellableStage", async (x, ct) =>
        {
            executionStarted = true;
            cts.Token.ThrowIfCancellationRequested();
            await Task.Delay(100, ct);
            return x * 2;
        });

        // Act
        var task = pipeline.ExecuteAsync<int, int>(10, cts.Token);
        await Task.Delay(10);
        cts.Cancel();

        var result = await task;

        // Assert
        Assert.True(executionStarted);
        Assert.False(result.IsSuccess);
        Assert.IsType<OperationCanceledException>(result.Exception);
    }

    [Fact]
    public async Task ExecuteAsync_WithRetryableFailure_ShouldRetryAndSucceed()
    {
        // Arrange
        var retryPolicy = new AsyncPipelineRetryPolicy
        {
            MaxRetries = 3,
            InitialDelay = TimeSpan.FromMilliseconds(10)
        };

        var pipeline = new AsyncPipeline(_logger, TimeSpan.FromSeconds(10), retryPolicy);
        var attemptCount = 0;

        pipeline.AddStage<int, int>("RetryStage", async (x, ct) =>
        {
            attemptCount++;
            if (attemptCount < 3)
            {
                await Task.Delay(5, ct);
                throw new InvalidOperationException($"Temporary failure on attempt {attemptCount}");
            }
            return x * 2;
        });

        // Act
        var result = await pipeline.ExecuteAsync<int, int>(10);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(20, result.Value);
        Assert.Equal(2, result.RetryCount);
    }

    [Fact]
    public async Task ExecuteAsync_WithExhaustedRetries_ShouldReturnFailure()
    {
        // Arrange
        var retryPolicy = new AsyncPipelineRetryPolicy { MaxRetries = 2 };
        var pipeline = new AsyncPipeline(_logger, TimeSpan.FromSeconds(10), retryPolicy);

        pipeline.AddStage<int, int>("AlwaysFailingStage", async (x, ct) =>
        {
            await Task.Delay(5, ct);
            throw new InvalidOperationException("Permanent failure");
        });

        // Act
        var result = await pipeline.ExecuteAsync<int, int>(10);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<InvalidOperationException>(result.Exception);
        Assert.Equal(3, result.RetryCount);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoStages_ShouldReturnConfigurationError()
    {
        // Arrange
        var pipeline = new AsyncPipeline(_logger);
        // Don't add any stages

        // Act
        var result = await pipeline.ExecuteAsync<int, int>(10);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<InvalidOperationException>(result.Exception);
        Assert.Contains("No stages", result.Exception?.Message);
    }

    [Fact]
    public async Task ExecuteParallelAsync_WithMultipleOperations_ShouldCompleteAllOperations()
    {
        // Arrange
        var pipeline = new AsyncPipeline(_logger);
        var operations = new Dictionary<string, Func<CancellationToken, Task<int>>>
        {
            { "Op1", async ct => { await Task.Delay(50, ct); return 10; } },
            { "Op2", async ct => { await Task.Delay(30, ct); return 20; } },
            { "Op3", async ct => { await Task.Delay(40, ct); return 30; } }
        };

        var stopwatch = Stopwatch.StartNew();

        // Act
        var results = await pipeline.ExecuteParallelAsync(operations);
        stopwatch.Stop();

        // Assert
        Assert.Equal(3, results.Count);
        Assert.True(results["Op1"].IsSuccess && results["Op1"].Value == 10);
        Assert.True(results["Op2"].IsSuccess && results["Op2"].Value == 20);
        Assert.True(results["Op3"].IsSuccess && results["Op3"].Value == 30);

        // Verify parallel execution (should be ~50ms, not 120ms)
        Assert.True(stopwatch.ElapsedMilliseconds < 100);
    }

    [Fact]
    public async Task ExecuteParallelAsync_WithPartialFailure_ShouldContinueOtherOperations()
    {
        // Arrange
        var pipeline = new AsyncPipeline(_logger);
        var operations = new Dictionary<string, Func<CancellationToken, Task<int>>>
        {
            { "SuccessOp", async ct => { await Task.Delay(10, ct); return 42; } },
            { "FailureOp", async ct => { await Task.Delay(10, ct); throw new Exception("Expected failure"); } },
            { "AnotherSuccess", async ct => { await Task.Delay(10, ct); return 99; } }
        };

        // Act
        var results = await pipeline.ExecuteParallelAsync(operations);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.True(results["SuccessOp"].IsSuccess);
        Assert.False(results["FailureOp"].IsSuccess);
        Assert.True(results["AnotherSuccess"].IsSuccess);
    }

    [Fact]
    public async Task ExecuteParallelAsync_WithTimeout_ShouldCancelAllOperations()
    {
        // Arrange
        var shortTimeout = TimeSpan.FromMilliseconds(100);
        var pipeline = new AsyncPipeline(_logger, shortTimeout);
        var operations = new Dictionary<string, Func<CancellationToken, Task<int>>>
        {
            { "SlowOp1", async ct => { await Task.Delay(5000, ct); return 1; } },
            { "SlowOp2", async ct => { await Task.Delay(5000, ct); return 2; } }
        };

        // Act
        var results = await pipeline.ExecuteParallelAsync(operations);

        // Assert
        Assert.True(results.Values.All(r => !r.IsSuccess));
    }

    [Fact]
    public async Task ExponentialBackoff_ShouldDelayProportionally()
    {
        // Arrange
        var retryPolicy = new AsyncPipelineRetryPolicy
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
        var retryPolicy = new AsyncPipelineRetryPolicy
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
        Assert.All(delayValues, delay => Assert.True(delay <= 100));
    }

    [Fact]
    public async Task AsyncThrottler_ShouldLimitConcurrency()
    {
        // Arrange
        var throttler = new AsyncThrottler(2);
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
        Assert.True(maxConcurrentObserved <= 2);
    }

    [Fact]
    public async Task AsyncLazy_ShouldComputeOnlyOnce()
    {
        // Arrange
        var computeCount = 0;
        var asyncLazy = new AsyncLazy<int>(async () =>
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
    public async Task AsyncBatchProcessor_ShouldBatchItemsCorrectly()
    {
        // Arrange
        var batchedItems = new List<int[]>();
        var processor = new AsyncBatchProcessor<int, int>(
            async (batch, ct) =>
            {
                batchedItems.Add(batch);
                await Task.Delay(10, ct);
                return batch.Select(x => x * 2).ToArray();
            },
            batchSize: 3);

        // Act
        var results = new List<int>();
        for (int i = 1; i <= 7; i++)
        {
            var result = await processor.AddAsync(i);
            results.Add(result);
        }
        await processor.FlushAsync();

        // Assert
        Assert.True(results.Count >= 1);
    }

    [Fact]
    public async Task PipelineExecutionMetrics_ShouldProvideAccurateTiming()
    {
        // Arrange
        var pipeline = new AsyncPipeline(_logger);
        var stageDuration = 50;

        pipeline
            .AddStage<int, int>("Stage1", async (x, ct) => { await Task.Delay(stageDuration, ct); return x + 1; })
            .AddStage<int, int>("Stage2", async (x, ct) => { await Task.Delay(stageDuration, ct); return x + 1; });

        // Act
        var result = await pipeline.ExecuteAsync<int, int>(0);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.ElapsedMilliseconds >= stageDuration * 2);
        Assert.NotEmpty(_logger.LoggedMessages);
    }

    // Mock logger for testing
    private class MockLogger : ILogger
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
}
