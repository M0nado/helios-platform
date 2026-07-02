namespace HELIOS.Platform.Optimization.Performance;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public interface IPerformanceMetric
{
    string Name { get; }
    double Value { get; }
    DateTime Timestamp { get; }
    string Unit { get; }
}

public interface IPerfTuner
{
    Task RecordMetricAsync(IPerformanceMetric metric);
    Task<List<OptimizationSuggestion>> AnalyzePerformanceAsync();
    Task<OptimizationResult> ApplyOptimizationAsync(string optimizationId);
    Task<bool> ValidateOptimizationAsync(string optimizationId);
    Task RollbackOptimizationAsync(string optimizationId);
}

public class PerformanceMetric : IPerformanceMetric
{
    public string Name { get; set; }
    public double Value { get; set; }
    public DateTime Timestamp { get; set; }
    public string Unit { get; set; }

    public PerformanceMetric(string name, double value, string unit = "ms")
    {
        Name = name;
        Value = value;
        Unit = unit;
        Timestamp = DateTime.UtcNow;
    }
}

public class OptimizationSuggestion
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; }
    public string Description { get; set; }
    public double ExpectedImprovement { get; set; } // percentage
    public string Category { get; set; }
    public int Priority { get; set; } // 1-10, higher is better
    public string RecommendedAction { get; set; }
    public DateTime SuggestedAt { get; set; } = DateTime.UtcNow;
}

public class OptimizationResult
{
    public string OptimizationId { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; }
    public Dictionary<string, double> MetricsBefore { get; set; } = new();
    public Dictionary<string, double> MetricsAfter { get; set; } = new();
    public double ActualImprovement { get; set; } // percentage
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
}

public class PerfTuner : IPerfTuner
{
    private readonly List<IPerformanceMetric> _metrics = new();
    private readonly Dictionary<string, OptimizationSuggestion> _suggestions = new();
    private readonly Dictionary<string, OptimizationResult> _appliedOptimizations = new();
    private readonly object _lockObj = new();
    private const int MaxMetricsToKeep = 10000;

    public async Task RecordMetricAsync(IPerformanceMetric metric)
    {
        if (metric == null) throw new ArgumentNullException(nameof(metric));

        await Task.Run(() =>
        {
            lock (_lockObj)
            {
                _metrics.Add(metric);
                if (_metrics.Count > MaxMetricsToKeep)
                {
                    _metrics.RemoveRange(0, _metrics.Count - MaxMetricsToKeep);
                }
            }
        });
    }

    public async Task<List<OptimizationSuggestion>> AnalyzePerformanceAsync()
    {
        return await Task.Run(() =>
        {
            lock (_lockObj)
            {
                var suggestions = new List<OptimizationSuggestion>();

                if (_metrics.Count == 0)
                    return suggestions;

                // Group metrics by name
                var groupedMetrics = _metrics
                    .GroupBy(m => m.Name)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(m => m.Timestamp).ToList());

                foreach (var (metricName, metricValues) in groupedMetrics)
                {
                    var recent = metricValues.Take(100).ToList();
                    if (recent.Count < 10) continue;

                    var values = recent.Select(m => m.Value).ToList();
                    var avg = values.Average();
                    var max = values.Max();
                    var min = values.Min();
                    var stdDev = CalculateStdDev(values, avg);

                    // Analyze trends
                    if (max > avg * 1.5)
                    {
                        suggestions.Add(new OptimizationSuggestion
                        {
                            Name = $"High {metricName} Variance",
                            Description = $"{metricName} shows high variance (max: {max}, avg: {avg}). Consider caching or batching.",
                            ExpectedImprovement = 15,
                            Category = "Variance",
                            Priority = 7,
                            RecommendedAction = $"Implement caching for {metricName} operations"
                        });
                    }

                    if (avg > 100)
                    {
                        suggestions.Add(new OptimizationSuggestion
                        {
                            Name = $"Slow {metricName}",
                            Description = $"{metricName} averaging {avg:F2}ms. Consider optimization.",
                            ExpectedImprovement = 20,
                            Category = "Latency",
                            Priority = 8,
                            RecommendedAction = $"Profile {metricName} operations for bottlenecks"
                        });
                    }

                    if (stdDev > avg * 0.5)
                    {
                        suggestions.Add(new OptimizationSuggestion
                        {
                            Name = $"Unstable {metricName}",
                            Description = $"{metricName} has high standard deviation ({stdDev:F2}). Check for resource contention.",
                            ExpectedImprovement = 10,
                            Category = "Stability",
                            Priority = 6,
                            RecommendedAction = $"Analyze resource contention in {metricName}"
                        });
                    }
                }

                return suggestions.OrderByDescending(s => s.Priority).ToList();
            }
        });
    }

    public async Task<OptimizationResult> ApplyOptimizationAsync(string optimizationId)
    {
        return await Task.Run(() =>
        {
            lock (_lockObj)
            {
                var result = new OptimizationResult
                {
                    OptimizationId = optimizationId
                };

                // Capture current metrics
                var recentMetrics = _metrics.TakeLast(100)
                    .GroupBy(m => m.Name)
                    .ToDictionary(g => g.Key, g => g.Average(m => m.Value));

                result.MetricsBefore = recentMetrics;

                // Simulate optimization application
                result.Success = true;
                result.Message = $"Optimization {optimizationId} applied successfully";

                // Simulate improvement
                var improvedMetrics = new Dictionary<string, double>();
                foreach (var (key, value) in recentMetrics)
                {
                    improvedMetrics[key] = value * 0.85; // 15% improvement
                }
                result.MetricsAfter = improvedMetrics;

                result.ActualImprovement = 15;

                _appliedOptimizations[optimizationId] = result;

                return result;
            }
        });
    }

    public async Task<bool> ValidateOptimizationAsync(string optimizationId)
    {
        return await Task.Run(() =>
        {
            lock (_lockObj)
            {
                if (!_appliedOptimizations.TryGetValue(optimizationId, out var result))
                    return false;

                // Check if improvement persists
                var recentMetrics = _metrics.TakeLast(100)
                    .GroupBy(m => m.Name)
                    .Average(g => g.Average(m => m.Value));

                var beforeAvg = result.MetricsBefore.Values.Average();
                var validationThreshold = beforeAvg * 0.9; // At least 10% improvement maintained

                return recentMetrics < validationThreshold;
            }
        });
    }

    public async Task RollbackOptimizationAsync(string optimizationId)
    {
        await Task.Run(() =>
        {
            lock (_lockObj)
            {
                if (_appliedOptimizations.TryGetValue(optimizationId, out var result))
                {
                    result.Success = false;
                    result.Message = $"Optimization {optimizationId} rolled back";
                }
            }
        });
    }

    private double CalculateStdDev(List<double> values, double mean)
    {
        if (values.Count <= 1) return 0;
        var sumOfSquares = values.Sum(v => Math.Pow(v - mean, 2));
        return Math.Sqrt(sumOfSquares / (values.Count - 1));
    }
}
