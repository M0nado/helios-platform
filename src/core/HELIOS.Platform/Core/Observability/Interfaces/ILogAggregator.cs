using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Observability.Interfaces;

/// <summary>
/// Centralized log aggregation and querying service.
/// Collects, indexes, and provides full-text search over system logs.
/// </summary>
public interface ILogAggregator
{
    /// <summary>
    /// Index a log entry.
    /// </summary>
    Task<string> IndexLogAsync(LogEntry entry);

    /// <summary>
    /// Query logs with filtering and aggregation.
    /// </summary>
    /// <param name="query">Search query (full-text or structured)</param>
    /// <param name="level">Optional log level filter</param>
    /// <param name="source">Optional source filter</param>
    /// <returns>Matching log entries</returns>
    Task<List<LogEntry>> QueryLogsAsync(
        string query,
        LogLevel? level = null,
        string? source = null,
        int limit = 1000);

    /// <summary>
    /// Get log statistics by level, source, or time.
    /// </summary>
    Task<LogStatistics> GetStatisticsAsync(TimeSpan? period = null);

    /// <summary>
    /// Stream logs in real-time.
    /// </summary>
    IAsyncEnumerable<LogEntry> StreamLogsAsync(LogFilter filter);

    /// <summary>
    /// Set retention policy for logs.
    /// </summary>
    Task<bool> SetRetentionPolicyAsync(TimeSpan retentionPeriod);

    /// <summary>
    /// Export logs to file.
    /// </summary>
    Task<string> ExportLogsAsync(string format = "json", TimeSpan? period = null);

    /// <summary>
    /// Create a saved search/view.
    /// </summary>
    Task<string> CreateSavedSearchAsync(string name, LogFilter filter);

    /// <summary>
    /// Get log patterns (frequency analysis).
    /// </summary>
    Task<List<LogPattern>> GetLogPatternsAsync(int topN = 20);
}

/// <summary>
/// Log entry structure.
/// </summary>
public class LogEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public LogLevel Level { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object> Context { get; set; } = new();
    public string? Exception { get; set; }
    public string? StackTrace { get; set; }
}

/// <summary>
/// Log level enumeration.
/// </summary>
public enum LogLevel
{
    Trace,
    Debug,
    Information,
    Warning,
    Error,
    Critical
}

/// <summary>
/// Log filtering criteria.
/// </summary>
public class LogFilter
{
    public LogLevel? MinLevel { get; set; }
    public string? Source { get; set; }
    public string? Query { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public List<string>? Tags { get; set; }
}

/// <summary>
/// Log statistics.
/// </summary>
public class LogStatistics
{
    public int TotalLogs { get; set; }
    public Dictionary<LogLevel, int> CountByLevel { get; set; } = new();
    public Dictionary<string, int> CountBySource { get; set; } = new();
    public double AverageLogsPerSecond { get; set; }
    public int ErrorCount { get; set; }
    public int CriticalCount { get; set; }
}

/// <summary>
/// Log pattern (recurring message patterns).
/// </summary>
public class LogPattern
{
    public string Pattern { get; set; } = string.Empty;
    public int Frequency { get; set; }
    public LogLevel Level { get; set; }
    public string Source { get; set; } = string.Empty;
    public double PercentageOfTotal { get; set; }
}
