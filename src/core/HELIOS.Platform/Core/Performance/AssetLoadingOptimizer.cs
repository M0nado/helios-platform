using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Performance
{
    /// <summary>
    /// Asset Loading Optimization Service for Phase 8 Stream 8
    /// Implements intelligent asset streaming and preloading through:
    /// - Asynchronous streaming for large assets
    /// - Intelligent prefetching based on usage patterns
    /// - Asset state caching and tracking
    /// - Streaming texture and audio loading
    /// - Predictive preloading from AI/Hub (Stream 6)
    /// </summary>
    public interface IAssetLoadingOptimizer
    {
        AssetLoadingMetrics GetMetrics();
        Task<T> LoadAssetAsync<T>(string assetPath, AssetPriority priority = AssetPriority.Normal) where T : class;
        void PrefetchAsset(string assetPath, AssetPriority priority = AssetPriority.Low);
        void ClearAssetCache();
        bool IsAssetLoaded(string assetPath);
        void OptimizeForTheme(string themeName);
    }

    public enum AssetPriority
    {
        Critical = 0,   // Load immediately
        High = 1,       // Load soon
        Normal = 2,     // Load when convenient
        Low = 3,        // Background load
        Deferred = 4    // Load on-demand only
    }

    public class AssetLoadingMetrics
    {
        public int LoadedAssetCount { get; set; }
        public long CacheSizeMB { get; set; }
        public double AverageLoadTimeMS { get; set; }
        public double MaxLoadTimeMS { get; set; }
        public int CacheHits { get; set; }
        public int CacheMisses { get; set; }
        public double CacheHitRate => (CacheHits + CacheMisses) > 0 
            ? (CacheHits * 100.0) / (CacheHits + CacheMisses) 
            : 0;
        public int PrefetchedAssets { get; set; }
        public double TotalLoadTimeSeconds { get; set; }
        public bool IsOptimized => AverageLoadTimeMS < 100 && CacheHitRate > 80;
    }

    public class AssetLoadingOptimizer : IAssetLoadingOptimizer
    {
        private readonly ConcurrentDictionary<string, CachedAsset> _assetCache = new();
        private readonly ConcurrentDictionary<string, Task> _pendingLoads = new();
        private long _cacheHits;
        private long _cacheMisses;
        private readonly Stopwatch _loadTimer = Stopwatch.StartNew();
        private List<double> _loadTimes = new();
        private const int MaxCacheSizeMB = 200; // Phase 8 target
        private long _currentCacheSizeMB;

        private class CachedAsset
        {
            public string Path { get; set; }
            public object Asset { get; set; }
            public DateTime LoadedAt { get; set; }
            public long SizeBytes { get; set; }
            public int AccessCount { get; set; }
            public AssetPriority Priority { get; set; }
            public bool IsStreaming { get; set; }
        }

        /// <summary>
        /// Loads an asset asynchronously with intelligent caching
        /// Target: <200ms total load time for entire startup sequence
        /// </summary>
        public async Task<T> LoadAssetAsync<T>(string assetPath, AssetPriority priority = AssetPriority.Normal) where T : class
        {
            // Check cache first
            if (_assetCache.TryGetValue(assetPath, out var cached))
            {
                Interlocked.Increment(ref _cacheHits);
                cached.AccessCount++;
                return cached.Asset as T;
            }

            // Check if already loading
            if (_pendingLoads.TryGetValue(assetPath, out var pendingTask))
            {
                await pendingTask.ConfigureAwait(false);
                if (_assetCache.TryGetValue(assetPath, out cached))
                {
                    return cached.Asset as T;
                }
            }

            Interlocked.Increment(ref _cacheMisses);

            // Start async load
            var loadTask = LoadAssetInternalAsync<T>(assetPath, priority);
            _pendingLoads.TryAdd(assetPath, loadTask);

            try
            {
                var result = await loadTask.ConfigureAwait(false);
                return result;
            }
            finally
            {
                _pendingLoads.TryRemove(assetPath, out _);
            }
        }

        /// <summary>
        /// Internal asset loading logic with streaming support
        /// </summary>
        private async Task<T> LoadAssetInternalAsync<T>(string assetPath, AssetPriority priority) where T : class
        {
            var startTime = Stopwatch.StartNew();

            try
            {
                // Simulate async loading (in real implementation, would load from disk/network)
                await Task.Delay(10).ConfigureAwait(false); // Async I/O simulation

                var asset = CreateMockAsset<T>(assetPath);
                var size = EstimateAssetSize(asset);

                // Check cache size and evict if necessary
                while (_currentCacheSizeMB + (size / (1024 * 1024)) > MaxCacheSizeMB && _assetCache.Count > 0)
                {
                    EvictLRUAsset();
                }

                var cached = new CachedAsset
                {
                    Path = assetPath,
                    Asset = asset,
                    LoadedAt = DateTime.UtcNow,
                    SizeBytes = size,
                    AccessCount = 1,
                    Priority = priority,
                    IsStreaming = IsLargeAsset(assetPath)
                };

                _assetCache.TryAdd(assetPath, cached);
                _currentCacheSizeMB += size / (1024 * 1024);

                startTime.Stop();
                _loadTimes.Add(startTime.Elapsed.TotalMilliseconds);
                if (_loadTimes.Count > 100)
                    _loadTimes.RemoveAt(0);

                return asset;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load asset: {assetPath}", ex);
            }
        }

        /// <summary>
        /// Prefetches an asset in the background
        /// Doesn't block - starts async load
        /// </summary>
        public void PrefetchAsset(string assetPath, AssetPriority priority = AssetPriority.Low)
        {
            if (_assetCache.ContainsKey(assetPath))
                return;

            // Queue for background loading
            _ = Task.Run(async () =>
            {
                try
                {
                    await LoadAssetAsync<object>(assetPath, priority).ConfigureAwait(false);
                }
                catch
                {
                    // Prefetch failure is not critical
                }
            });
        }

        /// <summary>
        /// Optimizes asset loading for a specific theme
        /// Prefetches theme-specific assets based on predicted access patterns
        /// </summary>
        public void OptimizeForTheme(string themeName)
        {
            // Predict theme assets from AI/Hub (Stream 6)
            var predictedAssets = PredictThemeAssets(themeName);

            // Prefetch with appropriate priorities
            foreach (var asset in predictedAssets)
            {
                PrefetchAsset(asset.Path, asset.Priority);
            }
        }

        /// <summary>
        /// Predicts which assets will be needed for a theme
        /// This would use AI predictions from Stream 6
        /// </summary>
        private List<AssetInfo> PredictThemeAssets(string themeName)
        {
            // In real implementation: would use Stream 6 AI predictions
            return new List<AssetInfo>
            {
                new AssetInfo { Path = $"themes/{themeName}/colors.json", Priority = AssetPriority.Critical },
                new AssetInfo { Path = $"themes/{themeName}/fonts.ttf", Priority = AssetPriority.High },
                new AssetInfo { Path = $"themes/{themeName}/icons.atlas", Priority = AssetPriority.High },
                new AssetInfo { Path = $"themes/{themeName}/animations.json", Priority = AssetPriority.Normal },
            };
        }

        private class AssetInfo
        {
            public string Path { get; set; }
            public AssetPriority Priority { get; set; }
        }

        /// <summary>
        /// Creates a mock asset for demonstration
        /// In real implementation: would load actual data
        /// </summary>
        private T CreateMockAsset<T>(string path) where T : class
        {
            // Return type-appropriate mock object
            if (typeof(T) == typeof(string))
                return (T)(object)$"MockAsset:{path}";
            
            return Activator.CreateInstance<T>();
        }

        /// <summary>
        /// Estimates asset size in bytes
        /// </summary>
        private long EstimateAssetSize(object asset)
        {
            if (asset == null)
                return 0;

            // Rough estimation based on type
            return asset.GetType().Name switch
            {
                "Byte[]" => ((byte[])asset).Length,
                "String" => ((string)asset).Length * 2,
                _ => 1024 // Default 1KB estimate
            };
        }

        /// <summary>
        /// Checks if asset is large and should be streamed
        /// </summary>
        private bool IsLargeAsset(string path)
        {
            // Large audio/video files should be streamed
            return path.EndsWith(".mp3") || path.EndsWith(".wav") || 
                   path.EndsWith(".mp4") || path.EndsWith(".webm");
        }

        /// <summary>
        /// Evicts least recently used asset to make room
        /// </summary>
        private void EvictLRUAsset()
        {
            var lruAsset = _assetCache.Values
                .OrderBy(a => a.AccessCount)
                .ThenBy(a => a.LoadedAt)
                .FirstOrDefault();

            if (lruAsset != null)
            {
                _assetCache.TryRemove(lruAsset.Path, out _);
                _currentCacheSizeMB -= lruAsset.SizeBytes / (1024 * 1024);
            }
        }

        /// <summary>
        /// Checks if asset is currently loaded
        /// </summary>
        public bool IsAssetLoaded(string assetPath)
        {
            return _assetCache.ContainsKey(assetPath);
        }

        /// <summary>
        /// Clears entire asset cache
        /// </summary>
        public void ClearAssetCache()
        {
            _assetCache.Clear();
            _currentCacheSizeMB = 0;
            _cacheHits = 0;
            _cacheMisses = 0;
            _loadTimes.Clear();
        }

        /// <summary>
        /// Returns comprehensive asset loading metrics
        /// </summary>
        public AssetLoadingMetrics GetMetrics()
        {
            var avgLoadTime = _loadTimes.Count > 0 ? _loadTimes.Average() : 0.0;
            var maxLoadTime = _loadTimes.Count > 0 ? _loadTimes.Max() : 0.0;

            return new AssetLoadingMetrics
            {
                LoadedAssetCount = _assetCache.Count,
                CacheSizeMB = _currentCacheSizeMB,
                AverageLoadTimeMS = avgLoadTime,
                MaxLoadTimeMS = maxLoadTime,
                CacheHits = (int)(_cacheHits % int.MaxValue),
                CacheMisses = (int)(_cacheMisses % int.MaxValue),
                PrefetchedAssets = _pendingLoads.Count,
                TotalLoadTimeSeconds = _loadTimer.Elapsed.TotalSeconds
            };
        }
    }
}
