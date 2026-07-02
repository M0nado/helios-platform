namespace MonadoBlade.Security.Abstractions;

/// <summary>
/// Enum for security event severity levels.
/// </summary>
public enum SecurityEventSeverity
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Enum for security event types.
/// </summary>
public enum SecurityEventType
{
    Login,
    Logout,
    AuthorizationFailure,
    EncryptionKeyGeneration,
    EncryptionKeyRotation,
    InputValidationFailure,
    RateLimitExceeded,
    XssAttempt,
    SqlInjectionAttempt,
    PathTraversalAttempt,
    CsrfTokenValidationFailure,
    ConfigurationChange,
    UnauthorizedAccess,
    ComplianceViolation
}

/// <summary>
/// Interface for secure, tamper-proof audit logging.
/// </summary>
public interface ISecureAuditLogger
{
    /// <summary>
    /// Logs a security event with integrity protection.
    /// </summary>
    /// <param name="eventType">Type of security event.</param>
    /// <param name="severity">Event severity level.</param>
    /// <param name="details">Event details.</param>
    /// <param name="userId">User ID associated with the event (if applicable).</param>
    /// <returns>The event ID assigned to this log entry.</returns>
    Task<string> LogSecurityEventAsync(SecurityEventType eventType, SecurityEventSeverity severity, string details, string? userId = null);

    /// <summary>
    /// Retrieves security events with optional filtering.
    /// </summary>
    /// <param name="eventType">Filter by event type (optional).</param>
    /// <param name="severity">Filter by severity level (optional).</param>
    /// <param name="hoursBack">Look back this many hours (default 24).</param>
    /// <returns>List of security events matching the criteria.</returns>
    Task<List<SecurityEventLog>> GetSecurityEventsAsync(SecurityEventType? eventType = null, SecurityEventSeverity? severity = null, int hoursBack = 24);

    /// <summary>
    /// Verifies the integrity of a log entry using HMAC validation.
    /// </summary>
    /// <param name="eventId">The event ID to verify.</param>
    /// <returns>True if log entry is valid and unmodified, false otherwise.</returns>
    Task<bool> VerifyLogIntegrityAsync(string eventId);

    /// <summary>
    /// Generates a compliance report for audit purposes.
    /// </summary>
    /// <param name="startDate">Report start date.</param>
    /// <param name="endDate">Report end date.</param>
    /// <param name="includeDetails">Include detailed event information.</param>
    /// <returns>Compliance report as JSON string.</returns>
    Task<string> GenerateComplianceReportAsync(DateTime startDate, DateTime endDate, bool includeDetails = false);

    /// <summary>
    /// Gets statistics about security events.
    /// </summary>
    /// <param name="hoursBack">Statistics for the last N hours.</param>
    /// <returns>Dictionary with event statistics.</returns>
    Task<Dictionary<string, int>> GetEventStatisticsAsync(int hoursBack = 24);

    /// <summary>
    /// Archives old log entries (for storage optimization).
    /// </summary>
    /// <param name="olderThanDays">Archive events older than this many days.</param>
    /// <returns>Number of events archived.</returns>
    Task<int> ArchiveOldEventsAsync(int olderThanDays = 90);
}

/// <summary>
/// Represents a security event log entry.
/// </summary>
public class SecurityEventLog
{
    public string EventId { get; set; } = string.Empty;
    public SecurityEventType EventType { get; set; }
    public SecurityEventSeverity Severity { get; set; }
    public DateTime Timestamp { get; set; }
    public string Details { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
}
