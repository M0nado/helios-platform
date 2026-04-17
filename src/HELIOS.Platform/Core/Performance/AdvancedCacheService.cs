using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using HELIOS.Platform.Core.Logging;

namespace HELIOS.Platform.Core.Performance
{
    /// <summary>
    /// Advanced two-tier caching strategy (L1 in-memory + L2 distributed)
    /// </summary>
    public interface IAdvancedCacheService
    {
        T GetCached<T>(string key, Func<T> factory, CachePolicy policy);
        Task<T> GetCachedAsync<T>(string key, Func<Task<T>> factory, CachePolicy policy);
        void InvalidateCache(string keyPattern);
        CacheMetrics GetMetrics();
    }

    /// <summary>
    /// Cache policy configuration
    /// </summary>
    public class CachePolicy
    {
        /// <summary>L1 (in-memory) cache duration</summary>
        public TimeSpan L1Duration { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>L2 (distributed) cache duration</summary>
        public TimeSpan L2Duration { get; set; } = TimeSpan.FromHours(1);

        /// <summary>Cache key namespace for isolation</summary>
        public string Namespace { get; set; } = "default";

        /// <summary>Enable L2 distributed caching</summary>
        public bool UseL2Cache { get; set; } = false;

        /// <summary>Invalidation pattern (e.g., "user:*")</summary>
        public string InvalidationPattern { get; set; }
    }

    /// <summary>
    /// Cache performance metrics
    /// </summary>
    public class CacheMetrics
    {
        public long L1Hits { get; set; }
        public long L1Misses { get; set; }
        public long L2Hits { get; set; }
        public long L2Misses { get; set; }
        public long Invalidations { get; set; }
        public double L1HitRate => (L1Hits + L1Misses) > 0 ? (double)L1Hits / (L1Hits + L1Misses) : 0;
        public double L2HitRate => (L2Hits + L2Misses) > 0 ? (double)L2Hits / (L2Hits + L2Misses) : 0;
        public double CombinedHitRate => ((L1Hits + L2Hits) + (L1Misses + L2Misses)) > 0 
            ? (double)(L1Hits + L2Hits) / (L1Hits + L1Misses + L2Hits + L2Misses) 
            : 0;
    }

    /// <summary>
    /// Advanced two-tier caching implementation
    /// </summary>
    public class AdvancedCacheService : IAdvancedCacheService
    {
        private readonly IL1CacheService _l1Cache;
        private readonly IDistributedCacheL2 _l2Cache;
        private readonly Logging.ILogger _logger;
        private readonly object _metricsLock = new();
        private long _l1Hits = 0;
        private long _l1Misses = 0;
        private long _l2Hits = 0;
        private long _l2Misses = 0;
        private long _invalidations = 0;

        public AdvancedCacheService(
            IL1CacheService l1Cache,
            IDistributedCacheL2 l2Cache,
            Logging.ILogger logger)
        {
            _l1Cache = l1Cache ?? throw new ArgumentNullException(nameof(l1Cache));
            _l2Cache = l2Cache ?? throw new ArgumentNullException(nameof(l2Cache));
            _logger = logger;
        }

        public T GetCached<T>(string key, Func<T> factory, CachePolicy policy)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            var namespacedKey = $"{policy?.Namespace ?? "default"}:{key}";

            // Try L1 first
            if (_l1Cache.TryGet(namespacedKey, out T l1Value))
            {
                Interlocked.Increment(ref _l1Hits);
                return l1Value;
            }

            Interlocked.Increment(ref _l1Misses);

            // Try L2 if enabled
            if (policy?.UseL2Cache == true && _l2Cache.TryGet(namespacedKey, out T l2Value))
            {
                Interlocked.Increment(ref _l2Hits);
                // Populate L1 from L2
                _l1Cache.Set(namespacedKey, l2Value, policy.L1Duration);
                return l2Value;
            }

            Interlocked.Increment(ref _l2Misses);

            // Cache miss - call factory
            var value = factory();

            // Store in both caches
            _l1Cache.Set(namespacedKey, value, policy.L1Duration);
            if (policy?.UseL2Cache == true)
                _l2Cache.Set(namespacedKey, value, policy.L2Duration);

            return value;
        }

