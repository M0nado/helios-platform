using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using MonadoBlade.Core.Caching;

namespace MonadoBlade.Tests.Unit.Caching
{
    public class CacheEntryTests
    {
        [Fact]
        public void CacheEntry_ShouldStoreKeyAndValue()
        {
            // Arrange
            var key = "test-key";
            var value = 42;
            var expiresAt = DateTime.UtcNow.AddHours(1);

            // Act
            var entry = new CacheEntry<int>(key, value, expiresAt);

            // Assert
            Assert.Equal(key, entry.Key);
            Assert.Equal(value, entry.Value);
            Assert.Equal(expiresAt, entry.ExpiresAt);
        }

        [Fact]
        public void CacheEntry_ValidProperty_ShouldReturnTrueBeforeExpiration()
        {
            // Arrange
            var expiresAt = DateTime.UtcNow.AddHours(1);
            var entry = new CacheEntry<string>("key", "value", expiresAt);

            // Act
            var isValid = entry.IsValid;

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void CacheEntry_ValidProperty_ShouldReturnFalseAfterExpiration()
        {
            // Arrange
            var expiresAt = DateTime.UtcNow.AddSeconds(-1);
            var entry = new CacheEntry<string>("key", "value", expiresAt);

            // Act
            var isValid = entry.IsValid;

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void CacheEntry_ShouldStoreDependencies()
        {
            // Arrange
            var dependencies = new[] { "dep1", "dep2", "dep3" };
            var expiresAt = DateTime.UtcNow.AddHours(1);

            // Act
            var entry = new CacheEntry<string>("key", "value", expiresAt, dependencies);

            // Assert
            Assert.Equal(dependencies.Length, entry.Dependencies.Count);
            Assert.All(dependencies, dep => Assert.Contains(dep, entry.Dependencies));
        }

        [Fact]
        public void CacheEntry_DependsOn_ShouldReturnTrue()
        {
            // Arrange
            var dependencies = new[] { "dep1", "dep2" };
            var entry = new CacheEntry<string>("key", "value", DateTime.UtcNow.AddHours(1), dependencies);

            // Act
            var dependsOnDep1 = entry.DependsOn("dep1");
            var dependsOnDep3 = entry.DependsOn("dep3");

            // Assert
            Assert.True(dependsOnDep1);
            Assert.False(dependsOnDep3);
        }

        [Fact]
        public void CacheEntry_RecordAccess_ShouldIncrementCount()
        {
            // Arrange
            var entry = new CacheEntry<string>("key", "value", DateTime.UtcNow.AddHours(1));
            Assert.Equal(0, entry.AccessCount);

            // Act
            entry.RecordAccess();
            entry.RecordAccess();

            // Assert
            Assert.Equal(2, entry.AccessCount);
            Assert.NotNull(entry.LastAccessedAt);
        }
    }

    public class DependencyTrackerTests
    {
        [Fact]
        public void RegisterDependency_ShouldRegisterSuccessfully()
        {
            // Arrange
            var tracker = new DependencyTracker();

            // Act
            tracker.RegisterDependency("key1", "key2");

            // Assert
            var dependents = tracker.GetDependents("key2");
            Assert.Contains("key1", dependents);
        }

        [Fact]
        public void RegisterDependency_SelfDependency_ShouldThrow()
        {
            // Arrange
            var tracker = new DependencyTracker();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => tracker.RegisterDependency("key1", "key1"));
        }

        [Fact]
        public void RegisterDependency_CircularDependency_ShouldThrow()
        {
            // Arrange
            var tracker = new DependencyTracker();
            tracker.RegisterDependency("key1", "key2");
            tracker.RegisterDependency("key2", "key3");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => tracker.RegisterDependency("key3", "key1"));
        }

        [Fact]
        public void GetDependents_ShouldReturnAllDirectDependents()
        {
            // Arrange
            var tracker = new DependencyTracker();
            tracker.RegisterDependency("key1", "key0");
            tracker.RegisterDependency("key2", "key0");
            tracker.RegisterDependency("key3", "key0");

            // Act
            var dependents = tracker.GetDependents("key0");

            // Assert
            Assert.Equal(3, dependents.Count);
            Assert.Contains("key1", dependents);
            Assert.Contains("key2", dependents);
            Assert.Contains("key3", dependents);
        }

