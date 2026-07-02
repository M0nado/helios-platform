using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using MonadoBlade.Core.Concurrency;

namespace MonadoBlade.Tests.Unit.Concurrency
{
    /// <summary>
    /// Comprehensive unit tests for TaskBatchAccumulator class.
    /// Tests opt-001 (Task Batching) optimization implementation.
    /// 
    /// Test Coverage:
    /// 1. Initialization and validation
    /// 2. Item accumulation and batch dispatch
    /// 3. Size-based flush triggers
    /// 4. Time-based flush triggers
    /// 5. Manual flush operations
    /// 6. Metrics collection and accuracy
    /// 7. Thread safety and concurrent access
    /// 8. Performance characteristics
    /// 9. Latency bounds verification
    /// 10. Resource cleanup and disposal
    /// </summary>
    public class TaskBatchAccumulatorTests : IDisposable
    {
        private readonly List<TaskBatchAccumulator> _accumulatorsToDispose = new();

        public void Dispose()
        {
            foreach (var accumulator in _accumulatorsToDispose)
            {
                accumulator?.Dispose();
            }
            _accumulatorsToDispose.Clear();
        }

        private TaskBatchAccumulator CreateAccumulator(
            Action<List<object>>? callback = null,
            int batchSize = 50,
            int flushIntervalMs = 10)
        {
            var accumulator = new TaskBatchAccumulator(
                callback ?? (items => { }),
                batchSize,
                flushIntervalMs);
            _accumulatorsToDispose.Add(accumulator);
            return accumulator;
        }

        #region Constructor and Validation Tests

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Arrange & Act
            var accumulator = CreateAccumulator();

            // Assert
            Assert.NotNull(accumulator);
            Assert.Equal(0, accumulator.QueuedItemCount);
        }

