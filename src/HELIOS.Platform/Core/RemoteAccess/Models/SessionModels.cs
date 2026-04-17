namespace HELIOS.Platform.Core.RemoteAccess.Models;

/// <summary>
/// Represents a user session for multi-user support.
/// </summary>
public class RemoteSession
{
    /// <summary>Gets or sets the unique session ID.</summary>
    public string SessionId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Gets or sets the user ID.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Gets or sets the username.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Gets or sets the session creation time.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the last activity time.</summary>
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the session expiration time.</summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>Gets or sets the session status.</summary>
    public SessionStatus Status { get; set; } = SessionStatus.Active;

    /// <summary>Gets or sets assigned permissions.</summary>
    public List<string> Permissions { get; set; } = new();

    /// <summary>Gets or sets assigned roles.</summary>
    public List<string> Roles { get; set; } = new();

    /// <summary>Gets or sets the authentication method.</summary>
    public AuthenticationMethod AuthenticationMethod { get; set; }

    /// <summary>Gets or sets the session metadata.</summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>Gets or sets connected resource IDs.</summary>
    public List<string> ConnectedResourceIds { get; set; } = new();

    /// <summary>Gets or sets the IP address of the session initiator.</summary>
    public string? ClientIpAddress { get; set; }

    /// <summary>Gets or sets session activity log entries.</summary>
    public List<SessionActivityLog> ActivityLogs { get; set; } = new();
}

/// <summary>
/// Enumeration of session statuses.
/// </summary>
public enum SessionStatus
{
    /// <summary>Session is active</summary>
    Active,

    /// <summary>Session is idle</summary>
    Idle,

    /// <summary>Session is suspended</summary>
    Suspended,

    /// <summary>Session is expired</summary>
    Expired,

    /// <summary>Session is terminated</summary>
    Terminated
}

/// <summary>
/// Enumeration of authentication methods.
/// </summary>
public enum AuthenticationMethod
{
    /// <summary>Username/password authentication</summary>
    UsernamePassword,

    /// <summary>SSH key authentication</summary>
    SshKey,

    /// <summary>Certificate-based authentication</summary>
    Certificate,

    /// <summary>Multi-factor authentication</summary>
    MultiFactorAuth,

    /// <summary>OAuth2 authentication</summary>
    OAuth2,

    /// <summary>SAML authentication</summary>
    SAML
}

/// <summary>
/// Represents a session activity log entry.
/// </summary>
public class SessionActivityLog
{
    /// <summary>Gets or sets the log entry ID.</summary>
    public string LogId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Gets or sets the activity type.</summary>
    public ActivityType ActivityType { get; set; }

    /// <summary>Gets or sets the activity description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the activity timestamp.</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the resource affected.</summary>
    public string? ResourceId { get; set; }

    /// <summary>Gets or sets the activity result.</summary>
    public ActivityResult Result { get; set; }

    /// <summary>Gets or sets detailed information about the activity.</summary>
    public Dictionary<string, object> Details { get; set; } = new();
}

/// <summary>
/// Enumeration of activity types.
/// </summary>
public enum ActivityType
{
    /// <summary>Login activity</summary>
    Login,

    /// <summary>Logout activity</summary>
    Logout,

    /// <summary>Command execution</summary>
    CommandExecution,

    /// <summary>File transfer</summary>
    FileTransfer,

    /// <summary>Configuration change</summary>
    ConfigurationChange,

    /// <summary>Permission change</summary>
    PermissionChange,

    /// <summary>Session suspension</summary>
    SessionSuspension,

    /// <summary>Unauthorized access attempt</summary>
    UnauthorizedAccess,

    /// <summary>Other activity</summary>
    Other
}

/// <summary>
/// Enumeration of activity results.
/// </summary>
public enum ActivityResult
{
    /// <summary>Activity succeeded</summary>
    Success,

    /// <summary>Activity failed</summary>
    Failure,

    /// <summary>Activity was denied</summary>
    Denied
}
