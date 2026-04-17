namespace HELIOS.Platform.Core.Logging;

/// <summary>
/// Comprehensive logging and diagnostics system for HELIOS Platform.
/// </summary>
public interface IHeliosLogger
{
    /// <summary>Logs a debug message.</summary>
    void LogDebug(string category, string message, Dictionary<string, object>? data = null);

    /// <summary>Logs an informational message.</summary>
    void LogInfo(string category, string message, Dictionary<string, object>? data = null);

    /// <summary>Logs a warning message.</summary>
    void LogWarning(string category, string message, Dictionary<string, object>? data = null);

    /// <summary>Logs an error with optional exception.</summary>
    void LogError(string category, string message, Exception? ex = null, Dictionary<string, object>? data = null);

    /// <summary>Logs a critical error.</summary>
    void LogCritical(string category, string message, Exception? ex = null, Dictionary<string, object>? data = null);

    /// <summary>Logs a security event.</summary>
    void LogSecurityEvent(string eventType, string message, Dictionary<string, object>? data = null);

    /// <summary>Logs a performance metric.</summary>
    void LogPerformance(string component, string metric, double value, string unit);

    /// <summary>Gets recent log entries.</summary>
    IEnumerable<LogEntry> GetLogs(LogLevel? level = null, int? count = null);

    /// <summary>Clears logs older than specified days.</summary>
    void ClearOldLogs(int daysOld);

    /// <summary>Exports logs to file.</summary>
    Task ExportLogsAsync(string filePath, LogLevel? level = null, DateTime? since = null);
}

/// <summary>
/// Log entry with comprehensive information.
/// </summary>
public class LogEntry
{
    public required Guid Id { get; init; }
    public required DateTime Timestamp { get; init; }
    public required LogLevel Level { get; init; }
    public required string Category { get; init; }
    public required string Message { get; init; }
    public Exception? Exception { get; init; }
    public Dictionary<string, object>? Data { get; init; }
    public string? StackTrace { get; init; }
    public int? ThreadId { get; init; }
}

/// <summary>
/// Log level enumeration.
/// </summary>
public enum LogLevel
{
    Debug,
    Information,
    Warning,
    Error,
    Critical,
    SecurityEvent,
    Performance
}

/// <summary>
/// Default implementation of comprehensive logging.
/// </summary>
public class HeliosLogger : IHeliosLogger
{
    private readonly List<LogEntry> _logs = new();
    private readonly string _logDirectory;
    private readonly int _maxLogSize = 100_000_000; // 100 MB
    private readonly object _lockObject = new();

    public HeliosLogger(string? logDirectory = null)
    {
        _logDirectory = logDirectory ?? Path.Combine(Path.GetTempPath(), "HELIOS", "Logs");
        EnsureLogDirectory();
    }

    public void LogDebug(string category, string message, Dictionary<string, object>? data = null)
    {
        Log(LogLevel.Debug, category, message, null, data);
    }

    public void LogInfo(string category, string message, Dictionary<string, object>? data = null)
    {
        Log(LogLevel.Information, category, message, null, data);
    }

    public void LogWarning(string category, string message, Dictionary<string, object>? data = null)
    {
        Log(LogLevel.Warning, category, message, null, data);
    }

    public void LogError(string category, string message, Exception? ex = null, Dictionary<string, object>? data = null)
    {
        Log(LogLevel.Error, category, message, ex, data);
    }

    public void LogCritical(string category, string message, Exception? ex = null, Dictionary<string, object>? data = null)
    {
        Log(LogLevel.Critical, category, message, ex, data);
    }

    public void LogSecurityEvent(string eventType, string message, Dictionary<string, object>? data = null)
    {
        var details = new Dictionary<string, object> { { "EventType", eventType } };
        if (data != null)
            foreach (var kvp in data)
                details[kvp.Key] = kvp.Value;

        Log(LogLevel.SecurityEvent, "SECURITY", message, null, details);
    }

