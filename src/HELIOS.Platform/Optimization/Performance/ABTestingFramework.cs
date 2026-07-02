namespace HELIOS.Platform.Optimization.Performance;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class ABTestingVariant
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; }
    public string Configuration { get; set; }
    public double AllocationPercentage { get; set; }
    public bool IsControlGroup { get; set; }
}

public class ABTestResult
{
    public string TestId { get; set; }
    public string VariantId { get; set; }
    public double MetricValue { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}

public interface IABTestingFramework
{
    Task<string> CreateTestAsync(string testName, List<ABTestingVariant> variants);
    Task RecordResultAsync(string testId, string variantId, double metricValue);
    Task<ABTestAnalysis> AnalyzeTestAsync(string testId);
    Task PromoteVariantAsync(string testId, string winningVariantId);
    Task RollbackTestAsync(string testId);
}

public class ABTestAnalysis
{
    public string TestId { get; set; }
    public Dictionary<string, TestVariantStats> VariantStats { get; set; } = new();
    public string RecommendedWinner { get; set; }
    public double ConfidenceLevel { get; set; }
    public bool IsStatisticallySignificant { get; set; }
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
}

public class TestVariantStats
{
    public string VariantId { get; set; }
    public double AverageMetric { get; set; }
    public double StandardDeviation { get; set; }
    public int SampleSize { get; set; }
    public double ConversionRate { get; set; }
}

public class ABTestingFramework : IABTestingFramework
{
    private readonly Dictionary<string, ABTest> _tests = new();
    private readonly List<ABTestResult> _results = new();
    private readonly object _lockObj = new();

    private class ABTest
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<ABTestingVariant> Variants { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } // active, paused, completed
    }

    public async Task<string> CreateTestAsync(string testName, List<ABTestingVariant> variants)
    {
        if (variants == null || variants.Count < 2)
            throw new ArgumentException("A/B test requires at least 2 variants");

        return await Task.Run(() =>
        {
            lock (_lockObj)
            {
                var testId = Guid.NewGuid().ToString();
                _tests[testId] = new ABTest
                {
                    Id = testId,
                    Name = testName,
                    Variants = variants,
                    CreatedAt = DateTime.UtcNow,
                    Status = "active"
                };
                return testId;
            }
        });
    }

    public async Task RecordResultAsync(string testId, string variantId, double metricValue)
    {
        await Task.Run(() =>
        {
            lock (_lockObj)
            {
                if (!_tests.ContainsKey(testId))
                    throw new ArgumentException($"Test {testId} not found");

                _results.Add(new ABTestResult
                {
                    TestId = testId,
                    VariantId = variantId,
                    MetricValue = metricValue
                });
            }
        });
    }

    public async Task<ABTestAnalysis> AnalyzeTestAsync(string testId)
    {
        return await Task.Run(() =>
        {
            lock (_lockObj)
            {
                if (!_tests.TryGetValue(testId, out var test))
                    throw new ArgumentException($"Test {testId} not found");

                var analysis = new ABTestAnalysis { TestId = testId };
                var testResults = _results.Where(r => r.TestId == testId).ToList();

                foreach (var variant in test.Variants)
                {
                    var variantResults = testResults.Where(r => r.VariantId == variant.Id).ToList();

                    if (variantResults.Count == 0)
                    {
                        analysis.VariantStats[variant.Id] = new TestVariantStats
                        {
                            VariantId = variant.Id,
                            SampleSize = 0
                        };
                        continue;
                    }

                    var metrics = variantResults.Select(r => r.MetricValue).ToList();
                    var avg = metrics.Average();
                    var stdDev = CalculateStdDev(metrics, avg);

                    analysis.VariantStats[variant.Id] = new TestVariantStats
                    {
                        VariantId = variant.Id,
                        AverageMetric = avg,
                        StandardDeviation = stdDev,
                        SampleSize = variantResults.Count,
                        ConversionRate = (avg / (metrics.Max() + 1)) * 100 // Normalized conversion
                    };
                }

                // Determine winner using statistical significance
                var nonEmptyStats = analysis.VariantStats.Values.Where(s => s.SampleSize > 0).ToList();
                if (nonEmptyStats.Count >= 2)
                {
                    var best = nonEmptyStats.OrderByDescending(s => s.ConversionRate).First();
                    analysis.RecommendedWinner = best.VariantId;

                    // Simple t-test equivalent for significance
                    var controlVariant = test.Variants.FirstOrDefault(v => v.IsControlGroup);
                    if (controlVariant != null && 
                        analysis.VariantStats.TryGetValue(controlVariant.Id, out var controlStats))
                    {
                        var tStatistic = CalculateTStatistic(controlStats, best);
                        analysis.IsStatisticallySignificant = tStatistic > 1.96; // 95% confidence
                        analysis.ConfidenceLevel = Math.Min(95, tStatistic * 50);
                    }
                }

                return analysis;
            }
        });
    }

    public async Task PromoteVariantAsync(string testId, string winningVariantId)
    {
        await Task.Run(() =>
        {
            lock (_lockObj)
            {
                if (!_tests.TryGetValue(testId, out var test))
                    throw new ArgumentException($"Test {testId} not found");

                test.Status = "completed";
                // In production, this would promote the variant to main deployment
            }
        });
    }

    public async Task RollbackTestAsync(string testId)
    {
        await Task.Run(() =>
        {
            lock (_lockObj)
            {
                if (_tests.TryGetValue(testId, out var test))
                {
                    test.Status = "paused";
                    // Revert all variants to control
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

    private double CalculateTStatistic(TestVariantStats control, TestVariantStats variant)
    {
        if (control.SampleSize < 2 || variant.SampleSize < 2)
            return 0;

        var pooledStdErr = Math.Sqrt(
            (control.StandardDeviation * control.StandardDeviation / control.SampleSize) +
            (variant.StandardDeviation * variant.StandardDeviation / variant.SampleSize)
        );

        if (pooledStdErr == 0) return 0;

        return Math.Abs(variant.AverageMetric - control.AverageMetric) / pooledStdErr;
    }
}
