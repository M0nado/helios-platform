namespace MonadoBlade.Core.Services;

/// <summary>
/// Represents the status of a synchronization operation.
/// </summary>
public class SyncStatus
{
    /// <summary>Gets or sets the entity type being synced.</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Gets or sets whether sync is currently active.</summary>
    public bool IsActive { get; set; }

    /// <summary>Gets or sets the number of items synced.</summary>
    public int SyncedItemCount { get; set; }

    /// <summary>Gets or sets the number of conflicts detected.</summary>
    public int ConflictCount { get; set; }

    /// <summary>Gets or sets the last successful sync time.</summary>
    public DateTime? LastSyncTime { get; set; }

    /// <summary>Gets or sets the sync error message if any.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Gets or sets the synchronization progress (0-100).</summary>
    public int ProgressPercentage { get; set; }
}

/// <summary>
/// Represents conflicting data that needs resolution.
/// </summary>
public class ConflictDetails
{
    /// <summary>Gets or sets the entity identifier.</summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>Gets or sets the entity type.</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Gets or sets the local version of the entity.</summary>
    public object? LocalVersion { get; set; }

    /// <summary>Gets or sets the remote version of the entity.</summary>
    public object? RemoteVersion { get; set; }

    /// <summary>Gets or sets the base/ancestor version (for 3-way merge).</summary>
    public object? BaseVersion { get; set; }

    /// <summary>Gets or sets the timestamp of local modification.</summary>
    public DateTime LocalModifiedAt { get; set; }

    /// <summary>Gets or sets the timestamp of remote modification.</summary>
    public DateTime RemoteModifiedAt { get; set; }

    /// <summary>Gets or sets the conflict type.</summary>
    public ConflictType Type { get; set; }
}

/// <summary>
/// Defines types of synchronization conflicts.
/// </summary>
public enum ConflictType
{
    /// <summary>Both local and remote versions were updated.</summary>
    UpdateUpdate,

    /// <summary>Local was updated, remote was deleted.</summary>
    UpdateDelete,

    /// <summary>Local was deleted, remote was updated.</summary>
    DeleteUpdate,

    /// <summary>Both local and remote were deleted.</summary>
    DeleteDelete,

    /// <summary>Version incompatibility detected.</summary>
    VersionIncompatibility
}

/// <summary>
/// Represents the resolution strategy for a conflict.
/// </summary>
public class ConflictResolution
{
    /// <summary>Gets or sets the resolution strategy to apply.</summary>
    public ResolutionStrategy Strategy { get; set; }

    /// <summary>Gets or sets the resolved entity (for custom resolution).</summary>
    public object? ResolvedEntity { get; set; }

    /// <summary>Gets or sets additional notes about the resolution.</summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Defines conflict resolution strategies.
/// </summary>
public enum ResolutionStrategy
{
    /// <summary>Keep the local version.</summary>
    PreferLocal,

    /// <summary>Accept the remote version.</summary>
    PreferRemote,

    /// <summary>Use the version with the latest timestamp.</summary>
    LastWriteWins,

    /// <summary>Manually merge (user-provided resolution).</summary>
    Manual,

    /// <summary>Keep both versions (create separate records).</summary>
    KeepBoth,

    /// <summary>Delete both versions.</summary>
    DeleteBoth
}

/// <summary>
/// Cloud synchronization service managing bidirectional data sync and conflict resolution.
/// Responsible for keeping local and remote data consistent across devices/services.
/// </summary>
public interface ICloudSyncService : IService
{
    /// <summary>
    /// Initiates synchronization for a specific entity type.
    /// </summary>
    /// <param name="entityType">The type of entities to sync.</param>
    /// <returns>A task representing the sync operation.</returns>
    /// <exception cref="ServiceUnavailableException">Thrown when sync service is unavailable.</exception>
    /// <exception cref="OperationFailedException">Thrown when sync fails.</exception>
    Task SyncAsync(string entityType);

    /// <summary>
    /// Retrieves the current synchronization status for an entity type.
    /// </summary>
    /// <param name="entityType">The type of entities to check.</param>
    /// <returns>The current sync status.</returns>
    Task<SyncStatus> GetStatusAsync(string entityType);

    /// <summary>
    /// Resolves a detected conflict using the specified strategy.
    /// </summary>
    /// <param name="conflict">The conflict details.</param>
    /// <param name="resolution">The resolution strategy to apply.</param>
    /// <returns>A task representing the resolution operation.</returns>
    /// <exception cref="OperationFailedException">Thrown when resolution fails.</exception>
    Task ResolveConflictAsync(ConflictDetails conflict, ConflictResolution resolution);

    /// <summary>
    /// Gets all active conflicts.
    /// </summary>
    /// <returns>A collection of unresolved conflicts.</returns>
    Task<ICollection<ConflictDetails>> GetConflictsAsync();

    /// <summary>
    /// Subscribes to sync events.
    /// </summary>
    /// <param name="handler">Handler to invoke when sync completes.</param>
    IDisposable SubscribeToSyncCompleted(Func<string, Task> handler);

    /// <summary>
    /// Sets the automatic sync interval.
    /// </summary>
    /// <param name="interval">The interval between automatic syncs.</param>
    Task SetAutoSyncIntervalAsync(TimeSpan interval);

    /// <summary>
    /// Pauses automatic synchronization.
    /// </summary>
    Task PauseAutoSyncAsync();

    /// <summary>
    /// Resumes automatic synchronization.
    /// </summary>
    Task ResumeAutoSyncAsync();
}
