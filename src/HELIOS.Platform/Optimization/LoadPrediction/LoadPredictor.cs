namespace HELIOS.Platform.Optimization.LoadPrediction;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class LoadForecast
{
    public DateTime ForecastPeriod { get; set; }
    public double PredictedLoad { get; set; }
    public double Confidence { get; set; } // 0-100
    public double UpperBound { get; set; }
    public double LowerBound { get; set; }
    public List<string> CapacityRecommendations { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class LoadDataPoint
{
    public DateTime Timestamp { get; set; }
    public double CpuUsage { get; set; }
    public double MemoryUsage { get; set; }
    public double DiskIO { get; set; }
    public double NetworkIO { get; set; }
    public int ActiveConnections { get; set; }
    public double RequestsPerSecond { get; set; }
}

public interface ILoadPredictor
{
    Task RecordLoadDataAsync(LoadDataPoint dataPoint);
    Task<LoadForecast> PredictLoadAsync(DateTime forecastPeriod);
    Task<CapacityPlan> GenerateCapacityPlanAsync();
    Task<List<string>> GetScalingRecommendationsAsync();
}

public class CapacityPlan
{
    public DateTime PlanDate { get; set; } = DateTime.UtcNow;
    public int CurrentCapacity { get; set; }
    public int RecommendedCapacity { get; set; }
    public int BufferPercentage { get; set; } = 20; // 20% headroom
    public TimeSpan ImplementationTimeline { get; set; }
    public List<ResourceAllocation> Allocations { get; set; } = new();
    public Dictionary<string, int> InstanceCounts { get; set; } = new();
}

public class ResourceAllocation
{
    public string ResourceType { get; set; } // CPU, Memory, Disk, Network
    public int Current { get; set; }
    public int Recommended { get; set; }
    public string Justification { get; set; }
}

public class LoadPredictor : ILoadPredictor
{
    private readonly List<LoadDataPoint> _historicalData = new();
    private readonly object _lockObj = new();

    public async Task RecordLoadDataAsync(LoadDataPoint dataPoint)
    {
        if (dataPoint == null) throw new ArgumentNullException(nameof(dataPoint));

        await Task.Run(() =>
        {
            lock (_lockObj)
            {
                _historicalData.Add(dataPoint);
                
                // Keep only last 30 days of data
                var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
                var toRemove = _historicalData.Where(d => d.Timestamp < thirtyDaysAgo).ToList();
                foreach (var point in toRemove)
                {
                    _historicalData.Remove(point);
                }
            }
        });
    }

    public async Task<LoadForecast> PredictLoadAsync(DateTime forecastPeriod)
    {
        return await Task.Run(() =>
        {
            lock (_lockObj)
            {
                var forecast = new LoadForecast { ForecastPeriod = forecastPeriod };

                if (_historicalData.Count < 2)
                {
                    forecast.Confidence = 0;
                    forecast.PredictedLoad = 50; // Default
                    return forecast;
                }

                // Simple linear regression prediction
                var cpuValues = _historicalData.Select(d => d.CpuUsage).ToList();
                var memValues = _historicalData.Select(d => d.MemoryUsage).ToList();

                var cpuTrend = CalculateTrend(cpuValues);
                var memTrend = CalculateTrend(memValues);

                forecast.PredictedLoad = (cpuValues.Average() + memValues.Average()) / 2;
                
                // Calculate confidence based on data consistency
                var cpuStdDev = CalculateStdDev(cpuValues, cpuValues.Average());
                forecast.Confidence = Math.Min(95, 100 - (cpuStdDev / cpuValues.Average()) * 50);

                // Calculate bounds
                var margin = forecast.PredictedLoad * 0.2;
                forecast.UpperBound = forecast.PredictedLoad + margin;
                forecast.LowerBound = Math.Max(0, forecast.PredictedLoad - margin);

                // Generate recommendations
                if (forecast.PredictedLoad > 80)
                {
                    forecast.CapacityRecommendations.Add("Scale up compute resources");
                    forecast.CapacityRecommendations.Add("Consider horizontal scaling");
                }
                else if (forecast.PredictedLoad > 60)
                {
                    forecast.CapacityRecommendations.Add("Monitor closely for spikes");
                    forecast.CapacityRecommendations.Add("Prepare scaling playbook");
                }
                else if (forecast.PredictedLoad < 30)
                {
                    forecast.CapacityRecommendations.Add("Opportunity for cost optimization");
                    forecast.CapacityRecommendations.Add("Consider rightsizing resources");
                }

                return forecast;
            }
        });
    }

    public async Task<CapacityPlan> GenerateCapacityPlanAsync()
    {
        var forecast = await PredictLoadAsync(DateTime.UtcNow.AddDays(7));

        var plan = new CapacityPlan
        {
            CurrentCapacity = 100,
            RecommendedCapacity = (int)(forecast.UpperBound * 1.2) // Add 20% buffer
        };

        plan.Allocations.Add(new ResourceAllocation
        {
            ResourceType = "CPU",
            Current = 100,
            Recommended = plan.RecommendedCapacity,
            Justification = $"Based on load forecast of {forecast.PredictedLoad:F2}"
        });

        plan.Allocations.Add(new ResourceAllocation
        {
            ResourceType = "Memory",
            Current = 100,
            Recommended = (int)(plan.RecommendedCapacity * 0.8),
            Justification = "Proportional to CPU allocation"
        });

        plan.InstanceCounts["WebServers"] = Math.Max(2, plan.RecommendedCapacity / 50);
        plan.InstanceCounts["DatabaseServers"] = 2;
        plan.InstanceCounts["CacheServers"] = Math.Max(1, plan.RecommendedCapacity / 100);

        plan.ImplementationTimeline = TimeSpan.FromHours(2);

        return plan;
    }

    public async Task<List<string>> GetScalingRecommendationsAsync()
    {
        var forecast = await PredictLoadAsync(DateTime.UtcNow.AddDays(1));
        var plan = await GenerateCapacityPlanAsync();

        var recommendations = new List<string>();

        if (forecast.PredictedLoad > 80)
        {
            recommendations.Add("URGENT: Scale up immediately to prevent service degradation");
        }
        else if (forecast.PredictedLoad > 70)
        {
            recommendations.Add("Plan to scale up within 24 hours");
        }

        if (plan.RecommendedCapacity > plan.CurrentCapacity * 1.5)
        {
            recommendations.Add($"Significant capacity increase recommended ({plan.CurrentCapacity} → {plan.RecommendedCapacity})");
        }

        recommendations.AddRange(forecast.CapacityRecommendations);

        return recommendations;
    }

    private double CalculateTrend(List<double> values)
    {
        if (values.Count < 2) return 0;

        var n = values.Count;
        var xSum = Enumerable.Range(0, n).Sum(i => (double)i);
        var ySum = values.Sum();
        var xySum = Enumerable.Range(0, n).Sum(i => i * values[i]);
        var x2Sum = Enumerable.Range(0, n).Sum(i => i * i);

        var slope = (n * xySum - xSum * ySum) / (n * x2Sum - xSum * xSum);
        return slope;
    }

    private double CalculateStdDev(List<double> values, double mean)
    {
        if (values.Count <= 1) return 0;
        var sumOfSquares = values.Sum(v => Math.Pow(v - mean, 2));
        return Math.Sqrt(sumOfSquares / (values.Count - 1));
    }
}

public class DemandForecaster
{
    private readonly List<(DateTime date, double demand)> _historicalDemand = new();

    public async Task<Dictionary<string, object>> ForecastDemandAsync(int forecastDays)
    {
        return await Task.Run(() =>
        {
            var forecast = new Dictionary<string, object>();
            
            if (_historicalDemand.Count < 7)
            {
                forecast["confidence"] = 0;
                return forecast;
            }

            // Extract day-of-week patterns
            var byDayOfWeek = _historicalDemand
                .GroupBy(d => d.date.DayOfWeek)
                .ToDictionary(g => g.Key, g => g.Average(d => d.demand));

            forecast["daily_patterns"] = byDayOfWeek;
            forecast["trend"] = _historicalDemand.TakeLast(7).Average(d => d.demand);
            forecast["forecast_days"] = forecastDays;

            return forecast;
        });
    }
}
