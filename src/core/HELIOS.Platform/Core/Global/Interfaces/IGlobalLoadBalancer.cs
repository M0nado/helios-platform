using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Global.Interfaces;

/// <summary>
/// Geo-distributed load balancing service.
/// </summary>
public interface IGlobalLoadBalancer
{
    /// <summary>Register a regional endpoint for load balancing.</summary>
    Task<bool> RegisterEndpointAsync(string regionName, string endpoint, int weightPercentage);
    
    /// <summary>Select the best endpoint for a request based on latency and load.</summary>
    Task<string> SelectEndpointAsync(string requestContext);
    
    /// <summary>Get current load distribution across regions.</summary>
    Task<LoadDistribution> GetLoadDistributionAsync();
    
    /// <summary>Update endpoint weights dynamically.</summary>
    Task<bool> UpdateEndpointWeightAsync(string regionName, int newWeightPercentage);
    
    /// <summary>Get health status of all endpoints.</summary>
    Task<Dictionary<string, EndpointHealthStatus>> GetEndpointHealthAsync();
    
    /// <summary>Set load balancing strategy.</summary>
    Task<bool> SetLoadBalancingStrategyAsync(LoadBalancingStrategy strategy);
    
    /// <summary>Get request distribution statistics.</summary>
    Task<RequestDistributionStats> GetRequestDistributionStatsAsync();
    
    /// <summary>Perform global traffic failover for maintenance.</summary>
    Task<bool> PerformMaintenanceFailoverAsync(string maintenanceRegion, string targetRegion);
}

/// <summary>
/// Current load distribution across regions.
/// </summary>
public class LoadDistribution
{
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, double> LoadByRegion { get; set; } = [];
    public Dictionary<string, int> RequestCountByRegion { get; set; } = [];
    public double AverageLoadPercent { get; set; }
    public double BalanceIndex { get; set; } // 0-1, 1 = perfectly balanced
    public bool IsBalanced { get; set; }
    public List<string> UnderutilizedRegions { get; set; } = [];
    public List<string> OverloadedRegions { get; set; } = [];
}

/// <summary>
/// Endpoint health status.
/// </summary>
public class EndpointHealthStatus
{
    public string RegionName { get; set; }
    public string Endpoint { get; set; }
    public bool IsHealthy { get; set; }
    public double LatencyMs { get; set; }
    public int RequestsPerSecond { get; set; }
    public double CpuUsagePercent { get; set; }
    public double MemoryUsagePercent { get; set; }
    public int ConsecutiveFailures { get; set; }
    public DateTime LastHealthCheck { get; set; } = DateTime.UtcNow;
    public string LastError { get; set; }
}

/// <summary>
/// Load balancing strategy enumeration.
/// </summary>
public enum LoadBalancingStrategy
{
    RoundRobin,
    LeastConnections,
    LatencyBased,
    WeightedRoundRobin,
    IPHash,
    CustomWeights,
    Predictive
}

/// <summary>
/// Request distribution statistics.
/// </summary>
public class RequestDistributionStats
{
    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
    public long TotalRequests { get; set; }
    public long RequestsLastHour { get; set; }
    public double AverageRequestsPerSecond { get; set; }
    public Dictionary<string, long> RequestsByRegion { get; set; } = [];
    public Dictionary<string, double> RequestPercentageByRegion { get; set; } = [];
    public long TotalRoutedRequests { get; set; }
    public long FailedRoutings { get; set; }
    public double SuccessRate { get; set; }
    public double AverageLatencyMs { get; set; }
    public double MaxLatencyMs { get; set; }
}
