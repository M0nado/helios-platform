using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Global.Interfaces;

/// <summary>
/// Cloud cost analysis and optimization service.
/// </summary>
public interface ICostOptimizer
{
    /// <summary>Analyze current cloud costs across regions.</summary>
    Task<CostAnalysis> AnalyzeCostsAsync();
    
    /// <summary>Get cost breakdown by service type.</summary>
    Task<Dictionary<string, ServiceCost>> GetCostsByServiceAsync();
    
    /// <summary>Get cost breakdown by region.</summary>
    Task<Dictionary<string, RegionCost>> GetCostsByRegionAsync();
    
    /// <summary>Generate optimization recommendations to reduce costs.</summary>
    Task<List<CostOptimizationRecommendation>> GetOptimizationRecommendationsAsync();
    
    /// <summary>Estimate savings from a specific optimization.</summary>
    Task<SavingsEstimate> EstimateSavingsAsync(string optimizationId);
    
    /// <summary>Get historical cost trends.</summary>
    Task<CostTrends> GetCostTrendsAsync(TimeSpan period);
    
    /// <summary>Calculate ROI for cost optimizations.</summary>
    Task<ROICalculation> CalculateROIAsync(List<string> optimizationIds);
    
    /// <summary>Set cost budget alerts.</summary>
    Task<bool> SetCostBudgetAlertAsync(string regionName, double monthlyBudgetUSD);
}

/// <summary>
/// Overall cost analysis report.
/// </summary>
public class CostAnalysis
{
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
    public double TotalMonthlyCostUSD { get; set; }
    public double ProjectedAnnualCostUSD { get; set; }
    public double CostPerComputeUnitUSD { get; set; }
    public int TotalResourcesDeployed { get; set; }
    public double UnderutilizationPercentage { get; set; }
    public double EstimatedWasteUSD { get; set; }
    public Dictionary<string, double> CostByRegion { get; set; } = [];
    public Dictionary<string, double> CostByService { get; set; } = [];
}

/// <summary>
/// Cost for a specific service.
/// </summary>
public class ServiceCost
{
    public string ServiceName { get; set; }
    public string ServiceType { get; set; } // Compute, Storage, Network, Database
    public double MonthlyCostUSD { get; set; }
    public double PercentOfTotal { get; set; }
    public int InstanceCount { get; set; }
    public double CostPerInstance { get; set; }
    public bool IsOptimizable { get; set; }
}

/// <summary>
/// Cost for a specific region.
/// </summary>
public class RegionCost
{
    public string RegionName { get; set; }
    public double MonthlyCostUSD { get; set; }
    public double PercentOfTotal { get; set; }
    public int ResourceCount { get; set; }
    public double AverageCostPerResource { get; set; }
    public bool IsExpensive { get; set; }
    public string CostTrendDirection { get; set; } // Up, Down, Stable
}

/// <summary>
/// Cost optimization recommendation.
/// </summary>
public class CostOptimizationRecommendation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; }
    public string Description { get; set; }
    public double EstimatedMonthlySavingsUSD { get; set; }
    public int Priority { get; set; } // 1-10
    public string Category { get; set; } // RightSizing, Scheduling, Deletion, Migration
    public string ImplementationEffort { get; set; } // Low, Medium, High
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsImplemented { get; set; }
}

/// <summary>
/// Savings estimate for an optimization.
/// </summary>
public class SavingsEstimate
{
    public string OptimizationId { get; set; }
    public double EstimatedMonthlySavingsUSD { get; set; }
    public double EstimatedAnnualSavingsUSD { get; set; }
    public int ImplementationDays { get; set; }
    public double PaybackPeriodMonths { get; set; }
    public int AffectedResources { get; set; }
    public string ImpactAssessment { get; set; }
}

/// <summary>
/// Cost trends over time.
/// </summary>
public class CostTrends
{
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan Period { get; set; }
    public List<double> MonthlyCosts { get; set; } = [];
    public List<string> Months { get; set; } = [];
    public double AverageMonthlyGrowth { get; set; }
    public double CostTrendPercentage { get; set; }
    public string Trend { get; set; } // Increasing, Decreasing, Stable
    public List<string> MainCostDrivers { get; set; } = [];
}

/// <summary>
/// ROI calculation for cost optimizations.
/// </summary>
public class ROICalculation
{
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    public List<string> OptimizationIds { get; set; } = [];
    public double TotalImplementationCostUSD { get; set; }
    public double TotalMonthlySavingsUSD { get; set; }
    public double TotalAnnualSavingsUSD { get; set; }
    public double ROIPercentage { get; set; }
    public double PaybackPeriodMonths { get; set; }
    public int BreakEvenMonths { get; set; }
    public bool IsPositiveROI { get; set; }
}
