using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using HELIOS.Platform.Core.Intelligence.Interfaces;

namespace HELIOS.Platform.Core.Intelligence
{
    /// <summary>
    /// Detects anomalies in data using statistical methods.
    /// </summary>
    public class AnomalyDetector : Interfaces.IAnomalyDetector, IDisposable
    {
        private readonly Dictionary<string, AnomalyModel> _models = new();
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly ILogger<AnomalyDetector> _logger;
        private int _detectionsCount = 0;
        private int _anomaliesFound = 0;
        private bool _disposed;

        private class AnomalyModel
        {
            public double Mean { get; set; }
            public double StdDev { get; set; }
            public double Min { get; set; }
            public double Max { get; set; }
            public DateTime LastTrained { get; set; }
            public int SampleSize { get; set; }
        }

        /// <summary>
        /// Initializes a new instance of the AnomalyDetector class.
        /// </summary>
        public AnomalyDetector(ILogger<AnomalyDetector> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("AnomalyDetector initialized");
        }

        /// <summary>
        /// Detects anomalies in a data point based on historical context.
        /// </summary>
        public async Task<double> DetectAnomalyAsync(string seriesName, double value, int sensitivity = 5)
        {
            if (string.IsNullOrWhiteSpace(seriesName))
                throw new ArgumentException("Series name cannot be null or whitespace", nameof(seriesName));
            if (sensitivity < 1 || sensitivity > 10)
                throw new ArgumentException("Sensitivity must be between 1 and 10", nameof(sensitivity));

            ThrowIfDisposed();
            await _semaphore.WaitAsync();
            try
            {
                _detectionsCount++;

                if (!_models.ContainsKey(seriesName))
                    return 0;

                var model = _models[seriesName];
                var anomalyScore = CalculateAnomalyScore(value, model, sensitivity);

                if (anomalyScore > 0.5)
                    _anomaliesFound++;

                _logger.LogDebug($"Anomaly detection for '{seriesName}': score={anomalyScore:F3}");
                return anomalyScore;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Batch detects anomalies in multiple values.
        /// </summary>
        public async Task<List<double>> DetectBatchAnomaliesAsync(string seriesName, List<double> values)
        {
            if (string.IsNullOrWhiteSpace(seriesName))
                throw new ArgumentException("Series name cannot be null or whitespace", nameof(seriesName));
            if (values == null || values.Count == 0)
                throw new ArgumentException("Values cannot be null or empty", nameof(values));

            ThrowIfDisposed();
            await _semaphore.WaitAsync();
            try
            {
                var stopwatch = Stopwatch.StartNew();
                var scores = new List<double>();

                if (!_models.ContainsKey(seriesName))
                    return values.Select(_ => 0.0).ToList();

                var model = _models[seriesName];
                foreach (var value in values)
                {
                    var score = CalculateAnomalyScore(value, model, 5);
                    scores.Add(score);
                    if (score > 0.5)
                        _anomaliesFound++;
                }

                stopwatch.Stop();
                _detectionsCount += values.Count;
                _logger.LogDebug($"Batch anomaly detection for {values.Count} values in {stopwatch.ElapsedMilliseconds}ms");
                return scores;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Sets the anomaly detection model for a series.
        /// </summary>
        public async Task TrainModelAsync(string seriesName, List<double> historicalData)
        {
            if (string.IsNullOrWhiteSpace(seriesName))
                throw new ArgumentException("Series name cannot be null or whitespace", nameof(seriesName));
            if (historicalData == null || historicalData.Count < 2)
                throw new ArgumentException("Historical data must contain at least 2 points", nameof(historicalData));

            ThrowIfDisposed();
            await _semaphore.WaitAsync();
            try
            {
                var mean = historicalData.Average();
                var stdDev = CalculateStandardDeviation(historicalData, mean);

                _models[seriesName] = new AnomalyModel
                {
                    Mean = mean,
                    StdDev = stdDev,
                    Min = historicalData.Min(),
                    Max = historicalData.Max(),
                    LastTrained = DateTime.UtcNow,
                    SampleSize = historicalData.Count
                };

                _logger.LogInformation($"Trained anomaly model for '{seriesName}' with {historicalData.Count} samples (mean={mean:F3}, stddev={stdDev:F3})");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Gets anomaly detection statistics.
        /// </summary>
        public async Task<Dictionary<string, object>> GetDetectionStatsAsync()
        {
            ThrowIfDisposed();
            await _semaphore.WaitAsync();
            try
            {
                return new Dictionary<string, object>
                {
                    { "TotalDetections", _detectionsCount },
                    { "AnomaliesFound", _anomaliesFound },
                    { "AnomalyRate", _detectionsCount > 0 ? (double)_anomaliesFound / _detectionsCount : 0 },
                    { "TrainedModels", _models.Count },
                    { "Models", _models.Select(m => new {
                        Series = m.Key,
                        Mean = m.Value.Mean,
                        StdDev = m.Value.StdDev,
                        SampleSize = m.Value.SampleSize,
                        LastTrained = m.Value.LastTrained
                    }).ToList() }
                };
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private static double CalculateAnomalyScore(double value, AnomalyModel model, int sensitivity)
        {
            // Modified Z-score based on sensitivity
            if (model.StdDev == 0)
                return 0;

            var zScore = Math.Abs((value - model.Mean) / model.StdDev);
            var threshold = 3.0 - (sensitivity - 1) * 0.2; // Sensitivity scales threshold

            // Sigmoid function to convert Z-score to probability
            var probability = 1.0 / (1.0 + Math.Exp(-zScore + threshold));
            return Math.Min(1.0, probability);
        }

        private static double CalculateStandardDeviation(List<double> values, double mean)
        {
            if (values.Count < 2)
                return 0;

            var sumOfSquares = values.Sum(v => Math.Pow(v - mean, 2));
            return Math.Sqrt(sumOfSquares / (values.Count - 1));
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AnomalyDetector));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _semaphore?.Dispose();
            _models.Clear();
            _disposed = true;
            _logger.LogInformation("AnomalyDetector disposed");
        }
    }
}
