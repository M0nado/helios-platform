using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HELIOS.Platform.Caching
{
    /// <summary>
    /// Implements ML-based intelligent caching with adaptive TTL and priority scoring.
    /// </summary>
    public class IntelligentCache
    {
        private readonly Dictionary<string, CacheEntry> _cache = new();
        private readonly ReaderWriterLockSlim _cacheLock = new();
        private readonly Dictionary<string, AccessMetrics> _metrics = new();
        private long _maxSize = 512 * 1024 * 1024; // 512 MB
        private long _currentSize;
        private DateTime _lastTuneTime = DateTime.UtcNow;

        public class CacheEntry
        {
            public string Key { get; set; } = string.Empty;
            public object? Value { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime LastAccessedAt { get; set; }
            public long SizeInBytes { get; set; }
            public int AccessCount { get; set; }
            public double PriorityScore { get; set; }
            public TimeSpan Ttl { get; set; }
            public bool IsExpired => DateTime.UtcNow - CreatedAt > Ttl;
        }

        public class AccessMetrics
        {
            public int Accesses { get; set; }
            public int Hits { get; set; }
            public int Misses { get; set; }
            public double HitRate { get; set; }
            public TimeSpan AverageAccessTime { get; set; }
            public DateTime LastChecked { get; set; }
        }

        public void Set(string key, object value, TimeSpan? customTtl = null)
        {
            var ttl = customTtl ?? PredictOptimalTtl(key);
            var size = EstimateSize(value);

            _cacheLock.EnterWriteLock();
            try
            {
                if (_cache.TryGetValue(key, out var existing))
                {
                    _currentSize -= existing.SizeInBytes;
                    _cache.Remove(key);
                }

                _currentSize += size;

                if (_currentSize > _maxSize)
                    EvictLRU();

                var entry = new CacheEntry
                {
                    Key = key,
                    Value = value,
                    CreatedAt = DateTime.UtcNow,
                    LastAccessedAt = DateTime.UtcNow,
                    SizeInBytes = size,
                    AccessCount = 0,
                    Ttl = ttl,
                    PriorityScore = CalculateInitialPriority(key)
                };

                _cache[key] = entry;
                UpdateMetrics(key, true);
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }

        public bool TryGet<T>(string key, out T? value)
        {
            value = default;

            _cacheLock.EnterWriteLock();
            try
            {
                if (!_cache.TryGetValue(key, out var entry))
                {
                    UpdateMetrics(key, false);
                    return false;
                }

                if (entry.IsExpired)
                {
                    _currentSize -= entry.SizeInBytes;
                    _cache.Remove(key);
                    UpdateMetrics(key, false);
                    return false;
                }

                entry.AccessCount++;
                entry.LastAccessedAt = DateTime.UtcNow;
                entry.PriorityScore = UpdatePriorityScore(entry);

                value = (T?)entry.Value;
                UpdateMetrics(key, true);
                return true;
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }

        public void Remove(string key)
        {
            _cacheLock.EnterWriteLock();
            try
            {
                if (_cache.TryGetValue(key, out var entry))
                {
                    _currentSize -= entry.SizeInBytes;
                    _cache.Remove(key);
                }
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }

        public void Clear()
        {
            _cacheLock.EnterWriteLock();
            try
            {
                _cache.Clear();
                _currentSize = 0;
                _metrics.Clear();
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }

        private TimeSpan PredictOptimalTtl(string key)
        {
            if (_metrics.TryGetValue(key, out var metric))
            {
                var baseExpiry = TimeSpan.FromMinutes(5);
                var hitScore = metric.HitRate;

                if (hitScore > 0.8)
                    return TimeSpan.FromMinutes(30);
                else if (hitScore > 0.5)
                    return TimeSpan.FromMinutes(15);
                else if (hitScore < 0.2)
                    return TimeSpan.FromMinutes(1);

                return baseExpiry;
            }

            return TimeSpan.FromMinutes(5);
        }

        private double CalculateInitialPriority(string key)
        {
            if (_metrics.TryGetValue(key, out var metric))
            {
                return Math.Min(1.0, (metric.HitRate * 0.7) + (metric.Accesses / 1000.0 * 0.3));
            }

            return 0.5;
        }

        private double UpdatePriorityScore(CacheEntry entry)
        {
            var age = (DateTime.UtcNow - entry.CreatedAt).TotalHours;
            var accessFrequency = entry.AccessCount / Math.Max(1, age);
            var recency = 1.0 - Math.Min(1.0, (DateTime.UtcNow - entry.LastAccessedAt).TotalHours / 24);

            return (accessFrequency * 0.5) + (recency * 0.5);
        }

        private void EvictLRU()
        {
            var candidates = _cache.Values
                .Where(e => !e.IsExpired)
                .OrderBy(e => e.PriorityScore)
                .ThenBy(e => e.LastAccessedAt)
                .ToList();

            var toRemove = (int)(_cache.Count * 0.2);

            foreach (var entry in candidates.Take(toRemove))
            {
                _currentSize -= entry.SizeInBytes;
                _cache.Remove(entry.Key);
            }
        }

        private void UpdateMetrics(string key, bool hit)
        {
            if (!_metrics.TryGetValue(key, out var metric))
            {
                metric = new AccessMetrics { LastChecked = DateTime.UtcNow };
                _metrics[key] = metric;
            }

            metric.Accesses++;
            if (hit)
                metric.Hits++;
            else
                metric.Misses++;

            metric.HitRate = metric.Hits / (double)metric.Accesses;
        }

        private long EstimateSize(object value)
        {
            if (value == null) return 0;

            return value switch
            {
                string s => s.Length * 2,
                byte[] b => b.Length,
                _ => 100
            };
        }

        public async Task AutoTune()
        {
            if ((DateTime.UtcNow - _lastTuneTime).TotalMinutes < 5)
                return;

            await Task.Run(() =>
            {
                _cacheLock.EnterUpgradeableReadLock();
                try
                {
                    var expired = _cache.Values.Where(e => e.IsExpired).ToList();

                    if (expired.Count > 0)
                    {
                        _cacheLock.EnterWriteLock();
                        try
                        {
                            foreach (var entry in expired)
                            {
                                _currentSize -= entry.SizeInBytes;
                                _cache.Remove(entry.Key);
                            }
                        }
                        finally
                        {
                            _cacheLock.ExitWriteLock();
                        }
                    }

                    _lastTuneTime = DateTime.UtcNow;
                }
                finally
                {
                    _cacheLock.ExitUpgradeableReadLock();
                }
            });
        }

        public CacheStatistics GetStatistics()
        {
            _cacheLock.EnterReadLock();
            try
            {
                var totalHits = _metrics.Values.Sum(m => m.Hits);
                var totalAccesses = _metrics.Values.Sum(m => m.Accesses);
                var hitRate = totalAccesses > 0 ? totalHits / (double)totalAccesses : 0.0;

                return new CacheStatistics
                {
                    EntryCount = _cache.Count,
                    TotalSizeBytes = _currentSize,
                    MaxSizeBytes = _maxSize,
                    HitRate = hitRate,
                    AveragePriority = _cache.Values.Count > 0 ? _cache.Values.Average(e => e.PriorityScore) : 0.0,
                    UtilizationPercent = (_currentSize / (double)_maxSize) * 100
                };
            }
            finally
            {
                _cacheLock.ExitReadLock();
            }
        }

        public void SetMaxSize(long sizeInBytes) => _maxSize = Math.Max(10 * 1024 * 1024, sizeInBytes);

        public class CacheStatistics
        {
            public int EntryCount { get; set; }
            public long TotalSizeBytes { get; set; }
            public long MaxSizeBytes { get; set; }
            public double HitRate { get; set; }
            public double AveragePriority { get; set; }
            public double UtilizationPercent { get; set; }
        }
    }
}
