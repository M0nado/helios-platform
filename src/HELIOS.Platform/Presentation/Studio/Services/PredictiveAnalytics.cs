using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HELIOS.Platform.Presentation.Studio.Services
{
    /// <summary>
    /// Predictive analytics for forecasting future capacity and performance needs
    /// </summary>
    public class PredictiveAnalytics
    {
        private readonly List<PredictionModel> _models;
        private readonly AnalyticsEngine _analyticsEngine;

        public PredictiveAnalytics(AnalyticsEngine analyticsEngine)
        {
            _analyticsEngine = analyticsEngine;
            _models = new List<PredictionModel>();
        }

        /// <summary>
        /// Generate prediction for a metric
        /// </summary>
        public Prediction PredictMetric(string metricName, TimeSpan lookAhead, int historyPeriodDays = 30)
        {
            var analytics = _analyticsEngine.GetAnalytics(metricName, TimeSpan.FromDays(historyPeriodDays));
            if (analytics == null)
                return null;

            // Simple linear trend prediction
            var trend = CalculateTrend(metricName, historyPeriodDays);
            
            var prediction = new Prediction
            {
                MetricName = metricName,
                PredictionTime = DateTime.UtcNow.Add(lookAhead),
                LookAheadPeriod = lookAhead,
                PredictedValue = trend.slope * lookAhead.TotalHours + trend.intercept,
                Confidence = CalculateConfidence(analytics),
                TrendDirection = trend.slope > 0 ? "Increasing" : trend.slope < 0 ? "Decreasing" : "Stable",
                CreatedAt = DateTime.UtcNow,
                HistoryAnalysis = analytics
            };

            _models.Add(new PredictionModel
            {
                Id = Guid.NewGuid().ToString("N"),
                Metric = metricName,
                Prediction = prediction,
                CreatedAt = DateTime.UtcNow
            });

            return prediction;
        }

        /// <summary>
        /// Predict resource exhaustion time
        /// </summary>
        public ResourceExhaustionForecast PredictResourceExhaustion(string metricName, double threshold, int maxDaysAhead = 90)
        {
            var analytics = _analyticsEngine.GetAnalytics(metricName, TimeSpan.FromDays(30));
            if (analytics == null)
                return null;

            var trend = CalculateTrend(metricName, 30);
            
            if (trend.slope <= 0)
                return new ResourceExhaustionForecast
                {
                    MetricName = metricName,
                    WillExhaust = false,
                    Reason = "Metric is not increasing"
                };

            // Calculate days until threshold
            var daysToThreshold = (threshold - analytics.Mean) / (trend.slope * 24);

            if (daysToThreshold < 0 || daysToThreshold > maxDaysAhead)
            {
                return new ResourceExhaustionForecast
                {
                    MetricName = metricName,
                    WillExhaust = false,
                    Reason = daysToThreshold < 0 ? "Already above threshold" : "Beyond forecast horizon"
                };
            }

            return new ResourceExhaustionForecast
            {
                MetricName = metricName,
                WillExhaust = true,
                ExhaustionDate = DateTime.UtcNow.AddDays(daysToThreshold),
                DaysUntilExhaustion = daysToThreshold,
                Threshold = threshold,
                CurrentValue = analytics.Mean,
                GrowthRate = trend.slope
            };
        }

        /// <summary>
        /// Get all predictions
        /// </summary>
        public IEnumerable<Prediction> GetAllPredictions()
        {
            return _models.OrderByDescending(m => m.CreatedAt).Select(m => m.Prediction);
        }

        /// <summary>
        /// Clear old predictions
        /// </summary>
        public void ClearOldPredictions(int daysToKeep = 30)
        {
            var cutoff = DateTime.UtcNow.AddDays(-daysToKeep);
            _models.RemoveAll(m => m.CreatedAt < cutoff);
        }

        private (double slope, double intercept) CalculateTrend(string metricName, int daysBack)
        {
            var analytics = _analyticsEngine.GetAnalytics(metricName, TimeSpan.FromDays(daysBack));
            if (analytics == null || analytics.Count < 2)
                return (0, analytics?.Mean ?? 0);

            // This would need actual data points, simplified for now
            // Real implementation would use least squares regression
            return (0.1, analytics.Mean); // Placeholder trend
        }

        private double CalculateConfidence(AnalyticsResult analytics)
        {
            if (analytics.Count < 100)
                return 0.3;
            if (analytics.StandardDeviation > analytics.Mean * 0.5)
                return 0.5;
            return 0.8;
        }
    }

    /// <summary>
    /// Capacity forecasting service
    /// </summary>
    public class CapacityForecaster
    {
        private readonly AnalyticsEngine _analyticsEngine;
        private readonly List<CapacityForecast> _forecasts;

        public CapacityForecaster(AnalyticsEngine analyticsEngine)
        {
            _analyticsEngine = analyticsEngine;
            _forecasts = new List<CapacityForecast>();
        }

        /// <summary>
        /// Generate capacity forecast
        /// </summary>
        public CapacityForecast GenerateCapacityForecast(string metricName, int daysAhead = 30)
        {
            var analytics = _analyticsEngine.GetAnalytics(metricName, TimeSpan.FromDays(30));
            if (analytics == null)
                return null;

            var forecast = new CapacityForecast
            {
                Id = Guid.NewGuid().ToString("N"),
                MetricName = metricName,
                ForecastDate = DateTime.UtcNow,
                DaysAhead = daysAhead,
                CurrentUsage = analytics.Mean,
                PeakUsage = analytics.Max,
                AverageGrowthRate = 0.05, // 5% daily growth (placeholder)
                ProjectedUsage30Days = analytics.Mean * Math.Pow(1.05, 30),
                ProjectedUsage90Days = analytics.Mean * Math.Pow(1.05, 90),
                CapacityHealthPercent = 100 - (analytics.Mean / 100 * 100)
            };

            _forecasts.Add(forecast);
            return forecast;
        }

        /// <summary>
        /// Get all capacity forecasts
        /// </summary>
        public IEnumerable<CapacityForecast> GetForecasts()
        {
            return _forecasts.OrderByDescending(f => f.ForecastDate);
        }

        /// <summary>
        /// Identify capacity risks
        /// </summary>
        public CapacityRisk AnalyzeCapacityRisk(string metricName)
        {
            var forecast = _forecasts.OrderByDescending(f => f.ForecastDate)
                .FirstOrDefault(f => f.MetricName == metricName);

            if (forecast == null)
                return null;

            var risk = new CapacityRisk
            {
                MetricName = metricName,
                CurrentRiskLevel = GetRiskLevel(forecast.CapacityHealthPercent),
                Projection30Days = GetRiskLevel(forecast.ProjectedUsage30Days),
                Projection90Days = GetRiskLevel(forecast.ProjectedUsage90Days),
                RecommendedAction = GetRecommendation(forecast.CapacityHealthPercent)
            };

            return risk;
        }

        private string GetRiskLevel(double percentUsage)
        {
            if (percentUsage > 90)
                return "Critical";
            if (percentUsage > 75)
                return "High";
            if (percentUsage > 50)
                return "Medium";
            return "Low";
        }

        private string GetRecommendation(double percentUsage)
        {
            if (percentUsage > 90)
                return "Immediate action required - consider scaling up";
            if (percentUsage > 75)
                return "Plan for capacity increase within 2 weeks";
            if (percentUsage > 50)
                return "Monitor closely, plan for future expansion";
            return "Adequate capacity available";
        }
    }

    /// <summary>
    /// Anomaly detection service
    /// </summary>
    public class AnomalyDetector
    {
        private readonly AnalyticsEngine _analyticsEngine;
        private readonly List<Anomaly> _detectedAnomalies;
        private const double StandardDeviationThreshold = 2.5;

        public AnomalyDetector(AnalyticsEngine analyticsEngine)
        {
            _analyticsEngine = analyticsEngine;
            _detectedAnomalies = new List<Anomaly>();
        }

        /// <summary>
        /// Detect anomalies in metric
        /// </summary>
        public List<Anomaly> DetectAnomalies(string metricName, TimeSpan period)
        {
            var analytics = _analyticsEngine.GetAnalytics(metricName, period);
            if (analytics == null)
                return new List<Anomaly>();

            var anomalies = new List<Anomaly>();
            var threshold = analytics.Mean + (analytics.StandardDeviation * StandardDeviationThreshold);
            var lowerThreshold = analytics.Mean - (analytics.StandardDeviation * StandardDeviationThreshold);

            // This is simplified - real implementation would analyze actual data points
            var currentValue = analytics.Max;
            
            if (currentValue > threshold)
            {
                var anomaly = new Anomaly
                {
                    Id = Guid.NewGuid().ToString("N"),
                    MetricName = metricName,
                    DetectedAt = DateTime.UtcNow,
                    Value = currentValue,
                    Threshold = threshold,
                    Severity = GetAnomalySeverity(currentValue, threshold),
                    Description = $"Value {currentValue:F2} exceeds threshold {threshold:F2}"
                };

                anomalies.Add(anomaly);
                _detectedAnomalies.Add(anomaly);
            }

            return anomalies;
        }

        /// <summary>
        /// Get all detected anomalies
        /// </summary>
        public IEnumerable<Anomaly> GetAnomalies(int daysBack = 7)
        {
            var cutoff = DateTime.UtcNow.AddDays(-daysBack);
            return _detectedAnomalies.Where(a => a.DetectedAt >= cutoff)
                .OrderByDescending(a => a.DetectedAt);
        }

        private string GetAnomalySeverity(double value, double threshold)
        {
            var percentAbove = ((value - threshold) / threshold) * 100;
            
            if (percentAbove > 50)
                return "Critical";
            if (percentAbove > 25)
                return "High";
            return "Medium";
        }
    }

    /// <summary>
    /// Prediction model
    /// </summary>
    public class Prediction
    {
        public string MetricName { get; set; }
        public DateTime PredictionTime { get; set; }
        public TimeSpan LookAheadPeriod { get; set; }
        public double PredictedValue { get; set; }
        public double Confidence { get; set; }
        public string TrendDirection { get; set; }
        public DateTime CreatedAt { get; set; }
        public AnalyticsResult HistoryAnalysis { get; set; }
    }

    /// <summary>
    /// Resource exhaustion forecast
    /// </summary>
    public class ResourceExhaustionForecast
    {
        public string MetricName { get; set; }
        public bool WillExhaust { get; set; }
        public DateTime? ExhaustionDate { get; set; }
        public double? DaysUntilExhaustion { get; set; }
        public double? Threshold { get; set; }
        public double? CurrentValue { get; set; }
        public double? GrowthRate { get; set; }
        public string Reason { get; set; }
    }

    /// <summary>
    /// Capacity forecast
    /// </summary>
    public class CapacityForecast
    {
        public string Id { get; set; }
        public string MetricName { get; set; }
        public DateTime ForecastDate { get; set; }
        public int DaysAhead { get; set; }
        public double CurrentUsage { get; set; }
        public double PeakUsage { get; set; }
        public double AverageGrowthRate { get; set; }
        public double ProjectedUsage30Days { get; set; }
        public double ProjectedUsage90Days { get; set; }
        public double CapacityHealthPercent { get; set; }
    }

    /// <summary>
    /// Capacity risk assessment
    /// </summary>
    public class CapacityRisk
    {
        public string MetricName { get; set; }
        public string CurrentRiskLevel { get; set; }
        public string Projection30Days { get; set; }
        public string Projection90Days { get; set; }
        public string RecommendedAction { get; set; }
    }

    /// <summary>
    /// Detected anomaly
    /// </summary>
    public class Anomaly
    {
        public string Id { get; set; }
        public string MetricName { get; set; }
        public DateTime DetectedAt { get; set; }
        public double Value { get; set; }
        public double Threshold { get; set; }
        public string Severity { get; set; }
        public string Description { get; set; }
    }

    /// <summary>
    /// Prediction model storage
    /// </summary>
    internal class PredictionModel
    {
        public string Id { get; set; }
        public string Metric { get; set; }
        public Prediction Prediction { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
