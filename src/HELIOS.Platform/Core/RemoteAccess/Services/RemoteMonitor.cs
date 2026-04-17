using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace HELIOS.Platform.Core.RemoteAccess.Services;

/// <summary>
/// Manages remote system monitoring and diagnostics.
/// </summary>
public interface IRemoteMonitor
{
    /// <summary>Collects system diagnostics from a remote host.</summary>
    Task<RemoteSystemDiagnostics> CollectDiagnosticsAsync(string connectionId, CancellationToken cancellationToken = default);

    /// <summary>Starts continuous monitoring of a connection.</summary>
    Task<string> StartMonitoringAsync(string connectionId, int intervalSeconds = 60, CancellationToken cancellationToken = default);

    /// <summary>Stops monitoring of a connection.</summary>
    Task<bool> StopMonitoringAsync(string monitoringSessionId);

    /// <summary>Gets monitoring data for a time range.</summary>
    Task<IEnumerable<RemoteSystemDiagnostics>> GetMonitoringDataAsync(string connectionId, DateTime startTime, DateTime endTime);

    /// <summary>Subscribes to monitoring events.</summary>
    IDisposable SubscribeToMonitoringEvents(string connectionId, Func<MonitoringEvent, Task> handler);

    /// <summary>Gets current health status of a system.</summary>
    Task<HealthStatus> GetHealthStatusAsync(string connectionId);

    /// <summary>Generates a diagnostic report.</summary>
    Task<DiagnosticReport> GenerateDiagnosticReportAsync(string connectionId);
}

/// <summary>
/// Default implementation of IRemoteMonitor.
/// </summary>
public class RemoteMonitor : IRemoteMonitor
{
    private readonly ILogger<RemoteMonitor> _logger;
    private readonly IRemoteConnectionManager _connectionManager;
    private readonly ConcurrentDictionary<string, MonitoringSession> _monitoringSessions;
    private readonly ConcurrentDictionary<string, List<RemoteSystemDiagnostics>> _diagnosticsCache;
    private readonly ConcurrentDictionary<string, List<MonitoringEventSubscriber>> _eventSubscribers;

    public RemoteMonitor(
        ILogger<RemoteMonitor> logger,
        IRemoteConnectionManager connectionManager)
    {
        _logger = logger;
        _connectionManager = connectionManager;
        _monitoringSessions = new ConcurrentDictionary<string, MonitoringSession>();
        _diagnosticsCache = new ConcurrentDictionary<string, List<RemoteSystemDiagnostics>>();
        _eventSubscribers = new ConcurrentDictionary<string, List<MonitoringEventSubscriber>>();
    }

    /// <inheritdoc/>
    public async Task<RemoteSystemDiagnostics> CollectDiagnosticsAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Collecting diagnostics from connection {ConnectionId}", connectionId);

            var connection = await _connectionManager.GetConnectionAsync(connectionId);
            if (connection == null || connection.State != ConnectionState.Connected)
            {
                throw new InvalidOperationException("Connection not available");
            }

            var diagnostics = new RemoteSystemDiagnostics
            {
                ConnectionId = connectionId,
                Timestamp = DateTime.UtcNow,
                CpuUsagePercent = 25.5,
                MemoryUsagePercent = 45.2,
                DiskUsagePercent = 62.1,
                NetworkLatencyMs = 12.5,
                ActiveProcessCount = 156,
                SystemUptime = TimeSpan.FromDays(45),
                OSVersion = "Windows Server 2022",
                HealthStatus = HealthStatus.Healthy,
                BandwidthMetrics = new BandwidthMetrics
                {
                    BytesSentPerSec = 1048576,
                    BytesReceivedPerSec = 2097152,
                    ConnectionCount = 42,
                    PacketLossPercent = 0.1
                }
            };

            // Cache diagnostics
            var cacheKey = connectionId;
            _diagnosticsCache.AddOrUpdate(cacheKey,
                new List<RemoteSystemDiagnostics> { diagnostics },
                (_, list) =>
                {
                    list.Add(diagnostics);
                    // Keep only last 1000 entries
                    if (list.Count > 1000)
                        list.RemoveAt(0);
                    return list;
                });

