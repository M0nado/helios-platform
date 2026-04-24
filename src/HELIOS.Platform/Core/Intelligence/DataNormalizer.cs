using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using HELIOS.Platform.Core.Intelligence.Interfaces;

namespace HELIOS.Platform.Core.Intelligence
{
    /// <summary>
    /// Standardizes metrics for ML processing using normalization techniques.
    /// </summary>
    public class DataNormalizer : IDataNormalizer, IDisposable
    {
        private readonly Dictionary<string, List<double>> _metricHistory = new();
        private readonly Dictionary<string, (double min, double max)> _metricBounds = new();
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly ILogger<DataNormalizer> _logger;
        private const int MaxHistorySize = 1000;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the DataNormalizer class.
        /// </summary>
        public DataNormalizer(ILogger<DataNormalizer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("DataNormalizer initialized");
        }

        /// <summary>
        /// Normalizes raw metrics using Z-score normalization.
        /// </summary>
        public async Task<Dictionary<string, double>> NormalizeAsync(Dictionary<string, double> metrics)
        {
            if (metrics == null)
                throw new ArgumentNullException(nameof(metrics));

            ThrowIfDisposed();
            await _semaphore.WaitAsync();
            try
            {
                var normalized = new Dictionary<string, double>();

                foreach (var metric in metrics)
                {
                    try
                    {
                        // Add to history
                        if (!_metricHistory.ContainsKey(metric.Key))
                            _metricHistory[metric.Key] = new List<double>();

                        _metricHistory[metric.Key].Add(metric.Value);
                        if (_metricHistory[metric.Key].Count > MaxHistorySize)
                            _metricHistory[metric.Key].RemoveAt(0);

                        // Calculate Z-score
                        var history = _metricHistory[metric.Key];
                        var mean = history.Average();
                        var stdDev = CalculateStandardDeviation(history, mean);

                        normalized[metric.Key] = stdDev > 0 ? (metric.Value - mean) / stdDev : 0;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Error normalizing metric '{metric.Key}': {ex.Message}");
                        normalized[metric.Key] = 0;
                    }
                }

                return normalized;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Registers a metric for normalization with min/max bounds.
        /// </summary>
        public async Task RegisterMetricBoundsAsync(string metricName, double minValue, double maxValue)
        {
            if (string.IsNullOrWhiteSpace(metricName))
                throw new ArgumentException("Metric name cannot be null or whitespace", nameof(metricName));
            if (minValue >= maxValue)
                throw new ArgumentException("Min value must be less than max value");

            ThrowIfDisposed();
            await _semaphore.WaitAsync();
            try
            {
                _metricBounds[metricName] = (minValue, maxValue);
                _logger.LogInformation($"Registered bounds for metric '{metricName}': [{minValue}, {maxValue}]");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Gets normalization statistics for a metric.
        /// </summary>
        public async Task<Dictionary<string, double>> GetNormalizationStatsAsync(string metricName)
        {
            if (string.IsNullOrWhiteSpace(metricName))
                throw new ArgumentException("Metric name cannot be null or whitespace", nameof(metricName));

            ThrowIfDisposed();
            await _semaphore.WaitAsync();
            try
            {
                if (!_metricHistory.ContainsKey(metricName))
                    return new Dictionary<string, double>();

                var history = _metricHistory[metricName];
                var mean = history.Average();
                var stdDev = CalculateStandardDeviation(history, mean);

                return new Dictionary<string, double>
                {
                    { "Mean", mean },
                    { "StdDev", stdDev },
                    { "Min", history.Min() },
                    { "Max", history.Max() },
                    { "Count", history.Count }
                };
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Clears historical data for normalization recalculation.
        /// </summary>
        public async Task ClearHistoryAsync()
        {
            ThrowIfDisposed();
            await _semaphore.WaitAsync();
            try
            {
                _metricHistory.Clear();
                _logger.LogInformation("Normalization history cleared");
            }
            finally
            {
                _semaphore.Release();
            }
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
                throw new ObjectDisposedException(nameof(DataNormalizer));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _semaphore?.Dispose();
            _disposed = true;
            _logger.LogInformation("DataNormalizer disposed");
        }
    }
}
