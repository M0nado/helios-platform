using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Server
{
    /// <summary>
    /// Distributed cache layer implementation with in-memory cache, TTL support, and LRU eviction policy.
    /// </summary>
    public class DistributedCacheLayer : IDistributedCacheLayer
    {
        private readonly Dictionary<string, CacheEntry> _cache = new();
        private readonly object _lock = new();
        private readonly int _maxCapacity;
        private long _hits;
        private long _misses;
        private long _evictions;

        private class CacheEntry
        {
            public string Value { get; set; }
            public DateTime? ExpiresAt { get; set; }
            public DateTime LastAccessedAt { get; set; }
            public int AccessCount { get; set; }

            public CacheEntry(string value, DateTime? expiresAt)
            {
                Value = value;
                ExpiresAt = expiresAt;
                LastAccessedAt = DateTime.UtcNow;
                AccessCount = 0;
            }

            public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow >= ExpiresAt.Value;
        }

        public int MaxCapacity => _maxCapacity;

        public DistributedCacheLayer(int maxCapacity = 10000)
        {
            if (maxCapacity <= 0)
                throw new ArgumentException("Max capacity must be greater than 0", nameof(maxCapacity));

            _maxCapacity = maxCapacity;
        }

        public Task<bool> SetAsync(string key, string value, int? ttlSeconds = null)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be empty", nameof(key));

            lock (_lock)
            {
                // If cache is full, evict LRU item
                if (_cache.Count >= _maxCapacity && !_cache.ContainsKey(key))
                {
                    EvictLruEntry();
                }

                var expiresAt = ttlSeconds.HasValue ? (DateTime?)DateTime.UtcNow.AddSeconds(ttlSeconds.Value) : null;
                _cache[key] = new CacheEntry(value, expiresAt);
                return Task.FromResult(true);
            }
        }

        public Task<string?> GetAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be empty", nameof(key));

            lock (_lock)
            {
                if (_cache.TryGetValue(key, out var entry))
                {
                    // Check if expired
                    if (entry.IsExpired)
                    {
                        _cache.Remove(key);
                        _misses++;
                        return Task.FromResult<string?>(null);
                    }

                    entry.LastAccessedAt = DateTime.UtcNow;
                    entry.AccessCount++;
                    _hits++;
                    return Task.FromResult<string?>(entry.Value);
                }

                _misses++;
                return Task.FromResult<string?>(null);
            }
        }

        public Task<Dictionary<string, string?>> MGetAsync(params string[] keys)
        {
            if (keys == null)
                throw new ArgumentNullException(nameof(keys));

            var result = new Dictionary<string, string?>();
            lock (_lock)
            {
                foreach (var key in keys)
                {
                    if (string.IsNullOrWhiteSpace(key))
                        continue;

                    if (_cache.TryGetValue(key, out var entry))
                    {
                        if (entry.IsExpired)
                        {
                            _cache.Remove(key);
                            result[key] = null;
                            _misses++;
                        }
                        else
                        {
                            entry.LastAccessedAt = DateTime.UtcNow;
                            entry.AccessCount++;
                            result[key] = entry.Value;
                            _hits++;
                        }
                    }
                    else
                    {
                        result[key] = null;
                        _misses++;
                    }
                }
            }

            return Task.FromResult(result);
        }

        public Task<bool> DeleteAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be empty", nameof(key));

            lock (_lock)
            {
                return Task.FromResult(_cache.Remove(key));
            }
        }

        public Task<int> DeleteAsync(params string[] keys)
        {
            if (keys == null)
                throw new ArgumentNullException(nameof(keys));

            int deleted = 0;
            lock (_lock)
            {
                foreach (var key in keys)
                {
                    if (string.IsNullOrWhiteSpace(key))
                        continue;

                    if (_cache.Remove(key))
                        deleted++;
                }
            }

            return Task.FromResult(deleted);
        }

        public Task<bool> ExistsAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be empty", nameof(key));

            lock (_lock)
            {
                if (_cache.TryGetValue(key, out var entry))
                {
                    if (entry.IsExpired)
                    {
                        _cache.Remove(key);
                        return Task.FromResult(false);
                    }
                    return Task.FromResult(true);
                }
                return Task.FromResult(false);
            }
        }

        public Task<long> GetTtlAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be empty", nameof(key));

            lock (_lock)
            {
                if (!_cache.TryGetValue(key, out var entry))
                    return Task.FromResult(-2L); // Key doesn't exist

                if (entry.IsExpired)
                {
                    _cache.Remove(key);
                    return Task.FromResult(-2L);
                }

                if (!entry.ExpiresAt.HasValue)
                    return Task.FromResult(-1L); // No expiration

                var ttl = (long)(entry.ExpiresAt.Value - DateTime.UtcNow).TotalSeconds;
                return Task.FromResult(Math.Max(0, ttl));
            }
        }

        public Task<bool> ExpireAsync(string key, int ttlSeconds)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be empty", nameof(key));
            if (ttlSeconds < 0)
                throw new ArgumentException("TTL must be non-negative", nameof(ttlSeconds));

            lock (_lock)
            {
                if (_cache.TryGetValue(key, out var entry))
                {
                    entry.ExpiresAt = DateTime.UtcNow.AddSeconds(ttlSeconds);
                    return Task.FromResult(true);
                }
                return Task.FromResult(false);
            }
        }

        public Task<long> IncrementAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be empty", nameof(key));

            lock (_lock)
            {
                if (_cache.TryGetValue(key, out var entry))
                {
                    if (!long.TryParse(entry.Value, out var current))
                        throw new InvalidOperationException($"Value at key '{key}' is not a valid integer");

                    var newValue = current + 1;
                    entry.Value = newValue.ToString();
                    entry.LastAccessedAt = DateTime.UtcNow;
                    return Task.FromResult(newValue);
                }

                // Ensure cache is not full
                if (_cache.Count >= _maxCapacity)
                    EvictLruEntry();

                _cache[key] = new CacheEntry("1", null);
                return Task.FromResult(1L);
            }
        }

        public Task<long> DecrementAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be empty", nameof(key));

            lock (_lock)
            {
                if (_cache.TryGetValue(key, out var entry))
                {
                    if (!long.TryParse(entry.Value, out var current))
                        throw new InvalidOperationException($"Value at key '{key}' is not a valid integer");

                    var newValue = current - 1;
                    entry.Value = newValue.ToString();
                    entry.LastAccessedAt = DateTime.UtcNow;
                    return Task.FromResult(newValue);
                }

                // Ensure cache is not full
                if (_cache.Count >= _maxCapacity)
                    EvictLruEntry();

                _cache[key] = new CacheEntry("-1", null);
                return Task.FromResult(-1L);
            }
        }

        public Task FlushAllAsync()
        {
            lock (_lock)
            {
                _cache.Clear();
                _hits = 0;
                _misses = 0;
                _evictions = 0;
            }
            return Task.CompletedTask;
        }

        public Task<CacheStatistics> GetStatisticsAsync()
        {
            lock (_lock)
            {
                return Task.FromResult(new CacheStatistics
                {
                    Hits = _hits,
                    Misses = _misses,
                    Evictions = _evictions,
                    CurrentSize = _cache.Count,
                    MaxCapacity = _maxCapacity
                });
            }
        }

        public Task<int> GetSizeAsync()
        {
            lock (_lock)
            {
                return Task.FromResult(_cache.Count);
            }
        }

        private void EvictLruEntry()
        {
            if (_cache.Count == 0)
                return;

            // Find the least recently used entry
            var lruEntry = _cache
                .OrderBy(kvp => kvp.Value.LastAccessedAt)
                .ThenBy(kvp => kvp.Value.AccessCount)
                .First();

            _cache.Remove(lruEntry.Key);
            _evictions++;
        }
    }
}
