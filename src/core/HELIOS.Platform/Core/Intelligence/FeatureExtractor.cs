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
    /// Extracts statistical features from time-series data for ML processing.
    /// </summary>
    public class FeatureExtractor : Interfaces.IFeatureExtractor, IDisposable
    {
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly ILogger<FeatureExtractor> _logger;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the FeatureExtractor class.
        /// </summary>
        public FeatureExtractor(ILogger<FeatureExtractor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("FeatureExtractor initialized");
        }

        /// <summary>
        /// Extracts features from time-series data.
        /// </summary>
        public async Task<Dictionary<string, double>> ExtractFeaturesAsync(List<double> dataPoints)
        {
            if (dataPoints == null || dataPoints.Count == 0)
                throw new ArgumentException("Data points cannot be null or empty", nameof(dataPoints));

            ThrowIfDisposed();
            await _semaphore.WaitAsync();
            try
            {
                var stopwatch = Stopwatch.StartNew();
                var features = new Dictionary<string, double>();

                // Basic statistics
                var mean = dataPoints.Average();
                var min = dataPoints.Min();
                var max = dataPoints.Max();
                var stdDev = CalculateStandardDeviation(dataPoints, mean);
                var range = max - min;

                features["Mean"] = mean;
                features["Min"] = min;
                features["Max"] = max;
                features["StdDev"] = stdDev;
                features["Range"] = range;
                features["Variance"] = stdDev * stdDev;
                features["Coefficient_of_Variation"] = stdDev > 0 ? stdDev / mean : 0;

                // Quartiles
                var sorted = dataPoints.OrderBy(x => x).ToList();
                features["Q1"] = CalculateQuartile(sorted, 0.25);
                features["Median"] = CalculateQuartile(sorted, 0.5);
                features["Q3"] = CalculateQuartile(sorted, 0.75);
                features["IQR"] = features["Q3"] - features["Q1"];

                // Skewness and Kurtosis
                features["Skewness"] = CalculateSkewness(dataPoints, mean, stdDev);
                features["Kurtosis"] = CalculateKurtosis(dataPoints, mean, stdDev);

                // Trend
                features["Trend_Slope"] = await CalculateTrendSlopeAsync(dataPoints);

                stopwatch.Stop();
                features["Extraction_Time_Ms"] = stopwatch.ElapsedMilliseconds;

                _logger.LogDebug($"Extracted {features.Count} features in {stopwatch.ElapsedMilliseconds}ms");
                return features;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Extracts moving average features for a window size.
        /// </summary>
        public async Task<List<double>> ExtractMovingAverageAsync(List<double> dataPoints, int windowSize)
        {
            if (dataPoints == null || dataPoints.Count == 0)
                throw new ArgumentException("Data points cannot be null or empty", nameof(dataPoints));
            if (windowSize <= 0 || windowSize > dataPoints.Count)
                throw new ArgumentException("Window size must be positive and not exceed data point count", nameof(windowSize));

            ThrowIfDisposed();
            await _semaphore.WaitAsync();
            try
            {
                var movingAverages = new List<double>();
                for (int i = 0; i <= dataPoints.Count - windowSize; i++)
                {
                    var window = dataPoints.Skip(i).Take(windowSize).Average();
                    movingAverages.Add(window);
                }
                _logger.LogDebug($"Calculated moving averages with window size {windowSize}");
                return movingAverages;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Calculates trend slope using linear regression.
        /// </summary>
        public async Task<double> CalculateTrendSlopeAsync(List<double> dataPoints)
        {
            if (dataPoints == null || dataPoints.Count < 2)
                return 0;

            ThrowIfDisposed();
            await _semaphore.WaitAsync();
            try
            {
                int n = dataPoints.Count;
                double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;

                for (int i = 0; i < n; i++)
                {
                    sumX += i;
                    sumY += dataPoints[i];
                    sumXY += i * dataPoints[i];
                    sumX2 += i * i;
                }

                double slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
                return slope;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Extracts seasonal components from data.
        /// </summary>
        public async Task<Dictionary<string, double>> ExtractSeasonalComponentsAsync(List<double> dataPoints, int seasonalityPeriod)
        {
            if (dataPoints == null || dataPoints.Count == 0)
                throw new ArgumentException("Data points cannot be null or empty", nameof(dataPoints));
            if (seasonalityPeriod <= 0)
                throw new ArgumentException("Seasonality period must be positive", nameof(seasonalityPeriod));

            ThrowIfDisposed();
            await _semaphore.WaitAsync();
            try
            {
                var components = new Dictionary<string, double>();
                
                // Calculate seasonal averages
                var seasonalAverages = new List<double>();
                for (int i = 0; i < seasonalityPeriod; i++)
                {
                    var values = new List<double>();
                    for (int j = i; j < dataPoints.Count; j += seasonalityPeriod)
                    {
                        values.Add(dataPoints[j]);
                    }
                    if (values.Count > 0)
                        seasonalAverages.Add(values.Average());
                }

                // Calculate seasonal indices
                var overallMean = dataPoints.Average();
                for (int i = 0; i < seasonalAverages.Count; i++)
                {
                    components[$"Seasonal_Index_{i}"] = overallMean > 0 ? seasonalAverages[i] / overallMean : 0;
                }

                // Calculate seasonal strength
                var seasonalVariance = seasonalAverages.Sum(s => Math.Pow(s - overallMean, 2)) / seasonalAverages.Count;
                var totalVariance = dataPoints.Sum(d => Math.Pow(d - overallMean, 2)) / dataPoints.Count;
                components["Seasonal_Strength"] = totalVariance > 0 ? seasonalVariance / totalVariance : 0;

                return components;
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

        private static double CalculateQuartile(List<double> sortedValues, double percentile)
        {
            int index = (int)((percentile * sortedValues.Count) - 0.5);
            index = Math.Max(0, Math.Min(index, sortedValues.Count - 1));
            return sortedValues[index];
        }

        private static double CalculateSkewness(List<double> values, double mean, double stdDev)
        {
            if (stdDev == 0 || values.Count < 3)
                return 0;

            var skewness = values.Sum(v => Math.Pow((v - mean) / stdDev, 3)) / values.Count;
            return skewness;
        }

        private static double CalculateKurtosis(List<double> values, double mean, double stdDev)
        {
            if (stdDev == 0 || values.Count < 4)
                return 0;

            var kurtosis = values.Sum(v => Math.Pow((v - mean) / stdDev, 4)) / values.Count - 3;
            return kurtosis;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(FeatureExtractor));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _semaphore?.Dispose();
            _disposed = true;
            _logger.LogInformation("FeatureExtractor disposed");
        }
    }
}
