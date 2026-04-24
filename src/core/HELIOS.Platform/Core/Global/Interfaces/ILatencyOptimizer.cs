using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Global.Interfaces;

/// <summary>
/// Network latency optimization service.
/// </summary>
public interface ILatencyOptimizer
{
    /// <summary>Measure latency to all regional endpoints.</summary>
    Task<LatencyMeasurements> MeasureLatencyAsync();
    
    /// <summary>Get latency metrics for a specific region pair.</summary>
    Task<RegionalLatency> GetLatencyAsync(string sourceRegion, string targetRegion);
    
    /// <summary>Analyze latency patterns and identify bottlenecks.</summary>
    Task<LatencyAnalysis> AnalyzeLatencyAsync();
    
    /// <summary>Generate latency optimization recommendations.</summary>
    Task<List<LatencyOptimizationRecommendation>> GetOptimizationRecommendationsAsync();
    
    /// <summary>Calculate optimal routing path between regions.</summary>
    Task<RoutingPath> CalculateOptimalPathAsync(string sourceRegion, string targetRegion);
    
    /// <summary>Get latency heatmap across all regions.</summary>
    Task<LatencyHeatmap> GetLatencyHeatmapAsync();
    
    /// <summary>Predict latency for high-load scenarios.</summary>
    Task<LatencyForecast> ForecastLatencyAsync(string regionPair, int forecastMinutes);
    
    /// <summary>Enable latency monitoring for a route.</summary>
    Task<bool> EnableLatencyMonitoringAsync(string sourceRegion, string targetRegion);
}

/// <summary>
/// Latency measurements across all regions.
/// </summary>
public class LatencyMeasurements
{
    public DateTime MeasuredAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, RegionalLatency> LatenciesByPair { get; set; } = [];
    public double AverageLatencyMs { get; set; }
    public double MinLatencyMs { get; set; }
    public double MaxLatencyMs { get; set; }
    public double P95LatencyMs { get; set; }
    public double P99LatencyMs { get; set; }
    public int TotalMeasurements { get; set; }
}

/// <summary>
/// Latency between two regions.
/// </summary>
public class RegionalLatency
{
    public string SourceRegion { get; set; }
    public string TargetRegion { get; set; }
    public double AverageLatencyMs { get; set; }
    public double MinLatencyMs { get; set; }
    public double MaxLatencyMs { get; set; }
    public double StdDeviation { get; set; }
    public int SampleCount { get; set; }
    public int PacketLossPercent { get; set; }
    public DateTime LastMeasured { get; set; } = DateTime.UtcNow;
    public string Quality { get; set; } // Excellent, Good, Fair, Poor
}

/// <summary>
/// Latency analysis findings.
/// </summary>
public class LatencyAnalysis
{
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
    public double GlobalAverageLatencyMs { get; set; }
    public List<LatencyBottleneck> Bottlenecks { get; set; } = [];
    public List<string> ProblematicRegionPairs { get; set; } = [];
    public Dictionary<string, LatencyTrend> TrendsByRegionPair { get; set; } = [];
    public bool IsLatencyAcceptable { get; set; }
    public string OverallStatus { get; set; } // Optimal, Warning, Critical
}

/// <summary>
/// Latency bottleneck identification.
/// </summary>
public class LatencyBottleneck
{
    public string BottleneckId { get; set; } = Guid.NewGuid().ToString();
    public string SourceRegion { get; set; }
    public string TargetRegion { get; set; }
    public double CurrentLatencyMs { get; set; }
    public double ThresholdLatencyMs { get; set; }
    public double ExcessLatencyMs { get; set; }
    public string ProbableCause { get; set; }
    public string RecommendedAction { get; set; }
    public int Severity { get; set; } // 1-5
    public int Priority { get; set; } // 1-10
}

/// <summary>
/// Latency optimization recommendation.
/// </summary>
public class LatencyOptimizationRecommendation
{
    public string RecommendationId { get; set; } = Guid.NewGuid().ToString();
    public string SourceRegion { get; set; }
    public string TargetRegion { get; set; }
    public string Recommendation { get; set; }
    public string Category { get; set; } // CDN, EdgeCache, DirectConnect, Compression, Routing
    public double ExpectedLatencyReductionMs { get; set; }
    public double ImplementationCost { get; set; }
    public int Priority { get; set; } // 1-10
}

/// <summary>
/// Optimal routing path between regions.
/// </summary>
public class RoutingPath
{
    public string SourceRegion { get; set; }
    public string TargetRegion { get; set; }
    public List<string> IntermediateNodes { get; set; } = [];
    public double TotalLatencyMs { get; set; }
    public double OptimalLatencyMs { get; set; }
    public int HopCount { get; set; }
    public double LatencyEfficiency { get; set; } // 0-1
    public bool IsDirect { get; set; }
    public List<string> AlternativePaths { get; set; } = [];
}

/// <summary>
/// Latency heatmap across regions.
/// </summary>
public class LatencyHeatmap
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, Dictionary<string, double>> LatencyMatrix { get; set; } = [];
    public List<string> Regions { get; set; } = [];
    public double HighestLatencyMs { get; set; }
    public double LowestLatencyMs { get; set; }
    public string HeatmapVisualizationUrl { get; set; }
}

/// <summary>
/// Latency trend analysis.
/// </summary>
public class LatencyTrend
{
    public string RegionPair { get; set; }
    public List<double> LatencyHistory { get; set; } = [];
    public double TrendSlope { get; set; }
    public string Trend { get; set; } // Improving, Degrading, Stable
    public double ProjectedLatencyMs { get; set; }
}

/// <summary>
/// Latency forecast for high-load scenarios.
/// </summary>
public class LatencyForecast
{
    public string RegionPair { get; set; }
    public DateTime ForecastedAt { get; set; } = DateTime.UtcNow;
    public int ForecastMinutes { get; set; }
    public double CurrentLatencyMs { get; set; }
    public double ForecastedLatencyMs { get; set; }
    public double MaxProjectedLatencyMs { get; set; }
    public double MinProjectedLatencyMs { get; set; }
    public List<LatencyProjection> HourlyProjections { get; set; } = [];
}

/// <summary>
/// Latency projection for a specific time point.
/// </summary>
public class LatencyProjection
{
    public DateTime TimePoint { get; set; }
    public double ProjectedLatencyMs { get; set; }
    public double Confidence { get; set; } // 0-1
}

