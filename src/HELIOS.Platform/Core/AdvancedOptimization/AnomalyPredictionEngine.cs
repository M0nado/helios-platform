using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace HELIOS.Platform.Core.AdvancedOptimization
{
    /// <summary>
    /// Anomaly Prediction Engine implementation.
    /// Provides predictive anomaly detection using pattern recognition.
    /// </summary>
    public class AnomalyPredictionEngine : IAnomalyPredictionEngine
    {
        private readonly ILogger<AnomalyPredictionEngine> _logger;
        private readonly SemaphoreSlim _semaphore;
        private readonly ConcurrentQueue<MetricsSnapshot> _metricsHistory;
        private readonly ConcurrentQueue<AnomalyPredictions> _predictionHistory;
        private Dictionary<string, (double Mean, double StdDev)> _learnedPatterns;
        private bool _isRunning;

        /// <summary>
        /// Initializes a new instance of the AnomalyPredictionEngine class.
        /// </summary>
        public AnomalyPredictionEngine(ILogger<AnomalyPredictionEngine> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _semaphore = new SemaphoreSlim(1, 1);
            _metricsHistory = new ConcurrentQueue<MetricsSnapshot>();
            _predictionHistory = new ConcurrentQueue<AnomalyPredictions>();
            _learnedPatterns = new Dictionary<string, (double, double)>();
            _isRunning = false;
        }

        /// <inheritdoc/>
        public string ServiceName => nameof(AnomalyPredictionEngine);

        /// <inheritdoc/>
        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                _logger.LogInformation("{ServiceName} initializing", ServiceName);
                await Task.CompletedTask;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                _isRunning = true;
                _logger.LogInformation("{ServiceName} started", ServiceName);
                await Task.CompletedTask;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                _isRunning = false;
                _logger.LogInformation("{ServiceName} stopped", ServiceName);
                await Task.CompletedTask;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public bool IsRunning() => _isRunning;

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            _semaphore?.Dispose();
            await Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task<AnomalyPredictions> PredictAnomaliesAsync(Dictionary<string, double> currentMetrics, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                var predictions = new AnomalyPredictions { Timestamp = DateTime.UtcNow };

                if (currentMetrics == null || currentMetrics.Count == 0)
                {
                    predictions.OverallAnomalyScore = 0;
                    _predictionHistory.Enqueue(predictions);
                    return predictions;
                }

                double totalScore = 0;

                foreach (var metric in currentMetrics)
                {
                    if (_learnedPatterns.ContainsKey(metric.Key))
                    {
                        var (mean, stdDev) = _learnedPatterns[metric.Key];
                        double zScore = stdDev > 0 ? Math.Abs((metric.Value - mean) / stdDev) : 0;
                        double anomalyScore = Math.Min(zScore * 25, 100);

                        if (anomalyScore > 50)
                        {
                            var anomaly = new AnomalyPrediction
                            {
                                MetricName = metric.Key,
                                CurrentValue = metric.Value,
                                ExpectedMin = mean - (2 * stdDev),
                                ExpectedMax = mean + (2 * stdDev),
                                AnomalyScore = anomalyScore,
                                AnomalyType = zScore > 0 ? "Spike" : "Drop",
                                Probability = Math.Min(zScore / 4.0, 1.0),
                                Severity = anomalyScore > 80 ? "Critical" : (anomalyScore > 60 ? "High" : "Medium")
                            };
                            predictions.Anomalies.Add(anomaly);
                        }

                        totalScore += anomalyScore;
                    }
                }

                predictions.OverallAnomalyScore = currentMetrics.Count > 0 ? totalScore / currentMetrics.Count : 0;
                predictions.CriticalCount = predictions.Anomalies.Count(a => a.Severity == "Critical");

                _predictionHistory.Enqueue(predictions);
                _logger.LogInformation("Anomaly prediction completed with score: {Score}", predictions.OverallAnomalyScore);

                return predictions;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public async Task LearnPatternsAsync(List<MetricsSnapshot> historicalMetrics, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                if (historicalMetrics == null || historicalMetrics.Count == 0)
                {
                    return;
                }

                var allMetricNames = new HashSet<string>();
                foreach (var snapshot in historicalMetrics)
                {
                    foreach (var metric in snapshot.Metrics.Keys)
                    {
                        allMetricNames.Add(metric);
                    }
                }

                foreach (var metricName in allMetricNames)
                {
                    var values = historicalMetrics
                        .Where(s => s.Metrics.ContainsKey(metricName))
                        .Select(s => s.Metrics[metricName])
                        .ToList();

                    if (values.Count > 0)
                    {
                        double mean = values.Average();
                        double stdDev = CalculateStandardDeviation(values);
                        _learnedPatterns[metricName] = (mean, stdDev);
                    }
                }

                _logger.LogInformation("Learned patterns for {Count} metrics", allMetricNames.Count);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<List<AnomalyAlert>> GenerateAlertsAsync(List<AnomalyPrediction> anomalies, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                var alerts = new List<AnomalyAlert>();

                if (anomalies == null)
                {
                    return alerts;
                }

                foreach (var anomaly in anomalies)
                {
                    var alert = new AnomalyAlert
                    {
                        Anomaly = anomaly,
                        Message = $"Detected {anomaly.AnomalyType} in {anomaly.MetricName}: {anomaly.CurrentValue:F2}",
                        RecommendedAction = GenerateRecommendation(anomaly),
                        Severity = anomaly.Severity,
                        IsAcknowledged = false
                    };
                    alerts.Add(alert);
                }

                return alerts;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public async Task RecordMetricsAsync(MetricsSnapshot snapshot)
        {
            await _semaphore.WaitAsync();
            try
            {
                _metricsHistory.Enqueue(snapshot);
                if (_metricsHistory.Count > 5000)
                {
                    _metricsHistory.TryDequeue(out _);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<List<AnomalyPredictions>> GetHistoryAsync(int limit = 100)
        {
            var results = new List<AnomalyPredictions>();
            int count = 0;

            foreach (var item in _predictionHistory.Reverse())
            {
                if (count >= limit) break;
                results.Add(item);
                count++;
            }

            return await Task.FromResult(results);
        }

        private double CalculateStandardDeviation(List<double> values)
        {
            if (!values.Any()) return 0;
            double average = values.Average();
            double sumOfSquares = values.Sum(v => Math.Pow(v - average, 2));
            return Math.Sqrt(sumOfSquares / values.Count());
        }

        private string GenerateRecommendation(AnomalyPrediction anomaly)
        {
            return anomaly.Severity switch
            {
                "Critical" => "Immediate investigation and corrective action required",
                "High" => "Priority attention needed within 1 hour",
                _ => "Monitor and take action if condition persists"
            };
        }
    }
}
