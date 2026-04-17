namespace HELIOS.Platform.Core.RemoteAccess.Models;

/// <summary>
/// Represents system diagnostics and monitoring information.
/// </summary>
public class RemoteSystemDiagnostics
{
    /// <summary>Gets or sets the unique diagnostic ID.</summary>
    public string DiagnosticId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Gets or sets the connection ID.</summary>
    public string ConnectionId { get; set; } = string.Empty;

    /// <summary>Gets or sets the diagnostic timestamp.</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets CPU usage percentage.</summary>
    public double CpuUsagePercent { get; set; }

    /// <summary>Gets or sets memory usage percentage.</summary>
    public double MemoryUsagePercent { get; set; }

    /// <summary>Gets or sets disk usage percentage.</summary>
    public double DiskUsagePercent { get; set; }

    /// <summary>Gets or sets network latency in milliseconds.</summary>
    public double NetworkLatencyMs { get; set; }

    /// <summary>Gets or sets bandwidth utilization.</summary>
    public BandwidthMetrics? BandwidthMetrics { get; set; }

    /// <summary>Gets or sets active process count.</summary>
    public int ActiveProcessCount { get; set; }

    /// <summary>Gets or sets system uptime.</summary>
    public TimeSpan SystemUptime { get; set; }

    /// <summary>Gets or sets OS information.</summary>
    public string OSVersion { get; set; } = string.Empty;

    /// <summary>Gets or sets system health status.</summary>
    public HealthStatus HealthStatus { get; set; } = HealthStatus.Healthy;

    /// <summary>Gets or sets diagnostics details.</summary>
    public Dictionary<string, object> Details { get; set; } = new();
}

/// <summary>
/// Represents bandwidth metrics.
/// </summary>
public class BandwidthMetrics
{
    /// <summary>Gets or sets bytes sent per second.</summary>
    public long BytesSentPerSec { get; set; }

    /// <summary>Gets or sets bytes received per second.</summary>
    public long BytesReceivedPerSec { get; set; }

    /// <summary>Gets or sets connection count.</summary>
    public int ConnectionCount { get; set; }

    /// <summary>Gets or sets packet loss percentage.</summary>
    public double PacketLossPercent { get; set; }
}

/// <summary>
/// Enumeration of system health statuses.
/// </summary>
public enum HealthStatus
{
    /// <summary>System is healthy</summary>
    Healthy,

    /// <summary>System has warnings</summary>
    Warning,

    /// <summary>System is critical</summary>
    Critical,

    /// <summary>System is offline</summary>
    Offline
}

/// <summary>
/// Represents a monitoring event.
/// </summary>
public class MonitoringEvent
{
    /// <summary>Gets or sets the event ID.</summary>
    public string EventId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Gets or sets the connection ID.</summary>
    public string ConnectionId { get; set; } = string.Empty;

    /// <summary>Gets or sets the event type.</summary>
    public EventType EventType { get; set; }

    /// <summary>Gets or sets the event severity.</summary>
    public EventSeverity Severity { get; set; }

    /// <summary>Gets or sets the event message.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Gets or sets the event timestamp.</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the source component.</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>Gets or sets event data.</summary>
    public Dictionary<string, object> Data { get; set; } = new();
}

/// <summary>
/// Enumeration of event types.
/// </summary>
public enum EventType
{
    /// <summary>Connection event</summary>
    Connection,

    /// <summary>Performance event</summary>
    Performance,

    /// <summary>Security event</summary>
    Security,

    /// <summary>Error event</summary>
    Error,

    /// <summary>Warning event</summary>
    Warning,

    /// <summary>Informational event</summary>
    Information
}

/// <summary>
/// Enumeration of event severity levels.
/// </summary>
public enum EventSeverity
{
    /// <summary>Low severity</summary>
    Low = 1,

    /// <summary>Medium severity</summary>
    Medium = 2,

    /// <summary>High severity</summary>
    High = 3,

    /// <summary>Critical severity</summary>
    Critical = 4
}
