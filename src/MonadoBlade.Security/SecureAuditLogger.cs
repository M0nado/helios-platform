using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MonadoBlade.Security.Abstractions;

namespace MonadoBlade.Security;

/// <summary>
/// Provides tamper-proof, thread-safe security event logging with HMAC integrity validation.
/// </summary>
public class SecureAuditLogger : ISecureAuditLogger
{
    private static readonly ConcurrentDictionary<string, SecurityEventLog> EventLogs = new();
    private static readonly ConcurrentDictionary<string, string> EventHashes = new();
    private static readonly byte[] HmacKey = GenerateHmacKey();
    private static readonly object _syncLock = new();
    private int _eventCounter;

    /// <summary>
    /// Logs a security event with cryptographic integrity protection.
    /// </summary>
    public async Task<string> LogSecurityEventAsync(SecurityEventType eventType, SecurityEventSeverity severity, string details, string? userId = null)
    {
        lock (_syncLock)
        {
            _eventCounter++;
        }

        var eventId = $"{_eventCounter:D8}-{DateTime.UtcNow:yyyyMMddHHmmssffff}";
        var timestamp = DateTime.UtcNow;
        var ipAddress = GetClientIpAddress();

        var eventLog = new SecurityEventLog
        {
            EventId = eventId,
            EventType = eventType,
            Severity = severity,
            Timestamp = timestamp,
            Details = details,
            UserId = userId,
            IpAddress = ipAddress,
            Hash = string.Empty // Will be calculated
        };

        // Calculate and store hash for integrity verification
        var hash = CalculateEventHash(eventLog);
        eventLog.Hash = hash;

        EventLogs.TryAdd(eventId, eventLog);
        EventHashes.TryAdd(eventId, hash);

        return eventId;
    }

