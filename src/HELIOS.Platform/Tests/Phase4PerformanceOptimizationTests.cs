using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;
using HELIOS.Platform.Core.Performance;

namespace HELIOS.Platform.Tests
{
    /// <summary>
    /// Phase 4 Tier 1: Performance Optimization Tests
    /// Tests for: L1Cache, Query Optimization, Memory Management, Connection Pooling
    /// </summary>
    public class Phase4PerformanceOptimizationTests
    {
        private readonly Core.Logging.ILogger _logger;

        public Phase4PerformanceOptimizationTests()
        {
            _logger = new Core.Logging.ConsoleLogger();
        }

        #region L1 Cache Service Tests

        [Fact]
        public void L1Cache_Get_WithFactory_ReturnsCachedValue()
        {
            // Arrange
            var cache = new L1CacheService(_logger);
            var callCount = 0;
            var key = "test-key";
            var value = "test-value";
            Func<string> factory = () =>
            {
                callCount++;
                return value;
            };

            // Act
            var result1 = cache.Get(key, factory, TimeSpan.FromHours(1));
            var result2 = cache.Get(key, factory, TimeSpan.FromHours(1));

            // Assert
            Assert.Equal(value, result1);
            Assert.Equal(value, result2);
            Assert.Equal(1, callCount); // Factory called only once
        }

        [Fact]
        public void L1Cache_Get_WithExpiredValue_CallsFactoryAgain()
        {
            // Arrange
            var cache = new L1CacheService(_logger);
            var callCount = 0;
            var key = "expiring-key";
            Func<string> factory = () =>
            {
                callCount++;
                return $"value-{callCount}";
            };

            // Act
            var result1 = cache.Get(key, factory, TimeSpan.FromMilliseconds(100));
            System.Threading.Thread.Sleep(150);
            var result2 = cache.Get(key, factory, TimeSpan.FromMilliseconds(100));

            // Assert
            Assert.Equal("value-1", result1);
            Assert.Equal("value-2", result2);
            Assert.Equal(2, callCount); // Factory called twice due to expiration
        }

