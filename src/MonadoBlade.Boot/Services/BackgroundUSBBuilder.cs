namespace MonadoBlade.Boot.Services;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MonadoBlade.Core.Abstractions;

/// <summary>
/// Async background service for building USB images with intelligent caching.
/// Handles priority queueing and non-blocking notifications.
/// </summary>
public class BackgroundUSBBuilder : ILifecycleService
{
    private readonly ILogger<BackgroundUSBBuilder> _logger;
    private readonly IUSBImageCache _cache;
    private readonly PriorityQueue<(USBCreationRequest Request, long QueuedTicks), int> _priorityQueue;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _backgroundTask;
    private bool _isInitialized;

    private static readonly Dictionary<string, int> ProfilePriorities = new()
    {
        { "Secure", 0 },
        { "Enterprise", 1 },
        { "Gamer", 2 },
        { "Developer", 3 },
        { "AI Research", 4 }
    };

    private long _cacheHits;
    private long _cacheMisses;
    private long _totalBuildTime;

    /// <summary>
    /// Initializes a new instance of the BackgroundUSBBuilder class.
    /// </summary>
    public BackgroundUSBBuilder(ILogger<BackgroundUSBBuilder> logger, IUSBImageCache cache)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _priorityQueue = new PriorityQueue<(USBCreationRequest, long), int>();
        _cacheHits = 0;
        _cacheMisses = 0;
        _totalBuildTime = 0;
    }

    /// <summary>
    /// Gets whether the service is initialized.
    /// </summary>
    public bool IsInitialized => _isInitialized;

    /// <summary>
    /// Initializes the background builder service.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        _logger.LogInformation("Initializing BackgroundUSBBuilder");

        _cancellationTokenSource = new CancellationTokenSource();
        _backgroundTask = ProcessQueueAsync(_cancellationTokenSource.Token);

        _isInitialized = true;
        await Task.CompletedTask;
    }

    /// <summary>
    /// Gets current performance metrics.
    /// </summary>
    public BackgroundBuilderMetrics GetMetrics()
    {
        var totalRequests = _cacheHits + _cacheMisses;
        var hitRate = totalRequests > 0 ? (double)_cacheHits / totalRequests * 100 : 0;

        return new BackgroundBuilderMetrics
        {
            CacheHits = _cacheHits,
            CacheMisses = _cacheMisses,
            CacheHitRate = hitRate,
            AverageBuildTimeMs = totalRequests > 0 ? _totalBuildTime / totalRequests : 0,
            QueuedItems = _priorityQueue.Count
        };
    }

    /// <summary>
    /// Queues a USB creation request with priority handling.
    /// </summary>
    public async Task QueueUSBBuildAsync(USBCreationRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var priority = GetPriority(request.Profile);
        _priorityQueue.Enqueue((request, Stopwatch.GetTimestamp()), priority);

        _logger.LogInformation(
            "USB build queued with priority {Priority} for profile '{Profile}'. Queue size: {QueueSize}",
            priority, request.Profile, _priorityQueue.Count);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously disposes resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        _logger.LogInformation("Disposing BackgroundUSBBuilder");

        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();

            if (_backgroundTask != null)
            {
                try
                {
                    await _backgroundTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancelling
                }
            }

            _cancellationTokenSource.Dispose();
        }

        _logger.LogInformation(
            "BackgroundUSBBuilder disposed. Cache hits: {Hits}, misses: {Misses}, average build: {AvgTime}ms",
            _cacheHits, _cacheMisses,
            (_cacheHits + _cacheMisses) > 0 ? _totalBuildTime / (_cacheHits + _cacheMisses) : 0);
    }

    /// <summary>
    /// Processes the priority queue in the background.
    /// </summary>
    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (_priorityQueue.Count > 0)
                {
                    var (request, queuedTicks) = _priorityQueue.Dequeue();
                    await ProcessUSBBuildAsync(request, queuedTicks, cancellationToken);
                }
                else
                {
                    await Task.Delay(100, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing USB build queue");
            }
        }
    }

    /// <summary>
    /// Processes a single USB build with caching support.
    /// </summary>
    private async Task ProcessUSBBuildAsync(USBCreationRequest request, long queuedTicks, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var cacheKey = GenerateCacheKey(request);

        // Check cache
        if (_cache.TryGetImage(cacheKey, out var cachedImage))
        {
            _logger.LogInformation("Cache hit for profile '{Profile}' (key: {CacheKey})",
                request.Profile, cacheKey);

            Interlocked.Increment(ref _cacheHits);

            // Use cached image
            await WriteCachedImageAsync(request, cachedImage, cancellationToken);
        }
        else
        {
            _logger.LogInformation("Cache miss for profile '{Profile}' (key: {CacheKey})",
                request.Profile, cacheKey);

            Interlocked.Increment(ref _cacheMisses);

            // Build new image
            var image = await BuildUSBImageAsync(request, cancellationToken);

            // Cache the image
            _cache.StoreImage(cacheKey, image);
            _logger.LogInformation("Cached new USB image (key: {CacheKey})", cacheKey);
        }

        sw.Stop();
        Interlocked.Add(ref _totalBuildTime, sw.ElapsedMilliseconds);

        var queueWaitTime = new TimeSpan((long)(Stopwatch.GetTimestamp() - queuedTicks) * 10000000 / Stopwatch.Frequency);
        _logger.LogInformation(
            "USB build completed for '{DeviceName}' in {BuildTime}ms (queue wait: {QueueWait}ms)",
            request.DeviceName, sw.ElapsedMilliseconds, queueWaitTime.TotalMilliseconds);

        // Send non-blocking notification
        SendBackgroundNotification(request, sw.Elapsed);
    }

    /// <summary>
    /// Builds a new USB image.
    /// </summary>
    private async Task<byte[]> BuildUSBImageAsync(USBCreationRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Building USB image for profile '{Profile}' on '{TargetDisk}'",
            request.Profile, request.TargetDisk);

        // Simulate USB build
        await Task.Delay(GetBuildDuration(request.Profile), cancellationToken);

        // Return simulated image
        var imageSize = GetImageSize(request.Profile);
        return new byte[imageSize];
    }

    /// <summary>
    /// Writes a cached image to the target disk.
    /// </summary>
    private async Task WriteCachedImageAsync(USBCreationRequest request, byte[] image, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Writing cached USB image to '{TargetDisk}' ({ImageSize}MB)",
            request.TargetDisk, image.Length / 1024 / 1024);

        // Simulate write (faster than building)
        await Task.Delay(100, cancellationToken);

        _logger.LogInformation("Cached image written successfully to '{TargetDisk}'", request.TargetDisk);
    }

    /// <summary>
    /// Generates a cache key from request.
    /// </summary>
    private string GenerateCacheKey(USBCreationRequest request)
    {
        return $"{request.Profile}_{request.AdvancedModeEnabled}_{string.Join("_", request.AdvancedOptions.OrderBy(x => x.Key))}";
    }

    /// <summary>
    /// Gets priority for a profile.
    /// </summary>
    private int GetPriority(string profile)
    {
        return ProfilePriorities.TryGetValue(profile, out var priority) ? priority : 99;
    }

    /// <summary>
    /// Gets build duration for a profile in milliseconds.
    /// </summary>
    private int GetBuildDuration(string profile)
    {
        return profile switch
        {
            "Secure" => 3000,
            "Enterprise" => 2500,
            "Gamer" => 2000,
            "Developer" => 1500,
            "AI Research" => 4000,
            _ => 2000
        };
    }

    /// <summary>
    /// Gets simulated image size for a profile in bytes.
    /// </summary>
    private int GetImageSize(string profile)
    {
        return profile switch
        {
            "Gamer" => 16 * 1024 * 1024,
            "Developer" => 8 * 1024 * 1024,
            "AI Research" => 32 * 1024 * 1024,
            "Secure" => 4 * 1024 * 1024,
            "Enterprise" => 8 * 1024 * 1024,
            _ => 8 * 1024 * 1024
        };
    }

    /// <summary>
    /// Sends a non-blocking background notification.
    /// </summary>
    private void SendBackgroundNotification(USBCreationRequest request, TimeSpan buildTime)
    {
        _logger.LogInformation(
            "🔔 USB '{DeviceName}' completed in background ({BuildTime}s)",
            request.DeviceName, buildTime.TotalSeconds);
    }
}

