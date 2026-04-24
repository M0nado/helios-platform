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
    /// Fast in-memory time-series database for metric storage and retrieval.
    /// </summary>
    public class InMemoryTimeSeriesDB : ITimeSeriesDB, IDisposable
    {
        private readonly Dictionary<string, List<(DateTime, double)>> _timeSeries = new();
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly ILogger<InMemoryTimeSeriesDB> _logger;
        private const int MaxPointsPerSeries = 10000;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the InMemoryTimeSeriesDB class.
        /// </summary>
        public InMemoryTimeSeriesDB(ILogger<InMemoryTimeSeriesDB> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("InMemoryTimeSeriesDB initialized");
        }

        /// <summary>
        /// Stores a data point with timestamp.
        /// </summary>
        public async Task StoreAsync(string seriesName, double value, DateTime? timestamp = null)
        {
            if (string.IsNullOrWhiteSpace(seriesName))
                throw new ArgumentException("Series name cannot be null or whitespace", nameof(seriesName));

            ThrowIfDisposed();
            await _semaphore.WaitAsync();
            try
            {
                if (!_timeSeries.ContainsKey(seriesName))
                    _timeSeries[seriesName] = new List<(DateTime, double)>();

                var ts = timestamp ?? DateTime.UtcNow;
                _timeSeries[seriesName].Add((ts, value));

                // Enforce max points limit
                if (_timeSeries[seriesName].Count > MaxPointsPerSeries)
                {
                    _timeSeries[seriesName].RemoveAt(0);
                    _logger.LogDebug($"Series '{seriesName}' exceeded max points, oldest entry removed");
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Retrieves data points within a time range.
        /// </summary>
        public async Task<List<(DateTime, double)>> QueryAsync(string seriesName, DateTime startTime, DateTime endTime)
        {
            if (string.IsNullOrWhiteSpace(seriesName))
                throw new ArgumentException("Series name cannot be null or whitespace", nameof(seriesName));

            ThrowIfDisposed();
            await _semaphore.WaitAsync();
            try
            {
                if (!_timeSeries.ContainsKey(seriesName))
                    return new List<(DateTime, double)>();

                var stopwatch = Stopwatch.StartNew();
                var result = _timeSeries[seriesName]
                    .Where(p => p.Item1 >= startTime && p.Item1 <= endTime)
                    .ToList();

                stopwatch.Stop();
                _logger.LogDebug($"Queried {result.Count} points from '{seriesName}' in {stopwatch.ElapsedMilliseconds}ms");
                return result;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Gets the latest N data points for a series.
        /// </summary>
        public async Task<List<(DateTime, double)>> GetRecentAsync(string seriesName, int count)
        {
            if (string.IsNullOrWhiteSpace(seriesName))
                throw new ArgumentException("Series name cannot be null or whitespace", nameof(seriesName));
            if (count <= 0)
                throw new ArgumentException("Count must be positive", nameof(count));

            ThrowIfDisposed();
            await _semaphore.WaitAsync();
            try
            {
                if (!_timeSeries.ContainsKey(seriesName))
                    return new List<(DateTime, double)>();

                var series = _timeSeries[seriesName];
                var skipCount = Math.Max(0, series.Count - count);
                return series.Skip(skipCount).ToList();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Deletes data older than specified age.
        /// </summary>
        public async Task<int> PurgeOldDataAsync(TimeSpan olderThan)
        {
            ThrowIfDisposed();
            await _semaphore.WaitAsync();
            try
            {
                int deletedCount = 0;
                var cutoffTime = DateTime.UtcNow - olderThan;

                foreach (var series in _timeSeries.Values)
                {
                    var beforeCount = series.Count;
                    series.RemoveAll(p => p.Item1 < cutoffTime);
                    deletedCount += beforeCount - series.Count;
                }

                _logger.LogInformation($"Purged {deletedCount} data points older than {olderThan.TotalHours}h");
                return deletedCount;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Gets aggregate statistics for a series within a time range.
        /// </summary>
        public async Task<Dictionary<string, double>> GetAggregateStatsAsync(string seriesName, DateTime startTime, DateTime endTime)
        {
            if (string.IsNullOrWhiteSpace(seriesName))
                throw new ArgumentException("Series name cannot be null or whitespace", nameof(seriesName));

            ThrowIfDisposed();
            await _semaphore.WaitAsync();
            try
            {
                var stats = new Dictionary<string, double>();

                if (!_timeSeries.ContainsKey(seriesName))
                    return stats;

                var dataPoints = _timeSeries[seriesName]
                    .Where(p => p.Item1 >= startTime && p.Item1 <= endTime)
                    .Select(p => p.Item2)
                    .ToList();

                if (dataPoints.Count == 0)
                    return stats;

                stats["Count"] = dataPoints.Count;
                stats["Min"] = dataPoints.Min();
                stats["Max"] = dataPoints.Max();
                stats["Avg"] = dataPoints.Average();
                stats["Sum"] = dataPoints.Sum();
                stats["StdDev"] = CalculateStandardDeviation(dataPoints);

                _logger.LogDebug($"Calculated aggregate stats for '{seriesName}': {stats["Count"]} points");
                return stats;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Gets available series names.
        /// </summary>
        public async Task<List<string>> GetSeriesNamesAsync()
        {
            ThrowIfDisposed();
            await _semaphore.WaitAsync();
            try
            {
                return _timeSeries.Keys.ToList();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private static double CalculateStandardDeviation(List<double> values)
        {
            if (values.Count < 2)
                return 0;

            var mean = values.Average();
            var sumOfSquares = values.Sum(v => Math.Pow(v - mean, 2));
            return Math.Sqrt(sumOfSquares / (values.Count - 1));
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(InMemoryTimeSeriesDB));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _semaphore?.Dispose();
            _timeSeries.Clear();
            _disposed = true;
            _logger.LogInformation("InMemoryTimeSeriesDB disposed");
        }
    }
}
