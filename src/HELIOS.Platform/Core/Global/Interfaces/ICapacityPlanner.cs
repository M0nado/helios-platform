using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Global.Interfaces;

/// <summary>
/// Predictive capacity management service for multi-region deployments.
/// </summary>
public interface ICapacityPlanner
{
    /// <summary>Analyze current capacity utilization.</summary>
    Task<CapacityAnalysis> AnalyzeCapacityAsync();
    
    /// <summary>Get capacity forecast for a specific region.</summary>
    Task<CapacityForecast> ForecastCapacityAsync(string regionName, int forecastDaysAhead);
    
    /// <summary>Identify capacity bottlenecks.</summary>
    Task<List<CapacityBottleneck>> IdentifyBottlenecksAsync();
    
    /// <summary>Generate capacity scaling recommendations.</summary>
    Task<List<ScalingRecommendation>> GetScalingRecommendationsAsync();
    
    /// <summary>Calculate required capacity for projected workload.</summary>
    Task<CapacityRequirement> CalculateRequiredCapacityAsync(WorkloadProjection projection);
    
    /// <summary>Get multi-region capacity balance.</summary>
    Task<CapacityBalance> GetCapacityBalanceAsync();
    
    /// <summary>Predict resource usage for next 30 days.</summary>
    Task<UsageForecast> Get30DayForecastAsync();
    
    /// <summary>Calculate capacity headroom in each region.</summary>
    Task<Dictionary<string, double>> CalculateCapacityHeadroomAsync();
}

/// <summary>
/// Current capacity analysis across regions.
/// </summary>
public class CapacityAnalysis
{
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
    public double OverallUtilizationPercent { get; set; }
    public int TotalCapacityUnits { get; set; }
    public int UsedCapacityUnits { get; set; }
    public int AvailableCapacityUnits { get; set; }
    public Dictionary<string, double> RegionalUtilization { get; set; } = [];
    public List<string> CriticalRegions { get; set; } = [];
    public bool IsCapacityConstrained { get; set; }
}

/// <summary>
/// Capacity forecast for a region.
/// </summary>
public class CapacityForecast
{
    public string RegionName { get; set; }
    public DateTime ForecastAt { get; set; } = DateTime.UtcNow;
    public int DaysAhead { get; set; }
    public List<DailyCapacityProjection> Projections { get; set; } = [];
    public double PeakUtilizationPercent { get; set; }
    public int DaysUntilCapacityFull { get; set; }
    public string CapacityStatus { get; set; } // Healthy, Warning, Critical
    public List<string> Recommendations { get; set; } = [];
}

/// <summary>
/// Daily capacity projection.
/// </summary>
public class DailyCapacityProjection
{
    public DateTime Date { get; set; }
    public double ProjectedUtilizationPercent { get; set; }
    public int RequiredCapacityUnits { get; set; }
    public double Confidence { get; set; } // 0-1
    public string Status { get; set; } // Healthy, Warning, Critical
}

/// <summary>
/// Capacity bottleneck identification.
/// </summary>
public class CapacityBottleneck
{
    public string BottleneckId { get; set; } = Guid.NewGuid().ToString();
    public string RegionName { get; set; }
    public string ResourceType { get; set; } // CPU, Memory, Network, Storage
    public double CurrentUtilizationPercent { get; set; }
    public int Severity { get; set; } // 1-5
    public int DaysUntilFull { get; set; }
    public string RecommendedAction { get; set; }
}

/// <summary>
/// Scaling recommendation.
/// </summary>
public class ScalingRecommendation
{
    public string RecommendationId { get; set; } = Guid.NewGuid().ToString();
    public string RegionName { get; set; }
    public string ScalingType { get; set; } // HorizontalScale, VerticalScale, CapacityAdd
    public int InstancesRequired { get; set; }
    public string InstanceType { get; set; }
    public double EstimatedCostUSD { get; set; }
    public int Priority { get; set; } // 1-10
    public int DaysUntilRequired { get; set; }
}

/// <summary>
/// Workload projection for capacity calculation.
/// </summary>
public class WorkloadProjection
{
    public string ProjectionId { get; set; } = Guid.NewGuid().ToString();
    public DateTime ProjectionDate { get; set; }
    public int ExpectedActiveUsers { get; set; }
    public double ExpectedTransactionsPerSecond { get; set; }
    public long ExpectedDataStorageBytes { get; set; }
    public double ExpectedNetworkBandwidthGbps { get; set; }
    public string Scenario { get; set; } // Normal, Peak, Holiday, Emergency
}

/// <summary>
/// Capacity requirement calculation result.
/// </summary>
public class CapacityRequirement
{
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    public WorkloadProjection Projection { get; set; }
    public int RequiredComputeUnits { get; set; }
    public long RequiredStorageBytes { get; set; }
    public double RequiredNetworkGbps { get; set; }
    public int RecommendedInstances { get; set; }
    public double EstimatedCostUSD { get; set; }
}

/// <summary>
/// Capacity balance across regions.
/// </summary>
public class CapacityBalance
{
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, double> UtilizationByRegion { get; set; } = [];
    public double BalanceIndex { get; set; } // 0-1, 1 = perfectly balanced
    public List<string> OverloadedRegions { get; set; } = [];
    public List<string> UnderutilizedRegions { get; set; } = [];
    public bool IsBalanced { get; set; }
    public List<string> RebalancingSuggestions { get; set; } = [];
}

/// <summary>
/// 30-day usage forecast.
/// </summary>
public class UsageForecast
{
    public DateTime ForecastAt { get; set; } = DateTime.UtcNow;
    public List<DailyUsageForecast> DailyForecasts { get; set; } = [];
    public double AverageDailyUsage { get; set; }
    public double PeakDayUsage { get; set; }
    public DateTime PeakDay { get; set; }
    public double TrendPercentage { get; set; }
    public string Trend { get; set; } // Increasing, Decreasing, Stable
}

/// <summary>
/// Daily usage forecast.
/// </summary>
public class DailyUsageForecast
{
    public DateTime Date { get; set; }
    public double ProjectedUsagePercent { get; set; }
    public int ProjectedUserCount { get; set; }
    public double Confidence { get; set; }
}
