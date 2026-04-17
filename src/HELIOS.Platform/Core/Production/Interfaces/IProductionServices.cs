using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Production.Interfaces;

/// <summary>
/// Distributed cache layer interface for Redis-compatible caching operations.
/// Provides high-performance caching with TTL support and pattern matching.
/// Performance Target: Less than 2ms per operation
/// Thread-Safe: Yes - all operations use synchronization
/// Async: Yes - all operations are async/await
/// </summary>
public interface IDistributedCacheLayer
{
    /// <summary>Sets a cache value with optional expiration.</summary>
    /// <param name="key">The cache key (non-null).</param>
    /// <param name="value">The value to cache (non-null).</param>
    /// <param name="expiration">Optional time-to-live duration.</param>
    /// <returns>True if set successfully, false otherwise.</returns>
    Task<bool> SetAsync(string key, string value, TimeSpan? expiration = null);

    /// <summary>Retrieves a value from cache.</summary>
    /// <param name="key">The cache key (non-null).</param>
    /// <returns>The cached value, or null if not found or expired.</returns>
    Task<string?> GetAsync(string key);

    /// <summary>Deletes a cache entry.</summary>
    /// <param name="key">The cache key to delete (non-null).</param>
    /// <returns>True if deleted, false if key didn't exist.</returns>
    Task<bool> DeleteAsync(string key);

    /// <summary>Checks if a key exists in cache and hasn't expired.</summary>
    /// <param name="key">The cache key (non-null).</param>
    /// <returns>True if key exists and is valid, false otherwise.</returns>
    Task<bool> ExistsAsync(string key);

    /// <summary>Retrieves all keys matching a regex pattern.</summary>
    /// <param name="pattern">Regex pattern for key matching (non-null).</param>
    /// <returns>List of matching keys.</returns>
    Task<List<string>> GetKeysAsync(string pattern);

    /// <summary>Increments a numeric cache value.</summary>
    /// <param name="key">The cache key (non-null).</param>
    /// <returns>The new incremented value, or -1 on error.</returns>
    Task<int> IncrementAsync(string key);
}

/// <summary>
/// Query execution plan details for analysis and optimization.
/// </summary>
public class QueryExecutionPlan
{
    /// <summary>The SQL query being analyzed.</summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>Estimated execution cost (arbitrary units).</summary>
    public int EstimatedCost { get; set; }
}

/// <summary>
/// Query plan analyzer interface for SQL query optimization.
/// Provides query analysis, optimization recommendations, and indexing support.
/// Performance Target: Less than 30ms per operation
/// Thread-Safe: Yes
/// Async: Yes
/// </summary>
public interface IQueryPlanAnalyzer
{
    /// <summary>Optimizes a SQL query.</summary>
    /// <param name="query">The SQL query to optimize (non-null).</param>
    /// <returns>The optimized query.</returns>
    Task<string> OptimizeQueryAsync(string query);

    /// <summary>Analyzes a SQL query and returns execution plan.</summary>
    /// <param name="query">The SQL query to analyze (non-null).</param>
    /// <returns>QueryExecutionPlan with analysis details.</returns>
    Task<QueryExecutionPlan> AnalyzeAsync(string query);

    /// <summary>Creates a database index on specified columns.</summary>
    /// <param name="tableName">The table name (non-null).</param>
    /// <param name="columns">List of column names to index (non-empty).</param>
    /// <returns>True if index creation succeeded, false otherwise.</returns>
    Task<bool> CreateIndexAsync(string tableName, List<string> columns);
}

/// <summary>
/// Server health information for load balancing.
/// </summary>
public class ServerHealth
{
    /// <summary>The server identifier.</summary>
    public string ServerId { get; set; } = string.Empty;

    /// <summary>Whether the server is currently healthy and operational.</summary>
    public bool IsHealthy { get; set; }

    /// <summary>Server load percentage (0-100).</summary>
    public double Load { get; set; }
}

/// <summary>
/// Production load balancer interface for distributed traffic management.
/// Implements round-robin load distribution with health monitoring.
/// Performance Target: Less than 10ms per operation
/// Thread-Safe: Yes
/// Async: Yes
/// </summary>
public interface IProductionLoadBalancer
{
    /// <summary>Registers a server for load balancing.</summary>
    /// <param name="serverId">Unique server identifier (non-null).</param>
    /// <param name="endpoint">Server endpoint URL (non-null).</param>
    /// <returns>True if registration succeeded, false otherwise.</returns>
    Task<bool> RegisterServerAsync(string serverId, string endpoint);

