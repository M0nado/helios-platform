using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HELIOS.Platform.Core.Global.Interfaces;
using HELIOS.Platform.Core.Logging;

namespace HELIOS.Platform.Core.Global;

/// <summary>
/// Predictive capacity management service implementation.
/// </summary>
public class CapacityPlanner : ICapacityPlanner
{
    private readonly Core.Logging.ILogger _logger;
    private readonly ReaderWriterLockSlim _capacityLock = new();
    private readonly Dictionary<string, RegionalCapacity> _regionalCapacity = new();

    public CapacityPlanner(Core.Logging.ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.Info("CapacityPlanner initialized");
        InitializeDefaultCapacity();
    }

    /// <summary>Analyze current capacity utilization.</summary>
    public async Task<CapacityAnalysis> AnalyzeCapacityAsync()
    {
        try
        {
            _capacityLock.EnterReadLock();
            var analysis = new CapacityAnalysis { AnalyzedAt = DateTime.UtcNow };

            analysis.TotalCapacityUnits = _regionalCapacity.Values.Sum(r => r.TotalCapacity);
            analysis.UsedCapacityUnits = _regionalCapacity.Values.Sum(r => r.UsedCapacity);
            analysis.AvailableCapacityUnits = analysis.TotalCapacityUnits - analysis.UsedCapacityUnits;
            analysis.OverallUtilizationPercent = (analysis.UsedCapacityUnits / (double)analysis.TotalCapacityUnits) * 100;

            foreach (var region in _regionalCapacity.Values)
            {
                var utilization = (region.UsedCapacity / (double)region.TotalCapacity) * 100;
                analysis.RegionalUtilization[region.RegionName] = utilization;
                if (utilization > 85)
                {
                    analysis.CriticalRegions.Add(region.RegionName);
                }
            }

            analysis.IsCapacityConstrained = analysis.OverallUtilizationPercent > 80;
            _logger.Debug($"Capacity analysis: {analysis.OverallUtilizationPercent:F1}% utilized");

            return await Task.FromResult(analysis);
        }
        finally
        {
            _capacityLock.ExitReadLock();
        }
    }

    /// <summary>Get capacity forecast for a specific region.</summary>
    public async Task<CapacityForecast> ForecastCapacityAsync(string regionName, int forecastDaysAhead)
    {
        try
        {
            var forecast = new CapacityForecast
            {
                RegionName = regionName,
                ForecastAt = DateTime.UtcNow,
                DaysAhead = forecastDaysAhead
            };

            var random = new Random();
            double currentUtilization = 60;

            for (int i = 0; i < forecastDaysAhead; i++)
            {
                currentUtilization = Math.Min(currentUtilization + random.NextDouble() * 2, 95);
                forecast.Projections.Add(new DailyCapacityProjection
                {
                    Date = DateTime.UtcNow.AddDays(i),
                    ProjectedUtilizationPercent = currentUtilization,
                    RequiredCapacityUnits = (int)(1000 * currentUtilization / 100),
                    Confidence = 0.95 - (i * 0.01),
                    Status = currentUtilization > 85 ? "Warning" : currentUtilization > 95 ? "Critical" : "Healthy"
                });
            }

            forecast.PeakUtilizationPercent = forecast.Projections.Max(p => p.ProjectedUtilizationPercent);
            forecast.DaysUntilCapacityFull = forecast.Projections.FirstOrDefault(p => p.ProjectedUtilizationPercent > 95)?.Date.Subtract(DateTime.UtcNow).Days ?? 30;
            forecast.CapacityStatus = forecast.PeakUtilizationPercent > 90 ? "Critical" : forecast.PeakUtilizationPercent > 80 ? "Warning" : "Healthy";
            forecast.Recommendations = new List<string> { "Monitor closely", "Consider scaling" };

            return await Task.FromResult(forecast);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error forecasting capacity: {ex.Message}", ex);
            return new CapacityForecast { RegionName = regionName };
        }
    }

    /// <summary>Identify capacity bottlenecks.</summary>
    public async Task<List<CapacityBottleneck>> IdentifyBottlenecksAsync()
    {
        try
        {
            var bottlenecks = new List<CapacityBottleneck>();
            _capacityLock.EnterReadLock();

            foreach (var region in _regionalCapacity.Values)
            {
                var utilization = (region.UsedCapacity / (double)region.TotalCapacity) * 100;
                if (utilization > 70)
                {
                    bottlenecks.Add(new CapacityBottleneck
                    {
                        RegionName = region.RegionName,
                        ResourceType = "CPU",
                        CurrentUtilizationPercent = utilization,
                        Severity = utilization > 90 ? 5 : utilization > 80 ? 4 : 3,
                        DaysUntilFull = (int)(30 * (1 - (100 - utilization) / 100)),
                        RecommendedAction = "Scale horizontally or optimize workload"
                    });
                }
            }

            _logger.Info($"Identified {bottlenecks.Count} capacity bottlenecks");
            return await Task.FromResult(bottlenecks);
        }
        finally
        {
            _capacityLock.ExitReadLock();
        }
    }

    /// <summary>Generate capacity scaling recommendations.</summary>
    public async Task<List<ScalingRecommendation>> GetScalingRecommendationsAsync()
    {
        try
        {
            var recommendations = new List<ScalingRecommendation>();
            var analysis = await AnalyzeCapacityAsync();

            foreach (var region in analysis.CriticalRegions)
            {
                recommendations.Add(new ScalingRecommendation
                {
                    RegionName = region,
                    ScalingType = "HorizontalScale",
                    InstancesRequired = 5,
                    InstanceType = "t3.large",
                    EstimatedCostUSD = 2500.00,
                    Priority = 10,
                    DaysUntilRequired = 7
                });
            }

            _logger.Info($"Generated {recommendations.Count} scaling recommendations");
            return await Task.FromResult(recommendations);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error generating scaling recommendations: {ex.Message}", ex);
            return new List<ScalingRecommendation>();
        }
    }

