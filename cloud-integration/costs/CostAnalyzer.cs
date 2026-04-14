using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace HELIOS.CloudIntegration.Costs
{
    /// <summary>
    /// Cost tracking and optimization for cloud services
    /// </summary>
    public interface ICostAnalyzer
    {
        Task<CostReport> GenerateReportAsync(CostReportRequest request);
        Task<List<CostOptimization>> AnalyzeOptimizationsAsync();
        Task<bool> AlertIfExceededAsync(string service, decimal threshold);
        Task<UsageMetrics> GetUsageMetricsAsync(string service, DateRange period);
    }

    /// <summary>
    /// Cost report request
    /// </summary>
    public class CostReportRequest
    {
        public DateRange Period { get; set; }
        public string[] Services { get; set; }
        public string GroupBy { get; set; } = "service"; // service, project, costcenter, date
        public bool IncludeProjections { get; set; } = true;
        public bool IncludeOptimizations { get; set; } = true;
    }

    /// <summary>
    /// Date range for reporting
    /// </summary>
    public class DateRange
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public DateRange(DateTime start, DateTime end)
        {
            StartDate = start;
            EndDate = end;
        }

        public static DateRange Current() => new(
            new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
            DateTime.Now);

        public static DateRange LastMonth()
        {
            var today = DateTime.Now;
            var firstDayOfCurrentMonth = new DateTime(today.Year, today.Month, 1);
            var lastDayOfPreviousMonth = firstDayOfCurrentMonth.AddDays(-1);
            var firstDayOfPreviousMonth = lastDayOfPreviousMonth.AddDays(-(lastDayOfPreviousMonth.Day - 1));
            return new(firstDayOfPreviousMonth, lastDayOfPreviousMonth);
        }
    }

    /// <summary>
    /// Comprehensive cost report
    /// </summary>
    public class CostReport
    {
        public DateRange Period { get; set; }
        public decimal TotalCost { get; set; }
        public decimal ProjectedMonthlyCost { get; set; }
        public Dictionary<string, CostBreakdown> ServiceCosts { get; set; } = new();
        public List<CostTrend> Trends { get; set; } = new();
        public List<CostOptimization> Recommendations { get; set; } = new();
        public Dictionary<string, decimal> BudgetStatus { get; set; } = new();
        public List<CostAlert> Alerts { get; set; } = new();

        public decimal AverageDailyCost => Period.EndDate > Period.StartDate
            ? TotalCost / (decimal)(Period.EndDate - Period.StartDate).TotalDays
            : 0;
    }

    /// <summary>
    /// Service cost breakdown
    /// </summary>
    public class CostBreakdown
    {
        public string Service { get; set; }
        public decimal TotalCost { get; set; }
        public Dictionary<string, decimal> CategoryCosts { get; set; } = new(); // compute, storage, data, etc.
        public long UnitsUsed { get; set; }
        public decimal CostPerUnit { get; set; }
        public List<UsageRecord> UsageRecords { get; set; } = new();

        public decimal PercentageOfTotal { get; set; }
    }

    /// <summary>
    /// Usage record
    /// </summary>
    public class UsageRecord
    {
        public DateTime Date { get; set; }
        public string Category { get; set; }
        public long Quantity { get; set; }
        public string Unit { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalCost { get; set; }
    }

    /// <summary>
    /// Cost trend over time
    /// </summary>
    public class CostTrend
    {
        public string Service { get; set; }
        public DateTime Date { get; set; }
        public decimal DailyCost { get; set; }
        public decimal MonthlyCostProjection { get; set; }
        public decimal PercentageChange { get; set; }
    }

    /// <summary>
    /// Cost optimization recommendation
    /// </summary>
    public class CostOptimization
    {
        public string Id { get; set; }
        public string Service { get; set; }
        public string Category { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public decimal CurrentCost { get; set; }
        public decimal ProjectedSavings { get; set; }
        public decimal PercentageSavings => CurrentCost > 0 ? (ProjectedSavings / CurrentCost) * 100 : 0;
        public OptimizationPriority Priority { get; set; }
        public string Implementation { get; set; }
        public int ImplementationDays { get; set; }
        public List<string> Benefits { get; set; } = new();
        public List<string> Risks { get; set; } = new();
    }

    /// <summary>
    /// Cost alert
    /// </summary>
    public class CostAlert
    {
        public string Id { get; set; }
        public DateTime AlertTime { get; set; }
        public AlertSeverity Severity { get; set; }
        public string Service { get; set; }
        public string Message { get; set; }
        public decimal CurrentCost { get; set; }
        public decimal ThresholdCost { get; set; }
        public decimal PercentageOver => ThresholdCost > 0 ? ((CurrentCost - ThresholdCost) / ThresholdCost) * 100 : 0;
    }

    /// <summary>
    /// Usage metrics
    /// </summary>
    public class UsageMetrics
    {
        public string Service { get; set; }
        public DateRange Period { get; set; }
        public Dictionary<string, long> MetricsByType { get; set; } = new();
        public List<HourlyUsage> HourlyData { get; set; } = new();
        public decimal AverageHourlyUsage { get; set; }
        public decimal PeakHourlyUsage { get; set; }
        public decimal? EstimatedMonthlyCost { get; set; }
    }

    /// <summary>
    /// Hourly usage data
    /// </summary>
    public class HourlyUsage
    {
        public DateTime Hour { get; set; }
        public long UsageAmount { get; set; }
        public decimal Cost { get; set; }
    }

    /// <summary>
    /// Cost analyzer implementation
    /// </summary>
    public class CostAnalyzer : ICostAnalyzer
    {
        private readonly IUsageTracker _usageTracker;
        private readonly IPricingProvider _pricingProvider;
        private readonly IBudgetManager _budgetManager;
        private readonly ILogger _logger;

        public CostAnalyzer(
            IUsageTracker usageTracker,
            IPricingProvider pricingProvider,
            IBudgetManager budgetManager,
            ILogger logger)
        {
            _usageTracker = usageTracker;
            _pricingProvider = pricingProvider;
            _budgetManager = budgetManager;
            _logger = logger;
        }

        public async Task<CostReport> GenerateReportAsync(CostReportRequest request)
        {
            var report = new CostReport { Period = request.Period };

            try
            {
                // Get usage for each service
                foreach (var service in request.Services ?? new[] { "*" })
                {
                    var metrics = await _usageTracker.GetUsageAsync(service, request.Period);
                    var pricing = await _pricingProvider.GetPricingAsync(service);

                    var costBreakdown = CalculateCostBreakdown(metrics, pricing);
                    report.ServiceCosts[service] = costBreakdown;
                    report.TotalCost += costBreakdown.TotalCost;

                    // Check budget
                    var budget = await _budgetManager.GetBudgetAsync(service);
                    if (budget != null)
                    {
                        report.BudgetStatus[service] = (costBreakdown.TotalCost / budget.Limit) * 100;
                        
                        if (costBreakdown.TotalCost > budget.Limit * 0.9m)
                        {
                            report.Alerts.Add(new CostAlert
                            {
                                Service = service,
                                Severity = AlertSeverity.Warning,
                                CurrentCost = costBreakdown.TotalCost,
                                ThresholdCost = budget.Limit * 0.9m,
                                Message = $"{service} approaching budget limit"
                            });
                        }
                    }
                }

                // Project monthly costs
                if (request.IncludeProjections)
                {
                    report.ProjectedMonthlyCost = ProjectMonthlyCost(report.Period, report.TotalCost);
                }

                // Generate recommendations
                if (request.IncludeOptimizations)
                {
                    report.Recommendations = await AnalyzeOptimizationsAsync();
                }

                _logger.Info($"Cost report generated: ${report.TotalCost:F2} in period");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error generating cost report: {ex.Message}");
            }

            return report;
        }

        public async Task<List<CostOptimization>> AnalyzeOptimizationsAsync()
        {
            var optimizations = new List<CostOptimization>();

            try
            {
                // Analyze each service for optimization opportunities
                var services = new[] { "azure", "openai", "github", "fabric", "office365" };

                foreach (var service in services)
                {
                    var metrics = await _usageTracker.GetUsageAsync(service, DateRange.Current());
                    var suggestions = GenerateOptimizations(service, metrics);
                    optimizations.AddRange(suggestions);
                }

                // Sort by potential savings
                optimizations = optimizations.OrderByDescending(o => o.ProjectedSavings).ToList();
            }
            catch (Exception ex)
            {
                _logger.Error($"Error analyzing optimizations: {ex.Message}");
            }

            return optimizations;
        }

        public async Task<bool> AlertIfExceededAsync(string service, decimal threshold)
        {
            try
            {
                var metrics = await _usageTracker.GetUsageAsync(service, DateRange.Current());
                var pricing = await _pricingProvider.GetPricingAsync(service);
                var costBreakdown = CalculateCostBreakdown(metrics, pricing);

                if (costBreakdown.TotalCost > threshold)
                {
                    _logger.Error($"ALERT: {service} cost (${costBreakdown.TotalCost:F2}) exceeds threshold (${threshold:F2})");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error checking threshold: {ex.Message}");
                return false;
            }
        }

        public async Task<UsageMetrics> GetUsageMetricsAsync(string service, DateRange period)
        {
            try
            {
                return await _usageTracker.GetUsageAsync(service, period);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error retrieving usage metrics: {ex.Message}");
                return new UsageMetrics { Service = service, Period = period };
            }
        }

        private CostBreakdown CalculateCostBreakdown(UsageMetrics metrics, ServicePricing pricing)
        {
            var breakdown = new CostBreakdown
            {
                Service = metrics.Service,
                UnitsUsed = metrics.MetricsByType.Values.Sum()
            };

            foreach (var metricType in metrics.MetricsByType)
            {
                var unitCost = pricing.GetUnitPrice(metricType.Key);
                var categoryCost = metricType.Value * unitCost;
                breakdown.CategoryCosts[metricType.Key] = categoryCost;
                breakdown.TotalCost += categoryCost;
            }

            breakdown.CostPerUnit = breakdown.UnitsUsed > 0 ? breakdown.TotalCost / breakdown.UnitsUsed : 0;
            return breakdown;
        }

        private decimal ProjectMonthlyCost(DateRange period, decimal currentCost)
        {
            var daysInPeriod = (period.EndDate - period.StartDate).TotalDays;
            var daysInMonth = 30m;
            return daysInPeriod > 0 ? (currentCost / (decimal)daysInPeriod) * daysInMonth : 0;
        }

        private List<CostOptimization> GenerateOptimizations(string service, UsageMetrics metrics)
        {
            var optimizations = new List<CostOptimization>();

            // Reserved instances recommendation
            if (metrics.EstimatedMonthlyCost > 500)
            {
                optimizations.Add(new CostOptimization
                {
                    Title = $"Reserved Capacity for {service}",
                    Description = $"Consider reserved instances for {service} to save 30-40%",
                    CurrentCost = metrics.EstimatedMonthlyCost ?? 0,
                    ProjectedSavings = (metrics.EstimatedMonthlyCost ?? 0) * 0.35m,
                    Priority = OptimizationPriority.High,
                    Implementation = "Purchase 1-year or 3-year reserved instances",
                    ImplementationDays = 1,
                    Benefits = new() { "30-40% cost reduction", "Predictable costs" },
                    Risks = new() { "Upfront commitment" }
                });
            }

            // Auto-scaling optimization
            if (metrics.PeakHourlyUsage > metrics.AverageHourlyUsage * 2)
            {
                optimizations.Add(new CostOptimization
                {
                    Title = $"Auto-scaling for {service}",
                    Description = "Usage varies significantly; enable auto-scaling to match demand",
                    CurrentCost = metrics.EstimatedMonthlyCost ?? 0,
                    ProjectedSavings = (metrics.EstimatedMonthlyCost ?? 0) * 0.15m,
                    Priority = OptimizationPriority.Medium,
                    Implementation = "Enable auto-scaling policies based on usage metrics",
                    ImplementationDays = 3,
                    Benefits = new() { "15-20% cost reduction", "Better performance", "On-demand scaling" },
                    Risks = new() { "Potential performance lag", "Scaling latency" }
                });
            }

            return optimizations;
        }
    }

    // Supporting interfaces and enums
    public enum OptimizationPriority { Low, Medium, High, Critical }
    public enum AlertSeverity { Info, Warning, Error, Critical }

    public interface IUsageTracker
    {
        Task<UsageMetrics> GetUsageAsync(string service, DateRange period);
    }

    public interface IPricingProvider
    {
        Task<ServicePricing> GetPricingAsync(string service);
    }

    public interface IBudgetManager
    {
        Task<Budget> GetBudgetAsync(string service);
        Task SetBudgetAsync(string service, decimal limit);
    }

    public class ServicePricing
    {
        public Dictionary<string, decimal> UnitPrices { get; set; } = new();

        public decimal GetUnitPrice(string type)
        {
            return UnitPrices.TryGetValue(type, out var price) ? price : 0;
        }
    }

    public class Budget
    {
        public string Service { get; set; }
        public decimal Limit { get; set; }
        public decimal Spent { get; set; }
        public DateTime ResetDate { get; set; }
    }

    public interface ILogger
    {
        void Info(string message);
        void Warn(string message);
        void Error(string message);
    }
}
