using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using HELIOS.Platform.Core.Logging;

namespace HELIOS.Platform.Core.Performance
{
    /// <summary>
    /// L1 In-Memory Cache with TTL support for high-frequency data
    /// </summary>
    public interface IL1CacheService
    {
        T Get<T>(string key, Func<T> factory, TimeSpan ttl);
        Task<T> GetAsync<T>(string key, Func<Task<T>> asyncFactory, TimeSpan ttl);
        void Set<T>(string key, T value, TimeSpan ttl);
        bool TryGet<T>(string key, out T value);
        void Remove(string key);
        void Clear();
        CacheStats GetStats();
    }

    /// <summary>
    /// Cache statistics for monitoring
    /// </summary>
    public class CacheStats
    {
        public long HitCount { get; set; }
        public long MissCount { get; set; }
        public long EvictionCount { get; set; }
        public double HitRate => (HitCount + MissCount) > 0 ? (double)HitCount / (HitCount + MissCount) : 0;
        public int EntryCount { get; set; }
    }

    /// <summary>
    /// High-performance L1 cache implementation
    /// </summary>
    public class L1CacheService : IL1CacheService
    {
        private class CacheEntry<T>
        {
            public T Value { get; set; }
            public DateTime ExpiresAt { get; set; }
            public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        }

        private readonly ConcurrentDictionary<string, object> _cache = new();
        private readonly Logging.ILogger _logger;
        private readonly object _statsLock = new object();
        private long _hits = 0;
        private long _misses = 0;
        private long _evictions = 0;

        public L1CacheService(Logging.ILogger logger) => _logger = logger;

        public T Get<T>(string key, Func<T> factory, TimeSpan ttl)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            if (_cache.TryGetValue(key, out var cached))
            {
                var entry = cached as CacheEntry<T>;
                if (entry != null && !entry.IsExpired)
                {
                    Interlocked.Increment(ref _hits);
                    return entry.Value;
                }
                else if (entry != null && entry.IsExpired)
                {
                    _cache.TryRemove(key, out _);
                    Interlocked.Increment(ref _evictions);
                }
            }

            Interlocked.Increment(ref _misses);
            var value = factory();
            var newEntry = new CacheEntry<T> { Value = value, ExpiresAt = DateTime.UtcNow.Add(ttl) };
            _cache[key] = newEntry;
            return value;
        }

        public async Task<T> GetAsync<T>(string key, Func<Task<T>> asyncFactory, TimeSpan ttl)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            if (_cache.TryGetValue(key, out var cached))
            {
                var entry = cached as CacheEntry<T>;
                if (entry != null && !entry.IsExpired)
                {
                    Interlocked.Increment(ref _hits);
                    return entry.Value;
                }
                else if (entry != null && entry.IsExpired)
                {
                    _cache.TryRemove(key, out _);
                    Interlocked.Increment(ref _evictions);
                }
            }

            Interlocked.Increment(ref _misses);
            var value = await asyncFactory();
            var newEntry = new CacheEntry<T> { Value = value, ExpiresAt = DateTime.UtcNow.Add(ttl) };
            _cache[key] = newEntry;
            return value;
        }

        public void Set<T>(string key, T value, TimeSpan ttl)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            var entry = new CacheEntry<T> { Value = value, ExpiresAt = DateTime.UtcNow.Add(ttl) };
            _cache[key] = entry;
        }

        public bool TryGet<T>(string key, out T value)
        {
            value = default;
            if (!_cache.TryGetValue(key, out var cached))
                return false;

            var entry = cached as CacheEntry<T>;
            if (entry == null || entry.IsExpired)
                return false;

            value = entry.Value;
            return true;
        }

        public void Remove(string key) => _cache.TryRemove(key, out _);

        public void Clear()
        {
            _cache.Clear();
            lock (_statsLock)
            {
                _hits = 0;
                _misses = 0;
                _evictions = 0;
            }
        }

        public CacheStats GetStats()
        {
            lock (_statsLock)
            {
                return new CacheStats
                {
                    HitCount = _hits,
                    MissCount = _misses,
                    EvictionCount = _evictions,
                    EntryCount = _cache.Count
                };
            }
        }
    }
}
