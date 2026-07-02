namespace MonadoBlade.Core.Services;

using MonadoBlade.Core.Abstractions;
using MonadoBlade.Core.Caching;
using MonadoBlade.Core.Optimization;
using System.Collections.Concurrent;
using System.Diagnostics;

/// <summary>
/// High-performance cloud synchronization service with integrated optimizations.
/// Features: String interning, intelligent caching, lock-free collections.
/// </summary>
public class CloudSyncService : ServiceBase, ILifecycleService
{
    private readonly IntelligentCache _cache;
    private readonly ConcurrentDictionary<string, SyncStatus> _syncStatuses;
    private readonly ConcurrentDictionary<string, ConflictDetails> _conflicts;
    private bool _initialized;

    public bool IsInitialized => _initialized;

    public CloudSyncService(ILogger logger) : base(logger)
    {
        _cache = new IntelligentCache();
        _syncStatuses = new ConcurrentDictionary<string, SyncStatus>();
        _conflicts = new ConcurrentDictionary<string, ConflictDetails>();
    }

    /// <summary>
    /// Initializes the cloud sync service.
    /// </summary>
    public async Task InitializeAsync()
    {
        LogInfo("CloudSyncService initializing");
        _initialized = true;
        await Task.CompletedTask;
    }

    /// <summary>
    /// Initiates synchronization for the specified entity type.
    /// </summary>
    public async Task<SyncStatus> BeginSyncAsync(string entityType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(entityType))
            throw new ArgumentNullException(nameof(entityType));

        // Intern the entity type string for performance
        var internedType = StringInterningPool.Instance.Intern(entityType);

        var cacheKey = $"sync_status_{internedType}";
        
        // Check cache first
        if (_cache.TryGetValue<SyncStatus>(cacheKey, out var cached))
            return cached!;

        var status = new SyncStatus
        {
            EntityType = internedType,
            IsActive = true,
            ProgressPercentage = 0,
            SyncedItemCount = 0,
            ConflictCount = 0
        };

        _syncStatuses.AddOrUpdate(internedType, status, (_, _) => status);
        
        // Cache for 5 minutes
        _cache.Set(cacheKey, status, TimeSpan.FromMinutes(5));

        LogInfo("Sync started for entity type: {EntityType}", internedType);
        await Task.CompletedTask;
        return status;
    }

    /// <summary>
    /// Gets the current synchronization status.
    /// </summary>
    public async Task<SyncStatus> GetSyncStatusAsync(string entityType, CancellationToken cancellationToken = default)
    {
        var internedType = StringInterningPool.Instance.Intern(entityType);
        var cacheKey = $"sync_status_{internedType}";

        // Try cache first (optimized path)
        if (_cache.TryGetValue<SyncStatus>(cacheKey, out var cached))
            return cached!;

        // Fallback to dictionary
        if (_syncStatuses.TryGetValue(internedType, out var status))
        {
            _cache.Set(cacheKey, status, TimeSpan.FromMinutes(5));
            return status;
        }

        return new SyncStatus { EntityType = internedType, IsActive = false };
    }

    /// <summary>
    /// Completes synchronization for the specified entity type.
    /// </summary>
    public async Task<SyncStatus> EndSyncAsync(string entityType, CancellationToken cancellationToken = default)
    {
        var internedType = StringInterningPool.Instance.Intern(entityType);
        var cacheKey = $"sync_status_{internedType}";

        if (_syncStatuses.TryGetValue(internedType, out var status))
        {
            status.IsActive = false;
            status.LastSyncTime = DateTime.UtcNow;
            
            // Invalidate cache to force refresh
            InvalidateCache(cacheKey);
            LogInfo("Sync completed for entity type: {EntityType}", internedType);
        }

        return status ?? new SyncStatus { EntityType = internedType };
    }

    /// <summary>
    /// Reports a conflict during synchronization.
    /// </summary>
    public async Task ReportConflictAsync(ConflictDetails conflict, CancellationToken cancellationToken = default)
    {
        if (conflict == null)
            throw new ArgumentNullException(nameof(conflict));

        var key = $"{conflict.EntityType}_{conflict.EntityId}";
        _conflicts.AddOrUpdate(key, conflict, (_, _) => conflict);

        // Update sync status conflict count
        if (_syncStatuses.TryGetValue(conflict.EntityType, out var status))
        {
            status.ConflictCount++;
        }

        LogWarning("Conflict reported: {EntityType} {EntityId}", conflict.EntityType, conflict.EntityId);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Resolves a conflict with a specific resolution strategy.
    /// </summary>
    public async Task<bool> ResolveConflictAsync(string entityId, string entityType, object resolvedValue, CancellationToken cancellationToken = default)
    {
        var key = $"{entityType}_{entityId}";
        if (_conflicts.TryRemove(key, out _))
        {
            LogInfo("Conflict resolved: {EntityType} {EntityId}", entityType, entityId);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets all currently unresolved conflicts.
    /// </summary>
    public async Task<List<ConflictDetails>> GetUnresolvedConflictsAsync(CancellationToken cancellationToken = default)
    {
        return new List<ConflictDetails>(_conflicts.Values);
    }

    /// <summary>
    /// Disposes the service.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        _cache?.Dispose();
        _syncStatuses.Clear();
        _conflicts.Clear();
        await Task.CompletedTask;
    }
}
