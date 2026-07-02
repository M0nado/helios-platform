using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace MonadoBlade.Monitoring
{
    /// <summary>
    /// Anomaly detection severity levels
    /// </summary>
    public enum AnomalySeverity
    {
        Info = 0,
        Warning = 1,
        Critical = 2
    }

    /// <summary>
    /// Anomaly alert information
    /// </summary>
    public class AnomalyAlert
    {
        public string MetricName { get; set; }
        public double CurrentValue { get; set; }
        public double Baseline { get; set; }
        public double Deviation { get; set; }
        public AnomalySeverity Severity { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, string> Tags { get; set; }
    }

    /// <summary>
    /// Interface for anomaly detection hooks
    /// </summary>
    public interface IAnomalyDetectionHooks
    {
        void CheckMetricAnomaly(string metricName, double value, string metricType, Dictionary<string, string> tags);
        void RegisterAlertCallback(Action<AnomalyAlert> callback);
        void SetLearningWindow(int sampleCount, TimeSpan duration);
        void SetDetectionThresholds(double stdDevMultiplier, double percentageDeviation);
        List<AnomalyAlert> GetRecentAlerts(int count = 10);
        double GetBaselineValue(string metricName);
        bool IsAnomaly(string metricName, double value);
    }

    /// <summary>
    /// Anomaly detection hooks with multiple detection algorithms
    /// </summary>
    public class AnomalyDetectionHooks : IAnomalyDetectionHooks
    {
        private readonly ConcurrentDictionary<string, MetricBaseline> _baselines = new();
        private readonly ConcurrentDictionary<string, List<double>> _historicalValues = new();
        private readonly ConcurrentQueue<AnomalyAlert> _recentAlerts = new();
        private readonly ReaderWriterLockSlim _baselineLock = new();
        private readonly List<Action<AnomalyAlert>> _alertCallbacks = new();

        private int _learningWindowSamples = 100;
        private TimeSpan _learningWindowDuration = TimeSpan.FromHours(1);
        private double _stdDevMultiplier = 2.5;
        private double _percentageDeviationThreshold = 0.3; // 30%
        private readonly int _maxAlertHistory = 1000;

        /// <summary>
        /// Check if a metric value represents an anomaly
        /// </summary>
        public void CheckMetricAnomaly(string metricName, double value, string metricType, Dictionary<string, string> tags)
        {
            if (IsAnomaly(metricName, value))
            {
                var baseline = GetBaselineValue(metricName);
                var deviation = baseline > 0 ? Math.Abs(value - baseline) / baseline : 0;
                var severity = CalculateSeverity(deviation);

                var alert = new AnomalyAlert
                {
                    MetricName = metricName,
                    CurrentValue = value,
                    Baseline = baseline,
                    Deviation = deviation,
                    Severity = severity,
                    Message = $"Anomaly detected in {metricName}: value={value}, baseline={baseline}, deviation={(deviation * 100):F2}%",
                    Timestamp = DateTime.UtcNow,
                    Tags = tags ?? new()
                };

                _recentAlerts.Enqueue(alert);
                if (_recentAlerts.Count > _maxAlertHistory)
                {
                    _recentAlerts.TryDequeue(out _);
                }

                NotifyAlertCallbacks(alert);
            }

            RecordMetricValue(metricName, value);
        }

        /// <summary>
        /// Register callback for anomaly alerts
        /// </summary>
        public void RegisterAlertCallback(Action<AnomalyAlert> callback)
        {
            if (callback != null)
            {
                lock (_alertCallbacks)
                {
                    _alertCallbacks.Add(callback);
                }
            }
        }

        /// <summary>
        /// Set learning window for baseline calculation
        /// </summary>
        public void SetLearningWindow(int sampleCount, TimeSpan duration)
        {
            _learningWindowSamples = sampleCount;
            _learningWindowDuration = duration;
        }

        /// <summary>
        /// Set detection thresholds
        /// </summary>
        public void SetDetectionThresholds(double stdDevMultiplier, double percentageDeviation)
        {
            if (stdDevMultiplier <= 0)
                throw new ArgumentException("stdDevMultiplier must be positive");
            if (percentageDeviation < 0 || percentageDeviation > 1)
                throw new ArgumentException("percentageDeviation must be between 0 and 1");

            _stdDevMultiplier = stdDevMultiplier;
            _percentageDeviationThreshold = percentageDeviation;
        }

        /// <summary>
        /// Get recent anomaly alerts
        /// </summary>
        public List<AnomalyAlert> GetRecentAlerts(int count = 10)
        {
            return _recentAlerts.TakeLast(count).ToList();
        }

        /// <summary>
        /// Get baseline value for a metric
        /// </summary>
        public double GetBaselineValue(string metricName)
        {
            _baselineLock.EnterReadLock();
            try
            {
                if (_baselines.TryGetValue(metricName, out var baseline))
                {
                    return baseline.Mean;
                }
                return 0;
            }
            finally
            {
                _baselineLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Check if a value is anomalous
        /// </summary>
        public bool IsAnomaly(string metricName, double value)
        {
            _baselineLock.EnterReadLock();
            try
            {
                if (!_baselines.TryGetValue(metricName, out var baseline))
                {
                    return false; // No baseline yet
                }

                if (baseline.SampleCount < _learningWindowSamples)
                {
                    return false; // Still learning
                }

                // Statistical anomaly: more than N standard deviations from mean
                var zScore = Math.Abs(value - baseline.Mean) / (baseline.StdDev > 0 ? baseline.StdDev : 1);
                if (zScore > _stdDevMultiplier)
                {
                    return true;
                }

                // Percentage deviation anomaly
                var percentDev = baseline.Mean > 0 
                    ? Math.Abs(value - baseline.Mean) / baseline.Mean 
                    : 0;
                
                if (percentDev > _percentageDeviationThreshold)
                {
                    return true;
                }

                // Drift detection: if recent values differ from historical baseline
                if (baseline.LastUpdateTime < DateTime.UtcNow.AddSeconds(-30))
                {
                    return DetectDrift(metricName, value, baseline);
                }

                return false;
            }
            finally
            {
                _baselineLock.ExitReadLock();
            }
        }

        private void RecordMetricValue(string metricName, double value)
        {
            if (!_historicalValues.ContainsKey(metricName))
            {
                _historicalValues[metricName] = new List<double>();
            }

            var values = _historicalValues[metricName];
            lock (values)
            {
                values.Add(value);

                if (values.Count > _learningWindowSamples * 2)
                {
                    values.RemoveRange(0, values.Count - _learningWindowSamples);
                }
            }

            UpdateBaseline(metricName);
        }

        private void UpdateBaseline(string metricName)
        {
            if (!_historicalValues.TryGetValue(metricName, out var values))
                return;

            lock (values)
            {
                if (values.Count == 0)
                    return;

                var mean = values.Average();
                var variance = values.Sum(v => Math.Pow(v - mean, 2)) / values.Count;
                var stdDev = Math.Sqrt(variance);

                _baselineLock.EnterWriteLock();
                try
                {
                    _baselines[metricName] = new MetricBaseline
                    {
                        Mean = mean,
                        StdDev = stdDev,
                        SampleCount = values.Count,
                        Min = values.Min(),
                        Max = values.Max(),
                        LastUpdateTime = DateTime.UtcNow
                    };
                }
                finally
                {
                    _baselineLock.ExitWriteLock();
                }
            }
        }

        private bool DetectDrift(string metricName, double value, MetricBaseline baseline)
        {
            if (!_historicalValues.TryGetValue(metricName, out var values))
                return false;

            lock (values)
            {
                var recentValues = values.TakeLast(Math.Max(10, values.Count / 4)).ToList();
                if (recentValues.Count < 3)
                    return false;

                var recentMean = recentValues.Average();
                var drift = Math.Abs(recentMean - baseline.Mean) / (baseline.Mean > 0 ? baseline.Mean : 1);

                return drift > 0.2; // 20% drift threshold
            }
        }

        private AnomalySeverity CalculateSeverity(double deviation)
        {
            if (deviation > 0.5)
                return AnomalySeverity.Critical;
            if (deviation > 0.25)
                return AnomalySeverity.Warning;
            return AnomalySeverity.Info;
        }

        private void NotifyAlertCallbacks(AnomalyAlert alert)
        {
            lock (_alertCallbacks)
            {
                foreach (var callback in _alertCallbacks)
                {
                    try
                    {
                        callback?.Invoke(alert);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Alert callback error: {ex.Message}");
                    }
                }
            }
        }

        private class MetricBaseline
        {
            public double Mean { get; set; }
            public double StdDev { get; set; }
            public double Min { get; set; }
            public double Max { get; set; }
            public int SampleCount { get; set; }
            public DateTime LastUpdateTime { get; set; }
        }
    }
}
