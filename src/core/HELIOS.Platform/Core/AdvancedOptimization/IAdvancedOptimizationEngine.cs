using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.AdvancedOptimization
{
    /// <summary>
    /// System-wide optimization engine that analyzes metrics and performance data
    /// to generate and apply automatic optimizations safely with rollback capability.
    /// </summary>
    public interface IAdvancedOptimizationEngine
    {
        Task<bool> InitializeAsync();
        Task<OptimizationRecommendation[]> AnalyzeSystemAsync();
        Task<OptimizationResult> ApplyOptimizationAsync(string optimizationId);
        Task<bool> RollbackOptimizationAsync(string optimizationId);
        Task<OptimizationMetrics> GetOptimizationMetricsAsync();
        Task<OptimizationImpactReport> GetOptimizationImpactAsync();
    }

    /// <summary>Represents an optimization recommendation.</summary>
    public class OptimizationRecommendation
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public OptimizationType Type { get; set; }
        public double ExpectedImpact { get; set; }
        public double SafetyScore { get; set; }
        public int Priority { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>Types of optimizations available.</summary>
    public enum OptimizationType
    {
        CPU,
        Memory,
        Disk,
        Network,
        Scheduling,
        Caching,
        Batching,
        Threading,
        ResourceAllocation
    }

    /// <summary>Represents the result of applying an optimization.</summary>
    public class OptimizationResult
    {
        public string OptimizationId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public double ImpactMeasured { get; set; }
        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
        public string Snapshot { get; set; } = string.Empty; // For rollback
        public List<string> AffectedSystems { get; set; } = new();
    }

    /// <summary>Tracks optimization metrics and statistics.</summary>
    public class OptimizationMetrics
    {
        public int TotalRecommendations { get; set; }
        public int AppliedOptimizations { get; set; }
        public int FailedOptimizations { get; set; }
        public int RolledBackOptimizations { get; set; }
        public double AverageSafetyScore { get; set; }
        public double CumulativeImpact { get; set; }
        public double CPUOptimizationPercent { get; set; }
        public double MemoryOptimizationPercent { get; set; }
        public DateTime LastAnalysisTime { get; set; }
        public long TotalAnalysisRuns { get; set; }
    }

    /// <summary>Represents the ROI impact of optimizations.</summary>
    public class OptimizationImpactReport
    {
        public List<OptimizationImpactItem> Impacts { get; set; } = new();
        public double TotalROI { get; set; }
        public double TotalResourcesSaved { get; set; }
        public DateTime ReportGeneratedAt { get; set; } = DateTime.UtcNow;
        public TimeSpan ReportingPeriod { get; set; }
        public Dictionary<OptimizationType, double> ImpactByType { get; set; } = new();
    }

    /// <summary>Individual optimization impact.</summary>
    public class OptimizationImpactItem
    {
        public string OptimizationId { get; set; } = string.Empty;
        public string OptimizationName { get; set; } = string.Empty;
        public double ResourcesSaved { get; set; }
        public double PerformanceGain { get; set; }
        public DateTime AppliedDate { get; set; }
        public long TimesSaved { get; set; }
        public double CostSaved { get; set; }
    }
}