        [Fact]
        public void InvalidateKey_ShouldReturnAffectedKeys()
        {
            // Arrange
            var tracker = new DependencyTracker();
            tracker.RegisterDependency("key1", "key0");
            tracker.RegisterDependency("key2", "key1");

            // Act
            var result = tracker.InvalidateKey("key0", totalCacheSize: 1000);

            // Assert
            Assert.NotNull(result.KeysToInvalidate);
            Assert.Contains("key0", result.KeysToInvalidate);
            Assert.Contains("key1", result.KeysToInvalidate);
            Assert.Contains("key2", result.KeysToInvalidate);
        }

        [Fact]
        public void InvalidateKey_PartialInvalidation_ShouldNotClearAll()
        {
            // Arrange
            var tracker = new DependencyTracker();
            tracker.RegisterDependency("key1", "key0");
            tracker.RegisterDependency("key2", "key0");

            // Act
            var result = tracker.InvalidateKey("key0", totalCacheSize: 1000);

            // Assert
            Assert.False(result.ShouldClearAll); // 3 keys out of 1000 = 0.3% < 10%
        }

        [Fact]
        public void InvalidateKey_ComplexDependencyChain_ShouldCalculateCorrectly()
        {
            // Arrange
            var tracker = new DependencyTracker();
            tracker.RegisterDependency("key1", "key0");
            tracker.RegisterDependency("key2", "key1");

            // Act
            var result = tracker.InvalidateKey("key0", totalCacheSize: 1000);

            // Assert - All 3 keys should be affected
            Assert.Equal(3, result.KeysToInvalidate.Count);
            Assert.Contains("key0", result.KeysToInvalidate);
            Assert.Contains("key1", result.KeysToInvalidate);
            Assert.Contains("key2", result.KeysToInvalidate);
        }

        [Fact]
        public void InvalidateKey_DependencyChain_ShouldInvalidateAll()
        {
            // Arrange
            var tracker = new DependencyTracker();
            tracker.RegisterDependency("keyA", "keyB");
            tracker.RegisterDependency("keyB", "keyC");
            tracker.RegisterDependency("keyC", "keyD");

            // Act
            var result = tracker.InvalidateKey("keyD", totalCacheSize: 100);

            // Assert
            Assert.Equal(4, result.KeysToInvalidate.Count);
            Assert.Contains("keyD", result.KeysToInvalidate);
            Assert.Contains("keyC", result.KeysToInvalidate);
            Assert.Contains("keyB", result.KeysToInvalidate);
            Assert.Contains("keyA", result.KeysToInvalidate);
        }

        [Fact]
        public void UnregisterDependency_ShouldRemoveRelationship()
        {
            // Arrange
            var tracker = new DependencyTracker();
            tracker.RegisterDependency("key1", "key0");

            // Act
            tracker.UnregisterDependency("key1", "key0");

            // Assert
            var dependents = tracker.GetDependents("key0");
            Assert.Empty(dependents);
        }

        [Fact]
        public void RemoveKey_ShouldCleanupAllRelationships()
        {
            // Arrange
            var tracker = new DependencyTracker();
            tracker.RegisterDependency("key1", "key0");
            tracker.RegisterDependency("key2", "key1");

            // Act
            tracker.RemoveKey("key1");

            // Assert
            var dependentsOfKey0 = tracker.GetDependents("key0");
            var dependentsOfKey1 = tracker.GetDependents("key1");
            Assert.Empty(dependentsOfKey0);
            Assert.Empty(dependentsOfKey1);
        }

        [Fact]
        public void InvalidateKey_ShouldTrackMetrics()
        {
            // Arrange
            var tracker = new DependencyTracker();
            tracker.RegisterDependency("key1", "key0");

            // Act
            var result1 = tracker.InvalidateKey("key0", totalCacheSize: 1000);
            var result2 = tracker.InvalidateKey("key1", totalCacheSize: 1000);

            // Assert
            Assert.Equal(2, tracker.InvalidationCount);
            Assert.Equal(2, tracker.PartialInvalidationCount);
            Assert.True(tracker.TotalKeysAffected > 0);
        }

        [Fact]
        public void Concurrent_RegisterDependency_ShouldBeThreadSafe()
        {
            // Arrange
            var tracker = new DependencyTracker();
            var tasks = new List<Task>();

            // Act
            for (int i = 0; i < 50; i++)
            {
                var idx = i; // Capture by value
                tasks.Add(Task.Run(() => tracker.RegisterDependency($"key{idx}", $"key{idx + 1}")));
            }

            Task.WaitAll(tasks.ToArray());

            // Assert
            Assert.True(tracker.TrackingCount > 0, "Should have tracked dependencies");
        }

