using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace HELIOS.Platform.Core.RemoteAccess.Services;

/// <summary>
/// Manages multi-user remote sessions.
/// </summary>
public interface IRemoteSessionManager
{
    /// <summary>Creates a new remote session.</summary>
    Task<string> CreateSessionAsync(string userId, string username, AuthenticationMethod authMethod, CancellationToken cancellationToken = default);

    /// <summary>Gets session information.</summary>
    Task<RemoteSession?> GetSessionAsync(string sessionId);

    /// <summary>Lists active sessions.</summary>
    Task<IEnumerable<RemoteSession>> ListActiveSessionsAsync();

    /// <summary>Lists sessions for a specific user.</summary>
    Task<IEnumerable<RemoteSession>> ListUserSessionsAsync(string userId);

    /// <summary>Terminates a session.</summary>
    Task<bool> TerminateSessionAsync(string sessionId);

    /// <summary>Grants permission to a session.</summary>
    Task<bool> GrantPermissionAsync(string sessionId, string permission);

    /// <summary>Revokes permission from a session.</summary>
    Task<bool> RevokePermissionAsync(string sessionId, string permission);

    /// <summary>Assigns role to a session.</summary>
    Task<bool> AssignRoleAsync(string sessionId, string role);

    /// <summary>Logs an activity to session.</summary>
    Task<bool> LogActivityAsync(string sessionId, SessionActivityLog activityLog);

    /// <summary>Gets session activity history.</summary>
    Task<IEnumerable<SessionActivityLog>> GetActivityHistoryAsync(string sessionId);

    /// <summary>Updates session status.</summary>
    Task<bool> UpdateSessionStatusAsync(string sessionId, SessionStatus status);
}

/// <summary>
/// Default implementation of IRemoteSessionManager.
/// </summary>
public class RemoteSessionManager : IRemoteSessionManager
{
    private readonly ILogger<RemoteSessionManager> _logger;
    private readonly ConcurrentDictionary<string, RemoteSession> _sessions;
    private readonly ConcurrentDictionary<string, List<RemoteSession>> _userSessions;

    public RemoteSessionManager(ILogger<RemoteSessionManager> logger)
    {
        _logger = logger;
        _sessions = new ConcurrentDictionary<string, RemoteSession>();
        _userSessions = new ConcurrentDictionary<string, List<RemoteSession>>();
    }

    /// <inheritdoc/>
    public async Task<string> CreateSessionAsync(string userId, string username, AuthenticationMethod authMethod, CancellationToken cancellationToken = default)
    {
        try
        {
            var session = new RemoteSession
            {
                UserId = userId,
                Username = username,
                AuthenticationMethod = authMethod,
                CreatedAt = DateTime.UtcNow,
                LastActivityAt = DateTime.UtcNow,
                Status = SessionStatus.Active
            };

            if (!_sessions.TryAdd(session.SessionId, session))
            {
                throw new InvalidOperationException("Failed to create session");
            }

            // Track session per user
            _userSessions.AddOrUpdate(userId,
                new List<RemoteSession> { session },
                (_, list) =>
                {
                    list.Add(session);
                    return list;
                });

            _logger.LogInformation("Created session {SessionId} for user {UserId} ({Username})",
                session.SessionId, userId, username);

            return session.SessionId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating session for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<RemoteSession?> GetSessionAsync(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            // Check for expiration
            if (session.ExpiresAt.HasValue && DateTime.UtcNow > session.ExpiresAt)
            {
                session.Status = SessionStatus.Expired;
                _logger.LogWarning("Session {SessionId} has expired", sessionId);
            }

            // Update last activity
            session.LastActivityAt = DateTime.UtcNow;

            return session;
        }

        return null;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<RemoteSession>> ListActiveSessionsAsync()
    {
        var activeSessions = _sessions.Values
            .Where(s => s.Status == SessionStatus.Active || s.Status == SessionStatus.Idle)
            .ToList();

        return activeSessions;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<RemoteSession>> ListUserSessionsAsync(string userId)
    {
        if (_userSessions.TryGetValue(userId, out var sessions))
        {
            return sessions.Where(s => s.Status != SessionStatus.Terminated).ToList();
        }

        return Enumerable.Empty<RemoteSession>();
    }

    /// <inheritdoc/>
    public async Task<bool> TerminateSessionAsync(string sessionId)
    {
        try
        {
            if (_sessions.TryGetValue(sessionId, out var session))
            {
                session.Status = SessionStatus.Terminated;
                _logger.LogInformation("Terminated session {SessionId}", sessionId);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error terminating session {SessionId}", sessionId);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> GrantPermissionAsync(string sessionId, string permission)
    {
        try
        {
            if (_sessions.TryGetValue(sessionId, out var session))
            {
                if (!session.Permissions.Contains(permission))
                {
                    session.Permissions.Add(permission);
                    _logger.LogInformation("Granted permission {Permission} to session {SessionId}",
                        permission, sessionId);
                }

                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error granting permission to session {SessionId}", sessionId);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> RevokePermissionAsync(string sessionId, string permission)
    {
        try
        {
            if (_sessions.TryGetValue(sessionId, out var session))
            {
                session.Permissions.Remove(permission);
                _logger.LogInformation("Revoked permission {Permission} from session {SessionId}",
                    permission, sessionId);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking permission from session {SessionId}", sessionId);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> AssignRoleAsync(string sessionId, string role)
    {
        try
        {
            if (_sessions.TryGetValue(sessionId, out var session))
            {
                if (!session.Roles.Contains(role))
                {
                    session.Roles.Add(role);
                    _logger.LogInformation("Assigned role {Role} to session {SessionId}",
                        role, sessionId);
                }

                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role to session {SessionId}", sessionId);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> LogActivityAsync(string sessionId, SessionActivityLog activityLog)
    {
        try
        {
            if (_sessions.TryGetValue(sessionId, out var session))
            {
                session.ActivityLogs.Add(activityLog);

                // Keep only last 1000 entries
                if (session.ActivityLogs.Count > 1000)
                {
                    session.ActivityLogs.RemoveAt(0);
                }

                _logger.LogInformation("Logged activity {ActivityType} to session {SessionId}",
                    activityLog.ActivityType, sessionId);

                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging activity to session {SessionId}", sessionId);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<SessionActivityLog>> GetActivityHistoryAsync(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            return session.ActivityLogs.ToList();
        }

        return Enumerable.Empty<SessionActivityLog>();
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateSessionStatusAsync(string sessionId, SessionStatus status)
    {
        try
        {
            if (_sessions.TryGetValue(sessionId, out var session))
            {
                session.Status = status;
                _logger.LogInformation("Updated session {SessionId} status to {Status}",
                    sessionId, status);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating session status for {SessionId}", sessionId);
            return false;
        }
    }
}