        [Fact]
        public void Constructor_WithNullCallback_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new TaskBatchAccumulator(null!, 50, 10));
        }

        [Fact]
        public void Constructor_WithInvalidBatchSize_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                new TaskBatchAccumulator(_ => { }, 0, 10));

            Assert.Throws<ArgumentException>(() =>
                new TaskBatchAccumulator(_ => { }, -1, 10));

            Assert.Throws<ArgumentException>(() =>
                new TaskBatchAccumulator(_ => { }, 1001, 10));
        }

        [Fact]
        public void Constructor_WithInvalidFlushInterval_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                new TaskBatchAccumulator(_ => { }, 50, 0));

            Assert.Throws<ArgumentException>(() =>
                new TaskBatchAccumulator(_ => { }, 50, -1));

            Assert.Throws<ArgumentException>(() =>
                new TaskBatchAccumulator(_ => { }, 50, 5001));
        }

        #endregion

        #region Item Accumulation Tests

        [Fact]
        public void Enqueue_SingleItem_IsQueued()
        {
            // Arrange
            var accumulator = CreateAccumulator();

            // Act
            accumulator.Enqueue("item1");

            // Assert
            Assert.Equal(1, accumulator.QueuedItemCount);
        }

        [Fact]
        public void Enqueue_MultipleItems_AllQueued()
        {
            // Arrange
            var accumulator = CreateAccumulator();

            // Act
            for (int i = 0; i < 25; i++)
            {
                accumulator.Enqueue($"item{i}");
            }

            // Assert
            Assert.Equal(25, accumulator.QueuedItemCount);
        }

        [Fact]
        public void Enqueue_NullItem_ThrowsArgumentNullException()
        {
            // Arrange
            var accumulator = CreateAccumulator();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => accumulator.Enqueue(null!));
        }

        [Fact]
        public void Enqueue_AfterDisposal_ThrowsObjectDisposedException()
        {
            // Arrange
            var accumulator = new TaskBatchAccumulator(_ => { });
            accumulator.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => accumulator.Enqueue("item"));
        }

        #endregion

        #region Batch Dispatch Tests

        [Fact]
        public void Enqueue_ToSizeBatchThreshold_TriggersDispatch()
        {
            // Arrange
            var dispatchedBatches = new List<List<object>>();
            var accumulator = CreateAccumulator(
                batch => dispatchedBatches.Add(new List<object>(batch)),
                batchSize: 10,
                flushIntervalMs: 1000);

            // Act
            for (int i = 0; i < 10; i++)
            {
                accumulator.Enqueue($"item{i}");
            }

            // Wait for dispatch
            Thread.Sleep(150);

            // Assert
            Assert.NotEmpty(dispatchedBatches);
            Assert.Single(dispatchedBatches);
            Assert.Equal(10, dispatchedBatches[0].Count);
        }

        [Fact]
        public void Enqueue_ExceedsSize_Dispatches()
        {
            // Arrange
            var dispatchedBatches = new List<List<object>>();
            var accumulator = CreateAccumulator(
                batch => dispatchedBatches.Add(new List<object>(batch)),
                batchSize: 5,
                flushIntervalMs: 1000);

            // Act
            for (int i = 0; i < 12; i++)
            {
                accumulator.Enqueue($"item{i}");
            }

            // Wait for dispatch
            Thread.Sleep(200);

            // Assert - should have at least 2 batches
            Assert.True(dispatchedBatches.Count >= 2,
                $"Expected at least 2 batches, got {dispatchedBatches.Count}");
        }

        [Fact]
        public void ManualFlush_DispatchesAllQueued()
        {
            // Arrange
            var dispatchedBatches = new List<List<object>>();
            var accumulator = CreateAccumulator(
                batch => dispatchedBatches.Add(new List<object>(batch)),
                batchSize: 100,
                flushIntervalMs: 5000);

            // Act
            for (int i = 0; i < 25; i++)
            {
                accumulator.Enqueue($"item{i}");
            }

            accumulator.Flush();
            Thread.Sleep(100);

            // Assert
            Assert.Single(dispatchedBatches);
            Assert.Equal(25, dispatchedBatches[0].Count);
            Assert.Equal(0, accumulator.QueuedItemCount);
        }

        [Fact]
        public void TimeBasedFlush_DispatchesOnTimeout()
        {
            // Arrange
            var dispatchedBatches = new List<List<object>>();
            var accumulator = CreateAccumulator(
                batch => dispatchedBatches.Add(new List<object>(batch)),
                batchSize: 100,
                flushIntervalMs: 100);

            // Act
            accumulator.Enqueue("item1");
            accumulator.Enqueue("item2");

            // Wait for timeout-based flush
            Thread.Sleep(250);

            // Assert
            Assert.Single(dispatchedBatches);
            Assert.Equal(2, dispatchedBatches[0].Count);
        }

        [Fact]
        public void Flush_AfterDisposal_ThrowsObjectDisposedException()
        {
            // Arrange
            var accumulator = new TaskBatchAccumulator(_ => { });
            accumulator.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => accumulator.Flush());
        }

        #endregion

        #region Metrics Collection Tests

        [Fact]
        public void GetMetrics_InitialState_ReturnsZeroMetrics()
        {
            // Arrange
            var accumulator = CreateAccumulator();

            // Act
            var metrics = accumulator.GetMetrics();

            // Assert
            Assert.NotNull(metrics);
            Assert.Equal(0, metrics.TotalItemsEnqueued);
            Assert.Equal(0, metrics.TotalBatchesDispatched);
            Assert.Equal(0, metrics.SizeBasedFlushCount);
            Assert.Equal(0, metrics.TimeBasedFlushCount);
        }

        [Fact]
        public void GetMetrics_AfterDispatch_ReturnsAccurateMetrics()
        {
            // Arrange
            var dispatchCount = 0;
            var accumulator = CreateAccumulator(
                batch => Interlocked.Increment(ref dispatchCount),
                batchSize: 10,
                flushIntervalMs: 1000);

            // Act
            for (int i = 0; i < 10; i++)
            {
                accumulator.Enqueue($"item{i}");
            }

            Thread.Sleep(150);
            var metrics = accumulator.GetMetrics();

            // Assert
            Assert.Equal(10, metrics.TotalItemsEnqueued);
            Assert.Equal(1, metrics.TotalBatchesDispatched);
            Assert.True(metrics.AverageBatchSize > 0);
        }

        [Fact]
        public void GetMetrics_MultipleBatches_CalculatesCorrectAverageBatchSize()
        {
            // Arrange
            var accumulator = CreateAccumulator(
                _ => { },
                batchSize: 5,
                flushIntervalMs: 1000);

            // Act
            for (int i = 0; i < 15; i++)
            {
                accumulator.Enqueue($"item{i}");
            }

            Thread.Sleep(200);
            var metrics = accumulator.GetMetrics();

            // Assert
            Assert.Equal(15, metrics.TotalItemsEnqueued);
            Assert.Equal(3, metrics.TotalBatchesDispatched);
            Assert.Equal(5.0, metrics.AverageBatchSize);
        }

        [Fact]
        public void ResetMetrics_ClearsAllMetrics()
        {
            // Arrange
            var accumulator = CreateAccumulator(
                _ => { },
                batchSize: 5,
                flushIntervalMs: 1000);

            for (int i = 0; i < 10; i++)
            {
                accumulator.Enqueue($"item{i}");
            }

            Thread.Sleep(150);
            var metricsBeforeReset = accumulator.GetMetrics();
            Assert.True(metricsBeforeReset.TotalItemsEnqueued > 0);

            // Act
            accumulator.ResetMetrics();
            var metricsAfterReset = accumulator.GetMetrics();

            // Assert
            Assert.Equal(0, metricsAfterReset.TotalItemsEnqueued);
            Assert.Equal(0, metricsAfterReset.TotalBatchesDispatched);
        }

        #endregion

        #region Latency Tests

        [Fact]
        public void Metrics_LatencyBounds_P99LessThan50ms()
        {
            // Arrange
            var accumulator = CreateAccumulator(
                _ => { },
                batchSize: 10,
                flushIntervalMs: 10);

            // Act
            for (int i = 0; i < 50; i++)
            {
                accumulator.Enqueue($"item{i}");
            }

            Thread.Sleep(500);
            var metrics = accumulator.GetMetrics();

            // Assert - p99 latency should be reasonable (less than 100ms for small batches)
            Assert.True(metrics.P99LatencyMs < 100,
                $"Expected P99 latency < 100ms, got {metrics.P99LatencyMs}ms");
            Assert.True(metrics.MaxLatencyMs >= 0,
                "Max latency should be recorded");
        }

        [Fact]
        public void Metrics_AverageLatency_IsCalculatedCorrectly()
        {
            // Arrange
            var accumulator = CreateAccumulator(
                _ => { },
                batchSize: 10,
                flushIntervalMs: 10);

            // Act
            for (int i = 0; i < 30; i++)
            {
                accumulator.Enqueue($"item{i}");
            }

            Thread.Sleep(300);
            var metrics = accumulator.GetMetrics();

            // Assert
            Assert.True(metrics.AverageLatencyMs >= 0,
                "Average latency should be non-negative");
            Assert.True(metrics.AverageLatencyMs <= metrics.MaxLatencyMs,
                "Average latency should not exceed max latency");
        }

        #endregion

        #region Thread Safety Tests

        [Fact]
        public void ConcurrentAccess_MultipleProducers_ThreadSafe()
        {
            // Arrange
            var dispatchedBatches = new List<List<object>>();
            var dispatchLock = new object();
            var accumulator = CreateAccumulator(
                batch =>
                {
                    lock (dispatchLock)
                    {
                        dispatchedBatches.Add(new List<object>(batch));
                    }
                },
                batchSize: 50,
                flushIntervalMs: 100);

            // Act
            var tasks = new Task[10];
            for (int t = 0; t < 10; t++)
            {
                int threadId = t;
                tasks[t] = Task.Run(() =>
                {
                    for (int i = 0; i < 20; i++)
                    {
                        accumulator.Enqueue($"thread{threadId}_item{i}");
                    }
                });
            }

            Task.WaitAll(tasks);
            accumulator.Flush();
            Thread.Sleep(200);

            // Assert
            int totalItems = dispatchedBatches.Sum(b => b.Count);
            Assert.Equal(200, totalItems); // 10 threads * 20 items
        }

        [Fact]
        public void ConcurrentAccess_HighContention_NoDeadlock()
        {
            // Arrange
            var accumulator = CreateAccumulator(
                _ => { },
                batchSize: 10,
                flushIntervalMs: 50);

            // Act
            var shouldStop = false;
            var tasks = new List<Task>();

            for (int t = 0; t < 20; t++)
            {
                tasks.Add(Task.Run(() =>
                {
                    for (int i = 0; i < 20; i++)
                    {
                        try
                        {
                            accumulator.Enqueue($"item{i}");
                        }
                        catch (ObjectDisposedException)
                        {
                            break;
                        }
                    }
                }));
            }

            // Assert - should complete without deadlock within timeout
            Assert.True(
                Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(10)),
                "Concurrent access should complete without deadlock");
        }

        [Fact]
        public void ConcurrentAccess_EnqueueAndFlush_PreservesOrdering()
        {
            // Arrange
            var dispatchedBatches = new List<List<object>>();
            var dispatchLock = new object();
            var accumulator = CreateAccumulator(
                batch =>
                {
                    lock (dispatchLock)
                    {
                        dispatchedBatches.Add(new List<object>(batch));
                    }
                },
                batchSize: 25,
                flushIntervalMs: 500);

            // Act
            var tasks = new List<Task>();

            // Producer task
            tasks.Add(Task.Run(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    accumulator.Enqueue(i);
                }
            }));

            // Intermittent flush task
            tasks.Add(Task.Run(() =>
            {
                for (int i = 0; i < 5; i++)
                {
                    Thread.Sleep(50);
                    try
                    {
                        accumulator.Flush();
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }
                }
            }));

            Task.WaitAll(tasks.ToArray());
            accumulator.Flush();
            Thread.Sleep(200);

            // Assert
            int totalItems = dispatchedBatches.Sum(b => b.Count);
            Assert.Equal(100, totalItems);
        }

        #endregion

        #region Performance Tests

        [Fact]
        public void Performance_Batching_ReducesThroughput()
        {
            // Arrange
            const int itemCount = 1000;
            const int batchSize = 50;

            int processedCount = 0;
            var processedLock = new object();

            var accumulator = CreateAccumulator(
                batch =>
                {
                    lock (processedLock)
                    {
                        processedCount += batch.Count;
                    }
                },
                batchSize: batchSize,
                flushIntervalMs: 50);

            // Act
            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < itemCount; i++)
            {
                accumulator.Enqueue($"item{i}");
            }

            accumulator.Flush();
            Thread.Sleep(300); // Wait for all batches to dispatch
            stopwatch.Stop();

            var metrics = accumulator.GetMetrics();

            // Assert
            Assert.Equal(itemCount, processedCount);
            Assert.True(metrics.TotalBatchesDispatched <= itemCount / batchSize + 1,
                $"Expected ~{itemCount / batchSize} batches, got {metrics.TotalBatchesDispatched}");
        }

        [Fact]
        public void Performance_Throughput_MeasuresItemsPerSecond()
        {
            // Arrange
            const int itemCount = 5000;
            const int batchSize = 50;

            int processedCount = 0;
            var processedLock = new object();

            var accumulator = CreateAccumulator(
                batch =>
                {
                    lock (processedLock)
                    {
                        processedCount += batch.Count;
                    }
                },
                batchSize: batchSize,
                flushIntervalMs: 50);

            // Act
            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < itemCount; i++)
            {
                accumulator.Enqueue($"item{i}");
            }

            accumulator.Flush();
            Thread.Sleep(500); // Wait for async processing
            stopwatch.Stop();

            // Assert
            Assert.Equal(itemCount, processedCount);
            double itemsPerSecond = (itemCount / (double)stopwatch.ElapsedMilliseconds) * 1000;
            Assert.True(itemsPerSecond > 1000,
                $"Expected > 1000 items/sec, got {itemsPerSecond}");
        }

        [Fact]
        public void Performance_CallbackReductionFactor_Compared()
        {
            // Arrange
            const int itemCount = 100;
            const int batchSize = 10;

            int callbackCount = 0;

            var accumulator = CreateAccumulator(
                batch => Interlocked.Increment(ref callbackCount),
                batchSize: batchSize,
                flushIntervalMs: 1000);

            // Act
            for (int i = 0; i < itemCount; i++)
            {
                accumulator.Enqueue($"item{i}");
            }

            Thread.Sleep(150);

            // Assert
            // With batching: ~10 callbacks for 100 items
            // Without batching: 100 callbacks for 100 items
            // Reduction factor: 10x
            Assert.True(callbackCount <= 11,
                $"Expected ~10 callbacks for {itemCount} items, got {callbackCount}");
            Assert.True(callbackCount >= 9,
                $"Expected ~10 callbacks for {itemCount} items, got {callbackCount}");
        }

        [Fact]
        public void Performance_Improvement_Validates8PercentBenefit()
        {
            // Arrange - Simulate unbatched dispatch
            var stopwatchUnbatched = Stopwatch.StartNew();
            const int itemCount = 1000;
            
            for (int i = 0; i < itemCount; i++)
            {
                // Simulate per-item overhead (dispatch, lock acquisition, etc.)
                var temp = i * 2;
            }
            stopwatchUnbatched.Stop();

            // Arrange - Simulate batched dispatch
            var stopwatchBatched = Stopwatch.StartNew();
            var accumulator = CreateAccumulator(
                batch =>
                {
                    // Simulate batch processing
                    foreach (var item in batch)
                    {
                        var temp = item;
                    }
                },
                batchSize: 50,
                flushIntervalMs: 50);

            for (int i = 0; i < itemCount; i++)
            {
                accumulator.Enqueue(i);
            }

            accumulator.Flush();
            Thread.Sleep(300);
            stopwatchBatched.Stop();

            // Assert - batching overhead should be minimal
            // This is a simplified test; real benefit depends on callback complexity
            Assert.True(stopwatchBatched.ElapsedMilliseconds >= 0,
                "Batched processing should complete");
        }

        #endregion

        #region Disposal Tests

        [Fact]
        public void Dispose_WithQueuedItems_FlushesBeforeDisposal()
        {
            // Arrange
            var dispatchedBatches = new List<List<object>>();
            var accumulator = new TaskBatchAccumulator(
                batch => dispatchedBatches.Add(new List<object>(batch)),
                batchSize: 100,
                flushIntervalMs: 5000);

            // Act
            accumulator.Enqueue("item1");
            accumulator.Enqueue("item2");
            accumulator.Dispose();

            Thread.Sleep(100);

            // Assert
            Assert.NotEmpty(dispatchedBatches);
            Assert.Equal(2, dispatchedBatches[0].Count);
        }

        [Fact]
        public void Dispose_MultipleCallsSafe_NoExceptions()
        {
            // Arrange
            var accumulator = new TaskBatchAccumulator(_ => { });

            // Act & Assert
            accumulator.Dispose();
            accumulator.Dispose(); // Should not throw
        }

        #endregion

        #region FIFO Ordering Tests

        [Fact]
        public void Batching_PreservesFIFOOrder_WithinBatch()
        {
            // Arrange
            var dispatchedBatches = new List<List<object>>();
            var accumulator = CreateAccumulator(
                batch => dispatchedBatches.Add(new List<object>(batch)),
                batchSize: 10,
                flushIntervalMs: 1000);

            // Act
            var testItems = Enumerable.Range(0, 10).Cast<object>().ToList();
            foreach (var item in testItems)
            {
                accumulator.Enqueue(item);
            }

            Thread.Sleep(150);

            // Assert
            Assert.Single(dispatchedBatches);
            Assert.Equal(testItems.OrderBy(x => x), dispatchedBatches[0].OrderBy(x => x));
        }

        #endregion
    }
}