        public async Task<T> GetCachedAsync<T>(string key, Func<Task<T>> factory, CachePolicy policy)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            var namespacedKey = $"{policy?.Namespace ?? "default"}:{key}";

            // Try L1 first
            if (_l1Cache.TryGet(namespacedKey, out T l1Value))
            {
                Interlocked.Increment(ref _l1Hits);
                return l1Value;
            }

            Interlocked.Increment(ref _l1Misses);

            // Try L2 if enabled
            if (policy?.UseL2Cache == true && _l2Cache.TryGet(namespacedKey, out T l2Value))
            {
                Interlocked.Increment(ref _l2Hits);
                _l1Cache.Set(namespacedKey, l2Value, policy.L1Duration);
                return l2Value;
            }

            Interlocked.Increment(ref _l2Misses);

            // Cache miss - call async factory
            var value = await factory();

            // Store in both caches
            _l1Cache.Set(namespacedKey, value, policy.L1Duration);
            if (policy?.UseL2Cache == true)
                _l2Cache.Set(namespacedKey, value, policy.L2Duration);

            return value;
        }

        public void InvalidateCache(string keyPattern)
        {
            _l1Cache.Clear();
            if (_l2Cache != null)
                _l2Cache.InvalidatePattern(keyPattern);

            Interlocked.Increment(ref _invalidations);
            _logger?.Info($"Cache invalidation: {keyPattern}");
        }

        public CacheMetrics GetMetrics()
        {
            lock (_metricsLock)
            {
                return new CacheMetrics
                {
                    L1Hits = _l1Hits,
                    L1Misses = _l1Misses,
                    L2Hits = _l2Hits,
                    L2Misses = _l2Misses,
                    Invalidations = _invalidations
                };
            }
        }
    }

    /// <summary>
    /// L2 (Distributed) cache abstraction for Redis/Memcached
    /// </summary>
    public interface IDistributedCacheL2
    {
        bool TryGet<T>(string key, out T value);
        void Set<T>(string key, T value, TimeSpan ttl);
        void Remove(string key);
        void InvalidatePattern(string pattern);
    }

    /// <summary>
    /// In-memory L2 cache implementation (for development/testing)
    /// </summary>
    public class InMemoryL2Cache : IDistributedCacheL2
    {
        private readonly ConcurrentDictionary<string, (object value, DateTime expireAt)> _cache = new();

        public bool TryGet<T>(string key, out T value)
        {
            value = default;
            if (_cache.TryGetValue(key, out var entry))
            {
                if (DateTime.UtcNow < entry.expireAt)
                {
                    value = (T)entry.value;
                    return true;
                }
                else
                {
                    _cache.TryRemove(key, out _);
                }
            }
            return false;
        }

        public void Set<T>(string key, T value, TimeSpan ttl)
        {
            _cache[key] = (value, DateTime.UtcNow.Add(ttl));
        }

        public void Remove(string key)
        {
            _cache.TryRemove(key, out _);
        }

        public void InvalidatePattern(string pattern)
        {
            var keysToRemove = _cache.Keys.Where(k => k.Contains(pattern)).ToList();
            foreach (var key in keysToRemove)
                _cache.TryRemove(key, out _);
        }
    }

    /// <summary>
    /// Cache-aside pattern implementation
    /// </summary>
    public interface ICacheAsidePattern
    {
        Task<T> GetAsync<T>(string key, Func<Task<T>> getCoreData, TimeSpan cacheDuration);
        Task SetAsync<T>(string key, T value);
        Task InvalidateAsync(string key);
    }

    /// <summary>
    /// Cache-aside implementation
    /// </summary>
    public class CacheAsidePattern : ICacheAsidePattern
    {
        private readonly IAdvancedCacheService _cache;

        public CacheAsidePattern(IAdvancedCacheService cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public async Task<T> GetAsync<T>(string key, Func<Task<T>> getCoreData, TimeSpan cacheDuration)
        {
            var policy = new CachePolicy { L1Duration = cacheDuration };
            return await _cache.GetCachedAsync(key, getCoreData, policy);
        }

        public async Task SetAsync<T>(string key, T value)
        {
            await Task.CompletedTask;
            // Data is stored in cache when accessed via GetAsync
        }

        public async Task InvalidateAsync(string key)
        {
            await Task.CompletedTask;
            _cache.InvalidateCache(key);
        }
    }
}
