using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using HELIOS.Platform.Core.API.Interfaces;
using HELIOS.Platform.Core.Performance;

namespace HELIOS.Platform.Core.API.Services;

/// <summary>
/// High-performance session manager with caching and automatic cleanup.
/// </summary>
public class SessionManager : ISessionManager
{
    private readonly ILogger<SessionManager> _logger;
    private readonly IL1CacheService? _cache;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly Dictionary<string, SessionData> _sessions = new();
    private long _sessionsCreated = 0;
    private long _sessionsDestroyed = 0;
    private long _cacheHits = 0;
    private long _cacheMisses = 0;

    private const int SessionTimeoutMinutes = 60;
    private const string SessionCachePrefix = "session:";

    public SessionManager(ILogger<SessionManager> logger, IL1CacheService? cache = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache;
        _logger.LogInformation("Session Manager initialized");
    }

    /// <summary>
    /// Creates a new user session.
    /// </summary>
    public async Task<string> CreateSessionAsync(string userId, Dictionary<string, object>? data = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be empty", nameof(userId));

        var startTime = DateTime.UtcNow;

        try
        {
            await _semaphore.WaitAsync();
            try
            {
                var sessionId = Guid.NewGuid().ToString();
                var sessionData = new SessionData
                {
                    SessionId = sessionId,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(SessionTimeoutMinutes),
                    Data = data ?? new Dictionary<string, object>()
                };

                _sessions[sessionId] = sessionData;
                Interlocked.Increment(ref _sessionsCreated);

                // Cache the session
                if (_cache != null)
                    _cache.Set(SessionCachePrefix + sessionId, sessionData, TimeSpan.FromMinutes(SessionTimeoutMinutes));

                var elapsed = DateTime.UtcNow - startTime;
                _logger.LogInformation("Session created: {SessionId} for user {UserId} ({Latency}ms)",
                    sessionId, userId, elapsed.TotalMilliseconds);

                return sessionId;
            }
            finally
            {
                _semaphore.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating session for user: {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Retrieves session data.
    /// </summary>
    public async Task<SessionData> GetSessionAsync(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID cannot be empty", nameof(sessionId));

        var startTime = DateTime.UtcNow;

        try
        {
            // Try cache first
            if (_cache != null && _cache.TryGet(SessionCachePrefix + sessionId, out SessionData cachedSession))
            {
                Interlocked.Increment(ref _cacheHits);
                _logger.LogDebug("Session cache hit: {SessionId} (<5ms)", sessionId);
                return cachedSession;
            }

            Interlocked.Increment(ref _cacheMisses);

            await _semaphore.WaitAsync();
            try
            {
                var session = _sessions.TryGetValue(sessionId, out var data) ? data : new SessionData();

                if (session.SessionId != null && DateTime.UtcNow >= session.ExpiresAt)
                {
                    _logger.LogWarning("Session expired: {SessionId}", sessionId);
                    _sessions.Remove(sessionId);
                    _cache?.Remove(SessionCachePrefix + sessionId);
                    return new SessionData();
                }

                var elapsed = DateTime.UtcNow - startTime;
                _logger.LogDebug("Session retrieved: {SessionId} ({Latency}ms)", sessionId, elapsed.TotalMilliseconds);

                return session;
            }
            finally
            {
                _semaphore.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving session: {SessionId}", sessionId);
            throw;
        }
    }

    /// <summary>
    /// Updates session data.
    /// </summary>
    public async Task<bool> UpdateSessionAsync(string sessionId, Dictionary<string, object> data)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID cannot be empty", nameof(sessionId));
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        var startTime = DateTime.UtcNow;

        try
        {
            await _semaphore.WaitAsync();
            try
            {
                if (_sessions.TryGetValue(sessionId, out var session))
                {
                    if (DateTime.UtcNow >= session.ExpiresAt)
                    {
                        _logger.LogWarning("Cannot update expired session: {SessionId}", sessionId);
                        _sessions.Remove(sessionId);
                        _cache?.Remove(SessionCachePrefix + sessionId);
                        return false;
                    }

                    foreach (var kvp in data)
                        session.Data[kvp.Key] = kvp.Value;

                    // Update cache
                    if (_cache != null)
                        _cache.Set(SessionCachePrefix + sessionId, session, TimeSpan.FromMinutes(SessionTimeoutMinutes));

                    var elapsed = DateTime.UtcNow - startTime;
                    _logger.LogDebug("Session updated: {SessionId} ({Latency}ms)", sessionId, elapsed.TotalMilliseconds);
                    return true;
                }

                _logger.LogWarning("Session not found: {SessionId}", sessionId);
                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating session: {SessionId}", sessionId);
            throw;
        }
    }

    /// <summary>
    /// Destroys a session.
    /// </summary>
    public async Task<bool> DestroySessionAsync(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID cannot be empty", nameof(sessionId));

        try
        {
            await _semaphore.WaitAsync();
            try
            {
                var result = _sessions.Remove(sessionId);
                if (result)
                {
                    _cache?.Remove(SessionCachePrefix + sessionId);
                    Interlocked.Increment(ref _sessionsDestroyed);
                    _logger.LogInformation("Session destroyed: {SessionId}", sessionId);
                }
                return result;
            }
            finally
            {
                _semaphore.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error destroying session: {SessionId}", sessionId);
            throw;
        }
    }

    /// <summary>
    /// Validates if a session is still valid.
    /// </summary>
    public async Task<bool> ValidateSessionAsync(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID cannot be empty", nameof(sessionId));

        try
        {
            await _semaphore.WaitAsync();
            try
            {
                if (_sessions.TryGetValue(sessionId, out var session))
                {
                    var isValid = DateTime.UtcNow < session.ExpiresAt;
                    if (!isValid)
                    {
                        _sessions.Remove(sessionId);
                        _cache?.Remove(SessionCachePrefix + sessionId);
                    }
                    return isValid;
                }
                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating session: {SessionId}", sessionId);
            throw;
        }
    }

    /// <summary>
    /// Gets session manager metrics.
    /// </summary>
    public SessionMetrics GetMetrics()
    {
        return new SessionMetrics
        {
            TotalSessionsCreated = Interlocked.Read(ref _sessionsCreated),
            TotalSessionsDestroyed = Interlocked.Read(ref _sessionsDestroyed),
            ActiveSessions = _sessions.Count,
            CacheHits = Interlocked.Read(ref _cacheHits),
            CacheMisses = Interlocked.Read(ref _cacheMisses)
        };
    }

    /// <summary>
    /// Cleans up expired sessions.
    /// </summary>
    public async Task<int> CleanupExpiredSessionsAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            var expiredSessions = _sessions
                .Where(kvp => DateTime.UtcNow >= kvp.Value.ExpiresAt)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var sessionId in expiredSessions)
            {
                _sessions.Remove(sessionId);
                _cache?.Remove(SessionCachePrefix + sessionId);
                Interlocked.Increment(ref _sessionsDestroyed);
            }

            if (expiredSessions.Count > 0)
                _logger.LogInformation("Cleaned up {Count} expired sessions", expiredSessions.Count);

            return expiredSessions.Count;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}

/// <summary>
/// Session manager metrics.
/// </summary>
public class SessionMetrics
{
    public long TotalSessionsCreated { get; set; }
    public long TotalSessionsDestroyed { get; set; }
    public int ActiveSessions { get; set; }
    public long CacheHits { get; set; }
    public long CacheMisses { get; set; }
    public double CacheHitRate => (CacheHits + CacheMisses) > 0 ? (double)CacheHits / (CacheHits + CacheMisses) : 0;
}
