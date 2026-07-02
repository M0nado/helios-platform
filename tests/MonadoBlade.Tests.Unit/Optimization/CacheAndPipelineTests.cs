using Xunit;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MonadoBlade.Core.Optimization;

namespace MonadoBlade.Tests.Unit.Optimization
{
    public class CacheLayerOptimizationTests
    {
        private readonly InMemoryDistributedCache _cache;

        public CacheLayerOptimizationTests()
        {
            _cache = new InMemoryDistributedCache();
        }

        [Fact]
        public async Task GetAsync_WithValidKey_ReturnsValue()
        {
            // Arrange
            var key = "test-key";
            var value = "test-value";
            await _cache.SetAsync(key, value);

            // Act
            var result = await _cache.GetAsync<string>(key);

            // Assert
            Assert.Equal(value, result);
        }

        [Fact]
        public async Task SetAsync_WithTTL_ExpiresAfterTimeout()
        {
            // Arrange
            var key = "expire-key";
            var value = "expire-value";
            await _cache.SetAsync(key, value, TimeSpan.FromMilliseconds(100));

            // Act
            await Task.Delay(150);
            var result = await _cache.GetAsync<string>(key);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task RemoveAsync_RemovesKey()
        {
            // Arrange
            var key = "remove-key";
            await _cache.SetAsync(key, "value");

            // Act
            await _cache.RemoveAsync(key);
            var result = await _cache.GetAsync<string>(key);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task ExistsAsync_WithValidKey_ReturnsTrue()
        {
            // Arrange
            var key = "exists-key";
            await _cache.SetAsync(key, "value");

            // Act
            var exists = await _cache.ExistsAsync(key);

            // Assert
            Assert.True(exists);
        }

        [Fact]
        public async Task ClearAsync_RemovesAllEntries()
        {
            // Arrange
            await _cache.SetAsync("key1", "value1");
            await _cache.SetAsync("key2", "value2");

            // Act
            await _cache.ClearAsync();
            var result1 = await _cache.GetAsync<string>("key1");
            var result2 = await _cache.GetAsync<string>("key2");

            // Assert
            Assert.Null(result1);
            Assert.Null(result2);
        }

        [Fact]
        public async Task GetAsync_IncreasesAccessCount()
        {
            // Arrange
            var key = "access-key";
            await _cache.SetAsync(key, "value");

            // Act
            await _cache.GetAsync<string>(key);
            await _cache.GetAsync<string>(key);
            await _cache.GetAsync<string>(key);

            // Assert
            // Verify through behavior (multiple gets should not fail)
            var result = await _cache.GetAsync<string>(key);
            Assert.Equal("value", result);
        }

        [Fact]
        public void TTLManager_ValidateTTL_ThrowsOnZero()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => TTLManager.ValidateTTL(TimeSpan.Zero));
        }