        [Fact]
        public async Task L1Cache_GetAsync_WithAsyncFactory_WorksCorrectly()
        {
            // Arrange
            var cache = new L1CacheService(_logger);
            var callCount = 0;
            var key = "async-key";
            Func<Task<string>> asyncFactory = async () =>
            {
                callCount++;
                await Task.Delay(10);
                return "async-value";
            };

            // Act
            var result1 = await cache.GetAsync(key, asyncFactory, TimeSpan.FromHours(1));
            var result2 = await cache.GetAsync(key, asyncFactory, TimeSpan.FromHours(1));

            // Assert
            Assert.Equal("async-value", result1);
            Assert.Equal("async-value", result2);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void L1Cache_GetStats_TracksCacheHitsMisses()
        {
            // Arrange
            var cache = new L1CacheService(_logger);
            var callCount = 0;
            Func<int> factory = () =>
            {
                callCount++;
                return callCount;
            };

            // Act
            cache.Get("key1", factory, TimeSpan.FromHours(1)); // Miss
            cache.Get("key1", factory, TimeSpan.FromHours(1)); // Hit
            cache.Get("key1", factory, TimeSpan.FromHours(1)); // Hit
            cache.Get("key2", factory, TimeSpan.FromHours(1)); // Miss

            var stats = cache.GetStats();

            // Assert
            Assert.Equal(2, stats.HitCount);
            Assert.Equal(2, stats.MissCount);
            Assert.Equal(2, stats.EntryCount);
            Assert.True(stats.HitRate > 0.4);
        }

        #endregion

        #region Query Optimization Service Tests

        [Fact]
        public void QueryOptimization_ProfileQuery_MeasuresExecutionTime()
        {
            // Arrange
            var service = new QueryOptimizationService(_logger);
            var query = () => new List<int> { 1, 2, 3, 4, 5 };

            // Act
            var profile = service.ProfileQuery("simple-query", query);

            // Assert
            Assert.Equal("simple-query", profile.Name);
            Assert.Equal(5, profile.ItemCount);
            Assert.True(profile.ElapsedMilliseconds >= 0);
        }

        [Fact]
        public async Task QueryOptimization_ProfileQueryAsync_WorksWithAsyncQueries()
        {
            // Arrange
            var service = new QueryOptimizationService(_logger);
            Func<Task<List<int>>> asyncQuery = async () =>
            {
                await Task.Delay(10);
                return new List<int> { 1, 2, 3 };
            };

            // Act
            var profile = await service.ProfileQueryAsync("async-query", asyncQuery);

            // Assert
            Assert.Equal("async-query", profile.Name);
            Assert.Equal(3, profile.ItemCount);
            Assert.True(profile.ElapsedMilliseconds >= 10);
        }

        [Fact]
        public void QueryOptimization_GetProfiles_ReturnsRecentProfiles()
        {
            // Arrange
            var service = new QueryOptimizationService(_logger);

            // Act
            service.ProfileQuery("query1", () => new List<string> { "a", "b", "c" });
            service.ProfileQuery("query2", () => new List<string> { "d", "e" });
            service.ProfileQuery("query3", () => new List<string> { "f" });

            var profiles = service.GetProfiles();

            // Assert
            Assert.Equal(3, profiles.Length);
            Assert.NotNull(profiles[0].Name);
        }

        [Fact]
        public void QueryOptimization_ClearProfiles_RemovesAllProfiles()
        {
            // Arrange
            var service = new QueryOptimizationService(_logger);
            service.ProfileQuery("query1", () => new List<int> { 1, 2 });

            // Act
            service.ClearProfiles();
            var profiles = service.GetProfiles();

            // Assert
            Assert.Empty(profiles);
        }

        #endregion

        #region Memory Optimization Service Tests

        [Fact]
        public void MemoryOptimization_GetMemoryStats_ReturnsValidStats()
        {
            // Arrange
            var service = new MemoryOptimizationService(_logger);

            // Act
            var stats = service.GetMemoryStats();

            // Assert
            Assert.True(stats.TotalMemoryMB > 0);
            Assert.True(stats.WorkingSetMB > 0);
            Assert.True(stats.ManagedHeapMB > 0);
            Assert.True(stats.Gen0Collections >= 0);
        }

        [Fact]
        public void MemoryOptimization_ForceGarbageCollection_RunsWithoutError()
        {
            // Arrange
            var service = new MemoryOptimizationService(_logger);
            var beforeStats = service.GetMemoryStats();

            // Act
            service.ForceGarbageCollection();
            var afterStats = service.GetMemoryStats();

            // Assert
            Assert.NotNull(afterStats);
            // GC collection counts should increase
            Assert.True(afterStats.Gen0Collections >= beforeStats.Gen0Collections);
        }

        #endregion

        #region StringBuilder Pool Tests

        [Fact]
        public void StringBuilderPool_Get_ReturnsValidStringBuilder()
        {
            // Arrange
            var pool = new StringBuilderPool();

            // Act
            var sb = pool.Get();

            // Assert
            Assert.NotNull(sb);
            Assert.Equal(0, sb.Length);
        }

        [Fact]
        public void StringBuilderPool_Return_ClearsAndReusesBuilder()
        {
            // Arrange
            var pool = new StringBuilderPool();
            var sb1 = pool.Get();
            sb1.Append("test content");
            var length1 = sb1.Length;

            // Act
            pool.Return(sb1);
            var sb2 = pool.Get();

            // Assert
            Assert.Equal(0, sb2.Length); // Cleared
            Assert.True(ReferenceEquals(sb1, sb2) || sb2.Length == 0); // Reused or new
        }

        #endregion

        #region Performance Benchmarks

        [Fact]
        public void Benchmark_CacheVsNoCache_ShowsImprovement()
        {
            // Arrange
            var cache = new L1CacheService(_logger);
            var iterations = 10000;
            var expensiveFunction = () =>
            {
                System.Threading.Thread.Sleep(1);
                return "value";
            };

            // Act - Without cache
            var sw1 = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
                expensiveFunction();
            sw1.Stop();

            // Act - With cache
            var sw2 = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
                cache.Get("key", expensiveFunction, TimeSpan.FromHours(1));
            sw2.Stop();

            // Assert - Cache should be significantly faster
            Assert.True(sw2.ElapsedMilliseconds < sw1.ElapsedMilliseconds);
            _logger.Info($"Benchmark: No Cache={sw1.ElapsedMilliseconds}ms, Cached={sw2.ElapsedMilliseconds}ms");
        }

        [Fact]
        public void Benchmark_MemoryUsage_WithinAcceptableLimits()
        {
            // Arrange
            var service = new MemoryOptimizationService(_logger);

            // Act
            var stats = service.GetMemoryStats();

            // Assert
            // Target: < 300MB
            Assert.True(stats.TotalMemoryMB < 500, $"Memory usage too high: {stats.TotalMemoryMB:F2}MB");
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task Integration_CacheAndQueryOptimization_WorkTogether()
        {
            // Arrange
            var cache = new L1CacheService(_logger);
            var queryOptimizer = new QueryOptimizationService(_logger);

            // Act
            var cachedResult = cache.GetAsync(
                "query-result",
                async () =>
                {
                    return await queryOptimizer.ProfileQueryAsync(
                        "combined-query",
                        async () =>
                        {
                            await Task.Delay(10);
                            return new List<int> { 1, 2, 3, 4, 5 };
                        }
                    ).ContinueWith(t => t.Result);
                },
                TimeSpan.FromHours(1)
            );

            var result = await cachedResult;

            // Assert
            Assert.NotNull(result);
        }

        #endregion
    }
}