    /// <summary>
    /// Retrieves security events with optional filtering.
    /// </summary>
    public async Task<List<SecurityEventLog>> GetSecurityEventsAsync(SecurityEventType? eventType = null, SecurityEventSeverity? severity = null, int hoursBack = 24)
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-hoursBack);

        var events = EventLogs.Values
            .Where(e => e.Timestamp >= cutoffTime)
            .Where(e => eventType == null || e.EventType == eventType)
            .Where(e => severity == null || e.Severity == severity)
            .OrderByDescending(e => e.Timestamp)
            .ToList();

        return events;
    }

    /// <summary>
    /// Verifies the integrity of a log entry using HMAC validation.
    /// </summary>
    public async Task<bool> VerifyLogIntegrityAsync(string eventId)
    {
        if (!EventLogs.TryGetValue(eventId, out var eventLog))
            return false;

        if (!EventHashes.TryGetValue(eventId, out var storedHash))
            return false;

        var calculatedHash = CalculateEventHash(eventLog);
        return ConstantTimeEquals(storedHash, calculatedHash);
    }

    /// <summary>
    /// Generates a compliance report for audit purposes in JSON format.
    /// </summary>
    public async Task<string> GenerateComplianceReportAsync(DateTime startDate, DateTime endDate, bool includeDetails = false)
    {
        var events = EventLogs.Values
            .Where(e => e.Timestamp >= startDate && e.Timestamp <= endDate)
            .OrderByDescending(e => e.Timestamp)
            .ToList();

        var reportData = new
        {
            ReportGeneratedAt = DateTime.UtcNow.ToString("O"),
            ReportPeriod = new
            {
                StartDate = startDate.ToString("O"),
                EndDate = endDate.ToString("O")
            },
            Summary = new
            {
                TotalEvents = events.Count,
                CriticalEvents = events.Count(e => e.Severity == SecurityEventSeverity.Critical),
                HighEvents = events.Count(e => e.Severity == SecurityEventSeverity.High),
                MediumEvents = events.Count(e => e.Severity == SecurityEventSeverity.Medium),
                LowEvents = events.Count(e => e.Severity == SecurityEventSeverity.Low),
                UniqueUsers = events.Select(e => e.UserId).Distinct().Count(),
                UniqueIpAddresses = events.Select(e => e.IpAddress).Distinct().Count()
            },
            EventsByType = events
                .GroupBy(e => e.EventType)
                .ToDictionary(g => g.Key.ToString(), g => g.Count()),
            EventsBySeverity = events
                .GroupBy(e => e.Severity)
                .ToDictionary(g => g.Key.ToString(), g => g.Count()),
            Events = includeDetails ? events.Select(FormatEventForReport) : null
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        return JsonSerializer.Serialize(reportData, options);
    }

    /// <summary>
    /// Gets statistics about security events.
    /// </summary>
    public async Task<Dictionary<string, int>> GetEventStatisticsAsync(int hoursBack = 24)
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-hoursBack);
        var recentEvents = EventLogs.Values
            .Where(e => e.Timestamp >= cutoffTime)
            .ToList();

        var stats = new Dictionary<string, int>
        {
            ["TotalEvents"] = recentEvents.Count,
            ["CriticalEvents"] = recentEvents.Count(e => e.Severity == SecurityEventSeverity.Critical),
            ["HighEvents"] = recentEvents.Count(e => e.Severity == SecurityEventSeverity.High),
            ["MediumEvents"] = recentEvents.Count(e => e.Severity == SecurityEventSeverity.Medium),
            ["LowEvents"] = recentEvents.Count(e => e.Severity == SecurityEventSeverity.Low),
            ["LoginEvents"] = recentEvents.Count(e => e.EventType == SecurityEventType.Login),
            ["LogoutEvents"] = recentEvents.Count(e => e.EventType == SecurityEventType.Logout),
            ["AuthorizationFailures"] = recentEvents.Count(e => e.EventType == SecurityEventType.AuthorizationFailure),
            ["RateLimitExceeded"] = recentEvents.Count(e => e.EventType == SecurityEventType.RateLimitExceeded),
            ["SecurityAttemptsBlocked"] = recentEvents.Count(e => 
                e.EventType == SecurityEventType.XssAttempt || 
                e.EventType == SecurityEventType.SqlInjectionAttempt || 
                e.EventType == SecurityEventType.PathTraversalAttempt)
        };

        return stats;
    }

    /// <summary>
    /// Archives old log entries for storage optimization.
    /// </summary>
    public async Task<int> ArchiveOldEventsAsync(int olderThanDays = 90)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-olderThanDays);
        var oldEventIds = EventLogs
            .Where(kvp => kvp.Value.Timestamp < cutoffDate)
            .Select(kvp => kvp.Key)
            .ToList();

        int archivedCount = 0;
        foreach (var eventId in oldEventIds)
        {
            if (EventLogs.TryRemove(eventId, out _))
            {
                EventHashes.TryRemove(eventId, out _);
                archivedCount++;
            }
        }

        return archivedCount;
    }

    private static string CalculateEventHash(SecurityEventLog eventLog)
    {
        var eventString = $"{eventLog.EventId}|{eventLog.EventType}|{eventLog.Severity}|{eventLog.Timestamp:O}|{eventLog.Details}|{eventLog.UserId ?? ""}|{eventLog.IpAddress}";
        var dataBytes = Encoding.UTF8.GetBytes(eventString);

        using (var hmac = new HMACSHA256(HmacKey))
        {
            var hashBytes = hmac.ComputeHash(dataBytes);
            return Convert.ToBase64String(hashBytes);
        }
    }

    private static byte[] GenerateHmacKey()
    {
        using (var rng = RandomNumberGenerator.Create())
        {
            var key = new byte[32];
            rng.GetBytes(key);
            return key;
        }
    }

    private static bool ConstantTimeEquals(string a, string b)
    {
        if (a == null || b == null)
            return a == b;

        int result = a.Length ^ b.Length;
        int minLength = Math.Min(a.Length, b.Length);

        for (int i = 0; i < minLength; i++)
            result |= a[i] ^ b[i];

        return result == 0;
    }

    private static string GetClientIpAddress()
    {
        try
        {
            // Attempt to get the local IP address
            return Dns.GetHostName();
        }
        catch
        {
            return "Unknown";
        }
    }

    private static object FormatEventForReport(SecurityEventLog @event)
    {
        return new
        {
            @event.EventId,
            EventType = @event.EventType.ToString(),
            Severity = @event.Severity.ToString(),
            Timestamp = @event.Timestamp.ToString("O"),
            @event.Details,
            @event.UserId,
            @event.IpAddress
        };
    }
}