    public void LogPerformance(string component, string metric, double value, string unit)
    {
        var data = new Dictionary<string, object>
        {
            { "Component", component },
            { "Metric", metric },
            { "Value", value },
            { "Unit", unit }
        };

        Log(LogLevel.Performance, "PERFORMANCE", $"{component}.{metric} = {value} {unit}", null, data);
    }

    public IEnumerable<LogEntry> GetLogs(LogLevel? level = null, int? count = null)
    {
        lock (_lockObject)
        {
            var query = _logs.AsEnumerable();
            if (level.HasValue)
                query = query.Where(l => l.Level == level.Value);

            return query.OrderByDescending(l => l.Timestamp)
                .Take(count ?? 1000)
                .ToList();
        }
    }

    public void ClearOldLogs(int daysOld)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);
        lock (_lockObject)
        {
            _logs.RemoveAll(l => l.Timestamp < cutoffDate);
        }

        // Also clean up old log files
        foreach (var file in Directory.GetFiles(_logDirectory, "*.log"))
        {
            var fileInfo = new FileInfo(file);
            if (fileInfo.CreationTimeUtc < cutoffDate)
                fileInfo.Delete();
        }
    }

    public async Task ExportLogsAsync(string filePath, LogLevel? level = null, DateTime? since = null)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? ".");

            lock (_lockObject)
            {
                var query = _logs.AsEnumerable();
                
                if (level.HasValue)
                    query = query.Where(l => l.Level == level.Value);
                
                if (since.HasValue)
                    query = query.Where(l => l.Timestamp >= since.Value);

                using var writer = new StreamWriter(filePath);
                writer.WriteLine("HELIOS Platform - Log Export");
                writer.WriteLine($"Exported: {DateTime.UtcNow:o}");
                writer.WriteLine(new string('=', 80));
                writer.WriteLine();

                foreach (var entry in query.OrderBy(l => l.Timestamp))
                {
                    WriteLogEntry(writer, entry);
                }
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to export logs to {filePath}: {ex.Message}", ex);
        }
    }

    private void Log(LogLevel level, string category, string message, Exception? ex, Dictionary<string, object>? data)
    {
        var entry = new LogEntry
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            Level = level,
            Category = category,
            Message = message,
            Exception = ex,
            Data = data,
            StackTrace = ex?.StackTrace,
            ThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId
        };

        lock (_lockObject)
        {
            _logs.Add(entry);

            // Maintain in-memory log size limit
            if (_logs.Count > 10000)
            {
                var toRemove = _logs.Count - 9000;
                _logs.RemoveRange(0, toRemove);
            }
        }

        // Write to file
        WriteLogToFile(entry);

        // Console output for important events
        WriteToConsole(entry);
    }

    private void WriteLogToFile(LogEntry entry)
    {
        try
        {
            var fileName = Path.Combine(_logDirectory, $"{entry.Timestamp:yyyy-MM-dd}.log");
            var logLine = FormatLogLine(entry);

            lock (_lockObject)
            {
                File.AppendAllText(fileName, logLine + Environment.NewLine);

                // Check and rotate if needed
                var fileInfo = new FileInfo(fileName);
                if (fileInfo.Length > _maxLogSize)
                {
                    var archiveName = Path.Combine(_logDirectory, $"{entry.Timestamp:yyyy-MM-dd_HH-mm-ss}.log.gz");
                    CompressFile(fileName, archiveName);
                }
            }
        }
        catch
        {
            // Silently fail file logging - don't break the application
        }
    }

    private void WriteToConsole(LogEntry entry)
    {
        var color = entry.Level switch
        {
            LogLevel.Debug => ConsoleColor.Gray,
            LogLevel.Information => ConsoleColor.White,
            LogLevel.Warning => ConsoleColor.Yellow,
            LogLevel.Error => ConsoleColor.Red,
            LogLevel.Critical => ConsoleColor.Magenta,
            LogLevel.SecurityEvent => ConsoleColor.Red,
            LogLevel.Performance => ConsoleColor.Cyan,
            _ => ConsoleColor.White
        };

        var oldColor = Console.ForegroundColor;
        try
        {
            Console.ForegroundColor = color;
            Console.WriteLine($"[{entry.Timestamp:HH:mm:ss.fff}] [{entry.Level}] {entry.Category}: {entry.Message}");
            
            if (entry.Exception != null)
                Console.WriteLine($"Exception: {entry.Exception.Message}");
        }
        finally
        {
            Console.ForegroundColor = oldColor;
        }
    }

    private string FormatLogLine(LogEntry entry)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append($"[{entry.Timestamp:o}] ");
        sb.Append($"[{entry.Level}] ");
        sb.Append($"[{entry.Category}] ");
        sb.Append($"[Thread:{entry.ThreadId}] ");
        sb.Append(entry.Message);

        if (entry.Exception != null)
        {
            sb.Append($" | Exception: {entry.Exception.Message}");
            if (!string.IsNullOrEmpty(entry.StackTrace))
                sb.Append($" | Stack: {entry.StackTrace.Substring(0, Math.Min(200, entry.StackTrace.Length))}");
        }

        if (entry.Data?.Count > 0)
        {
            sb.Append(" | Data: ");
            sb.Append(string.Join(", ", entry.Data.Select(kvp => $"{kvp.Key}={kvp.Value}")));
        }

        return sb.ToString();
    }

    private void WriteLogEntry(TextWriter writer, LogEntry entry)
    {
        writer.WriteLine($"[{entry.Timestamp:o}] [{entry.Level}] {entry.Category}");
        writer.WriteLine($"  Message: {entry.Message}");

        if (entry.Exception != null)
        {
            writer.WriteLine($"  Exception: {entry.Exception.Message}");
            writer.WriteLine($"  Type: {entry.Exception.GetType().Name}");
            if (!string.IsNullOrEmpty(entry.StackTrace))
                writer.WriteLine($"  Stack: {entry.StackTrace}");
        }

        if (entry.Data?.Count > 0)
        {
            writer.WriteLine("  Data:");
            foreach (var kvp in entry.Data)
                writer.WriteLine($"    {kvp.Key}: {kvp.Value}");
        }

        writer.WriteLine();
    }

    private void EnsureLogDirectory()
    {
        try
        {
            Directory.CreateDirectory(_logDirectory);
        }
        catch
        {
            // Log directory creation failed - continue anyway
        }
    }

    private void CompressFile(string sourceFile, string destFile)
    {
        try
        {
            using var sourceStream = File.OpenRead(sourceFile);
            using var destStream = File.Create(destFile);
            using var gzip = new System.IO.Compression.GZipStream(destStream, System.IO.Compression.CompressionMode.Compress);
            sourceStream.CopyTo(gzip);
            File.Delete(sourceFile);
        }
        catch
        {
            // Compression failed - continue
        }
    }
}