/// <summary>
/// Interface for USB image caching.
/// </summary>
public interface IUSBImageCache : IService
{
    /// <summary>
    /// Tries to get a cached USB image.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="image">The cached image data.</param>
    /// <returns>True if found, false otherwise.</returns>
    bool TryGetImage(string key, out byte[] image);

    /// <summary>
    /// Stores a USB image in the cache.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="image">The image data to cache.</param>
    void StoreImage(string key, byte[] image);

    /// <summary>
    /// Gets cache statistics.
    /// </summary>
    /// <returns>Cache statistics.</returns>
    CacheStatistics GetStatistics();

    /// <summary>
    /// Clears the cache.
    /// </summary>
    void Clear();
}

/// <summary>
/// Metrics for the background USB builder.
/// </summary>
public class BackgroundBuilderMetrics
{
    /// <summary>
    /// Gets or sets the number of cache hits.
    /// </summary>
    public long CacheHits { get; set; }

    /// <summary>
    /// Gets or sets the number of cache misses.
    /// </summary>
    public long CacheMisses { get; set; }

    /// <summary>
    /// Gets or sets the cache hit rate as a percentage.
    /// </summary>
    public double CacheHitRate { get; set; }

    /// <summary>
    /// Gets or sets the average build time in milliseconds.
    /// </summary>
    public long AverageBuildTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the number of queued items.
    /// </summary>
    public int QueuedItems { get; set; }
}

/// <summary>
/// Statistics for the USB image cache.
/// </summary>
public class CacheStatistics
{
    /// <summary>
    /// Gets or sets the number of items in the cache.
    /// </summary>
    public int ItemCount { get; set; }

    /// <summary>
    /// Gets or sets the total size of cached items in bytes.
    /// </summary>
    public long TotalSizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the maximum cache size in bytes.
    /// </summary>
    public long MaxSizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the cache utilization as a percentage.
    /// </summary>
    public double UtilizationPercent { get; set; }
}
