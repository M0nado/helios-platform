namespace HELIOS.Platform.Core.AdvancedML.Interfaces;

/// <summary>
/// Predictive anomaly detection using machine learning.
/// Predicts anomalies before they occur based on patterns.
/// </summary>
public interface IAnomalyPrediction
{
    /// <summary>
    /// Predicts anomalies in time-series data.
    /// </summary>
    /// <param name="timeSeries">Historical time-series values.</param>
    /// <param name="stepsAhead">Number of steps ahead to predict anomalies.</param>
    /// <returns>Predicted anomalies with risk scores.</returns>
    Task<List<PredictedAnomaly>> PredictAnomaliesAsync(double[] timeSeries, int stepsAhead = 5);

    /// <summary>
    /// Detects anomalies in real-time streaming data.
    /// </summary>
    Task<AnomalyDetectionResult> DetectRealtimeAsync(double value, double[] historicalContext);

    /// <summary>
    /// Trains anomaly detector on normal behavior examples.
    /// </summary>
    Task TrainOnNormalBehaviorAsync(double[] normalData);

    /// <summary>
    /// Identifies anomaly root causes from multivariate data.
    /// </summary>
    Task<AnomalyRootCauseAnalysis> AnalyzeRootCausesAsync(double[] anomalousValues, string[] featureNames);

    /// <summary>
    /// Gets anomaly detection model statistics.
    /// </summary>
    Task<AnomalyModelStats> GetModelStatsAsync();
}

/// <summary>
/// Predicted anomaly with risk assessment.
/// </summary>
public class PredictedAnomaly
{
    /// <summary>Predicted timestamp of anomaly.</summary>
    public DateTime PredictedTime { get; set; }

    /// <summary>Anomaly risk score (0-1, higher=more likely).</summary>
    public double RiskScore { get; set; }

    /// <summary>Confidence in prediction (0-1).</summary>
    public double Confidence { get; set; }

    /// <summary>Type of anomaly (outlier, drift, level-shift, spike).</summary>
    public string AnomalyType { get; set; } = string.Empty;

    /// <summary>Expected range if anomaly occurs.</summary>
    public (double min, double max) ExpectedRange { get; set; }

    /// <summary>Potential severity (low, medium, high, critical).</summary>
    public string Severity { get; set; } = "low";

    /// <summary>Recommended preventive actions.</summary>
    public List<string> RecommendedActions { get; set; } = new();

    /// <summary>Related features contributing to prediction.</summary>
    public Dictionary<string, double> FeatureImportance { get; set; } = new();

    /// <summary>Prediction timestamp.</summary>
    public DateTime PredictedAt { get; set; }
}

/// <summary>
/// Real-time anomaly detection result.
/// </summary>
public class AnomalyDetectionResult
{
    /// <summary>Is current value anomalous.</summary>
    public bool IsAnomaly { get; set; }

    /// <summary>Anomaly score (0-1).</summary>
    public double AnomalyScore { get; set; }

    /// <summary>Confidence in detection (0-1).</summary>
    public double Confidence { get; set; }

    /// <summary>Expected normal range.</summary>
    public (double min, double max) NormalRange { get; set; }

    /// <summary>Deviation from normal (in standard deviations).</summary>
    public double StandardDeviations { get; set; }

    /// <summary>Anomaly detection method used.</summary>
    public string DetectionMethod { get; set; } = string.Empty;

    /// <summary>Similar historical anomalies.</summary>
    public List<int> SimilarHistoricalIndices { get; set; } = new();

    /// <summary>Detection timestamp.</summary>
    public DateTime DetectedAt { get; set; }
}

/// <summary>
/// Root cause analysis for detected anomalies.
/// </summary>
public class AnomalyRootCauseAnalysis
{
    /// <summary>Primary root cause identified.</summary>
    public string PrimaryRootCause { get; set; } = string.Empty;

    /// <summary>Contributing factors ranked by importance.</summary>
    public List<(string factor, double importance)> ContributingFactors { get; set; } = new();

    /// <summary>Feature correlation changes during anomaly.</summary>
    public Dictionary<string, double> CorrelationChanges { get; set; } = new();

    /// <summary>Anomaly propagation path (features involved).</summary>
    public List<string> PropagationPath { get; set; } = new();

    /// <summary>Time until anomaly impact reached peak.</summary>
    public TimeSpan TimeToImpact { get; set; }

    /// <summary>Confidence in root cause analysis (0-1).</summary>
    public double ConfidenceScore { get; set; }

    /// <summary>Recommended remediation steps.</summary>
    public List<string> RemediationSteps { get; set; } = new();

    /// <summary>Analysis timestamp.</summary>
    public DateTime AnalyzedAt { get; set; }
}

/// <summary>
/// Anomaly detection model statistics.
/// </summary>
public class AnomalyModelStats
{
    /// <summary>Number of normal samples seen during training.</summary>
    public int NormalSampleCount { get; set; }

    /// <summary>Number of anomalous samples seen during training.</summary>
    public int AnomalousSampleCount { get; set; }

    /// <summary>Detection precision (TP/(TP+FP)).</summary>
    public double Precision { get; set; }

    /// <summary>Detection recall (TP/(TP+FN)).</summary>
    public double Recall { get; set; }

    /// <summary>F1 score combining precision and recall.</summary>
    public double F1Score { get; set; }

    /// <summary>Area under ROC curve (0-1).</summary>
    public double AUC_ROC { get; set; }

    /// <summary>False positive rate.</summary>
    public double FalsePositiveRate { get; set; }

    /// <summary>False negative rate.</summary>
    public double FalseNegativeRate { get; set; }

    /// <summary>Detection latency (ms).</summary>
    public long DetectionLatencyMs { get; set; }

    /// <summary>Model last trained at.</summary>
    public DateTime LastTrainedAt { get; set; }

    /// <summary>Feature count used in detection.</summary>
    public int FeatureCount { get; set; }
}
