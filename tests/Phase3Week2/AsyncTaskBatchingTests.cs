using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using MonadoBlade.Core.Concurrency;

namespace MonadoBlade.Tests.Phase3Week2
{
    /// <summary>
    /// Comprehensive test suite for async task batching optimization.
    /// Tests correctness, performance, and thread safety.
    /// </summary>
    public class AsyncTaskBatchingTests
    {
        [Fact]
        public void TaskBatcher_Enqueue_Single_Item()
        {
            var results = new List<int>();
            var batcher = new TaskBatcher<int>(
                batch => results.AddRange(batch),
                batchSize: 5,
                flushInterval: 100
            );

            batcher.Enqueue(42);
            batcher.Flush();

            Assert.Single(results);
            Assert.Equal(42, results[0]);
        }

        [Fact]
        public void TaskBatcher_Enqueue_Multiple_Items_Below_Threshold()
        {
            var results = new List<int>();
            var batcher = new TaskBatcher<int>(
                batch => results.AddRange(batch),
                batchSize: 10,
                flushInterval: 100
            );

            for (int i = 0; i < 5; i++)
                batcher.Enqueue(i);

            batcher.Flush();

            Assert.Equal(5, results.Count);
            Assert.True(results.SequenceEqual(new[] { 0, 1, 2, 3, 4 }));
        }

        [Fact]
        public void TaskBatcher_AutoFlush_On_BatchSize_Reached()
        {
            var results = new List<int>();
            var autoFlushTriggered = false;

            var batcher = new TaskBatcher<int>(
                batch =>
                {
                    autoFlushTriggered = true;
                    results.AddRange(batch);
                },
                batchSize: 5,
                flushInterval: 1000 // High to avoid timer-based flush
            );

            // Add exactly batch size items
            for (int i = 0; i < 5; i++)
                batcher.Enqueue(i);

            // Give time for async flush
            Thread.Sleep(100);

            Assert.True(autoFlushTriggered, "Auto-flush should be triggered when batch size reached");
            Assert.Equal(5, results.Count);
        }

        [Fact]
        public void TaskBatcher_AutoFlush_On_Timeout()
        {
            var results = new List<int>();
            var batcher = new TaskBatcher<int>(
                batch => results.AddRange(batch),
                batchSize: 100,
                flushInterval: 50 // Short timeout
            );

            // Add items below batch size
            for (int i = 0; i < 3; i++)
                batcher.Enqueue(i);

            // Wait for timer to flush
            Thread.Sleep(200);

            Assert.Equal(3, results.Count);
        }

        [Fact]
        public void TaskBatcher_Preserves_Order()
        {
            var results = new List<int>();
            var batcher = new TaskBatcher<int>(
                batch => results.AddRange(batch),
                batchSize: 50,
                flushInterval: 100
            );

            var expected = Enumerable.Range(0, 100).ToList();
            foreach (var item in expected)
                batcher.Enqueue(item);

            batcher.Flush();

            Assert.Equal(expected, results);
        }

        [Fact]
        public void TaskBatcher_ConcurrentEnqueue_ThreadSafe()
        {
            var results = new List<int>();
            var resultLock = new object();
            var batcher = new TaskBatcher<int>(
                batch =>
                {
                    lock (resultLock)
                    {
                        results.AddRange(batch);
                    }
                },
                batchSize: 50,
                flushInterval: 100
            );

            var tasks = new List<Task>();
            for (int t = 0; t < 10; t++)
            {
                tasks.Add(Task.Run(() =>
                {
                    for (int i = 0; i < 100; i++)
                    {
                        batcher.Enqueue(i);
                    }
                }));
            }

            Task.WaitAll(tasks.ToArray());
            batcher.Flush();

            // Should have processed 10 * 100 = 1000 items
            Assert.Equal(1000, results.Count);
        }

        [Fact]
        public void TaskBatcher_Multiple_Flushes()
        {
            var flushCount = 0;
            var batcher = new TaskBatcher<int>(
                batch => Interlocked.Increment(ref flushCount),
                batchSize: 5,
                flushInterval: 100
            );

            // First batch
            for (int i = 0; i < 5; i++)
                batcher.Enqueue(i);
            batcher.Flush();

            // Wait for processing
            Thread.Sleep(50);
            Assert.Equal(1, flushCount);

            // Second batch
            for (int i = 0; i < 3; i++)
                batcher.Enqueue(i);
            batcher.Flush();

            Thread.Sleep(50);
            Assert.Equal(2, flushCount);
        }