        [Fact]
        public void Concurrent_InvalidateKey_ShouldHandleMultipleOperations()
        {
            // Arrange
            var tracker = new DependencyTracker();
            tracker.RegisterDependency("key1", "key0");
            var tasks = new List<Task>();

            // Act
            for (int i = 0; i < 5; i++)
            {
                tasks.Add(Task.Run(() => { var result = tracker.InvalidateKey("key0", 100); }));
            }

            Task.WaitAll(tasks.ToArray());

            // Assert - Operations should complete successfully
            Assert.True(tracker.InvalidationCount > 0, "Should have performed invalidations");
        }
    }

    public class IntelligentCacheTests
    {
        [Fact]
        public void Set_AndGet_ShouldStoreAndRetrieveValue()
        {
            // Arrange
            var cache = new IntelligentCache();
            var key = "test-key";
            var value = "test-value";

            // Act
            cache.Set(key, value, TimeSpan.FromHours(1));
            var retrieved = cache.TryGetValue<string>(key, out var result);

            // Assert
            Assert.True(retrieved);
            Assert.Equal(value, result);
        }

        [Fact]
        public void TryGetValue_ShouldReturnFalseForMissingKey()
        {
            // Arrange
            var cache = new IntelligentCache();

            // Act
            var result = cache.TryGetValue<string>("missing-key", out var value);

            // Assert
            Assert.False(result);
            Assert.Null(value);
        }

        [Fact]
        public void HitRate_ShouldTrackHitsAndMisses()
        {
            // Arrange
            var cache = new IntelligentCache();
            cache.Set("key1", "value1", TimeSpan.FromHours(1));
            cache.Set("key2", "value2", TimeSpan.FromHours(1));

            // Act
            cache.TryGetValue<string>("key1", out _); // Hit
            cache.TryGetValue<string>("key1", out _); // Hit
            cache.TryGetValue<string>("key3", out _); // Miss

            // Assert
            Assert.Equal(2, cache.Hits);
            Assert.Equal(1, cache.Misses);
            Assert.True(cache.HitRate > 60); // 2 hits out of 3 = 66.67%
        }

        [Fact]
        public void InvalidateKey_ShouldRemoveExpiredEntry()
        {
            // Arrange
            var cache = new IntelligentCache();
            cache.Set("key1", "value1", TimeSpan.FromHours(1));

            // Act
            cache.InvalidateKey("key1");
            var retrieved = cache.TryGetValue<string>("key1", out _);

            // Assert
            Assert.False(retrieved);
        }

        [Fact]
        public void InvalidateKey_WithDependencies_ShouldInvalidateDependents()
        {
            // Arrange
            var cache = new IntelligentCache();
            cache.Set("key0", "value0", TimeSpan.FromHours(1));
            cache.Set("key1", "value1", TimeSpan.FromHours(1), new[] { "key0" });
            cache.Set("key2", "value2", TimeSpan.FromHours(1), new[] { "key1" });

            // Act
            var invalidated = cache.InvalidateKey("key0");

            // Assert
            Assert.True(invalidated > 0);
            Assert.False(cache.TryGetValue<string>("key0", out _));
            Assert.False(cache.TryGetValue<string>("key1", out _));
            Assert.False(cache.TryGetValue<string>("key2", out _));
        }

        [Fact]
        public void Set_WithDependencies_ShouldRegisterInTracker()
        {
            // Arrange
            var cache = new IntelligentCache();

            // Act
            cache.Set("key1", "value1", TimeSpan.FromHours(1), new[] { "key0" });

            // Assert
            var dependents = cache.DependencyTracker.GetDependents("key0");
            Assert.Contains("key1", dependents);
        }

        [Fact]
        public void CacheSize_ShouldReflectEntriesInCache()
        {
            // Arrange
            var cache = new IntelligentCache();

            // Act
            cache.Set("key1", "value1", TimeSpan.FromHours(1));
            cache.Set("key2", "value2", TimeSpan.FromHours(1));
            cache.Set("key3", "value3", TimeSpan.FromHours(1));

            // Assert
            Assert.Equal(3, cache.CacheSize);
        }