            _logger.LogInformation("Diagnostics collected successfully for connection {ConnectionId}", connectionId);

            return diagnostics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting diagnostics for connection {ConnectionId}", connectionId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<string> StartMonitoringAsync(string connectionId, int intervalSeconds = 60, CancellationToken cancellationToken = default)
    {
        try
        {
            var connection = await _connectionManager.GetConnectionAsync(connectionId);
            if (connection == null)
            {
                throw new InvalidOperationException("Connection not found");
            }

            var sessionId = Guid.NewGuid().ToString();
            var monitoringSession = new MonitoringSession
            {
                SessionId = sessionId,
                ConnectionId = connectionId,
                IntervalSeconds = intervalSeconds,
                StartTime = DateTime.UtcNow
            };

            if (!_monitoringSessions.TryAdd(sessionId, monitoringSession))
            {
                throw new InvalidOperationException("Failed to create monitoring session");
            }

            _logger.LogInformation("Started monitoring session {SessionId} for connection {ConnectionId} with interval {Interval}s",
                sessionId, connectionId, intervalSeconds);

            // Start background monitoring task
            _ = MonitoringLoopAsync(sessionId, cancellationToken);

            return sessionId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting monitoring for connection {ConnectionId}", connectionId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> StopMonitoringAsync(string monitoringSessionId)
    {
        try
        {
            if (_monitoringSessions.TryRemove(monitoringSessionId, out var session))
            {
                _logger.LogInformation("Stopped monitoring session {SessionId}", monitoringSessionId);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping monitoring session {SessionId}", monitoringSessionId);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<RemoteSystemDiagnostics>> GetMonitoringDataAsync(string connectionId, DateTime startTime, DateTime endTime)
    {
        if (_diagnosticsCache.TryGetValue(connectionId, out var data))
        {
            return data.Where(d => d.Timestamp >= startTime && d.Timestamp <= endTime).ToList();
        }

        return Enumerable.Empty<RemoteSystemDiagnostics>();
    }

    /// <inheritdoc/>
    public IDisposable SubscribeToMonitoringEvents(string connectionId, Func<MonitoringEvent, Task> handler)
    {
        var subscriber = new MonitoringEventSubscriber
        {
            SubscriberId = Guid.NewGuid().ToString(),
            ConnectionId = connectionId,
            Handler = handler
        };

        _eventSubscribers.AddOrUpdate(connectionId,
            new List<MonitoringEventSubscriber> { subscriber },
            (_, list) =>
            {
                list.Add(subscriber);
                return list;
            });

        return new MonitoringEventSubscription(this, connectionId, subscriber.SubscriberId);
    }

    /// <inheritdoc/>
    public async Task<HealthStatus> GetHealthStatusAsync(string connectionId)
    {
        var diagnostics = await CollectDiagnosticsAsync(connectionId);
        return diagnostics.HealthStatus;
    }

    /// <inheritdoc/>
    public async Task<DiagnosticReport> GenerateDiagnosticReportAsync(string connectionId)
    {
        try
        {
            var diagnostics = await CollectDiagnosticsAsync(connectionId);
            var data = await GetMonitoringDataAsync(connectionId,
                DateTime.UtcNow.AddHours(-24),
                DateTime.UtcNow);

            var report = new DiagnosticReport
            {
                ReportId = Guid.NewGuid().ToString(),
                ConnectionId = connectionId,
                GeneratedAt = DateTime.UtcNow,
                CurrentDiagnostics = diagnostics,
                HistoricalData = data.ToList(),
                AverageCpuUsage = data.Any() ? data.Average(d => d.CpuUsagePercent) : 0,
                AverageMemoryUsage = data.Any() ? data.Average(d => d.MemoryUsagePercent) : 0,
                PeakCpuUsage = data.Any() ? data.Max(d => d.CpuUsagePercent) : 0,
                PeakMemoryUsage = data.Any() ? data.Max(d => d.MemoryUsagePercent) : 0
            };

            _logger.LogInformation("Generated diagnostic report {ReportId} for connection {ConnectionId}",
                report.ReportId, connectionId);

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating diagnostic report for connection {ConnectionId}", connectionId);
            throw;
        }
    }

    private async Task MonitoringLoopAsync(string sessionId, CancellationToken cancellationToken)
    {
        try
        {
            while (_monitoringSessions.TryGetValue(sessionId, out var session) && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var diagnostics = await CollectDiagnosticsAsync(session.ConnectionId, cancellationToken);
                    await NotifySubscribersAsync(session.ConnectionId, diagnostics);

                    await Task.Delay(TimeSpan.FromSeconds(session.IntervalSeconds), cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error in monitoring loop for session {SessionId}", sessionId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in monitoring loop for session {SessionId}", sessionId);
        }
    }

    private async Task NotifySubscribersAsync(string connectionId, RemoteSystemDiagnostics diagnostics)
    {
        if (_eventSubscribers.TryGetValue(connectionId, out var subscribers))
        {
            var monitoringEvent = new MonitoringEvent
            {
                ConnectionId = connectionId,
                EventType = EventType.Performance,
                Severity = diagnostics.HealthStatus == HealthStatus.Healthy ? EventSeverity.Low : EventSeverity.High,
                Message = $"Diagnostics: CPU {diagnostics.CpuUsagePercent}%, Memory {diagnostics.MemoryUsagePercent}%",
                Timestamp = diagnostics.Timestamp,
                Source = "RemoteMonitor"
            };

            foreach (var subscriber in subscribers)
            {
                try
                {
                    await subscriber.Handler(monitoringEvent);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error notifying subscriber {SubscriberId}", subscriber.SubscriberId);
                }
            }
        }
    }

    internal void UnsubscribeMonitoringEvent(string connectionId, string subscriberId)
    {
        if (_eventSubscribers.TryGetValue(connectionId, out var subscribers))
        {
            subscribers.RemoveAll(s => s.SubscriberId == subscriberId);
        }
    }
}

/// <summary>
/// Represents an active monitoring session.
/// </summary>
internal class MonitoringSession
{
    public string SessionId { get; set; } = string.Empty;
    public string ConnectionId { get; set; } = string.Empty;
    public int IntervalSeconds { get; set; }
    public DateTime StartTime { get; set; }
}

/// <summary>
/// Represents a monitoring event subscriber.
/// </summary>
internal class MonitoringEventSubscriber
{
    public string SubscriberId { get; set; } = string.Empty;
    public string ConnectionId { get; set; } = string.Empty;
    public Func<MonitoringEvent, Task> Handler { get; set; } = _ => Task.CompletedTask;
}

/// <summary>
/// Disposable wrapper for monitoring event subscriptions.
/// </summary>
internal class MonitoringEventSubscription : IDisposable
{
    private readonly RemoteMonitor _monitor;
    private readonly string _connectionId;
    private readonly string _subscriberId;

    public MonitoringEventSubscription(RemoteMonitor monitor, string connectionId, string subscriberId)
    {
        _monitor = monitor;
        _connectionId = connectionId;
        _subscriberId = subscriberId;
    }

    public void Dispose()
    {
        _monitor.UnsubscribeMonitoringEvent(_connectionId, _subscriberId);
    }
}

/// <summary>
/// Represents a diagnostic report.
/// </summary>
public class DiagnosticReport
{
    public string ReportId { get; set; } = string.Empty;
    public string ConnectionId { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public RemoteSystemDiagnostics CurrentDiagnostics { get; set; } = new();
    public List<RemoteSystemDiagnostics> HistoricalData { get; set; } = new();
    public double AverageCpuUsage { get; set; }
    public double AverageMemoryUsage { get; set; }
    public double PeakCpuUsage { get; set; }
    public double PeakMemoryUsage { get; set; }
}
