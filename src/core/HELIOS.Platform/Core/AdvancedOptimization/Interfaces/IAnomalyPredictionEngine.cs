namespace HELIOS.Platform.Core.AdvancedOptimization.Interfaces
{
    /// <summary>
    /// Interface for the Anomaly Prediction Engine service.
    /// Provides predictive anomaly detection using pattern recognition.
    /// </summary>
    public interface IAnomalyPredictionEngine : IService
    {
        /// <summary>
        /// Predicts potential anomalies based on current data and patterns.
        /// </summary>
        /// <param name="currentMetrics">Current system metrics.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation with predictions.</returns>
        Task<AnomalyPredictions> PredictAnomaliesAsync(Dictionary<string, double> currentMetrics, CancellationToken cancellationToken = default);

        /// <summary>
        /// Learns patterns from historical data.
        /// </summary>
        /// <param name="historicalMetrics">Historical metrics data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task LearnPatternsAsync(List<MetricsSnapshot> historicalMetrics, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates alerts for detected anomalies.
        /// </summary>
        /// <param name="anomalies">Detected anomalies.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation with alerts.</returns>
        Task<List<AnomalyAlert>> GenerateAlertsAsync(List<AnomalyPrediction> anomalies, CancellationToken cancellationToken = default);

        /// <summary>
        /// Records a metrics snapshot for pattern learning.
        /// </summary>
        /// <param name="snapshot">Metrics snapshot.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RecordMetricsAsync(MetricsSnapshot snapshot);

        /// <summary>
        /// Gets anomaly prediction history.
        /// </summary>
        /// <param name="limit">Maximum records to retrieve.</param>
        /// <returns>List of historical anomaly predictions.</returns>
        Task<List<AnomalyPredictions>> GetHistoryAsync(int limit = 100);
    }

    /// <summary>
    /// Represents anomaly predictions.
    /// </summary>
    public class AnomalyPredictions
    {
        /// <summary>
        /// Prediction identifier.
        /// </summary>
        public string PredictionId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Timestamp of prediction.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// List of detected anomalies.
        /// </summary>
        public List<AnomalyPrediction> Anomalies { get; set; } = new();

        /// <summary>
        /// Overall anomaly score (0-100).
        /// </summary>
        public double OverallAnomalyScore { get; set; }

        /// <summary>
        /// Number of critical anomalies detected.
        /// </summary>
        public int CriticalCount { get; set; }
    }

    /// <summary>
    /// Represents a single anomaly prediction.
    /// </summary>
    public class AnomalyPrediction
    {
        /// <summary>
        /// Metric name where anomaly is predicted.
        /// </summary>
        public string MetricName { get; set; } = string.Empty;

        /// <summary>
        /// Current value of the metric.
        /// </summary>
        public double CurrentValue { get; set; }

        /// <summary>
        /// Expected normal range minimum.
        /// </summary>
        public double ExpectedMin { get; set; }

        /// <summary>
        /// Expected normal range maximum.
        /// </summary>
        public double ExpectedMax { get; set; }

        /// <summary>
        /// Anomaly score (0-100).
        /// </summary>
        public double AnomalyScore { get; set; }

        /// <summary>
        /// Type of anomaly.
        /// </summary>
        public string AnomalyType { get; set; } = string.Empty;

        /// <summary>
        /// Probability of anomaly (0-1).
        /// </summary>
        public double Probability { get; set; }

        /// <summary>
        /// Severity level (Low, Medium, High, Critical).
        /// </summary>
        public string Severity { get; set; } = "Low";
    }

    /// <summary>
    /// Represents an anomaly alert.
    /// </summary>
    public class AnomalyAlert
    {
        /// <summary>
        /// Alert identifier.
        /// </summary>
        public string AlertId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Alert timestamp.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Associated anomaly prediction.
        /// </summary>
        public AnomalyPrediction? Anomaly { get; set; }

        /// <summary>
        /// Alert message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Recommended action.
        /// </summary>
        public string RecommendedAction { get; set; } = string.Empty;

        /// <summary>
        /// Alert severity level.
        /// </summary>
        public string Severity { get; set; } = "Medium";

        /// <summary>
        /// Whether alert was acknowledged.
        /// </summary>
        public bool IsAcknowledged { get; set; }
    }

    /// <summary>
    /// Represents a metrics snapshot at a point in time.
    /// </summary>
    public class MetricsSnapshot
    {
        /// <summary>
        /// Snapshot timestamp.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Metrics data.
        /// </summary>
        public Dictionary<string, double> Metrics { get; set; } = new();

        /// <summary>
        /// System state information.
        /// </summary>
        public Dictionary<string, string> StateInfo { get; set; } = new();
    }
}