    /// <summary>Calculate required capacity for projected workload.</summary>
    public async Task<CapacityRequirement> CalculateRequiredCapacityAsync(WorkloadProjection projection)
    {
        try
        {
            var requirement = new CapacityRequirement
            {
                CalculatedAt = DateTime.UtcNow,
                Projection = projection,
                RequiredComputeUnits = (int)(projection.ExpectedActiveUsers * 0.5),
                RequiredStorageBytes = projection.ExpectedDataStorageBytes,
                RequiredNetworkGbps = projection.ExpectedNetworkBandwidthGbps,
                RecommendedInstances = (int)Math.Ceiling(projection.ExpectedTransactionsPerSecond / 100),
                EstimatedCostUSD = Math.Ceiling(projection.ExpectedTransactionsPerSecond / 100) * 1000
            };

            return await Task.FromResult(requirement);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error calculating capacity requirement: {ex.Message}", ex);
            return new CapacityRequirement();
        }
    }

    /// <summary>Get multi-region capacity balance.</summary>
    public async Task<CapacityBalance> GetCapacityBalanceAsync()
    {
        try
        {
            var balance = new CapacityBalance { CalculatedAt = DateTime.UtcNow };
            _capacityLock.EnterReadLock();

            var utilizations = new List<double>();
            foreach (var region in _regionalCapacity.Values)
            {
                var utilization = (region.UsedCapacity / (double)region.TotalCapacity) * 100;
                balance.UtilizationByRegion[region.RegionName] = utilization;
                utilizations.Add(utilization);

                if (utilization > 80)
                    balance.OverloadedRegions.Add(region.RegionName);
                else if (utilization < 40)
                    balance.UnderutilizedRegions.Add(region.RegionName);
            }

            // Calculate balance index (0-1, where 1 is perfectly balanced)
            if (utilizations.Count > 0)
            {
                var avg = utilizations.Average();
                var variance = utilizations.Sum(u => Math.Pow(u - avg, 2)) / utilizations.Count;
                var stdDev = Math.Sqrt(variance);
                balance.BalanceIndex = Math.Max(0, 1 - (stdDev / 50)); // Normalize
                balance.IsBalanced = balance.BalanceIndex > 0.75;
            }

            balance.RebalancingSuggestions = new List<string> { "Migrate workloads from overloaded to underutilized regions", "Implement auto-scaling" };

            _logger.Debug($"Capacity balance index: {balance.BalanceIndex:F2}");
            return await Task.FromResult(balance);
        }
        finally
        {
            _capacityLock.ExitReadLock();
        }
    }

    /// <summary>Predict resource usage for next 30 days.</summary>
    public async Task<UsageForecast> Get30DayForecastAsync()
    {
        try
        {
            var forecast = new UsageForecast { ForecastAt = DateTime.UtcNow };
            var random = new Random();
            double currentUsage = 50;

            for (int i = 0; i < 30; i++)
            {
                currentUsage = Math.Min(currentUsage + (random.NextDouble() - 0.4), 95);
                forecast.DailyForecasts.Add(new DailyUsageForecast
                {
                    Date = DateTime.UtcNow.AddDays(i),
                    ProjectedUsagePercent = currentUsage,
                    ProjectedUserCount = (int)(10000 + currentUsage * 100),
                    Confidence = 0.95 - (i * 0.01)
                });
            }

            forecast.AverageDailyUsage = forecast.DailyForecasts.Average(d => d.ProjectedUsagePercent);
            forecast.PeakDayUsage = forecast.DailyForecasts.Max(d => d.ProjectedUsagePercent);
            forecast.PeakDay = forecast.DailyForecasts.OrderByDescending(d => d.ProjectedUsagePercent).First().Date;
            forecast.TrendPercentage = ((forecast.DailyForecasts.Last().ProjectedUsagePercent - forecast.DailyForecasts.First().ProjectedUsagePercent) / forecast.DailyForecasts.First().ProjectedUsagePercent) * 100;
            forecast.Trend = forecast.TrendPercentage > 5 ? "Increasing" : forecast.TrendPercentage < -5 ? "Decreasing" : "Stable";

            return await Task.FromResult(forecast);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error generating 30-day forecast: {ex.Message}", ex);
            return new UsageForecast();
        }
    }

    /// <summary>Calculate capacity headroom in each region.</summary>
    public async Task<Dictionary<string, double>> CalculateCapacityHeadroomAsync()
    {
        try
        {
            var headroom = new Dictionary<string, double>();
            _capacityLock.EnterReadLock();

            foreach (var region in _regionalCapacity.Values)
            {
                var utilization = (region.UsedCapacity / (double)region.TotalCapacity) * 100;
                headroom[region.RegionName] = 100 - utilization;
            }

            return await Task.FromResult(headroom);
        }
        finally
        {
            _capacityLock.ExitReadLock();
        }
    }

    private void InitializeDefaultCapacity()
    {
        _regionalCapacity["us-east-1"] = new RegionalCapacity { RegionName = "us-east-1", TotalCapacity = 1000, UsedCapacity = 650 };
        _regionalCapacity["eu-west-1"] = new RegionalCapacity { RegionName = "eu-west-1", TotalCapacity = 800, UsedCapacity = 720 };
        _regionalCapacity["ap-southeast-1"] = new RegionalCapacity { RegionName = "ap-southeast-1", TotalCapacity = 600, UsedCapacity = 300 };
    }

    private class RegionalCapacity
    {
        public string RegionName { get; set; }
        public int TotalCapacity { get; set; }
        public int UsedCapacity { get; set; }
    }
}

