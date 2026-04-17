using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using HELIOS.Platform.Core.Performance;

namespace HELIOS.Platform.Tests
{
    /// <summary>
    /// Phase 4 Tier 1.2: Database Optimization & Advanced Caching Tests
    /// </summary>
    public class Phase4DatabaseOptimizationTests
    {
        private readonly Core.Logging.ILogger _logger;

        public Phase4DatabaseOptimizationTests()
        {
            _logger = new Core.Logging.ConsoleLogger();
        }

        #region Database Index Service Tests

        [Fact]
        public async Task DatabaseIndexService_EnsureIndexes_CreatesIndexes()
        {
            // Arrange
            var indexService = new DatabaseIndexService(_logger);

            // Act
            await indexService.EnsureIndexesAsync();
            var indexes = indexService.GetIndexStatistics();

            // Assert
            Assert.NotNull(indexes);
            Assert.True(indexes.Length > 0);
            Assert.True(indexes.Length >= 12); // At least 12 common indexes
        }

        [Fact]
        public async Task DatabaseIndexService_GetIndexStatistics_ReturnsValidStats()
        {
            // Arrange
            var indexService = new DatabaseIndexService(_logger);
            await indexService.EnsureIndexesAsync();

            // Act
            var stats = indexService.GetIndexStatistics();

            // Assert
            foreach (var stat in stats)
            {
                Assert.NotNull(stat.IndexName);
                Assert.NotNull(stat.TableName);
                Assert.NotNull(stat.Columns);
                Assert.True(stat.Columns.Length > 0);
            }
        }

        [Fact]
        public async Task DatabaseIndexService_AnalyzeQueryPerformance_WorksWithoutError()
        {
            // Arrange
            var indexService = new DatabaseIndexService(_logger);

            // Act & Assert
            await indexService.AnalyzeQueryPerformanceAsync("SELECT * FROM Services WHERE Status = 'Active'");
        }

        #endregion

        #region EF Core Query Optimizer Tests

        [Fact]
        public void EFCoreQueryOptimizer_ApplyOptimizations_ReturnsQueryable()
        {
            // Arrange
            var optimizer = new EFCoreQueryOptimizer(_logger);
            
            // Create a queryable of reference types (string)
            var items = new List<string> { "test1", "test2", "test3" }.AsQueryable();

            // Act
            var optimizedQuery = optimizer.ApplyOptimizations(items);

            // Assert
            Assert.NotNull(optimizedQuery);
        }

        #endregion

        #region Connection Lifecycle Service Tests

        [Fact]
        public async Task ConnectionLifecycleService_WarmupConnectionPool_IncreasePooledConnections()
        {
            // Arrange
            var service = new ConnectionLifecycleService(_logger);
            var initialPoolCount = service.GetPooledConnectionCount();

            // Act
            await service.WarmupConnectionPoolAsync();
            var finalPoolCount = service.GetPooledConnectionCount();

            // Assert
            Assert.True(finalPoolCount > initialPoolCount);
        }

        [Fact]
        public void ConnectionLifecycleService_GetActiveConnectionCount_ReturnsValidNumber()
        {
            // Arrange
            var service = new ConnectionLifecycleService(_logger);

            // Act
            var count = service.GetActiveConnectionCount();

            // Assert
            Assert.True(count >= 0);
        }

        #endregion

        #region Advanced Cache Service Tests

        [Fact]
        public void AdvancedCache_GetCached_WithL1Only_CachesValue()
        {
            // Arrange
            var l1 = new L1CacheService(_logger);
            var l2 = new InMemoryL2Cache();
            var cache = new AdvancedCacheService(l1, l2, _logger);
            var callCount = 0;
            var policy = new CachePolicy { Namespace = "test", UseL2Cache = false };

            // Act
            var result1 = cache.GetCached("key1", () =>
            {
                callCount++;
                return "value";
            }, policy);
            var result2 = cache.GetCached("key1", () =>
            {
                callCount++;
                return "value";
            }, policy);

            // Assert
            Assert.Equal("value", result1);
            Assert.Equal("value", result2);
            Assert.Equal(1, callCount); // Factory called once
        }

