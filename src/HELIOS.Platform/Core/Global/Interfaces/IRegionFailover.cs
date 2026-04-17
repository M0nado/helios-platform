using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Global.Interfaces;

/// <summary>
/// Automatic regional failover management service.
/// </summary>
public interface IRegionFailover
{
    /// <summary>Register a region as a failover candidate.</summary>
    Task<bool> RegisterFailoverRegionAsync(string primaryRegion, string failoverRegion, int failoverPriority);
    
    /// <summary>Monitor region health continuously.</summary>
    Task StartRegionMonitoringAsync();
    
    /// <summary>Stop region monitoring.</summary>
    Task StopRegionMonitoringAsync();
    
    /// <summary>Manually trigger failover to another region.</summary>
    Task<FailoverResult> TriggerFailoverAsync(string fromRegion, string toRegion);
    
    /// <summary>Failback to primary region after recovery.</summary>
    Task<FailoverResult> FailbackAsync(string primaryRegion);
    
    /// <summary>Get failover status and configuration.</summary>
    Task<FailoverStatus> GetFailoverStatusAsync();
    
    /// <summary>Get failover history.</summary>
    Task<List<FailoverEvent>> GetFailoverHistoryAsync(int maxEvents = 50);
    
    /// <summary>Set automatic failover thresholds.</summary>
    Task<bool> SetFailoverThresholdsAsync(FailoverThresholds thresholds);
    
    /// <summary>Check if automatic failover should be triggered.</summary>
    Task<bool> ShouldTriggerAutoFailoverAsync(string regionName);
}

/// <summary>
/// Failover result details.
/// </summary>
public class FailoverResult
{
    public string FailoverEventId { get; set; } = Guid.NewGuid().ToString();
    public string FromRegion { get; set; }
    public string ToRegion { get; set; }
    public DateTime InitiatedAt { get; set; } = DateTime.UtcNow;
    public DateTime CompletedAt { get; set; }
    public double DurationSeconds { get; set; }
    public bool IsSuccessful { get; set; }
    public string Status { get; set; } // InProgress, Completed, Failed, Rolled Back
    public int RequestsInterrupted { get; set; }
    public int RequestsRecovered { get; set; }
    public string ErrorMessage { get; set; }
    public List<string> AffectedServices { get; set; } = [];
}

/// <summary>
/// Failover status information.
/// </summary>
public class FailoverStatus
{
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public Dictionary<string, RegionFailoverConfig> RegionConfigs { get; set; } = [];
    public Dictionary<string, bool> RegionHealthStatus { get; set; } = [];
    public string CurrentActiveRegion { get; set; }
    public bool IsAutoFailoverEnabled { get; set; }
    public bool IsMonitoring { get; set; }
    public int TotalFailoverCount { get; set; }
    public FailoverThresholds CurrentThresholds { get; set; } = new();
}

/// <summary>
/// Regional failover configuration.
/// </summary>
public class RegionFailoverConfig
{
    public string PrimaryRegion { get; set; }
    public string FailoverRegion { get; set; }
    public int FailoverPriority { get; set; }
    public bool IsEnabled { get; set; }
    public double HealthCheckIntervalSeconds { get; set; } = 30.0;
    public int MaxConsecutiveFailures { get; set; } = 3;
}

/// <summary>
/// Failover event record.
/// </summary>
public class FailoverEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString();
    public DateTime OccurredAt { get; set; }
    public string FromRegion { get; set; }
    public string ToRegion { get; set; }
    public string TriggerReason { get; set; } // HealthCheckFailed, ManualTrigger, ThresholdExceeded
    public bool WasAutomatic { get; set; }
    public bool WasSuccessful { get; set; }
    public double DurationSeconds { get; set; }
    public int ConnectionsAffected { get; set; }
    public string Details { get; set; }
}

/// <summary>
/// Automatic failover thresholds.
/// </summary>
public class FailoverThresholds
{
    public int MaxConsecutiveHealthCheckFailures { get; set; } = 3;
    public double MaxLatencyMs { get; set; } = 5000.0;
    public double MinSuccessRatePercent { get; set; } = 80.0;
    public double MaxCpuUsagePercent { get; set; } = 95.0;
    public double MaxMemoryUsagePercent { get; set; } = 95.0;
    public int MaxErrorsPerMinute { get; set; } = 100;
    public double HealthCheckIntervalSeconds { get; set; } = 30.0;
    public int MinHealthyReplicasRequired { get; set; } = 1;
}
