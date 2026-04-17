using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Observability.Interfaces;

/// <summary>
/// Health check orchestration service for system availability monitoring and SLA tracking.
/// </summary>
public interface IHealthCheckOrchestrator
{
    /// <summary>
    /// Register a health check probe.
    /// </summary>
    /// <param name="serviceName">Name of service to monitor</param>
    /// <param name="checkConfig">Health check configuration</param>
    Task<string> RegisterHealthCheckAsync(string serviceName, HealthCheckConfig checkConfig);

    /// <summary>
    /// Deregister a health check.
    /// </summary>
    Task<bool> DeregisterHealthCheckAsync(string checkId);

    /// <summary>
    /// Execute a health check immediately.
    /// </summary>
    Task<HealthCheckResult> ExecuteHealthCheckAsync(string checkId);

    /// <summary>
    /// Execute all registered health checks.
    /// </summary>
    Task<List<HealthCheckResult>> ExecuteAllHealthChecksAsync();

    /// <summary>
    /// Get health status for a service.
    /// </summary>
    Task<ServiceHealthStatus> GetServiceHealthAsync(string serviceName);

    /// <summary>
    /// Get overall system health.
    /// </summary>
    Task<SystemHealthSummary> GetSystemHealthAsync();

    /// <summary>
    /// Get health history for trending.
    /// </summary>
    Task<List<HealthCheckResult>> GetHealthHistoryAsync(string? serviceName = null, TimeSpan? period = null);

    /// <summary>
    /// Configure cascade dependencies (e.g., DB health affects API health).
    /// </summary>
    Task<bool> SetDependencyAsync(string dependentService, string dependsOnService);

    /// <summary>
    /// Start continuous health monitoring.
    /// </summary>
    Task<bool> StartMonitoringAsync();

    /// <summary>
    /// Stop continuous health monitoring.
    /// </summary>
    Task<bool> StopMonitoringAsync();

    /// <summary>
    /// Perform automatic remediation on health check failure.
    /// </summary>
    Task<RemediationResult> RemediateAsync(string serviceName);
}

/// <summary>
/// Health check configuration.
/// </summary>
public class HealthCheckConfig
{
    public string? Id { get; set; }
    public HealthCheckType Type { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);
    public int FailureThreshold { get; set; } = 3;
    public int SuccessThreshold { get; set; } = 2;
    public List<HealthCheckProbe> Probes { get; set; } = new();
}

/// <summary>
/// Health check type.
/// </summary>
public enum HealthCheckType
{
    HTTP,
    TCP,
    DNS,
    Database,
    Kubernetes,
    CustomScript
}

/// <summary>
/// Individual health check probe.
/// </summary>
public class HealthCheckProbe
{
    public string Name { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
    public object? ExpectedResult { get; set; }
    public string? JsonPath { get; set; }
}

/// <summary>
/// Health check result.
/// </summary>
public class HealthCheckResult
{
    public string CheckId { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public HealthStatus Status { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan ResponseTime { get; set; }
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object> Details { get; set; } = new();
}

/// <summary>
/// Health status enumeration.
/// </summary>
public enum HealthStatus
{
    Healthy,
    Degraded,
    Unhealthy,
    Unknown
}

/// <summary>
/// Service health status with trending.
/// </summary>
public class ServiceHealthStatus
{
    public string ServiceName { get; set; } = string.Empty;
    public HealthStatus CurrentStatus { get; set; }
    public DateTime LastCheck { get; set; }
    public int ConsecutiveFailures { get; set; }
    public double UpTimePercentage { get; set; }
    public List<HealthCheckResult> RecentChecks { get; set; } = new();
}

/// <summary>
/// System-wide health summary.
/// </summary>
public class SystemHealthSummary
{
    public HealthStatus OverallStatus { get; set; }
    public int HealthyServices { get; set; }
    public int DegradedServices { get; set; }
    public int UnhealthyServices { get; set; }
    public double AverageUpTime { get; set; }
    public DateTime ScanTime { get; set; } = DateTime.UtcNow;
    public List<ServiceHealthStatus> ServiceStatuses { get; set; } = new();
}

/// <summary>
/// Remediation result.
/// </summary>
public class RemediationResult
{
    public string ServiceName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Action { get; set; } = string.Empty;
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public string Message { get; set; } = string.Empty;
}
