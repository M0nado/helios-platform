namespace HELIOS.Platform.Optimization.CostOptimization;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class ResourceCost
{
    public string ResourceId { get; set; }
    public string ResourceType { get; set; } // Compute, Storage, Database, Network, etc.
    public string ResourceName { get; set; }
    public double DailyCost { get; set; }
    public double MonthlyCost { get; set; }
    public double AnnualCost { get; set; }
    public double UtilizationPercentage { get; set; }
    public DateTime MeasuredAt { get; set; } = DateTime.UtcNow;
}

public class CostOptimizationRecommendation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ResourceId { get; set; }
    public string RecommendationType { get; set; } // Resize, Shutdown, Reserve, etc.
    public string Description { get; set; }
    public double EstimatedMonthlySavings { get; set; }
    public double EstimatedAnnualSavings { get; set; }
    public int Priority { get; set; } // 1-10
    public string Action { get; set; }
    public DateTime SuggestedAt { get; set; } = DateTime.UtcNow;
}

public class CostOptimizationResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public double CostBefore { get; set; }
    public double CostAfter { get; set; }
    public double MonthlySavings { get; set; }
    public double AnnualSavings { get; set; }
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
}

public interface ICostOptimizer
{
    Task<List<ResourceCost>> GetResourceCostsAsync();
    Task RecordResourceCostAsync(ResourceCost cost);
    Task<List<CostOptimizationRecommendation>> AnalyzeCostsAsync();
    Task<CostOptimizationResult> ApplyCostOptimizationAsync(string recommendationId);
    Task<CostReport> GenerateCostReportAsync();
}

public class CostReport
{
    public DateTime ReportPeriod { get; set; }
    public double TotalDailyCost { get; set; }
    public double TotalMonthlyCost { get; set; }
    public double TotalAnnualCost { get; set; }
    public Dictionary<string, double> CostByResourceType { get; set; } = new();
    public Dictionary<string, double> UtilizationByResourceType { get; set; } = new();
    public List<CostOptimizationRecommendation> Recommendations { get; set; } = new();
    public double EstimatedAnnualSavingsPotential { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class CostOptimizer : ICostOptimizer
{
    private readonly List<ResourceCost> _resourceCosts = new();
    private readonly Dictionary<string, CostOptimizationResult> _appliedOptimizations = new();
    private readonly object _lockObj = new();

    public async Task<List<ResourceCost>> GetResourceCostsAsync()
    {
        return await Task.Run(() =>
        {
            lock (_lockObj)
            {
                return _resourceCosts.OrderByDescending(r => r.MonthlyCost).ToList();
            }
        });
    }

    public async Task RecordResourceCostAsync(ResourceCost cost)
    {
        if (cost == null) throw new ArgumentNullException(nameof(cost));

        await Task.Run(() =>
        {
            lock (_lockObj)
            {
                _resourceCosts.Add(cost);
                
                // Keep only last 90 days
                var ninetyDaysAgo = DateTime.UtcNow.AddDays(-90);
                _resourceCosts.RemoveAll(r => r.MeasuredAt < ninetyDaysAgo);
            }
        });
    }

    public async Task<List<CostOptimizationRecommendation>> AnalyzeCostsAsync()
    {
        return await Task.Run(() =>
        {
            lock (_lockObj)
            {
                var recommendations = new List<CostOptimizationRecommendation>();

                var groupedByCost = _resourceCosts
                    .GroupBy(r => r.ResourceId)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.MeasuredAt).First());

                foreach (var (resourceId, cost) in groupedByCost)
                {
                    // Identify underutilized resources
                    if (cost.UtilizationPercentage < 20)
                    {
                        recommendations.Add(new CostOptimizationRecommendation
                        {
                            ResourceId = resourceId,
                            RecommendationType = "Shutdown",
                            Description = $"{cost.ResourceName} is only {cost.UtilizationPercentage}% utilized",
                            EstimatedMonthlySavings = cost.MonthlyCost,
                            EstimatedAnnualSavings = cost.AnnualCost,
                            Priority = 9,
                            Action = $"Terminate {cost.ResourceName} or reassign workload"
                        });
                    }
                    else if (cost.UtilizationPercentage < 50)
                    {
                        recommendations.Add(new CostOptimizationRecommendation
                        {
                            ResourceId = resourceId,
                            RecommendationType = "Resize",
                            Description = $"{cost.ResourceName} can be downsized from {cost.UtilizationPercentage}% utilization",
                            EstimatedMonthlySavings = cost.MonthlyCost * 0.4,
                            EstimatedAnnualSavings = cost.AnnualCost * 0.4,
                            Priority = 7,
                            Action = $"Right-size {cost.ResourceName} to smaller instance type"
                        });
                    }

                    // Recommend reserved instances for consistent workloads
                    if (cost.UtilizationPercentage > 70 && cost.MonthlyCost > 500)
                    {
                        recommendations.Add(new CostOptimizationRecommendation
                        {
                            ResourceId = resourceId,
                            RecommendationType = "Reserve",
                            Description = $"{cost.ResourceName} is consistently used, good candidate for reserved capacity",
                            EstimatedMonthlySavings = cost.MonthlyCost * 0.30,
                            EstimatedAnnualSavings = cost.AnnualCost * 0.30,
                            Priority = 6,
                            Action = $"Purchase 1-year or 3-year reserved capacity for {cost.ResourceName}"
                        });
                    }
                }

                return recommendations.OrderByDescending(r => r.Priority).ToList();
            }
        });
    }