/// <summary>
/// Health status monitoring for HELIOS Platform.
/// </summary>
public interface IHealthMonitor
{
    /// <summary>Gets overall system health.</summary>
    SystemHealth GetHealthStatus();

    /// <summary>Gets health for specific component.</summary>
    ComponentHealth GetComponentHealth(string component);

    /// <summary>Gets all component health statuses.</summary>
    IEnumerable<ComponentHealth> GetAllComponentHealth();

    /// <summary>Registers a health check for a component.</summary>
    void RegisterHealthCheck(string component, Func<Task<HealthCheckResult>> check);

    /// <summary>Runs all health checks and returns results.</summary>
    Task<SystemHealth> RunHealthChecksAsync();
}

/// <summary>
/// Health status of the system.
/// </summary>
public class SystemHealth
{
    public required HealthStatus Status { get; init; }
    public required DateTime CheckedAt { get; init; }
    public double CPUUsage { get; init; }
    public double MemoryUsage { get; init; }
    public List<ComponentHealth> Components { get; init; } = new();
    public List<string> Issues { get; init; } = new();

    public int HealthPercentage => Components.Count > 0
        ? (int)(Components.Sum(c => c.HealthPercentage) / Components.Count)
        : 100;
}

/// <summary>
/// Health status of a component.
/// </summary>
public class ComponentHealth
{
    public required string Name { get; init; }
    public required HealthStatus Status { get; init; }
    public int HealthPercentage { get; init; }
    public string? Message { get; init; }
    public DateTime LastChecked { get; init; }
    public Dictionary<string, object>? Metrics { get; init; }
}