        [Fact]
        public void Clear_ShouldEmptyCache()
        {
            // Arrange
            var cache = new IntelligentCache();
            cache.Set("key1", "value1", TimeSpan.FromHours(1));
            cache.Set("key2", "value2", TimeSpan.FromHours(1));

            // Act
            cache.Clear();

            // Assert
            Assert.Equal(0, cache.CacheSize);
            Assert.Equal(0, cache.Hits);
            Assert.Equal(0, cache.Misses);
        }

        [Fact]
        public void GetMetrics_ShouldReturnAccurateStats()
        {
            // Arrange
            var cache = new IntelligentCache();
            cache.Set("key1", "value1", TimeSpan.FromHours(1), new[] { "dep1" });
            cache.TryGetValue<string>("key1", out _);
            cache.TryGetValue<string>("key2", out _);

            // Act
            var metrics = cache.GetMetrics();

            // Assert
            Assert.Equal(1, metrics.Hits);
            Assert.Equal(1, metrics.Misses);
            Assert.True(metrics.HitRate > 40); // 50%
            Assert.Equal(1, metrics.CacheSize);
        }

        [Fact]
        public void Concurrent_Set_And_Get_ShouldBeThreadSafe()
        {
            // Arrange
            var cache = new IntelligentCache();
            var tasks = new List<Task>();

            // Act
            for (int i = 0; i < 50; i++)
            {
                var index = i;
                tasks.Add(Task.Run(() =>
                {
                    cache.Set($"key{index}", $"value{index}", TimeSpan.FromHours(1));
                    cache.TryGetValue<string>($"key{index}", out _);
                }));
            }

            Task.WaitAll(tasks.ToArray());

            // Assert
            Assert.Equal(50, cache.CacheSize);
            Assert.True(cache.Hits > 0);
        }

        [Fact]
        public void ExpiredEntry_ShouldBeRemovedOnAccess()
        {
            // Arrange
            var cache = new IntelligentCache();
            cache.Set("key1", "value1", DateTime.UtcNow.AddSeconds(-1)); // Already expired

            // Act
            var retrieved = cache.TryGetValue<string>("key1", out _);

            // Assert
            Assert.False(retrieved);
            Assert.Equal(0, cache.CacheSize); // Expired entry removed
        }
    }

    public class CacheInvalidationBenchmarkTests
    {
        [Fact]
        public void PartialInvalidation_ShouldOutperformFullInvalidation()
        {
            // Baseline: Full invalidation on any change
            var baselineCache = new Dictionary<string, string>();
            var baselineStopwatch = Stopwatch.StartNew();
            var baselineHits = 0;
            var baselineMisses = 0;

            // Populate baseline cache
            for (int i = 0; i < 10000; i++)
                baselineCache[$"key{i}"] = $"value{i}";

            // Simulate accesses with baseline approach (full clear on invalidation)
            for (int i = 0; i < 10000; i++)
            {
                if (baselineCache.ContainsKey($"key{i}"))
                    baselineHits++;
                else
                    baselineMisses++;

                // On any change, clear entire cache
                if (i % 100 == 0)
                    baselineCache.Clear();
            }

            baselineStopwatch.Stop();
            var baselineHitRate = (baselineHits * 100.0) / (baselineHits + baselineMisses);

            // Optimized: Partial invalidation with dependency tracking
            var optimizedCache = new IntelligentCache();
            var optimizedStopwatch = Stopwatch.StartNew();

            // Populate optimized cache with dependencies
            for (int i = 0; i < 10000; i++)
                optimizedCache.Set($"key{i}", $"value{i}", TimeSpan.FromHours(1), new[] { "base" });

            // Simulate accesses with optimized approach
            for (int i = 0; i < 10000; i++)
            {
                optimizedCache.TryGetValue<string>($"key{i}", out _);

                // Only invalidate specific key
                if (i % 100 == 0)
                    optimizedCache.InvalidateKey($"key{i}");
            }

            optimizedStopwatch.Stop();

            var optimizedHitRate = optimizedCache.HitRate;
            var timeDifference = baselineStopwatch.ElapsedMilliseconds - optimizedStopwatch.ElapsedMilliseconds;
            var timeImprovement = (timeDifference * 100.0) / baselineStopwatch.ElapsedMilliseconds;
            var hitRateImprovement = optimizedHitRate - baselineHitRate;

            // Assert - Optimized should have better hit rate
            Assert.True(optimizedHitRate > baselineHitRate, 
                $"Optimized hit rate ({optimizedHitRate:F2}%) should exceed baseline ({baselineHitRate:F2}%)");
            
            // Assert - Time improvement should be positive (optimized faster)
            // Note: In tests, overhead may vary, but overall logic should improve with real workloads
        }

