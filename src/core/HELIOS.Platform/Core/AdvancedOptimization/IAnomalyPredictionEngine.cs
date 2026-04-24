using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.AdvancedOptimization
{
    /// <summary>
    /// Predictive anomaly detection engine that learns normal patterns from
    /// historical data and predicts future anomalies before they occur.
    /// </summary>
    public interface IAnomalyPredictionEngine
    {
        Task<bool> InitializeAsync();
        Task<bool> LearnPatternAsync(string metricName, List<MetricDataPoint> data);
        Task<AnomalyPrediction[]> PredictAnomaliesAsync(int lookAheadMinutes);
        Task<bool> ReportAnomalyAsync(string metricName, AnomalyEvent anomaly);
        Task<AnomalyMetrics> GetAnomalyMetricsAsync();
        Task<AnomalyConfidenceReport> GetConfidenceReportAsync();
    }

    /// <summary>Represents a single metric data point.</summary>
    public class MetricDataPoint
    {
        public DateTime Timestamp { get; set; }
        public double Value { get; set; }
        public double StandardDeviation { get; set; }
        public Dictionary<string, object> Context { get; set; } = new();
    }

    /// <summary>Represents a predicted anomaly.</summary>
    public class AnomalyPrediction
    {
        public string PredictionId { get; set; } = Guid.NewGuid().ToString();
        public string MetricName { get; set; } = string.Empty;
        public AnomalyType Type { get; set; }
        public double Confidence { get; set; }
        public DateTime PredictedTime { get; set; }
        public double PredictedValue { get; set; }
        public double ThresholdValue { get; set; }
        public string Reason { get; set; } = string.Empty;
        public int SeverityLevel { get; set; }
    }

    /// <summary>Types of anomalies.</summary>
    public enum AnomalyType
    {
        Spike,
        Drop,
        Trend,
        Seasonal,
        Outlier,
        Pattern,
        Threshold
    }

    /// <summary>Represents an observed anomaly.</summary>
    public class AnomalyEvent
    {
        public string EventId { get; set; } = Guid.NewGuid().ToString();
        public string MetricName { get; set; } = string.Empty;
        public AnomalyType Type { get; set; }
        public DateTime DetectedTime { get; set; } = DateTime.UtcNow;
        public double MeasuredValue { get; set; }
        public double ExpectedValue { get; set; }
        public double Severity { get; set; }
        public bool WasPredicted { get; set; }
        public string Impact { get; set; } = string.Empty;
    }

    /// <summary>Anomaly detection metrics and statistics.</summary>
    public class AnomalyMetrics
    {
        public int PatternsLearned { get; set; }
        public int AnomaliesPredicted { get; set; }
        public int AnomaliesDetected { get; set; }
        public int CorrectPredictions { get; set; }
        public int FalsePositives { get; set; }
        public int FalseNegatives { get; set; }
        public double PrecisionScore { get; set; }
        public double RecallScore { get; set; }
        public double F1Score { get; set; }
        public DateTime LastPredictionTime { get; set; }
        public long TotalPredictionRuns { get; set; }
        public double AveragePredictionAccuracy { get; set; }
    }

    /// <summary>Anomaly prediction confidence report.</summary>
    public class AnomalyConfidenceReport
    {
        public List<MetricConfidence> MetricConfidences { get; set; } = new();
        public double OverallConfidence { get; set; }
        public double ModelStability { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public List<string> HighRiskMetrics { get; set; } = new();
        public List<string> LowConfidenceMetrics { get; set; } = new();
    }

    /// <summary>Confidence metrics for a specific metric.</summary>
    public class MetricConfidence
    {
        public string MetricName { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public int SamplesUsed { get; set; }
        public double StandardErrorOfEstimate { get; set; }
        public bool IsReliable { get; set; }
    }
}