/// <summary>
/// Result of a health check.
/// </summary>
public class HealthCheckResult
{
    public required bool IsHealthy { get; init; }
    public string? Message { get; init; }
    public Dictionary<string, object>? Metrics { get; init; }
}

/// <summary>
/// Health status enum.
/// </summary>
public enum HealthStatus
{
    Healthy,
    Degraded,
    Unhealthy,
    Unknown
}

/// <summary>
/// Default implementation of health monitoring.
/// </summary>
public class HealthMonitor : IHealthMonitor
{
    private readonly Dictionary<string, Func<Task<HealthCheckResult>>> _healthChecks = new();
    private readonly Dictionary<string, ComponentHealth> _componentHealth = new();

    public void RegisterHealthCheck(string component, Func<Task<HealthCheckResult>> check)
    {
        _healthChecks[component] = check ?? throw new ArgumentNullException(nameof(check));
    }

    public SystemHealth GetHealthStatus()
    {
        return new SystemHealth
        {
            Status = CalculateOverallStatus(),
            CheckedAt = DateTime.UtcNow,
            CPUUsage = GetCPUUsage(),
            MemoryUsage = GetMemoryUsage(),
            Components = _componentHealth.Values.ToList()
        };
    }

    public ComponentHealth GetComponentHealth(string component)
    {
        return _componentHealth.TryGetValue(component, out var health)
            ? health
            : new ComponentHealth { Name = component, Status = HealthStatus.Unknown, HealthPercentage = 0 };
    }

    public IEnumerable<ComponentHealth> GetAllComponentHealth()
    {
        return _componentHealth.Values;
    }

    public async Task<SystemHealth> RunHealthChecksAsync()
    {
        foreach (var kvp in _healthChecks)
        {
            try
            {
                var result = await kvp.Value();
                _componentHealth[kvp.Key] = new ComponentHealth
                {
                    Name = kvp.Key,
                    Status = result.IsHealthy ? HealthStatus.Healthy : HealthStatus.Unhealthy,
                    HealthPercentage = result.IsHealthy ? 100 : 0,
                    Message = result.Message,
                    Metrics = result.Metrics,
                    LastChecked = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _componentHealth[kvp.Key] = new ComponentHealth
                {
                    Name = kvp.Key,
                    Status = HealthStatus.Unhealthy,
                    HealthPercentage = 0,
                    Message = $"Health check failed: {ex.Message}",
                    LastChecked = DateTime.UtcNow
                };
            }
        }

        return GetHealthStatus();
    }

    private HealthStatus CalculateOverallStatus()
    {
        if (_componentHealth.Count == 0)
            return HealthStatus.Unknown;

        var unhealthyCount = _componentHealth.Count(c => c.Value.Status == HealthStatus.Unhealthy);
        var degradedCount = _componentHealth.Count(c => c.Value.Status == HealthStatus.Degraded);

        if (unhealthyCount > 0)
            return HealthStatus.Unhealthy;
        if (degradedCount > 0)
            return HealthStatus.Degraded;

        return HealthStatus.Healthy;
    }

    private double GetCPUUsage()
    {
        try
        {
            using var cpuCounter = new System.Diagnostics.PerformanceCounter("Processor", "% Processor Time", "_Total");
            return cpuCounter.NextValue();
        }
        catch
        {
            return 0;
        }
    }

    private double GetMemoryUsage()
    {
        try
        {
            var totalMemory = GC.GetTotalMemory(false) / (1024d * 1024d);
            return totalMemory;
        }
        catch
        {
            return 0;
        }
    }
}