    public async Task<CostOptimizationResult> ApplyCostOptimizationAsync(string recommendationId)
    {
        return await Task.Run(() =>
        {
            lock (_lockObj)
            {
                var result = new CostOptimizationResult();
                var totalBefore = _resourceCosts.Sum(r => r.MonthlyCost);

                // Simulate optimization
                result.CostBefore = totalBefore;
                result.CostAfter = totalBefore * 0.75; // 25% reduction
                result.MonthlySavings = result.CostBefore - result.CostAfter;
                result.AnnualSavings = result.MonthlySavings * 12;
                result.Success = true;
                result.Message = $"Applied cost optimization {recommendationId}";

                _appliedOptimizations[recommendationId] = result;

                return result;
            }
        });
    }

    public async Task<CostReport> GenerateCostReportAsync()
    {
        var costs = await GetResourceCostsAsync();
        var recommendations = await AnalyzeCostsAsync();

        var report = new CostReport
        {
            ReportPeriod = DateTime.UtcNow.AddDays(-30),
            TotalDailyCost = costs.Sum(c => c.DailyCost),
            TotalMonthlyCost = costs.Sum(c => c.MonthlyCost),
            TotalAnnualCost = costs.Sum(c => c.AnnualCost),
            Recommendations = recommendations
        };

        // Group by resource type
        report.CostByResourceType = costs
            .GroupBy(c => c.ResourceType)
            .ToDictionary(g => g.Key, g => g.Sum(c => c.MonthlyCost));

        report.UtilizationByResourceType = costs
            .GroupBy(c => c.ResourceType)
            .ToDictionary(g => g.Key, g => g.Average(c => c.UtilizationPercentage));

        report.EstimatedAnnualSavingsPotential = recommendations.Sum(r => r.EstimatedAnnualSavings);

        return report;
    }
}

public class BudgetAlert
{
    public string BudgetName { get; set; }
    public double DailyLimit { get; set; }
    public double MonthlyLimit { get; set; }
    public double CurrentDailySpend { get; set; }
    public double CurrentMonthlySpend { get; set; }
    public bool IsDailyLimitExceeded { get; set; }
    public bool IsMonthlyLimitExceeded { get; set; }
    public DateTime AlertTime { get; set; } = DateTime.UtcNow;
}

public class BudgetMonitor
{
    private readonly Dictionary<string, BudgetAlert> _budgets = new();
    private readonly object _lockObj = new();

    public async Task<BudgetAlert> CheckBudgetAsync(string budgetName, double currentSpend)
    {
        return await Task.Run(() =>
        {
            lock (_lockObj)
            {
                if (!_budgets.TryGetValue(budgetName, out var budget))
                {
                    budget = new BudgetAlert { BudgetName = budgetName };
                    _budgets[budgetName] = budget;
                }

                budget.CurrentDailySpend = currentSpend;
                budget.IsDailyLimitExceeded = currentSpend > budget.DailyLimit;

                return budget;
            }
        });
    }
}