        [Fact]
        public void TaskBatcher_Exception_In_Callback_DoesNotCrash()
        {
            var batcher = new TaskBatcher<int>(
                batch =>
                {
                    throw new InvalidOperationException("Intentional test exception");
                },
                batchSize: 5,
                flushInterval: 100
            );

            // Should not throw
            for (int i = 0; i < 5; i++)
                batcher.Enqueue(i);

            batcher.Flush();
            Thread.Sleep(100); // Allow async processing

            // Batcher should still be usable
            batcher.Enqueue(99);
            batcher.Dispose();
        }

        [Fact]
        public void TaskBatcher_Dispose_Flushes_Remaining()
        {
            var results = new List<int>();
            var batcher = new TaskBatcher<int>(
                batch => results.AddRange(batch),
                batchSize: 100,
                flushInterval: 1000
            );

            for (int i = 0; i < 10; i++)
                batcher.Enqueue(i);

            batcher.Dispose();
            Thread.Sleep(100); // Allow async processing

            // Should have flushed on dispose
            Assert.Equal(10, results.Count);
        }

        [Fact]
        public void TaskBatcher_QueuedItemCount_Accurate()
        {
            var batcher = new TaskBatcher<int>(
                batch => { },
                batchSize: 10,
                flushInterval: 1000
            );

            Assert.Equal(0, batcher.QueuedItemCount);

            batcher.Enqueue(1);
            Assert.Equal(1, batcher.QueuedItemCount);

            batcher.Enqueue(2);
            Assert.Equal(2, batcher.QueuedItemCount);

            batcher.Flush();
            Thread.Sleep(100);

            Assert.Equal(0, batcher.QueuedItemCount);
        }

        [Fact]
        public void TaskBatcher_Performance_HighThroughput()
        {
            var sw = Stopwatch.StartNew();
            var processedCount = 0;
            var processedLock = new object();

            var batcher = new TaskBatcher<int>(
                batch =>
                {
                    lock (processedLock)
                    {
                        processedCount += batch.Count;
                    }
                },
                batchSize: 100,
                flushInterval: 50
            );

            var itemCount = 10000;
            for (int i = 0; i < itemCount; i++)
                batcher.Enqueue(i);

            batcher.Flush();
            Thread.Sleep(200); // Allow final processing

            sw.Stop();

            Assert.Equal(itemCount, processedCount);
            
            // Performance check: should process 10K items in <500ms
            Assert.True(sw.ElapsedMilliseconds < 500, 
                $"Processing took {sw.ElapsedMilliseconds}ms, expected <500ms");

            var throughput = itemCount * 1000 / sw.ElapsedMilliseconds;
            Assert.True(throughput > 20000, 
                $"Throughput {throughput} items/sec, expected >20K items/sec");
        }
    }

    /// <summary>
    /// Integration tests for async task batching with full system.
    /// </summary>
    public class AsyncTaskBatchingIntegrationTests
    {
        [Fact]
        public async Task TaskBatcher_Integration_WithAsync_Operations()
        {
            var results = new List<int>();
            var resultLock = new object();

            var batcher = new TaskBatcher<Func<Task>>(
                batch =>
                {
                    Task.WhenAll(batch.Select(func => func())).Wait();
                },
                batchSize: 10,
                flushInterval: 100
            );

            // Enqueue async operations
            for (int i = 0; i < 50; i++)
            {
                var value = i;
                batcher.Enqueue(async () =>
                {
                    await Task.Delay(10);
                    lock (resultLock)
                    {
                        results.Add(value);
                    }
                });
            }

            batcher.Flush();
            await Task.Delay(500);

            Assert.Equal(50, results.Count);
        }

        [Fact]
        public void TaskBatcher_Integration_Distributed_System_Simulation()
        {
            var messageQueue = new List<string>();
            var queueLock = new object();

            var batcher = new TaskBatcher<string>(
                batch =>
                {
                    lock (queueLock)
                    {
                        messageQueue.AddRange(batch);
                    }
                },
                batchSize: 25,
                flushInterval: 100
            );

            // Simulate 5 services sending messages
            var tasks = new List<Task>();
            for (int service = 0; service < 5; service++)
            {
                tasks.Add(Task.Run(() =>
                {
                    for (int msg = 0; msg < 200; msg++)
                    {
                        batcher.Enqueue($"Message-{msg}");
                    }
                }));
            }

            Task.WaitAll(tasks.ToArray());
            batcher.Flush();
            Thread.Sleep(200);

            Assert.Equal(5 * 200, messageQueue.Count);
        }
    }
}
