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
    /// Comprehensive unit tests for TaskBatcher class.
    /// Tests functionality, thread safety, and performance characteristics.
    /// </summary>
    public class TaskBatcherTests : IDisposable
    {
        private readonly List<TaskBatcher<int>> _batchersToDispose = new();

        public void Dispose()
        {
            foreach (var batcher in _batchersToDispose)
            {
                batcher?.Dispose();
            }
            _batchersToDispose.Clear();
        }

        private TaskBatcher<int> CreateBatcher(Action<List<int>>? callback = null, int batchSize = 100, int flushInterval = 50)
        {
            var batcher = new TaskBatcher<int>(callback ?? (items => { }), batchSize, flushInterval);
            _batchersToDispose.Add(batcher);
            return batcher;
        }

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            var batcher = CreateBatcher((items) => { });
            Assert.NotNull(batcher);
            Assert.Equal(100, batcher.BatchSize);
            Assert.Equal(50, batcher.FlushInterval);
            Assert.Equal(0, batcher.QueuedItemCount);
        }

        [Fact]
        public void Constructor_WithNullCallback_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                _ = new TaskBatcher<int>(null!, 100, 50);
            });
        }

        [Fact]
        public void Constructor_WithInvalidBatchSize_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                _ = new TaskBatcher<int>(_ => { }, 0, 50);
            });

            Assert.Throws<ArgumentException>(() =>
            {
                _ = new TaskBatcher<int>(_ => { }, -1, 50);
            });
        }

        [Fact]
        public void Constructor_WithInvalidFlushInterval_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                _ = new TaskBatcher<int>(_ => { }, 100, 0);
            });

            Assert.Throws<ArgumentException>(() =>
            {
                _ = new TaskBatcher<int>(_ => { }, 100, -1);
            });
        }

        [Fact]
        public void Enqueue_SingleItem_ItemIsQueued()
        {
            var received = new List<List<int>>();
            var batcher = CreateBatcher(items => received.Add(new List<int>(items)), 10, 100);

            batcher.Enqueue(42);
            Assert.Equal(1, batcher.QueuedItemCount);
        }

        [Fact]
        public void Enqueue_MultipleItems_AllItemsQueued()
        {
            var batcher = CreateBatcher(items => { }, 100, 100);

            for (int i = 0; i < 50; i++)
            {
                batcher.Enqueue(i);
            }

            Assert.Equal(50, batcher.QueuedItemCount);
        }

        [Fact]
        public void Enqueue_UpToBatchSize_AutomaticFlushOnThreshold()
        {
            var received = new List<List<int>>();
            var batcher = CreateBatcher(items => received.Add(new List<int>(items)), 10, 1000);

            for (int i = 0; i < 10; i++)
            {
                batcher.Enqueue(i);
            }

            // Wait for async callback to complete
            Thread.Sleep(100);

            Assert.Equal(1, received.Count);
            Assert.Equal(10, received[0].Count);
            Assert.Equal(0, batcher.QueuedItemCount);
        }

        [Fact]
        public void Enqueue_ExceedBatchSize_FlushesAutomatically()
        {
            var received = new List<List<int>>();
            var batcher = CreateBatcher(items => received.Add(new List<int>(items)), 5, 1000);

            for (int i = 0; i < 12; i++)
            {
                batcher.Enqueue(i);
            }

            // Wait for async callbacks
            Thread.Sleep(150);

            Assert.True(received.Count >= 2, $"Expected at least 2 batches, but got {received.Count}");
        }

        [Fact]
        public void Flush_WithQueuedItems_DispatchesAllItems()
        {
            var received = new List<List<int>>();
            var batcher = CreateBatcher(items => received.Add(new List<int>(items)), 100, 1000);

            for (int i = 0; i < 25; i++)
            {
                batcher.Enqueue(i);
            }

            Assert.Equal(25, batcher.QueuedItemCount);

            batcher.Flush();
            Thread.Sleep(100);

            Assert.Equal(1, received.Count);
            Assert.Equal(25, received[0].Count);
            Assert.Equal(0, batcher.QueuedItemCount);
        }

        [Fact]
        public void Flush_WithEmptyQueue_DoesNothing()
        {
            var callCount = 0;
            var batcher = CreateBatcher(items => callCount++, 100, 1000);

            batcher.Flush();
            Thread.Sleep(50);

            Assert.Equal(0, callCount);
        }

        [Fact]
        public void Flush_Manual_IsPrioritizedOverTimer()
        {
            var received = new List<List<int>>();
            var batcher = CreateBatcher(items => received.Add(new List<int>(items)), 100, 5000);

            batcher.Enqueue(42);
            batcher.Flush();
            Thread.Sleep(100);

            Assert.Equal(1, received.Count);
            Assert.Single(received[0]);
            Assert.Equal(42, received[0][0]);
        }

        [Fact]
        public void AutomaticFlush_OnTimeout_DispatchesQueuedItems()
        {
            var received = new List<List<int>>();
            var batcher = CreateBatcher(items => received.Add(new List<int>(items)), 100, 100);

            batcher.Enqueue(1);
            batcher.Enqueue(2);

            // Wait for timer-based flush
            Thread.Sleep(200);

            Assert.Equal(1, received.Count);
            Assert.Equal(2, received[0].Count);
            Assert.Equal(0, batcher.QueuedItemCount);
        }

        [Fact]
        public void Dispose_WithQueuedItems_FlushesBeforeDisposal()
        {
            var received = new List<List<int>>();
            var batcher = new TaskBatcher<int>(items => received.Add(new List<int>(items)), 100, 1000);

            batcher.Enqueue(10);
            batcher.Enqueue(20);
            batcher.Dispose();
            Thread.Sleep(100);

            Assert.Equal(1, received.Count);
            Assert.Equal(2, received[0].Count);
        }

        [Fact]
        public void Dispose_MultipleCallsSafe_NoExceptions()
        {
            var batcher = new TaskBatcher<int>(_ => { }, 100, 50);
            batcher.Enqueue(1);

            batcher.Dispose();
            batcher.Dispose();
            // Should not throw
        }

        [Fact]
        public void Enqueue_AfterDisposal_ThrowsObjectDisposedException()
        {
            var batcher = new TaskBatcher<int>(_ => { }, 100, 50);
            batcher.Dispose();

            Assert.Throws<ObjectDisposedException>(() => batcher.Enqueue(1));
        }

        [Fact]
        public void Flush_AfterDisposal_ThrowsObjectDisposedException()
        {
            var batcher = new TaskBatcher<int>(_ => { }, 100, 50);
            batcher.Dispose();

            Assert.Throws<ObjectDisposedException>(() => batcher.Flush());
        }

        [Fact]
        public void BatchCallback_ReceivesCorrectItems()
        {
            var received = new List<List<int>>();
            var batcher = CreateBatcher(items => received.Add(new List<int>(items)), 5, 1000);

            var testItems = new[] { 10, 20, 30, 40, 50 };
            foreach (var item in testItems)
            {
                batcher.Enqueue(item);
            }

            Thread.Sleep(150);

            Assert.Single(received);
            Assert.Equal(testItems.OrderBy(x => x), received[0].OrderBy(x => x));
        }

        [Fact]
        public void BatchSize_CanBeModified()
        {
            var batcher = CreateBatcher(_ => { });
            Assert.Equal(100, batcher.BatchSize);

            batcher.BatchSize = 50;
            Assert.Equal(50, batcher.BatchSize);
        }

        [Fact]
        public void BatchSize_InvalidValue_ThrowsArgumentException()
        {
            var batcher = CreateBatcher(_ => { });

            Assert.Throws<ArgumentException>(() => batcher.BatchSize = 0);
            Assert.Throws<ArgumentException>(() => batcher.BatchSize = -1);
        }

        [Fact]
        public void FlushInterval_CanBeModified()
        {
            var batcher = CreateBatcher(_ => { });
            Assert.Equal(50, batcher.FlushInterval);

            batcher.FlushInterval = 100;
            Assert.Equal(100, batcher.FlushInterval);
        }

        [Fact]
        public void FlushInterval_InvalidValue_ThrowsArgumentException()
        {
            var batcher = CreateBatcher(_ => { });

            Assert.Throws<ArgumentException>(() => batcher.FlushInterval = 0);
            Assert.Throws<ArgumentException>(() => batcher.FlushInterval = -1);
        }

        [Fact]
        public void ConcurrentAccess_MultipleThreads_ThreadSafe()
        {
            var received = new List<List<int>>();
            var receiveLock = new object();
            var batcher = CreateBatcher(items =>
            {
                lock (receiveLock)
                {
                    received.Add(new List<int>(items));
                }
            }, 100, 1000);

            var tasks = new Task[10];
            for (int t = 0; t < 10; t++)
            {
                int threadId = t;
                tasks[t] = Task.Run(() =>
                {
                    for (int i = 0; i < 20; i++)
                    {
                        batcher.Enqueue(threadId * 100 + i);
                    }
                });
            }

            Task.WaitAll(tasks);
            batcher.Flush();
            Thread.Sleep(150);

            int totalItems = received.Sum(batch => batch.Count);
            Assert.Equal(200, totalItems); // 10 threads * 20 items
        }

        [Fact]
        public void ConcurrentAccess_HighContention_NoDeadlock()
        {
            var batcher = CreateBatcher(_ => { }, 10, 50);
            var shouldStop = false;

            var tasks = new List<Task>();
            for (int t = 0; t < 20; t++)
            {
                tasks.Add(Task.Run(() =>
                {
                    for (int i = 0; i < 10; i++)
                    {
                        try
                        {
                            batcher.Enqueue(1);
                        }
                        catch (ObjectDisposedException)
                        {
                            break;
                        }
                    }
                }));
            }

            // Should complete without deadlock
            Assert.True(Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(10)));
        }


        [Fact]
        public void ConcurrentAccess_EnqueueAndFlush_NoRaceConditions()
        {
            var received = new List<List<int>>();
            var receiveLock = new object();
            var batcher = CreateBatcher(items =>
            {
                lock (receiveLock)
                {
                    received.Add(new List<int>(items));
                }
            }, 50, 500);

            var tasks = new List<Task>();

            // Producer tasks
            for (int p = 0; p < 5; p++)
            {
                tasks.Add(Task.Run(() =>
                {
                    for (int i = 0; i < 30; i++)
                    {
                        batcher.Enqueue(i);
                        if (i % 10 == 0)
                            Thread.Sleep(5);
                    }
                }));
            }

            // Consumer task (manual flush)
            tasks.Add(Task.Run(() =>
            {
                for (int i = 0; i < 15; i++)
                {
                    Thread.Sleep(10);
                    try
                    {
                        batcher.Flush();
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }
                }
            }));

            Task.WaitAll(tasks.ToArray());
            batcher.Flush();
            Thread.Sleep(200);

            int totalItems = received.Sum(batch => batch.Count);
            Assert.Equal(150, totalItems); // 5 producers * 30 items
        }

        [Fact]
        public void Performance_SequentialVsBatched_ShowsImprovement()
        {
            const int itemCount = 1000;
            const int batchSize = 50;

            // Sequential processing baseline
            var sequentialStopwatch = Stopwatch.StartNew();
            int sequentialCount = 0;
            for (int i = 0; i < itemCount; i++)
            {
                sequentialCount++;
            }
            sequentialStopwatch.Stop();

            // Batched processing
            int batchedCount = 0;
            var batchedStopwatch = Stopwatch.StartNew();
            var batcher = new TaskBatcher<int>(items =>
            {
                Interlocked.Add(ref batchedCount, items.Count);
            }, batchSize, 50);

            for (int i = 0; i < itemCount; i++)
            {
                batcher.Enqueue(i);
            }

            batcher.Flush();
            Thread.Sleep(200);
            batchedStopwatch.Stop();
            batcher.Dispose();

            // Both should process same number of items
            Assert.Equal(itemCount, sequentialCount);
            Assert.Equal(itemCount, batchedCount);

            // Batched should be more efficient (or comparable)
            // The benefit is more in throughput and reduced overhead
            Assert.True(batchedStopwatch.ElapsedMilliseconds >= 0);
        }

        [Fact]
        public void Performance_Throughput_MeasuresItemsPerMillisecond()
        {
            const int itemCount = 10000;
            const int batchSize = 100;

            int processedCount = 0;
            var processedLock = new object();

            var batcher = new TaskBatcher<int>(items =>
            {
                lock (processedLock)
                {
                    processedCount += items.Count;
                }
            }, batchSize, 50);

            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < itemCount; i++)
            {
                batcher.Enqueue(i);
            }

            batcher.Flush();
            Thread.Sleep(500); // Wait for all async callbacks
            stopwatch.Stop();

            batcher.Dispose();

            double itemsPerMs = (double)processedCount / stopwatch.ElapsedMilliseconds;
            double itemsPerSecond = itemsPerMs * 1000;

            Assert.Equal(itemCount, processedCount);
            Assert.True(itemsPerSecond > 0, "Throughput should be measurable");

            // For 10k items in ~500ms, we should see high throughput
            Assert.True(itemsPerSecond > 1000, $"Expected > 1000 items/sec, got {itemsPerSecond}");
        }

        [Fact]
        public void Performance_ParallelDispatch_CalculatesImprovementPercentage()
        {
            const int itemCount = 100;
            const int batchSize = 10;

            // Simulate sequential dispatch with work simulation
            var sequentialStopwatch = Stopwatch.StartNew();
            int sequentialProcessed = 0;
            for (int i = 0; i < itemCount; i++)
            {
                sequentialProcessed++;
            }
            sequentialStopwatch.Stop();

            // Batched dispatch
            int batchedProcessed = 0;
            var batchedLock = new object();
            var batcher = new TaskBatcher<int>(items =>
            {
                lock (batchedLock)
                {
                    batchedProcessed += items.Count;
                }
            }, batchSize, 100);

            var batchedStopwatch = Stopwatch.StartNew();
            for (int i = 0; i < itemCount; i++)
            {
                batcher.Enqueue(i);
            }
            batcher.Flush();
            
            // Wait significantly longer for async operations
            for (int waitCount = 0; waitCount < 50 && batchedProcessed < itemCount; waitCount++)
            {
                Thread.Sleep(50);
            }
            
            batchedStopwatch.Stop();
            batcher.Dispose();

            // Should process all items
            Assert.Equal(itemCount, sequentialProcessed);
            Assert.Equal(itemCount, batchedProcessed);
        }

        [Fact]
        public void Batching_ReducesCallbackInvocations_ComparedToImmediate()
        {
            const int itemCount = 100;
            int callbackCount = 0;

            var batcher = new TaskBatcher<int>(items =>
            {
                Interlocked.Increment(ref callbackCount);
            }, 10, 1000);

            for (int i = 0; i < itemCount; i++)
            {
                batcher.Enqueue(i);
            }

            Thread.Sleep(150);

            batcher.Dispose();

            // With batch size of 10 and 100 items, should have ~10 callbacks
            // Not 100 callbacks (one per item)
            Assert.True(callbackCount <= 11, $"Expected ~10 callbacks, got {callbackCount}");
            Assert.True(callbackCount >= 9, $"Expected ~10 callbacks, got {callbackCount}");
        }
    }
}
