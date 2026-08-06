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
    /// Provides predictive analytics and trend forecasting using statistical methods.
    /// </summary>
    public class PredictiveAnalytics : Interfaces.IPredictiveAnalytics, IDisposable
    {
        private readonly Dictionary<string, List<double>> _historicalData = new();
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly ILogger<PredictiveAnalytics> _logger;
        private const int MaxHistorySize = 5000;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the PredictiveAnalytics class.
        /// </summary>
        public PredictiveAnalytics(ILogger<PredictiveAnalytics> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("PredictiveAnalytics initialized");
        }

        /// <summary>
        /// Predicts future values using trend analysis.
        /// </summary>
        public async Task<List<double>> PredictTrendAsync(string seriesName, int periodsAhead)
        {
            if (string.IsNullOrWhiteSpace(seriesName))
                throw new ArgumentException("Series name cannot be null or whitespace", nameof(seriesName));
            if (periodsAhead <= 0)
                throw new ArgumentException("Periods ahead must be positive", nameof(periodsAhead));

            ThrowIfDisposed();
            await _semaphore.WaitAsync();
            try
            {
                if (!_historicalData.ContainsKey(seriesName) || _historicalData[seriesName].Count < 2)
                    return new List<double>();

                var stopwatch = Stopwatch.StartNew();
                var data = _historicalData[seriesName];
                var (slope, intercept) = CalculateLinearRegression(data);

                var predictions = new List<double>();
                for (int i = 1; i <= periodsAhead; i++)
                {
                    var predicted = slope * (data.Count + i) + intercept;
                    predictions.Add(predicted);
                }

                stopwatch.Stop();
                _logger.LogDebug($"Predicted {periodsAhead} periods for '{seriesName}' in {stopwatch.ElapsedMilliseconds}ms");
                return predictions;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Calculates confidence intervals for predictions.
        /// </summary>
        public async Task<List<(double, double)>> GetPredictionConfidenceIntervalsAsync(string seriesName, int periodsAhead, double confidenceLevel = 0.95)
        {
            if (string.IsNullOrWhiteSpace(seriesName))
                throw new ArgumentException("Series name cannot be null or whitespace", nameof(seriesName));
            if (periodsAhead <= 0)
                throw new ArgumentException("Periods ahead must be positive", nameof(periodsAhead));
            if (confidenceLevel <= 0 || confidenceLevel >= 1)
                throw new ArgumentException("Confidence level must be between 0 and 1", nameof(confidenceLevel));

            ThrowIfDisposed();
            await _semaphore.WaitAsync();
            try
            {
                if (!_historicalData.ContainsKey(seriesName) || _historicalData[seriesName].Count < 2)
                    return new List<(double, double)>();

                var data = _historicalData[seriesName];
                var predictions = await PredictTrendAsync(seriesName, periodsAhead);
                var stdError = CalculateStandardError(data);

                // Z-score for confidence level (simplified)
                var zScore = GetZScoreForConfidence(confidenceLevel);
                var marginOfError = zScore * stdError;

                var intervals = predictions.Select(p => (p - marginOfError, p + marginOfError)).ToList();
                _logger.LogDebug($"Calculated {intervals.Count} confidence intervals for '{seriesName}'");
                return intervals;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Forecasts peak values for resource planning.
        /// </summary>
        public async Task<(double peakValue, DateTime peakTime)> ForecastPeakAsync(string seriesName, int lookAheadPeriod = 24)
        {
            if (string.IsNullOrWhiteSpace(seriesName))
                throw new ArgumentException("Series name cannot be null or whitespace", nameof(seriesName));
            if (lookAheadPeriod <= 0)
                throw new ArgumentException("Look ahead period must be positive", nameof(lookAheadPeriod));

            ThrowIfDisposed();
            await _semaphore.WaitAsync();
            try
            {
                if (!_historicalData.ContainsKey(seriesName) || _historicalData[seriesName].Count < 2)
                    return (0, DateTime.UtcNow);

                var predictions = await PredictTrendAsync(seriesName, lookAheadPeriod);
                if (predictions.Count == 0)
                    return (0, DateTime.UtcNow);

                var maxIndex = predictions.FindIndex(p => p == predictions.Max());
                var peakTime = DateTime.UtcNow.AddHours(maxIndex + 1);

                return (predictions[maxIndex], peakTime);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Predicts if a metric will exceed a threshold soon.
        /// </summary>
        public async Task<double> PredictThresholdBreachAsync(string seriesName, double threshold, int lookAheadPeriods)
        {
            if (string.IsNullOrWhiteSpace(seriesName))
                throw new ArgumentException("Series name cannot be null or whitespace", nameof(seriesName));
            if (lookAheadPeriods <= 0)
                throw new ArgumentException("Look ahead periods must be positive", nameof(lookAheadPeriods));

            ThrowIfDisposed();
            await _semaphore.WaitAsync();
            try
            {
                if (!_historicalData.ContainsKey(seriesName) || _historicalData[seriesName].Count < 2)
                    return 0;

                var predictions = await PredictTrendAsync(seriesName, lookAheadPeriods);
                if (predictions.Count == 0)
                    return 0;

                var breachCount = predictions.Count(p => p > threshold);
                var probability = (double)breachCount / predictions.Count;

                _logger.LogDebug($"Threshold breach probability for '{seriesName}': {probability:F3}");
                return probability;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Adds historical data for a series (used internally for predictions).
        /// </summary>
        internal async Task AddHistoricalDataAsync(string seriesName, List<double> data)
        {
            if (string.IsNullOrWhiteSpace(seriesName))
                throw new ArgumentException("Series name cannot be null or whitespace", nameof(seriesName));
            if (data == null || data.Count == 0)
                throw new ArgumentException("Data cannot be null or empty", nameof(data));

            await _semaphore.WaitAsync();
            try
            {
                if (!_historicalData.ContainsKey(seriesName))
                    _historicalData[seriesName] = new List<double>();

                _historicalData[seriesName].AddRange(data);
                if (_historicalData[seriesName].Count > MaxHistorySize)
                    _historicalData[seriesName].RemoveRange(0, _historicalData[seriesName].Count - MaxHistorySize);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private static (double slope, double intercept) CalculateLinearRegression(List<double> data)
        {
            int n = data.Count;
            double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;

            for (int i = 0; i < n; i++)
            {
                sumX += i;
                sumY += data[i];
                sumXY += i * data[i];
                sumX2 += i * i;
            }

            double slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            double intercept = (sumY - slope * sumX) / n;

            return (slope, intercept);
        }

        private static double CalculateStandardError(List<double> data)
        {
            var mean = data.Average();
            var sumOfSquares = data.Sum(v => Math.Pow(v - mean, 2));
            var variance = sumOfSquares / (data.Count - 1);
            var stdDev = Math.Sqrt(variance);
            return stdDev / Math.Sqrt(data.Count);
        }

        private static double GetZScoreForConfidence(double confidence)
        {
            return confidence switch
            {
                0.90 => 1.645,
                0.95 => 1.96,
                0.99 => 2.576,
                _ => 1.96
            };
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PredictiveAnalytics));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _semaphore?.Dispose();
            _historicalData.Clear();
            _disposed = true;
            _logger.LogInformation("PredictiveAnalytics disposed");
        }
    }
}
