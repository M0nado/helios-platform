namespace HELIOS.Platform.Core.ML.Interfaces;

/// <summary>
/// Detects anomalies and unusual behavior patterns in system metrics.
/// Identifies deviations from normal operation.
/// </summary>
public interface IAnomalyDetector
{
    /// <summary>
    /// Detects anomalies in recent metric data.
    /// </summary>
    Task<AnomalyDetectionResult> DetectAnomaliesAsync(string serviceName, string metricName, 
        int lookbackMinutes = 60, CancellationToken cancellationToken = default);

    /// <summary>
    /// Trains anomaly detection model on historical data.
    /// </summary>
    Task TrainAnomalyModelAsync(string serviceName, DateTime start, DateTime end, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs real-time anomaly detection on streaming data.
    /// </summary>
    Task<bool> IsAnomalyAsync(string serviceName, string metricName, double value, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets anomaly scores for metrics (0-1, higher = more anomalous).
    /// </summary>
    Task<IDictionary<string, double>> GetAnomalyScoresAsync(string serviceName, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets anomaly detection sensitivity threshold.
    /// </summary>
    void SetSensitivityThreshold(string serviceName, double threshold);

    /// <summary>
    /// Gets anomaly statistics and trends.
    /// </summary>
    Task<AnomalyStats> GetAnomalyStatsAsync(string serviceName, DateTime start, DateTime end, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of anomaly detection analysis.
/// </summary>
public class AnomalyDetectionResult
{
    public bool HasAnomalies { get; set; }
    public IList<Anomaly> Anomalies { get; set; } = new List<Anomaly>();
    public double OverallAnomalyScore { get; set; }
    public DateTime AnalysisTime { get; set; }
    public string DiagnosticMessage { get; set; } = string.Empty;
}

/// <summary>
/// Represents a detected anomaly.
/// </summary>
public class Anomaly
{
    public string MetricName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
    public double ExpectedValue { get; set; }
    public double Deviation { get; set; }
    public double AnomalyScore { get; set; } // 0-1
    public AnomalySeverity Severity { get; set; }
    public string AnomalyType { get; set; } = string.Empty; // statistical_outlier, contextual, etc
}

/// <summary>
/// Severity levels for anomalies.
/// </summary>
public enum AnomalySeverity
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Statistics about detected anomalies.
/// </summary>
public class AnomalyStats
{
    public int TotalAnomalies { get; set; }
    public int CriticalAnomalies { get; set; }
    public int HighSeverityAnomalies { get; set; }
    public double AnomalyFrequency { get; set; } // anomalies per hour
    public IDictionary<string, int> AnomaliesByMetric { get; set; } = new Dictionary<string, int>();
    public IDictionary<string, int> AnomaliesBySeverity { get; set; } = new Dictionary<string, int>();
    public double MeanAnomalyScore { get; set; }
}
