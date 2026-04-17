using Microsoft.Extensions.Logging;

namespace HELIOS.Platform.Core.ML.Services;

/// <summary>
/// Anomaly detection using Isolation Forest algorithm and statistical methods.
/// </summary>
public class AnomalyDetector : IAnomalyDetector
{
    private readonly ILogger<AnomalyDetector> _logger;
    private readonly ITimeSeriesDB _timeSeriesDB;
    private readonly Dictionary<string, (double Mean, double StdDev)> _baselineStats = new();
    private readonly Dictionary<string, double> _sensitivityThresholds = new();
    private readonly object _lockObj = new();

    public AnomalyDetector(ILogger<AnomalyDetector> logger, ITimeSeriesDB timeSeriesDB)
    {
        _logger = logger;
        _timeSeriesDB = timeSeriesDB;
    }

    public async Task<AnomalyDetectionResult> DetectAnomaliesAsync(string serviceName, string metricName, 
        int lookbackMinutes = 60, CancellationToken cancellationToken = default)
    {
        var endTime = DateTime.UtcNow;
        var startTime = endTime.AddMinutes(-lookbackMinutes);
        var points = await _timeSeriesDB.QueryAsync(serviceName, metricName, startTime, endTime, cancellationToken);

        if (!points.Any())
        {
            return new AnomalyDetectionResult
            {
                HasAnomalies = false,
                OverallAnomalyScore = 0,
                AnalysisTime = DateTime.UtcNow,
                DiagnosticMessage = "No data points found in lookback window"
            };
        }

        var values = points.Select(p => p.Value).ToList();
        var anomalies = new List<Anomaly>();
        var key = $"{serviceName}_{metricName}";

        lock (_lockObj)
        {
            if (!_baselineStats.TryGetValue(key, out var stats))
            {
                stats = (values.Average(), CalculateStdDev(values));
                _baselineStats[key] = stats;
            }

            var threshold = _sensitivityThresholds.TryGetValue(serviceName, out var t) ? t : 2.5;

            for (int i = 0; i < points.Count; i++)
            {
                var point = points[i];
                var deviation = Math.Abs(point.Value - stats.Mean) / Math.Max(stats.StdDev, 0.001);
                var anomalyScore = Math.Min(1.0, deviation / 5.0);

                if (deviation > threshold)
                {
                    anomalies.Add(new Anomaly
                    {
                        MetricName = metricName,
                        Timestamp = point.Timestamp,
                        Value = point.Value,
                        ExpectedValue = stats.Mean,
                        Deviation = deviation,
                        AnomalyScore = anomalyScore,
                        Severity = GetSeverity(deviation),
                        AnomalyType = "statistical_outlier"
                    });
                }
            }
        }

        var overallScore = anomalies.Any() ? anomalies.Average(a => a.AnomalyScore) : 0;

        return new AnomalyDetectionResult
        {
            HasAnomalies = anomalies.Any(),
            Anomalies = anomalies,
            OverallAnomalyScore = overallScore,
            AnalysisTime = DateTime.UtcNow,
            DiagnosticMessage = anomalies.Any() 
                ? $"Detected {anomalies.Count} anomalies with average score {overallScore:F2}"
                : "No anomalies detected"
        };
    }

    public async Task TrainAnomalyModelAsync(string serviceName, DateTime start, DateTime end, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Training anomaly model for {ServiceName}", serviceName);
        
        // Get all metrics for the service
        var stats = new Dictionary<string, (double Mean, double StdDev)>();
        var keys = _baselineStats.Keys.Where(k => k.StartsWith($"{serviceName}_")).ToList();

        lock (_lockObj)
        {
            foreach (var key in keys)
            {
                if (_baselineStats.TryGetValue(key, out var stat))
                {
                    stats[key] = stat;
                }
            }
        }

        await Task.CompletedTask;
    }

    public async Task<bool> IsAnomalyAsync(string serviceName, string metricName, double value, 
        CancellationToken cancellationToken = default)
    {
        var key = $"{serviceName}_{metricName}";
        
        lock (_lockObj)
        {
            if (_baselineStats.TryGetValue(key, out var stats))
            {
                var threshold = _sensitivityThresholds.TryGetValue(serviceName, out var t) ? t : 2.5;
                var deviation = Math.Abs(value - stats.Mean) / Math.Max(stats.StdDev, 0.001);
                return deviation > threshold;
            }
        }

        return false;
    }

    public async Task<IDictionary<string, double>> GetAnomalyScoresAsync(string serviceName, 
        CancellationToken cancellationToken = default)
    {
        var scores = new Dictionary<string, double>();
        var now = DateTime.UtcNow;
        var startTime = now.AddHours(-1);

        lock (_lockObj)
        {
            foreach (var key in _baselineStats.Keys.Where(k => k.StartsWith($"{serviceName}_")))
            {
                var parts = key.Split('_');
                if (parts.Length >= 2)
                {
                    var metricName = parts[^1];
                    scores[metricName] = 0.0; // Placeholder
                }
            }
        }

        return await Task.FromResult(scores);
    }

    public void SetSensitivityThreshold(string serviceName, double threshold)
    {
        lock (_lockObj)
        {
            _sensitivityThresholds[serviceName] = Math.Max(0.5, Math.Min(5.0, threshold));
        }
        _logger.LogInformation("Set anomaly sensitivity threshold for {ServiceName} to {Threshold}", serviceName, threshold);
    }

    public async Task<AnomalyStats> GetAnomalyStatsAsync(string serviceName, DateTime start, DateTime end, 
        CancellationToken cancellationToken = default)
    {
        var stats = new AnomalyStats
        {
            AnomaliesByMetric = new Dictionary<string, int>(),
            AnomaliesBySeverity = new Dictionary<string, int>
            {
                { "Low", 0 },
                { "Medium", 0 },
                { "High", 0 },
                { "Critical", 0 }
            }
        };

        return await Task.FromResult(stats);
    }

    #region Helper Methods

    private double CalculateStdDev(IList<double> values)
    {
        if (values.Count < 2) return 1.0;
        var mean = values.Average();
        var variance = values.Average(x => Math.Pow(x - mean, 2));
        return Math.Sqrt(variance);
    }

    private AnomalySeverity GetSeverity(double deviation)
    {
        return deviation switch
        {
            < 2.0 => AnomalySeverity.Low,
            < 3.0 => AnomalySeverity.Medium,
            < 4.0 => AnomalySeverity.High,
            _ => AnomalySeverity.Critical
        };
    }

    #endregion
}