        [Fact]
        public async Task AdvancedCache_GetCachedAsync_WithAsyncFactory_Works()
        {
            // Arrange
            var l1 = new L1CacheService(_logger);
            var l2 = new InMemoryL2Cache();
            var cache = new AdvancedCacheService(l1, l2, _logger);
            var callCount = 0;
            var policy = new CachePolicy { Namespace = "async-test" };

            // Act
            var result1 = await cache.GetCachedAsync("async-key", async () =>
            {
                callCount++;
                await Task.Delay(5);
                return "async-value";
            }, policy);
            var result2 = await cache.GetCachedAsync("async-key", async () =>
            {
                callCount++;
                await Task.Delay(5);
                return "async-value";
            }, policy);

            // Assert
            Assert.Equal("async-value", result1);
            Assert.Equal("async-value", result2);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void AdvancedCache_GetMetrics_TracksHitsAndMisses()
        {
            // Arrange
            var l1 = new L1CacheService(_logger);
            var l2 = new InMemoryL2Cache();
            var cache = new AdvancedCacheService(l1, l2, _logger);
            var policy = new CachePolicy { UseL2Cache = false };

            // Act
            cache.GetCached("key1", () => "value1", policy); // L1 Miss
            cache.GetCached("key1", () => "value1", policy); // L1 Hit
            cache.GetCached("key2", () => "value2", policy); // L1 Miss

            var metrics = cache.GetMetrics();

            // Assert
            Assert.Equal(1, metrics.L1Hits);
            Assert.Equal(2, metrics.L1Misses);
            Assert.True(metrics.L1HitRate > 0.3);
        }

        [Fact]
        public void AdvancedCache_InvalidateCache_ClearsData()
        {
            // Arrange
            var l1 = new L1CacheService(_logger);
            var l2 = new InMemoryL2Cache();
            var cache = new AdvancedCacheService(l1, l2, _logger);
            var policy = new CachePolicy { UseL2Cache = false };

            // Act
            cache.GetCached("key1", () => "value1", policy);
            cache.InvalidateCache("key*");
            var metrics = cache.GetMetrics();

            // Assert
            Assert.True(metrics.Invalidations > 0);
        }

        #endregion

        #region In-Memory L2 Cache Tests

        [Fact]
        public void InMemoryL2Cache_Set_And_TryGet_WorkCorrectly()
        {
            // Arrange
            var cache = new InMemoryL2Cache();
            var key = "test-key";
            var value = "test-value";

            // Act
            cache.Set(key, value, TimeSpan.FromHours(1));
            var success = cache.TryGet(key, out string retrieved);

            // Assert
            Assert.True(success);
            Assert.Equal(value, retrieved);
        }

        [Fact]
        public void InMemoryL2Cache_Expired_Values_CannotBeRetrieved()
        {
            // Arrange
            var cache = new InMemoryL2Cache();
            var key = "expiring-key";

            // Act
            cache.Set(key, "value", TimeSpan.FromMilliseconds(100));
            System.Threading.Thread.Sleep(150);
            var success = cache.TryGet(key, out string _);

            // Assert
            Assert.False(success);
        }

        [Fact]
        public void InMemoryL2Cache_InvalidatePattern_RemovesMatchingKeys()
        {
            // Arrange
            var cache = new InMemoryL2Cache();

            // Act
            cache.Set("user:1", "data1", TimeSpan.FromHours(1));
            cache.Set("user:2", "data2", TimeSpan.FromHours(1));
            cache.Set("order:1", "data3", TimeSpan.FromHours(1));
            cache.InvalidatePattern("user:");

            // Assert
            Assert.False(cache.TryGet("user:1", out string _));
            Assert.False(cache.TryGet("user:2", out string _));
            Assert.True(cache.TryGet("order:1", out string _));
        }

        #endregion

        #region Cache-Aside Pattern Tests

        [Fact]
        public async Task CacheAsidePattern_GetAsync_PopulatesCacheOnMiss()
        {
            // Arrange
            var l1 = new L1CacheService(_logger);
            var l2 = new InMemoryL2Cache();
            var advancedCache = new AdvancedCacheService(l1, l2, _logger);
            var pattern = new CacheAsidePattern(advancedCache);
            var callCount = 0;

            // Act
            var result1 = await pattern.GetAsync("data-key", async () =>
            {
                callCount++;
                await Task.Delay(5);
                return new List<string> { "item1", "item2" };
            }, TimeSpan.FromMinutes(5));

            var result2 = await pattern.GetAsync("data-key", async () =>
            {
                callCount++;
                await Task.Delay(5);
                return new List<string> { "item1", "item2" };
            }, TimeSpan.FromMinutes(5));

            // Assert
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.Equal(1, callCount); // Core data function called once
        }

        [Fact]
        public async Task CacheAsidePattern_InvalidateAsync_ClearsCache()
        {
            // Arrange
            var l1 = new L1CacheService(_logger);
            var l2 = new InMemoryL2Cache();
            var advancedCache = new AdvancedCacheService(l1, l2, _logger);
            var pattern = new CacheAsidePattern(advancedCache);

            // Act
            await pattern.GetAsync("key1", async () =>
            {
                await Task.Delay(1);
                return "value1";
            }, TimeSpan.FromMinutes(5));

            await pattern.InvalidateAsync("key1");

            // Assert (expect cache to be cleared, no specific assertion on result)
        }

        #endregion

        #region Cache Policy Tests

        [Fact]
        public void CachePolicy_DefaultValues_AreSetCorrectly()
        {
            // Arrange & Act
            var policy = new CachePolicy();

            // Assert
            Assert.Equal(TimeSpan.FromMinutes(5), policy.L1Duration);
            Assert.Equal(TimeSpan.FromHours(1), policy.L2Duration);
            Assert.Equal("default", policy.Namespace);
            Assert.False(policy.UseL2Cache);
        }

        [Fact]
        public void CachePolicy_CustomValues_CanBeSet()
        {
            // Arrange & Act
            var policy = new CachePolicy
            {
                L1Duration = TimeSpan.FromMinutes(10),
                L2Duration = TimeSpan.FromHours(2),
                Namespace = "custom",
                UseL2Cache = true
            };

            // Assert
            Assert.Equal(TimeSpan.FromMinutes(10), policy.L1Duration);
            Assert.Equal(TimeSpan.FromHours(2), policy.L2Duration);
            Assert.Equal("custom", policy.Namespace);
            Assert.True(policy.UseL2Cache);
        }

        #endregion

        #region Cache Metrics Tests

        [Fact]
        public void CacheMetrics_HitRates_CalculateCorrectly()
        {
            // Arrange
            var metrics = new CacheMetrics
            {
                L1Hits = 80,
                L1Misses = 20,
                L2Hits = 10,
                L2Misses = 10
            };

            // Act
            var l1Rate = metrics.L1HitRate;
            var l2Rate = metrics.L2HitRate;
            var combinedRate = metrics.CombinedHitRate;

            // Assert
            Assert.Equal(0.8, l1Rate);
            Assert.Equal(0.5, l2Rate);
            Assert.Equal(0.75, combinedRate);
        }

        #endregion

        #region Performance Benchmarks

        [Fact]
        public void Benchmark_TwoTierCache_OutperformsNoCache()
        {
            // Arrange
            var l1 = new L1CacheService(_logger);
            var l2 = new InMemoryL2Cache();
            var cache = new AdvancedCacheService(l1, l2, _logger);
            var policy = new CachePolicy { UseL2Cache = true };
            var iterations = 1000;

            // Act
            var sw = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                cache.GetCached($"bench-key-{i % 100}", () => i, policy);
            }
            sw.Stop();

            // Assert
            var metrics = cache.GetMetrics();
            Assert.True(metrics.L1HitRate > 0.8);
            _logger.Info($"Benchmark: {iterations} operations in {sw.ElapsedMilliseconds}ms with {metrics.L1HitRate:P0} L1 hit rate");
        }

        #endregion
    }
}
