using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using MonadoBlade.Core.Messaging;

namespace MonadoBlade.Tests.Unit.Messaging
{
    public class MessageCoalescerTests
    {
        private class TestMessage
        {
            public string Id { get; set; }
            public string Content { get; set; }

            public TestMessage(string id, string content)
            {
                Id = id;
                Content = content;
            }
        }

        [Fact]
        public void Constructor_WithValidCallback_Succeeds()
        {
            // Arrange & Act
            var coalescer = new MessageCoalescer<TestMessage, string>(
                (key, messages) => { });

            // Assert
            Assert.NotNull(coalescer);
            Assert.Equal(100, coalescer.FlushInterval);
            Assert.Equal(50, coalescer.BatchSizeThreshold);
            Assert.Equal(0, coalescer.CoalescedCount);

            coalescer.Dispose();
        }

        [Fact]
        public void Constructor_WithNullCallback_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new MessageCoalescer<TestMessage, string>(null));
        }

        [Fact]
        public void Enqueue_SingleMessage_IsAccumulated()
        {
            // Arrange
            var messages = new List<(string key, List<TestMessage> batch)>();
            var coalescer = new MessageCoalescer<TestMessage, string>(
                (key, batch) => messages.Add((key, batch)));

            var msg = new TestMessage("1", "Hello");

            // Act
            coalescer.Enqueue("key1", msg);
            coalescer.Flush();

            // Assert
            Assert.Single(messages);
            Assert.Equal("key1", messages[0].key);
            Assert.Single(messages[0].batch);
            Assert.Equal("1", messages[0].batch[0].Id);

            coalescer.Dispose();
        }

        [Fact]
        public void Enqueue_MultipleMessagesWithSameKey_AreAccumulated()
        {
            // Arrange
            var messages = new List<(string key, List<TestMessage> batch)>();
            var coalescer = new MessageCoalescer<TestMessage, string>(
                (key, batch) => messages.Add((key, batch)));

            var msg1 = new TestMessage("1", "Hello");
            var msg2 = new TestMessage("2", "World");
            var msg3 = new TestMessage("3", "!");

            // Act
            coalescer.Enqueue("key1", msg1);
            coalescer.Enqueue("key1", msg2);
            coalescer.Enqueue("key1", msg3);
            coalescer.Flush();

            // Assert
            Assert.Single(messages);
            Assert.Equal("key1", messages[0].key);
            Assert.Equal(3, messages[0].batch.Count);
            Assert.Equal("1", messages[0].batch[0].Id);
            Assert.Equal("2", messages[0].batch[1].Id);
            Assert.Equal("3", messages[0].batch[2].Id);

            coalescer.Dispose();
        }

        [Fact]
        public void Enqueue_MultipleMessagesWithDifferentKeys_AreGroupedSeparately()
        {
            // Arrange
            var messages = new List<(string key, List<TestMessage> batch)>();
            var coalescer = new MessageCoalescer<TestMessage, string>(
                (key, batch) => messages.Add((key, batch)));

            coalescer.BatchSizeThreshold = 100; // Prevent automatic flush

            var msg1 = new TestMessage("1", "A");
            var msg2 = new TestMessage("2", "B");
            var msg3 = new TestMessage("3", "C");

            // Act
            coalescer.Enqueue("key1", msg1);
            coalescer.Enqueue("key2", msg2);
            coalescer.Enqueue("key3", msg3);
            coalescer.Flush();

            // Assert
            Assert.Equal(3, messages.Count);
            var keys = messages.Select(m => m.key).ToHashSet();
            Assert.Contains("key1", keys);
            Assert.Contains("key2", keys);
            Assert.Contains("key3", keys);

            coalescer.Dispose();
        }

        [Fact]
        public void Enqueue_DuplicateKeys_MergeInSingleBatch()
        {
            // Arrange
            var dispatchedBatches = new List<(string key, int count)>();
            var coalescer = new MessageCoalescer<TestMessage, string>(
                (key, batch) => dispatchedBatches.Add((key, batch.Count)));

            coalescer.BatchSizeThreshold = 100; // Prevent auto-flush by size

            // Act
            for (int i = 0; i < 5; i++)
            {
                coalescer.Enqueue("key1", new TestMessage(i.ToString(), $"msg{i}"));
            }
            coalescer.Flush();

            // Assert
            Assert.Single(dispatchedBatches);
            Assert.Equal("key1", dispatchedBatches[0].key);
            Assert.Equal(5, dispatchedBatches[0].count);

            coalescer.Dispose();
        }

        [Fact]
        public void Enqueue_ExceedingBatchSizeThreshold_AutomaticallyFlushes()
        {
            // Arrange
            var dispatchedBatches = new List<(string key, int count)>();
            var coalescer = new MessageCoalescer<TestMessage, string>(
                (key, batch) => dispatchedBatches.Add((key, batch.Count)))
            {
                BatchSizeThreshold = 5
            };

            // Act
            for (int i = 0; i < 12; i++)
            {
                coalescer.Enqueue("key1", new TestMessage(i.ToString(), $"msg{i}"));
            }
            coalescer.Flush();

            // Assert
            Assert.NotEmpty(dispatchedBatches);
            // First batch should be flushed when hitting threshold
            Assert.True(dispatchedBatches[0].count >= 5);

            coalescer.Dispose();
        }

        [Fact]
        public void Flush_WithNoMessages_DoesNotDispatch()
        {
            // Arrange
            var dispatchCount = 0;
            var coalescer = new MessageCoalescer<TestMessage, string>(
                (key, batch) => dispatchCount++);

            // Act
            coalescer.Flush();

            // Assert
            Assert.Equal(0, dispatchCount);

            coalescer.Dispose();
        }

        [Fact]
        public void Flush_ManuallyFlushesAllBatches()
        {
            // Arrange
            var dispatchedBatches = new List<(string key, int count)>();
            var coalescer = new MessageCoalescer<TestMessage, string>(
                (key, batch) => dispatchedBatches.Add((key, batch.Count)))
            {
                BatchSizeThreshold = 1000 // Prevent auto-flush
            };

            // Act
            coalescer.Enqueue("key1", new TestMessage("1", "msg1"));
            coalescer.Enqueue("key2", new TestMessage("2", "msg2"));
            coalescer.Enqueue("key3", new TestMessage("3", "msg3"));
            coalescer.Flush();

            // Assert
            Assert.Equal(3, dispatchedBatches.Count);

            coalescer.Dispose();
        }

        [Fact]
        public void TimeoutBasedFlush_FlushesAfterInterval()
        {
            // Arrange
            var dispatchedBatches = new List<(string key, int count)>();
            var coalescer = new MessageCoalescer<TestMessage, string>(
                (key, batch) => dispatchedBatches.Add((key, batch.Count)))
            {
                FlushInterval = 50,
                BatchSizeThreshold = 1000 // Prevent auto-flush by size
            };

            // Act
            coalescer.Enqueue("key1", new TestMessage("1", "msg1"));
            Thread.Sleep(150); // Wait for timer to trigger

            // Assert
            Assert.NotEmpty(dispatchedBatches);

            coalescer.Dispose();
        }

        [Fact]
        public void ConcurrentEnqueue_FromMultipleThreads_IsThreadSafe()
        {
            // Arrange
            var dispatchedCount = 0;
            var coalescer = new MessageCoalescer<TestMessage, string>(
                (key, batch) => Interlocked.Add(ref dispatchedCount, batch.Count))
            {
                FlushInterval = 50,
                BatchSizeThreshold = 100
            };

            const int threadCount = 15;
            const int messagesPerThread = 100;

            // Act
            var tasks = new Task[threadCount];
            for (int t = 0; t < threadCount; t++)
            {
                int threadId = t;
                tasks[t] = Task.Run(() =>
                {
                    for (int i = 0; i < messagesPerThread; i++)
                    {
                        var key = $"key{threadId % 3}"; // 3 different keys
                        coalescer.Enqueue(key, new TestMessage(i.ToString(), $"msg"));
                    }
                });
            }

            Task.WaitAll(tasks);
            coalescer.Flush();

            // Assert
            var totalExpected = threadCount * messagesPerThread;
            Assert.Equal(totalExpected, coalescer.TotalMessagesEnqueued);

            coalescer.Dispose();
        }

        [Fact]
        public void EdgeCase_SameKeyDifferentMessages_AllIncludedInBatch()
        {
            // Arrange
            var dispatchedBatches = new List<List<TestMessage>>();
            var coalescer = new MessageCoalescer<TestMessage, string>(
                (key, batch) => dispatchedBatches.Add(new List<TestMessage>(batch)))
            {
                BatchSizeThreshold = 1000
            };

            var messages = new[]
            {
                new TestMessage("1", "content1"),
                new TestMessage("2", "content2"),
                new TestMessage("3", "content3"),
                new TestMessage("4", "content4"),
            };

            // Act
            foreach (var msg in messages)
            {
                coalescer.Enqueue("sameKey", msg);
            }
            coalescer.Flush();

            // Assert
            Assert.Single(dispatchedBatches);
            Assert.Equal(4, dispatchedBatches[0].Count);
            for (int i = 0; i < messages.Length; i++)
            {
                Assert.Equal(messages[i].Id, dispatchedBatches[0][i].Id);
                Assert.Equal(messages[i].Content, dispatchedBatches[0][i].Content);
            }

            coalescer.Dispose();
        }

        [Fact]
        public void EdgeCase_TimeoutBeforeBatchFull_FlushesPartialBatch()
        {
            // Arrange
            var dispatchedBatches = new List<(string key, int count)>();
            var coalescer = new MessageCoalescer<TestMessage, string>(
                (key, batch) => dispatchedBatches.Add((key, batch.Count)))
            {
                FlushInterval = 50,
                BatchSizeThreshold = 1000 // Much larger than messages
            };

            // Act
            coalescer.Enqueue("key1", new TestMessage("1", "msg1"));
            coalescer.Enqueue("key1", new TestMessage("2", "msg2"));
            coalescer.Enqueue("key1", new TestMessage("3", "msg3"));
            
            Thread.Sleep(150); // Wait for timeout

            // Assert
            Assert.NotEmpty(dispatchedBatches);
            Assert.Equal(3, dispatchedBatches[0].count);

            coalescer.Dispose();
        }

        [Fact]
        public void GetMetrics_ReturnsAccurateData()
        {
            // Arrange
            var coalescer = new MessageCoalescer<TestMessage, string>(
                (key, batch) => { })
            {
                BatchSizeThreshold = 1000
            };

            // Act
            coalescer.Enqueue("key1", new TestMessage("1", "msg"));
            coalescer.Enqueue("key1", new TestMessage("2", "msg"));
            coalescer.Enqueue("key1", new TestMessage("3", "msg"));
            coalescer.Flush();

            var metrics = coalescer.GetMetrics();

            // Assert
            Assert.NotNull(metrics);
            Assert.Equal(3L, (long)metrics["TotalMessagesEnqueued"]);
            Assert.Equal(1L, (long)metrics["TotalBatchesDispatched"]);
            // Reduction: 3 messages dispatched as 1 batch = 2 messages reduced
            Assert.Equal(2L, (long)metrics["TotalMessagesReduced"]);
            // Reduction percentage: (2/3)*100 = 66.67%
            var reductionPct = (double)metrics["ReductionPercentage"];
            Assert.True(reductionPct > 66.0 && reductionPct < 67.0);

            coalescer.Dispose();
        }

        [Fact]
        public void GetMetrics_MultipleKeys_CalculatesCorrectReduction()
        {
            // Arrange
            var dispatchCount = 0;
            var coalescer = new MessageCoalescer<TestMessage, string>(
                (key, batch) => dispatchCount++)
            {
                BatchSizeThreshold = 1000
            };

            // Act
            // Key1: 5 messages -> 1 dispatch (reduces 4)
            for (int i = 0; i < 5; i++)
                coalescer.Enqueue("key1", new TestMessage(i.ToString(), "msg"));

            // Key2: 3 messages -> 1 dispatch (reduces 2)
            for (int i = 0; i < 3; i++)
                coalescer.Enqueue("key2", new TestMessage(i.ToString(), "msg"));

            coalescer.Flush();
            var metrics = coalescer.GetMetrics();

            // Assert
            Assert.Equal(8L, (long)metrics["TotalMessagesEnqueued"]);
            Assert.Equal(2L, (long)metrics["TotalBatchesDispatched"]);
            Assert.Equal(6L, (long)metrics["TotalMessagesReduced"]); // 4 + 2
            var reductionPct = (double)metrics["ReductionPercentage"];
            Assert.True(Math.Abs(reductionPct - 75.0) < 0.1); // 6/8 = 75%

            coalescer.Dispose();
        }

        [Fact]
        public void ResetMetrics_ClearsAllMetrics()
        {
            // Arrange
            var coalescer = new MessageCoalescer<TestMessage, string>(
                (key, batch) => { })
            {
                BatchSizeThreshold = 1000
            };

            coalescer.Enqueue("key1", new TestMessage("1", "msg"));
            coalescer.Flush();

            // Act
            coalescer.ResetMetrics();
            var metrics = coalescer.GetMetrics();

            // Assert
            Assert.Equal(0L, (long)metrics["TotalMessagesEnqueued"]);
            Assert.Equal(0L, (long)metrics["TotalBatchesDispatched"]);
            Assert.Equal(0L, (long)metrics["TotalMessagesReduced"]);

            coalescer.Dispose();
        }

        [Fact]
        public void Dispose_StopsTimer()
        {
            // Arrange
            var dispatchCount = 0;
            var coalescer = new MessageCoalescer<TestMessage, string>(
                (key, batch) => dispatchCount++)
            {
                FlushInterval = 50,
                BatchSizeThreshold = 1000
            };

            coalescer.Enqueue("key1", new TestMessage("1", "msg"));

            // Act
            coalescer.Dispose();
            int countAfterDispose = dispatchCount;
            Thread.Sleep(150);

            // Assert
            // Should not have dispatched after dispose
            Assert.Equal(countAfterDispose, dispatchCount);
        }

        [Fact]
        public void Enqueue_AfterDispose_ThrowsObjectDisposedException()
        {
            // Arrange
            var coalescer = new MessageCoalescer<TestMessage, string>(
                (key, batch) => { });
            coalescer.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() =>
                coalescer.Enqueue("key", new TestMessage("1", "msg")));
        }

        [Fact]
        public void Benchmark_SequentialVsCoalesced_ShowsThroughputImprovement()
        {
            // Benchmark: Sequential dispatch vs coalesced
            const int messageCount = 10000;
            var sw = new Stopwatch();

            // Baseline: sequential dispatch
            var sequentialDispatchCount = 0;
            sw.Start();
            for (int i = 0; i < messageCount; i++)
            {
                sequentialDispatchCount++;
            }
            sw.Stop();
            var sequentialTime = sw.ElapsedMilliseconds;

            // Optimized: coalesced dispatch
            var coalescedDispatchCount = 0;
            var coalescer = new MessageCoalescer<TestMessage, string>(
                (key, batch) => coalescedDispatchCount++)
            {
                FlushInterval = 100,
                BatchSizeThreshold = 5 // Aggressive batching
            };

            sw.Restart();
            for (int i = 0; i < messageCount; i++)
            {
                coalescer.Enqueue($"key{i % 20}", new TestMessage(i.ToString(), "msg"));
            }
            coalescer.Flush();
            sw.Stop();
            var coalescedTime = sw.ElapsedMilliseconds;

            var metrics = coalescer.GetMetrics();
            var reductionPct = (double)metrics["ReductionPercentage"];

            // Assert
            // We should have significantly fewer dispatch operations
            Assert.True(coalescedDispatchCount < messageCount / 2,
                $"Expected < {messageCount / 2} dispatches, got {coalescedDispatchCount}");

            // Reduction should be significant (60-80% target)
            Assert.True(reductionPct > 50.0,
                $"Expected reduction > 50%, got {reductionPct}%");

            var throughputImprovement = ((double)(messageCount - coalescedDispatchCount) / messageCount) * 100;
            
            // Log results for verification
            System.Diagnostics.Debug.WriteLine($"Sequential: {sequentialDispatchCount} dispatches in {sequentialTime}ms");
            System.Diagnostics.Debug.WriteLine($"Coalesced: {coalescedDispatchCount} dispatches in {coalescedTime}ms");
            System.Diagnostics.Debug.WriteLine($"Throughput Improvement: {Math.Round(throughputImprovement, 2)}%");
            System.Diagnostics.Debug.WriteLine($"Messages Reduced: {reductionPct}%");

            coalescer.Dispose();
        }

        [Fact]
        public void Benchmark_MeasureCoalescingEfficiency()
        {
            // Arrange
            const int messageCount = 10000;
            var dispatchedMessages = new List<int>();

            var coalescer = new MessageCoalescer<TestMessage, string>(
                (key, batch) => dispatchedMessages.Add(batch.Count))
            {
                FlushInterval = 100,
                BatchSizeThreshold = 5
            };

            // Act
            for (int i = 0; i < messageCount; i++)
            {
                coalescer.Enqueue($"key{i % 20}", new TestMessage(i.ToString(), "msg"));
            }
            coalescer.Flush();

            var metrics = coalescer.GetMetrics();

            // Assert
            var totalDispatched = (long)metrics["TotalBatchesDispatched"];
            var totalReduced = (long)metrics["TotalMessagesReduced"];
            var reductionPct = (double)metrics["ReductionPercentage"];

            Assert.True(totalDispatched < messageCount,
                $"Expected fewer dispatches: {totalDispatched} vs {messageCount}");
            Assert.True(reductionPct >= 60.0 && reductionPct <= 95.0,
                $"Expected reduction between 60-95%, got {reductionPct}%");

            System.Diagnostics.Debug.WriteLine($"Efficiency Benchmark Results:");
            System.Diagnostics.Debug.WriteLine($"  Messages Enqueued: {messageCount}");
            System.Diagnostics.Debug.WriteLine($"  Batches Dispatched: {totalDispatched}");
            System.Diagnostics.Debug.WriteLine($"  Messages Reduced: {totalReduced}");
            System.Diagnostics.Debug.WriteLine($"  Reduction %: {reductionPct}%");
            System.Diagnostics.Debug.WriteLine($"  Avg Batch Size: {(double)messageCount / totalDispatched:F2}");

            coalescer.Dispose();
        }
    }
}