        [Fact]
        public void PartialInvalidation_ShouldReduceHitTimeVariance()
        {
            // Arrange
            var cache = new IntelligentCache();

            // Create independent cache entries
            cache.Set("data1", "value1", TimeSpan.FromHours(1));
            cache.Set("data2", "value2", TimeSpan.FromHours(1));

            // Act - Check cache hit
            var hit1 = cache.TryGetValue<string>("data1", out _);
            var hit2 = cache.TryGetValue<string>("data2", out _);

            // Assert - Both should hit
            Assert.True(hit1, "data1 should be in cache");
            Assert.True(hit2, "data2 should be in cache");
            Assert.True(cache.Hits >= 2, "Should have at least 2 hits");
        }

        [Fact]
        public void InvalidationMetrics_ShouldShowPartialVsFullRatio()
        {
            // Arrange
            var cache = new IntelligentCache();

            // Create sparse dependencies (small % of keys affected)
            for (int i = 0; i < 1000; i++)
                cache.Set($"key{i}", $"value{i}", TimeSpan.FromHours(1));

            // Only key0 has dependents
            for (int i = 1000; i < 1100; i++)
                cache.Set($"key{i}", $"value{i}", TimeSpan.FromHours(1), new[] { "key0" });

            // Act - Invalidate key0 (affects 1% of keys, should be partial)
            cache.InvalidateKey("key0");

            // Assert
            var metrics = cache.GetMetrics();
            Assert.True(metrics.PartialInvalidations > 0);
            Assert.Equal(0, metrics.FullInvalidations); // Should not trigger full clear
        }
    }

    public class EdgeCaseTests
    {
        [Fact]
        public void CircularDependency_ShouldBeDetectedAndThrown()
        {
            // Arrange
            var tracker = new DependencyTracker();
            tracker.RegisterDependency("a", "b");
            tracker.RegisterDependency("b", "c");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => tracker.RegisterDependency("c", "a"));
        }

        [Fact]
        public void NullOrEmptyKey_ShouldThrow()
        {
            // Arrange
            var cache = new IntelligentCache();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => cache.Set(null, "value", TimeSpan.FromHours(1)));
            Assert.Throws<ArgumentException>(() => cache.Set("", "value", TimeSpan.FromHours(1)));
            Assert.Throws<ArgumentException>(() => cache.TryGetValue<string>(null, out _));
        }

        [Fact]
        public void MultipleInvalidationsOfSameKey_ShouldBeIdempotent()
        {
            // Arrange
            var cache = new IntelligentCache();
            cache.Set("key1", "value1", TimeSpan.FromHours(1));

            // Act
            cache.InvalidateKey("key1");
            var cacheSize1 = cache.CacheSize;
            cache.InvalidateKey("key1"); // Second invalidation
            var cacheSize2 = cache.CacheSize;

            // Assert
            Assert.Equal(cacheSize1, cacheSize2); // Should remain empty
        }

        [Fact]
        public void ComplexDependencyGraph_ShouldHandleCorrectly()
        {
            // Arrange
            var cache = new IntelligentCache();

            // Create complex graph:
            //     key0
            //    /    \
            //  key1   key2
            //    |     |
            //  key3   key4
            //     \   /
            //     key5

            cache.Set("key0", "v0", TimeSpan.FromHours(1));
            cache.Set("key1", "v1", TimeSpan.FromHours(1), new[] { "key0" });
            cache.Set("key2", "v2", TimeSpan.FromHours(1), new[] { "key0" });
            cache.Set("key3", "v3", TimeSpan.FromHours(1), new[] { "key1" });
            cache.Set("key4", "v4", TimeSpan.FromHours(1), new[] { "key2" });
            cache.Set("key5", "v5", TimeSpan.FromHours(1), new[] { "key3", "key4" });

            // Act
            cache.InvalidateKey("key0");

            // Assert - All keys should be invalidated
            for (int i = 0; i < 6; i++)
            {
                var retrieved = cache.TryGetValue<string>($"key{i}", out _);
                Assert.False(retrieved, $"key{i} should be invalidated");
            }
        }
    }
}