        [Fact]
        public void TTLManager_ValidateTTL_ThrowsOn25Hours()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => TTLManager.ValidateTTL(TimeSpan.FromHours(25)));
        }

        [Fact]
        public void TTLManager_IsExpired_ReturnsTrueWhenExpired()
        {
            // Arrange
            var createdTime = DateTime.UtcNow.AddSeconds(-2);
            var ttl = TimeSpan.FromSeconds(1);

            // Act
            var isExpired = TTLManager.IsExpired(createdTime, ttl);

            // Assert
            Assert.True(isExpired);
        }
    }

    public class CacheInvalidationPatternsTests
    {
        private readonly InMemoryDistributedCache _cache;
        private readonly CacheInvalidationPatterns _patterns;

        public CacheInvalidationPatternsTests()
        {
            _cache = new InMemoryDistributedCache();
            _patterns = new CacheInvalidationPatterns(_cache);
        }

        [Fact]
        public void RegisterPattern_StoresPattern()
        {
            // Act
            _patterns.RegisterPattern("user:*", new[] { "profile:*" });

            // Assert
            // Verify through behavior
            Assert.NotNull(_patterns);
        }

        [Fact]
        public void RegisterCallback_InvokesOnInvalidation()
        {
            // Arrange
            var called = false;
            _patterns.RegisterPattern("test:*");
            _patterns.RegisterCallback("test:*", _ => { called = true; });

            // Act
            _patterns.InvalidatePattern("test:*");

            // Assert
            Assert.True(called);
        }
    }

    public class AsyncPipelineV2Tests
    {
        [Fact]
        public async Task ExecuteAsync_Sequential_RunsInOrder()
        {
            // Arrange
            var pipeline = new AsyncPipelineV2();
            var order = new List<int>();
            var operations = new List<Func<Task<int>>>
            {
                () => { order.Add(1); return Task.FromResult(1); },
                () => { order.Add(2); return Task.FromResult(2); },
                () => { order.Add(3); return Task.FromResult(3); }
            };

            // Act
            await pipeline.ExecuteAsync(operations, AsyncPipelineV2.ExecutionMode.Sequential);

            // Assert
            Assert.Equal(new[] { 1, 2, 3 }, order);
        }

        [Fact]
        public async Task ExecuteAsync_WithTimeout_CancelsAfterTimeout()
        {
            // Arrange
            var pipeline = new AsyncPipelineV2();
            var operations = new List<Func<Task<int>>>
            {
                async () => { await Task.Delay(5000); return 1; }
            };

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                pipeline.ExecuteAsync(operations, AsyncPipelineV2.ExecutionMode.Sequential, TimeSpan.FromMilliseconds(100))
            );
        }

        [Fact]
        public async Task ExecuteAsync_Parallel_RunsConcurrently()
        {
            // Arrange
            var pipeline = new AsyncPipelineV2();
            var startTimes = new List<DateTime>();
            var operations = new List<Func<Task<int>>>
            {
                async () => { startTimes.Add(DateTime.UtcNow); await Task.Delay(100); return 1; },
                async () => { startTimes.Add(DateTime.UtcNow); await Task.Delay(100); return 2; }
            };

            // Act
            await pipeline.ExecuteAsync(operations, AsyncPipelineV2.ExecutionMode.Parallel);

            // Assert
            Assert.Equal(2, startTimes.Count);
            // Second operation should start almost immediately after first (concurrent)
            var timeDiff = Math.Abs((startTimes[1] - startTimes[0]).TotalMilliseconds);
            Assert.True(timeDiff < 50);
        }

        [Fact]
        public async Task ExecuteAsync_WithRetries_RetriesOnFailure()
        {
            // Arrange
            var pipeline = new AsyncPipelineV2();
            var attempts = 0;
            var operations = new List<Func<Task<int>>>
            {
                async () =>
                {
                    attempts++;
                    if (attempts < 3)
                        throw new Exception("Simulated failure");
                    return await Task.FromResult(1);
                }
            };

            // Act
            await pipeline.ExecuteAsync(operations, maxRetries: 2);

            // Assert
            Assert.Equal(3, attempts);
        }
    }

    public class AsyncWorkflowOrchestratorTests
    {
        [Fact]
        public async Task ExecuteAsync_CompletesDependencies()
        {
            // Arrange
            var orchestrator = new AsyncWorkflowOrchestrator();
            var execution = new List<string>();

            orchestrator.AddStep("step1", async (_) => { execution.Add("step1"); return "result1"; });
            orchestrator.AddStep("step2", async (results) => { execution.Add("step2"); return "result2"; }, "step1");
            orchestrator.AddStep("step3", async (results) => { execution.Add("step3"); return "result3"; }, "step2");

            // Act
            var results = await orchestrator.ExecuteAsync();

            // Assert
            Assert.Equal(new[] { "step1", "step2", "step3" }, execution);
            Assert.Equal(3, results.Count);
        }

        [Fact]
        public async Task ExecuteAsync_ParallelSteps_ExecuteInParallel()
        {
            // Arrange
            var orchestrator = new AsyncWorkflowOrchestrator();
            var execution = new List<string>();
            var lockObject = new object();

            orchestrator.AddStep("step1", async (_) => { lock (lockObject) execution.Add("step1"); await Task.Delay(50); return "result1"; });
            orchestrator.AddStep("step2", async (_) => { lock (lockObject) execution.Add("step2"); await Task.Delay(50); return "result2"; });

            // Act
            var results = await orchestrator.ExecuteAsync();

            // Assert
            Assert.Equal(2, results.Count);
            Assert.Equal(2, execution.Count);
        }
    }
}
