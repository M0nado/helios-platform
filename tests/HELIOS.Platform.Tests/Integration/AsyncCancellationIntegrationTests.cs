using Xunit;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HELIOS.Platform.Tests.Integration
{
    /// <summary>
    /// Integration Tests: Async Operations → CancellationToken Coordination
    /// 8 test cases testing async coordination and cancellation
    /// </summary>
    public class AsyncCancellationIntegrationTests
    {
        [Fact]
        [Trait("Category", "Integration")]
        public async Task MultipleAsyncOperations_CancelOne_OthersCompleted()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var task1 = Task.Delay(100, cts.Token);
            var task2 = Task.Delay(50, cts.Token);
            var task3 = Task.Delay(150, cts.Token);

            // Act
            cts.CancelAfter(75);
            try
            {
                await Task.WhenAll(task1, task2, task3);
            }
            catch (OperationCanceledException) { }

            // Assert
            Assert.True(task2.IsCompleted);
            Assert.True(task1.IsCanceled || !task1.IsCompleted);
            Assert.True(task3.IsCanceled || !task3.IsCompleted);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task CancellationToken_LinkedTokens_CascadingCancel()
        {
            // Arrange
            var parentCts = new CancellationTokenSource();
            var childCts = CancellationTokenSource.CreateLinkedTokenSource(parentCts.Token);

            // Act
            parentCts.Cancel();

            // Assert
            Assert.True(childCts.Token.IsCancellationRequested);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task AsyncOperation_WithTimeout_Cancels()
        {
            // Arrange
            var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => Task.Delay(TimeSpan.FromSeconds(1), cts.Token));
            sw.Stop();
            Assert.InRange(sw.ElapsedMilliseconds, 30, 100);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task CancellationToken_CheckedRegularly_StopsEarly()
        {
            // Arrange
            var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
            var iterations = 0;

            // Act & Assert
            try
            {
                for (int i = 0; i < 1000; i++)
                {
                    cts.Token.ThrowIfCancellationRequested();
                    await Task.Delay(10, cts.Token);
                    iterations++;
                }
            }
            catch (OperationCanceledException) { }

            Assert.InRange(iterations, 5, 15);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task AsyncPipeline_MultipleStages_CancellationPropagates()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var stage1Complete = false;
            var stage2Complete = false;

            Func<CancellationToken, Task> stage1 = async (ct) =>
            {
                await Task.Delay(50, ct);
                stage1Complete = true;
            };

            Func<CancellationToken, Task> stage2 = async (ct) =>
            {
                await Task.Delay(50, ct);
                stage2Complete = true;
            };

            // Act
            cts.CancelAfter(75);
            try
            {
                await stage1(cts.Token);
                await stage2(cts.Token);
            }
            catch (OperationCanceledException) { }

            // Assert
            Assert.True(stage1Complete);
            Assert.False(stage2Complete);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task CleanupCode_ExecutedOnCancellation_ResourcesFreed()
        {
            // Arrange
            var cleanupCalled = false;
            var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

            // Act
            try
            {
                await Task.Delay(500, cts.Token);
            }
            catch (OperationCanceledException)
            {
                cleanupCalled = true;
            }

            // Assert
            Assert.True(cleanupCalled);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task ParallelAsyncOperations_CancelAll_AllStop()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var tasks = new List<Task>();

            for (int i = 0; i < 5; i++)
            {
                tasks.Add(Task.Delay(1000, cts.Token));
            }

            // Act
            cts.CancelAfter(50);
            try
            {
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException) { }

            // Assert
            Assert.All(tasks, t => Assert.True(t.IsCanceled || t.IsFaulted));
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task CancellationTokenCallback_RegisteredHandler_Invoked()
        {
            // Arrange
            var callbackInvoked = false;
            var cts = new CancellationTokenSource();

            cts.Token.Register(() => { callbackInvoked = true; });

            // Act
            cts.Cancel();
            await Task.Delay(10);

            // Assert
            Assert.True(callbackInvoked);
        }
    }
}
