namespace MonadoBlade.Core.Services;

/// <summary>
/// Manage service providing administrative operations and configuration management.
/// Responsible for system settings, permissions, auditing, and administrative tasks.
/// </summary>
/// <remarks>
/// Segregation Pattern: Administrative Operations
/// - System configuration and settings
/// - Permission and role management
/// - Audit logging
/// - Administrative operations
/// </remarks>
public interface IManageService : IService
{
    /// <summary>
    /// Gets a system configuration value.
    /// </summary>
    /// <typeparam name="T">The configuration value type.</typeparam>
    /// <param name="configKey">The configuration key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The configuration value or default.</returns>
    Task<T?> GetConfigurationAsync<T>(string configKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a system configuration value.
    /// </summary>
    /// <typeparam name="T">The configuration value type.</typeparam>
    /// <param name="configKey">The configuration key.</param>
    /// <param name="value">The configuration value.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task SetConfigurationAsync<T>(string configKey, T value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all system configuration values.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary of all configuration values.</returns>
    Task<Dictionary<string, object>> GetAllConfigurationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Grants a permission to a user or role.
    /// </summary>
    /// <param name="principalId">The user or role identifier.</param>
    /// <param name="permission">The permission to grant.</param>
    /// <param name="resourceId">Optional resource identifier for resource-level permissions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task GrantPermissionAsync(string principalId, string permission, string? resourceId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a permission from a user or role.
    /// </summary>
    /// <param name="principalId">The user or role identifier.</param>
    /// <param name="permission">The permission to revoke.</param>
    /// <param name="resourceId">Optional resource identifier for resource-level permissions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task RevokePermissionAsync(string principalId, string permission, string? resourceId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a principal has a specific permission.
    /// </summary>
    /// <param name="principalId">The user or role identifier.</param>
    /// <param name="permission">The permission to check.</param>
    /// <param name="resourceId">Optional resource identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if principal has permission; otherwise false.</returns>
    Task<bool> HasPermissionAsync(string principalId, string permission, string? resourceId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all permissions for a principal.
    /// </summary>
    /// <param name="principalId">The user or role identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of granted permissions.</returns>
    Task<List<Permission>> GetPermissionsAsync(string principalId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs an audit event.
    /// </summary>
    /// <param name="auditLog">The audit log entry.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the logging operation.</returns>
    Task LogAuditAsync(AuditLog auditLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs with filtering and pagination.
    /// </summary>
    /// <param name="filter">The audit log filter criteria.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated audit logs matching the filter.</returns>
    Task<PagedResult<AuditLog>> GetAuditLogsAsync(AuditLogFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Purges old audit logs.
    /// </summary>
    /// <param name="olderThan">Delete logs older than this date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of audit logs deleted.</returns>
    Task<int> PurgeAuditLogsAsync(DateTime olderThan, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets system health status and metrics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary of system health metrics.</returns>
    Task<Dictionary<string, object>> GetSystemHealthAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiates a system backup operation.
    /// </summary>
    /// <param name="options">Backup options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The backup operation identifier.</returns>
    Task<string> StartBackupAsync(BackupOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status of a backup operation.
    /// </summary>
    /// <param name="backupId">The backup operation identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The backup status.</returns>
    Task<BackupStatus> GetBackupStatusAsync(string backupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiates a system restore operation.
    /// </summary>
    /// <param name="backupId">The backup identifier to restore from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The restore operation identifier.</returns>
    Task<string> StartRestoreAsync(string backupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears system caches.
    /// </summary>
    /// <param name="cacheType">Optional specific cache type to clear.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task ClearCachesAsync(string? cacheType = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rebuilds indexes for better query performance.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task RebuildIndexesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a permission in the system.
/// </summary>
public class Permission
{
    /// <summary>Gets or sets the permission name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the resource identifier if permission is resource-scoped.</summary>
    public string? ResourceId { get; set; }

    /// <summary>Gets or sets when the permission was granted.</summary>
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets when the permission expires (optional).</summary>
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// Represents an audit log entry.
/// </summary>
public class AuditLog
{
    /// <summary>Gets or sets the audit log ID.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Gets or sets the action performed.</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Gets or sets the entity type affected.</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Gets or sets the entity identifier affected.</summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>Gets or sets the principal (user) who performed the action.</summary>
    public string Principal { get; set; } = string.Empty;

    /// <summary>Gets or sets the timestamp of the action.</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets additional context data.</summary>
    public Dictionary<string, object> Context { get; set; } = new();

    /// <summary>Gets or sets the status (success, failure, etc).</summary>
    public string Status { get; set; } = "success";

    /// <summary>Gets or sets error message if action failed.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Gets or sets the IP address of the request origin.</summary>
    public string? IpAddress { get; set; }
}

/// <summary>
/// Filter criteria for audit log queries.
/// </summary>
public class AuditLogFilter
{
    /// <summary>Gets or sets the start date.</summary>
    public DateTime? StartDate { get; set; }

    /// <summary>Gets or sets the end date.</summary>
    public DateTime? EndDate { get; set; }

    /// <summary>Gets or sets the action filter.</summary>
    public string? Action { get; set; }

    /// <summary>Gets or sets the entity type filter.</summary>
    public string? EntityType { get; set; }

    /// <summary>Gets or sets the principal filter.</summary>
    public string? Principal { get; set; }

    /// <summary>Gets or sets the page number.</summary>
    public int Page { get; set; } = 1;

    /// <summary>Gets or sets the page size.</summary>
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// Options for backup operations.
/// </summary>
public class BackupOptions
{
    /// <summary>Gets or sets whether to backup database.</summary>
    public bool IncludeDatabase { get; set; } = true;

    /// <summary>Gets or sets whether to backup file storage.</summary>
    public bool IncludeFileStorage { get; set; } = true;

    /// <summary>Gets or sets whether to backup configuration.</summary>
    public bool IncludeConfiguration { get; set; } = true;

    /// <summary>Gets or sets the backup description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the backup destination.</summary>
    public string? Destination { get; set; }
}

/// <summary>
/// Status information for a backup operation.
/// </summary>
public class BackupStatus
{
    /// <summary>Gets or sets the backup identifier.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the operation state.</summary>
    public BackupState State { get; set; }

    /// <summary>Gets or sets the progress percentage (0-100).</summary>
    public int ProgressPercentage { get; set; }

    /// <summary>Gets or sets when the backup started.</summary>
    public DateTime StartedAt { get; set; }

    /// <summary>Gets or sets when the backup completed.</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>Gets or sets the backup size in bytes.</summary>
    public long SizeBytes { get; set; }

    /// <summary>Gets or sets any error message if operation failed.</summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Defines states for backup operations.
/// </summary>
public enum BackupState
{
    /// <summary>Backup is queued and waiting to start.</summary>
    Queued,

    /// <summary>Backup is in progress.</summary>
    InProgress,

    /// <summary>Backup completed successfully.</summary>
    Completed,

    /// <summary>Backup failed with error.</summary>
    Failed,

    /// <summary>Backup was cancelled.</summary>
    Cancelled
}
