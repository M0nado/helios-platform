using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HELIOS.Platform.Tests.Phase3;

/// <summary>
/// Test cases for Phase 3 ML services - designed for xUnit testing framework.
/// </summary>
public class Phase3MLServicesTests
{
    private readonly ILogger<DataCollector> _dataCollectorLogger;
    private readonly ILogger<InMemoryTimeSeriesDB> _timeSeriesLogger;

    public Phase3MLServicesTests()
    {
        _dataCollectorLogger = NullLogger<DataCollector>.Instance;
        _timeSeriesLogger = NullLogger<InMemoryTimeSeriesDB>.Instance;
    }

    // Test: DataCollector_RegisterMetricProvider_SuccessfullyRegisters
    public async Task Test_DataCollector_RegisterMetricProvider()
    {
        var db = new InMemoryTimeSeriesDB(_timeSeriesLogger);
        var collector = new DataCollector(_dataCollectorLogger, db);

        Func<Task<IDictionary<string, object>>> provider = async () =>
        {
            await Task.Delay(1);
            return new Dictionary<string, object> { { "cpu", 45.5 }, { "memory", 2048 } };
        };

        collector.RegisterMetricProvider("TestService", provider);
        var providers = collector.GetRegisteredProviders().ToList();

        if (!providers.Contains("TestService"))
            throw new Exception("Provider not registered");
    }

    // Test: DataCollector_CollectMetricsAsync_AggregatesFromMultipleProviders
    public async Task Test_DataCollector_CollectMetricsAsync()
    {
        var db = new InMemoryTimeSeriesDB(_timeSeriesLogger);
        var collector = new DataCollector(_dataCollectorLogger, db);

        collector.RegisterMetricProvider("Service1", async () =>
        {
            await Task.Delay(1);
            return new Dictionary<string, object> { { "cpu", 50 } };
        });

        collector.RegisterMetricProvider("Service2", async () =>
        {
            await Task.Delay(1);
            return new Dictionary<string, object> { { "memory", 2000 } };
        });

        var metrics = await collector.CollectMetricsAsync();

        if (!metrics.Keys.Contains("Service1_metrics"))
            throw new Exception("Service1 metrics not found");
        if (!metrics.Keys.Contains("Service2_metrics"))
            throw new Exception("Service2 metrics not found");
    }

    // Test: TimeSeriesDB_WritePoint_Successfully
    public async Task Test_TimeSeriesDB_WritePoint()
    {
        var db = new InMemoryTimeSeriesDB(_timeSeriesLogger);
        await db.WritePointAsync("TestService", "cpu_usage", 45.5);
        var stats = await db.GetStatsAsync();

        if (stats.TotalPoints != 1)
            throw new Exception($"Expected 1 point, got {stats.TotalPoints}");
    }

    // Test: TimeSeriesDB_WriteBatch_Successfully
    public async Task Test_TimeSeriesDB_WriteBatch()
    {
        var db = new InMemoryTimeSeriesDB(_timeSeriesLogger);
        var points = new List<MetricPoint>
        {
            new() { ServiceName = "S1", MetricName = "cpu", Value = 50, Timestamp = DateTime.UtcNow },
            new() { ServiceName = "S1", MetricName = "memory", Value = 2048, Timestamp = DateTime.UtcNow },
            new() { ServiceName = "S2", MetricName = "cpu", Value = 30, Timestamp = DateTime.UtcNow }
        };

        await db.WriteBatchAsync(points);
        var stats = await db.GetStatsAsync();

        if (stats.TotalPoints != 3)
            throw new Exception($"Expected 3 points, got {stats.TotalPoints}");
        if (stats.ServiceCount != 2)
            throw new Exception($"Expected 2 services, got {stats.ServiceCount}");
    }

