using Xunit;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HELIOS.Platform.Tests.Unit
{
    /// <summary>
    /// Unit Tests for Async Methods - 10 test cases
    /// Category: Unit
    /// Tests async patterns and cancellation
    /// </summary>
    public class AsyncMethodsTests
    {
        [Fact]
        [Trait("Category", "Unit")]
        public async Task AsyncMethod_WithTimeout_CompletesBeforeTimeout()
        {
            // Arrange
            var delay = TimeSpan.FromMilliseconds(100);
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

            // Act
            await Task.Delay(delay, cts.Token);

            // Assert - Should not throw
            Assert.False(cts.Token.IsCancellationRequested);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task AsyncMethod_ExceededTimeout_ThrowsOperationCanceledException()
        {
            // Arrange
            var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                async () => await Task.Delay(TimeSpan.FromSeconds(1), cts.Token));
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task MultipleAsyncOperations_RunInParallel_AllComplete()
        {
            // Arrange
            var tasks = new List<Task>();
            for (int i = 0; i < 5; i++)
            {
                tasks.Add(Task.Delay(100));
            }

            // Act
            var sw = System.Diagnostics.Stopwatch.StartNew();
            await Task.WhenAll(tasks);
            sw.Stop();

            // Assert - Should complete in ~100ms, not 500ms
            Assert.InRange(sw.ElapsedMilliseconds, 50, 200);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task AsyncMethod_WithException_ThrowsCorrectException()
        {
            // Arrange
            var mockTask = Task.FromException(new InvalidOperationException("Test error"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await mockTask);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task AsyncMethod_ReturnsValue_ValueIsCorrect()
        {
            // Arrange
            var expectedValue = 42;
            var task = Task.FromResult(expectedValue);

            // Act
            var result = await task;

            // Assert
            Assert.Equal(expectedValue, result);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task AsyncMethod_CancellationRequested_StopsEarly()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // Act
            cts.CancelAfter(50);
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(10), cts.Token);
            }
            catch (OperationCanceledException) { }
            sw.Stop();

            // Assert
            Assert.InRange(sw.ElapsedMilliseconds, 30, 150);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task AsyncMethod_SequentialOperations_ExecuteInOrder()
        {
            // Arrange
            var sequence = new List<int>();

            // Act
            await Task.Delay(10);
            sequence.Add(1);
            await Task.Delay(10);
            sequence.Add(2);
            await Task.Delay(10);
            sequence.Add(3);

            // Assert
            Assert.Equal(new[] { 1, 2, 3 }, sequence);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task AsyncMethod_ReturnsTask_CanAwait()
        {
            // Arrange
            Func<Task<string>> asyncFunc = async () =>
            {
                await Task.Delay(10);
                return "Result";
            };

            // Act
            var result = await asyncFunc();

            // Assert
            Assert.Equal("Result", result);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task AsyncMethod_WaitAny_ReturnsFirstCompleted()
        {
            // Arrange
            var task1 = Task.Delay(200);
            var task2 = Task.Delay(50);
            var task3 = Task.Delay(150);

            // Act
            var completedIndex = await Task.WhenAny(task1, task2, task3).ContinueWith(
                t => Array.IndexOf(new[] { task1, task2, task3 }, t.Result));

            // Assert
            Assert.Equal(1, completedIndex);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task AsyncMethod_MultipleResults_CollectsAll()
        {
            // Arrange
            var tasks = new List<Task<int>>();
            for (int i = 1; i <= 5; i++)
            {
                var value = i;
                tasks.Add(Task.FromResult(value));
            }

            // Act
            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, results);
        }
    }
}
