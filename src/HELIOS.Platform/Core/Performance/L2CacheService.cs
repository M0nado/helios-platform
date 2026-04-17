using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Performance
{
    /// <summary>
    /// L2 Cache Service: Advanced caching with invalidation, TTL, and statistics
    /// Optimized for Phase 1-2 services to reduce repeated allocations and database queries
    /// </summary>
    public interface IL2CacheService
    {
        Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? ttl = null);
        Task<T> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? ttl = null);
        Task RemoveAsync(string key);
        Task InvalidatePatternAsync(string pattern);
        void Clear();
        CacheStatistics GetStatistics();
    }

    public class CacheStatistics
    {
        public long Hits { get; set; }
        public long Misses { get; set; }
        public long Evictions { get; set; }
        public long InvalidatedKeys { get; set; }
        public int CurrentItemCount { get; set; }
        public long MemoryUsageBytes { get; set; }
        public double HitRate => (Hits + Misses) > 0 ? (Hits * 100.0) / (Hits + Misses) : 0;
    }

    public class L2CacheService : IL2CacheService
    {
        private class CacheEntry<T>
        {
            public T Value { get; set; }
            public DateTime ExpirationTime { get; set; }
            public long Size { get; set; }
        }

        private readonly ConcurrentDictionary<string, object> _cache = new();
        private readonly ConcurrentDictionary<string, Timer> _timers = new();
        private readonly ReaderWriterLockSlim _lock = new();
        private readonly TimeSpan _defaultTtl;
        private readonly long _maxMemoryBytes;
        private long _currentMemoryBytes;
        private long _hits;
        private long _misses;
        private long _evictions;
        private long _invalidations;

        public L2CacheService(TimeSpan? defaultTtl = null, long maxMemoryMB = 500)
        {
            _defaultTtl = defaultTtl ?? TimeSpan.FromHours(1);
            _maxMemoryBytes = maxMemoryMB * 1024 * 1024;
        }

        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? ttl = null)
        {
            if (TryGetCached(key, out T cached))
            {
                Interlocked.Increment(ref _hits);
                return cached;
            }

            Interlocked.Increment(ref _misses);
            var value = await factory();
            await SetAsync(key, value, ttl);
            return value;
        }

        public Task<T> GetAsync<T>(string key)
        {
            if (TryGetCached(key, out T value))
            {
                Interlocked.Increment(ref _hits);
                return Task.FromResult(value);
            }

            Interlocked.Increment(ref _misses);
            return Task.FromResult<T>(default);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? ttl = null)
        {
            if (value == null)
                return Task.CompletedTask;

            _lock.EnterWriteLock();
            try
            {
                var effectiveTtl = ttl ?? _defaultTtl;
                var size = EstimateSize(value);

                // Evict if necessary
                while (_currentMemoryBytes + size > _maxMemoryBytes && _cache.Count > 0)
                {
                    EvictLru();
                }

                var entry = new CacheEntry<T>
                {
                    Value = value,
                    ExpirationTime = DateTime.UtcNow.Add(effectiveTtl),
                    Size = size
                };

                if (_cache.TryGetValue(key, out var existing) && existing is CacheEntry<T> existingEntry)
                {
                    _currentMemoryBytes -= existingEntry.Size;
                    _timers.TryRemove(key, out var oldTimer);
                    oldTimer?.Dispose();
                }

                _cache[key] = entry;
                _currentMemoryBytes += size;

                // Schedule expiration
                if (_timers.TryGetValue(key, out var existingTimer))
                    existingTimer.Dispose();

                var timer = new Timer(_ => RemoveAsync(key), null, effectiveTtl, Timeout.InfiniteTimeSpan);
                _timers[key] = timer;
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key)
        {
            _lock.EnterWriteLock();
            try
            {
                if (_cache.TryRemove(key, out var obj) && obj is not null)
                {
                    var type = obj.GetType();
                    var sizeProperty = type.GetProperty("Size");
                    if (sizeProperty?.GetValue(obj) is long size)
                    {
                        _currentMemoryBytes -= size;
                    }
                }

                if (_timers.TryRemove(key, out var timer))
                    timer?.Dispose();
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            return Task.CompletedTask;
        }

        public Task InvalidatePatternAsync(string pattern)
        {
            _lock.EnterWriteLock();
            try
            {
                var keysToRemove = new List<string>();
                var regexPattern = WildcardToRegex(pattern);
                var regex = new System.Text.RegularExpressions.Regex(regexPattern);

                foreach (var key in _cache.Keys)
                {
                    if (regex.IsMatch(key))
                        keysToRemove.Add(key);
                }

                foreach (var key in keysToRemove)
                {
                    _cache.TryRemove(key, out _);
                    if (_timers.TryRemove(key, out var timer))
                        timer?.Dispose();

                    Interlocked.Increment(ref _invalidations);
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            return Task.CompletedTask;
        }

        public void Clear()
        {
            _lock.EnterWriteLock();
            try
            {
                foreach (var timer in _timers.Values)
                    timer?.Dispose();

                _timers.Clear();
                _cache.Clear();
                _currentMemoryBytes = 0;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public CacheStatistics GetStatistics()
        {
            return new CacheStatistics
            {
                Hits = _hits,
                Misses = _misses,
                Evictions = _evictions,
                InvalidatedKeys = _invalidations,
                CurrentItemCount = _cache.Count,
                MemoryUsageBytes = _currentMemoryBytes
            };
        }

        private bool TryGetCached<T>(string key, out T value)
        {
            _lock.EnterReadLock();
            try
            {
                if (_cache.TryGetValue(key, out var obj))
                {
                    if (obj is CacheEntry<T> entry)
                    {
                        if (entry.ExpirationTime > DateTime.UtcNow)
                        {
                            value = entry.Value;
                            return true;
                        }
                    }
                }

                value = default;
                return false;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        private void EvictLru()
        {
            var oldestKey = string.Empty;
            DateTime oldestTime = DateTime.MaxValue;

            foreach (var kvp in _cache)
            {
                if (kvp.Value is not null)
                {
                    var type = kvp.Value.GetType();
                    var expProperty = type.GetProperty("ExpirationTime");
                    if (expProperty?.GetValue(kvp.Value) is DateTime expTime)
                    {
                        if (expTime < oldestTime)
                        {
                            oldestTime = expTime;
                            oldestKey = kvp.Key;
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(oldestKey))
            {
                _cache.TryRemove(oldestKey, out var removed);
                if (_timers.TryRemove(oldestKey, out var timer))
                    timer?.Dispose();

                Interlocked.Increment(ref _evictions);
            }
        }

        private long EstimateSize(object obj)
        {
            if (obj == null) return 0;
            return 100 + System.Runtime.InteropServices.Marshal.SizeOf(obj);
        }

        private string WildcardToRegex(string pattern)
        {
            var regex = System.Text.RegularExpressions.Regex.Escape(pattern);
            regex = regex.Replace("\\*", ".*").Replace("\\?", ".");
            return $"^{regex}$";
        }
    }
}
