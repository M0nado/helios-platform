using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.AdvancedOptimization
{
    /// <summary>
    /// System performance forecasting engine that predicts performance degradation,
    /// generates capacity alerts, and recommends preventive actions.
    /// </summary>
    public interface IPerformancePredictorAI
    {
        Task<bool> InitializeAsync();
        Task<PerformancePrediction> PredictPerformanceAsync(int hoursAhead);
        Task<CapacityAlert[]> GetCapacityAlertsAsync();
        Task<bool> RecordMetricAsync(string metricName, double value);
        Task<PreventiveAction[]> GetRecommendedActionsAsync();
        Task<PredictionAccuracyReport> GetAccuracyReportAsync();
    }

    /// <summary>Performance prediction for system metrics.</summary>
    public class PerformancePrediction
    {
        public string PredictionId { get; set; } = Guid.NewGuid().ToString();
        public DateTime PredictionTime { get; set; } = DateTime.UtcNow;
        public DateTime ForecastFor { get; set; }
        public Dictionary<string, MetricForecast> MetricForecasts { get; set; } = new();
        public double OverallHealthScore { get; set; }
        public List<string> RiskFactors { get; set; } = new();
        public double ConfidenceLevel { get; set; }
    }

    /// <summary>Forecast for a specific metric.</summary>
    public class MetricForecast
    {
        public string MetricName { get; set; } = string.Empty;
        public double CurrentValue { get; set; }
        public double PredictedValue { get; set; }
        public double ChangePercent { get; set; }
        public double Variance { get; set; }
        public bool WillExceedThreshold { get; set; }
        public double ThresholdValue { get; set; }
        public List<double> ForecastTrend { get; set; } = new();
    }

    /// <summary>Capacity alert when resources approach limits.</summary>
    public class CapacityAlert
    {
        public string AlertId { get; set; } = Guid.NewGuid().ToString();
        public string ResourceType { get; set; } = string.Empty;
        public int AlertLevel { get; set; } // 1-5, 5 is critical
        public double CurrentUsage { get; set; }
        public double Capacity { get; set; }
        public double UsagePercent { get; set; }
        public DateTime AlertTime { get; set; } = DateTime.UtcNow;
        public DateTime? ProjectedExhaustionTime { get; set; }
        public string RecommendedAction { get; set; } = string.Empty;
    }

    /// <summary>Preventive action recommendations.</summary>
    public class PreventiveAction
    {
        public string ActionId { get; set; } = Guid.NewGuid().ToString();
        public ActionPriority Priority { get; set; }
        public string ActionDescription { get; set; } = string.Empty;
        public string ImpactDescription { get; set; } = string.Empty;
        public TimeSpan TimeToImplement { get; set; }
        public double RiskMitigation { get; set; }
        public string ResourcesRequired { get; set; } = string.Empty;
    }

    /// <summary>Action priorities.</summary>
    public enum ActionPriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>Prediction accuracy metrics.</summary>
    public class PredictionAccuracyReport
    {
        public int TotalPredictions { get; set; }
        public int AccuratePredictions { get; set; }
        public int PartiallyAccuratePredictions { get; set; }
        public int InaccuratePredictions { get; set; }
        public double Accuracy { get; set; }
        public double Precision { get; set; }
        public double Recall { get; set; }
        public double MeanAbsoluteError { get; set; }
        public DateTime ReportGeneratedAt { get; set; } = DateTime.UtcNow;
        public List<string> TopPredictedMetrics { get; set; } = new();
    }
}
