using Xunit;
using MonadoBlade.Core.Optimization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;

namespace MonadoBlade.Tests.Unit.Optimization
{
    public class TaskBatchAccumulatorTests
    {
        [Fact]
        public async Task Enqueue_SingleItem_AddsToQueue()
        {
            // Arrange
            var processed = new List<int>();
            var accumulator = new TaskBatchAccumulator<int>(
                async batch =>
                {
                    processed.AddRange(batch);
                    await Task.CompletedTask;
                },
                batchSize: 10,
                flushIntervalMs: 100
            );

            // Act
            accumulator.Enqueue(42);
            await Task.Delay(150); // Wait for flush timer

            // Assert
            Assert.Equal(1, accumulator.TotalEnqueued);
            Assert.Contains(42, processed);
        }

        [Fact]
        public async Task EnqueueRange_MultipleItems_BatchesCorrectly()
        {
            // Arrange
            var processed = new List<int>();
            var accumulator = new TaskBatchAccumulator<int>(
                async batch =>
                {
                    processed.AddRange(batch);
                    await Task.CompletedTask;
                },
                batchSize: 5,
                flushIntervalMs: 100
            );

            // Act
            accumulator.EnqueueRange(new[] { 1, 2, 3, 4, 5, 6, 7 });
            await Task.Delay(150);

            // Assert
            Assert.Equal(7, accumulator.TotalEnqueued);
            Assert.NotEmpty(processed);
        }

        [Fact]
        public async Task Flush_ExecutesImmediately()
        {
            // Arrange
            var processed = new List<int>();
            var accumulator = new TaskBatchAccumulator<int>(
                async batch =>
                {
                    processed.AddRange(batch);
                    await Task.CompletedTask;
                },
                batchSize: 100,
                flushIntervalMs: 10000
            );

            // Act
            accumulator.Enqueue(99);
            await accumulator.FlushAsync();

            // Assert
            Assert.Contains(99, processed);
        }

        [Fact]
        public async Task Performance_HighThroughputScenario()
        {
            // Arrange
            int processedCount = 0;
            var accumulator = new TaskBatchAccumulator<int>(
                async batch =>
                {
                    processedCount += batch.Count;
                    await Task.CompletedTask;
                },
                batchSize: 100,
                flushIntervalMs: 50
            );

            // Act
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 10000; i++)
            {
                accumulator.Enqueue(i);
            }
            await accumulator.FlushAsync();
            sw.Stop();

            // Assert
            Assert.Equal(10000, accumulator.TotalEnqueued);
            Assert.Equal(10000, processedCount);
            Assert.True(sw.ElapsedMilliseconds < 5000);
        }

        [Fact]
        public async Task BatchEfficiency_CalculatesCorrectly()
        {
            // Arrange
            var accumulator = new TaskBatchAccumulator<int>(
                async batch => await Task.CompletedTask,
                batchSize: 50,
                flushIntervalMs: 100
            );

            // Act
            for (int i = 0; i < 150; i++)
            {
                accumulator.Enqueue(i);
            }
            await accumulator.FlushAsync();

            // Assert
            Assert.True(accumulator.BatchEfficiency > 40);
        }

        [Fact]
        public void Dispose_StopsProcessing()
        {
            // Arrange
            var accumulator = new TaskBatchAccumulator<int>(
                async batch => await Task.CompletedTask,
                batchSize: 10,
                flushIntervalMs: 100
            );

            // Act
            accumulator.Dispose();

            // Assert
            Assert.Throws<ObjectDisposedException>(() => accumulator.Enqueue(1));
        }
    }
}
