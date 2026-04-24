using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Global.Interfaces;

/// <summary>
/// Multi-region metrics aggregation service for global monitoring.
/// </summary>
public interface IGlobalMetricsAggregator
{
    /// <summary>Register a regional metric source.</summary>
    Task<bool> RegisterRegionAsync(string regionName, string endpoint);
    
    /// <summary>Collect metrics from all registered regions.</summary>
    Task<AggregatedMetrics> CollectMetricsAsync();
    
    /// <summary>Get metrics for a specific region.</summary>
    Task<RegionalMetrics> GetRegionMetricsAsync(string regionName);
    
    /// <summary>Get aggregated metrics across all regions.</summary>
    Task<GlobalMetricsSnapshot> GetGlobalSnapshotAsync();
    
    /// <summary>Calculate multi-region averages.</summary>
    Task<RegionalAverages> CalculateRegionalAveragesAsync();
    
    /// <summary>Track metric trends over time.</summary>
    Task<MetricTrends> GetMetricTrendsAsync(string regionName, TimeSpan timeWindow);
    
    /// <summary>Remove a region from aggregation.</summary>
    Task<bool> UnregisterRegionAsync(string regionName);
    
    /// <summary>Get health status of all regions.</summary>
    Task<Dictionary<string, RegionHealthStatus>> GetRegionHealthAsync();
}

/// <summary>
/// Aggregated metrics from all regions.
/// </summary>
public class AggregatedMetrics
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int TotalRegions { get; set; }
    public int HealthyRegions { get; set; }
    public double GlobalCpuUsagePercent { get; set; }
    public double GlobalMemoryUsagePercent { get; set; }
    public double GlobalNetworkLatencyMs { get; set; }
    public long GlobalThroughputBytesPerSec { get; set; }
    public Dictionary<string, RegionalMetrics> RegionalBreakdown { get; set; } = [];
}

/// <summary>
/// Metrics for a specific region.
/// </summary>
public class RegionalMetrics
{
    public string RegionName { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public double CpuUsagePercent { get; set; }
    public double MemoryUsagePercent { get; set; }
    public double NetworkLatencyMs { get; set; }
    public long ThroughputBytesPerSec { get; set; }
    public int ActiveConnections { get; set; }
    public int ErrorCount { get; set; }
    public bool IsHealthy { get; set; }
}

/// <summary>
/// Global metrics snapshot across all regions.
/// </summary>
public class GlobalMetricsSnapshot
{
    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;
    public int TotalRegions { get; set; }
    public double AverageCpuUsagePercent { get; set; }
    public double AverageMemoryUsagePercent { get; set; }
    public double AverageLatencyMs { get; set; }
    public long TotalThroughputBytesPerSec { get; set; }
    public int TotalActiveConnections { get; set; }
    public int CriticalRegionCount { get; set; }
    public List<RegionalMetrics> AllRegionMetrics { get; set; } = [];
}

/// <summary>
/// Regional averages for comparison.
/// </summary>
public class RegionalAverages
{
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    public double AvgCpuUsagePercent { get; set; }
    public double AvgMemoryUsagePercent { get; set; }
    public double AvgLatencyMs { get; set; }
    public double AvgThroughputBytesPerSec { get; set; }
    public Dictionary<string, double> RegionCpuMap { get; set; } = [];
    public Dictionary<string, double> RegionLatencyMap { get; set; } = [];
}

/// <summary>
/// Metric trends over time for a region.
/// </summary>
public class MetricTrends
{
    public string RegionName { get; set; }
    public TimeSpan TimeWindow { get; set; }
    public List<double> CpuTrend { get; set; } = [];
    public List<double> MemoryTrend { get; set; } = [];
    public List<double> LatencyTrend { get; set; } = [];
    public double CpuTrendSlope { get; set; }
    public double MemoryTrendSlope { get; set; }
    public double LatencyTrendSlope { get; set; }
    public bool IsTrendingUp { get; set; }
}

/// <summary>
/// Health status of a region.
/// </summary>
public class RegionHealthStatus
{
    public string RegionName { get; set; }
    public bool IsHealthy { get; set; }
    public int SuccessfulConnections { get; set; }
    public int FailedConnections { get; set; }
    public double SuccessRate { get; set; }
    public DateTime LastHealthCheck { get; set; } = DateTime.UtcNow;
    public string LastError { get; set; }
}