    /// <summary>Gets the next server using round-robin distribution.</summary>
    /// <param name="requestId">Request identifier for tracing (non-null).</param>
    /// <returns>The selected server endpoint, or empty string if no servers registered.</returns>
    Task<string> GetNextServerAsync(string requestId);

    /// <summary>Gets health status of all registered servers.</summary>
    /// <returns>List of server health information.</returns>
    Task<List<ServerHealth>> GetServerHealthAsync();
}

/// <summary>
/// Access control request for policy evaluation.
/// </summary>
public class AccessRequest
{
    /// <summary>The user requesting access.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>The resource being accessed.</summary>
    public string Resource { get; set; } = string.Empty;
}

/// <summary>
/// Access control decision result.
/// </summary>
public class AccessDecision
{
    /// <summary>Whether access is allowed.</summary>
    public bool Allowed { get; set; }

    /// <summary>Reason for the decision.</summary>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Access audit log entry.
/// </summary>
public class AccessLog
{
    /// <summary>The user identifier.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>The resource accessed.</summary>
    public string Resource { get; set; } = string.Empty;

    /// <summary>Timestamp of the access (UTC).</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Zero-trust security implementation interface.
/// Enforces strict authentication and authorization policies with audit logging.
/// Performance Target: Less than 20ms per operation
/// Thread-Safe: Yes
/// Async: Yes
/// </summary>
public interface IZeroTrustImplementation
{
    /// <summary>Authenticates a user with provided credentials.</summary>
    /// <param name="userId">User identifier (non-null).</param>
    /// <param name="credential">Authentication credential (non-null).</param>
    /// <returns>True if authentication succeeds, false otherwise.</returns>
    Task<bool> AuthenticateAsync(string userId, string credential);

    /// <summary>Evaluates access control policy for a resource request.</summary>
    /// <param name="request">Access request details (non-null).</param>
    /// <returns>AccessDecision with authorization result and reason.</returns>
    Task<AccessDecision> EvaluatePolicyAsync(AccessRequest request);

    /// <summary>Logs an access event to the audit trail.</summary>
    /// <param name="log">Access log entry (non-null).</param>
    /// <returns>True if logging succeeded, false otherwise.</returns>
    Task<bool> LogAccessAsync(AccessLog log);
}

/// <summary>
/// Backup information details.
/// </summary>
public class BackupInfo
{
    /// <summary>The unique backup identifier.</summary>
    public string BackupId { get; set; } = string.Empty;

    /// <summary>When the backup was created (UTC).</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Disaster recovery status and metrics.
/// </summary>
public class DisasterRecoveryStatus
{
    /// <summary>Whether the disaster recovery system is healthy.</summary>
    public bool IsHealthy { get; set; }

    /// <summary>Recovery Time Objective in minutes.</summary>
    public int RTO { get; set; }

    /// <summary>Recovery Point Objective in minutes.</summary>
    public int RPO { get; set; }
}

/// <summary>
/// Disaster recovery orchestrator interface.
/// Manages backup creation, restoration, and recovery status monitoring.
/// Performance Target: Less than 500ms per operation
/// Thread-Safe: Yes
/// Async: Yes
/// </summary>
public interface IDisasterRecoveryOrchestrator
{
    /// <summary>Creates a backup of current system state.</summary>
    /// <param name="label">Descriptive label for the backup (non-null).</param>
    /// <returns>True if backup creation succeeded, false otherwise.</returns>
    Task<bool> CreateBackupAsync(string label);

    /// <summary>Lists all available backups in reverse chronological order.</summary>
    /// <returns>List of backup information sorted by creation date (newest first).</returns>
    Task<List<BackupInfo>> ListBackupsAsync();

    /// <summary>Restores system from a specific backup.</summary>
    /// <param name="backupId">The backup identifier to restore from (non-null).</param>
    /// <returns>True if restoration succeeded, false otherwise.</returns>
    Task<bool> RestoreFromBackupAsync(string backupId);

    /// <summary>Gets the current disaster recovery status and metrics.</summary>
    /// <returns>DisasterRecoveryStatus with health and recovery metrics.</returns>
    Task<DisasterRecoveryStatus> GetStatusAsync();
}