    // Test: TimeSeriesDB_QueryAsync_ReturnsCorrectPoints
    public async Task Test_TimeSeriesDB_QueryAsync()
    {
        var db = new InMemoryTimeSeriesDB(_timeSeriesLogger);
        var now = DateTime.UtcNow;
        
        await db.WritePointAsync("Service1", "metric1", 10, null);
        await Task.Delay(10);
        await db.WritePointAsync("Service1", "metric1", 20, null);

        var results = await db.QueryAsync("Service1", "metric1", now.AddMinutes(-5), now.AddMinutes(5));

        if (results.Count != 2)
            throw new Exception($"Expected 2 results, got {results.Count}");
    }

    // Test: AnomalyDetector_DetectAnomalies
    public async Task Test_AnomalyDetector_DetectAnomalies()
    {
        var db = new InMemoryTimeSeriesDB(_timeSeriesLogger);
        var logger = new NullLogger<AnomalyDetector>();
        var detector = new AnomalyDetector(logger, db);

        // Train baseline
        for (int i = 0; i < 10; i++)
        {
            await db.WritePointAsync("Service1", "cpu", 50 + (i % 5));
        }

        var result = await detector.DetectAnomaliesAsync("Service1", "cpu", 60);
        
        // Should have basic detection capability
        if (result.AnalysisTime == DateTime.MinValue)
            throw new Exception("Analysis time not set");
    }

    // Test: FeatureExtractor_ExtractFeatures
    public async Task Test_FeatureExtractor_ExtractFeatures()
    {
        var logger = new NullLogger<FeatureExtractor>();
        var extractor = new FeatureExtractor(logger);

        var timeSeries = new List<MetricSnapshot>
        {
            new() { Timestamp = DateTime.UtcNow, Metrics = new Dictionary<string, object> { { "cpu", 50.0 } }, ServiceName = "S1" },
            new() { Timestamp = DateTime.UtcNow.AddSeconds(1), Metrics = new Dictionary<string, object> { { "cpu", 55.0 } }, ServiceName = "S1" },
            new() { Timestamp = DateTime.UtcNow.AddSeconds(2), Metrics = new Dictionary<string, object> { { "cpu", 52.0 } }, ServiceName = "S1" }
        };

        var features = await extractor.ExtractFeaturesAsync(timeSeries);

        if (!features.Features.ContainsKey("mean"))
            throw new Exception("Mean feature not extracted");
        if (features.Features["mean"] < 50 || features.Features["mean"] > 56)
            throw new Exception($"Mean calculation incorrect: {features.Features["mean"]}");
    }

    // Test: DataNormalizer_NormalizeMetric
    public async Task Test_DataNormalizer_NormalizeMetric()
    {
        var logger = new NullLogger<DataNormalizer>();
        var normalizer = new DataNormalizer(logger);

        var metric = await normalizer.NormalizeMetricAsync("cpu_usage", 75, "percent");

        if (metric.Value < 0 || metric.Value > 100)
            throw new Exception($"Normalized value out of range: {metric.Value}");
        if (metric.StandardUnit != "%")
            throw new Exception($"Wrong unit: {metric.StandardUnit}");
    }

    // Test: PredictiveAnalytics_Forecast
    public async Task Test_PredictiveAnalytics_Forecast()
    {
        var dbLogger = new NullLogger<InMemoryTimeSeriesDB>();
        var db = new InMemoryTimeSeriesDB(dbLogger);
        var logger = new NullLogger<PredictiveAnalytics>();
        var analytics = new PredictiveAnalytics(logger, db);

        // Add historical data
        var now = DateTime.UtcNow;
        for (int i = 0; i < 30; i++)
        {
            await db.WritePointAsync("Service1", "metric1", 50 + i * 0.5, null);
        }

        var forecast = await analytics.ForecastAsync("Service1", "metric1", 5, TimeSpan.FromHours(1));

        if (!forecast.Points.Any())
            throw new Exception("No forecast points generated");
        if (forecast.ServiceName != "Service1")
            throw new Exception("Wrong service name in forecast");
    }
}

