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
    /// Aggregates metrics from all services in real-time with caching support.
    /// </summary>
    public class DataCollector : Interfaces.IDataCollector, IDisposable
    {
        private readonly Dictionary<string, Func<Task<double>>> _metricSources = new();
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly ILogger<DataCollector> _logger;
        private Dictionary<string, long> _collectionTimes = new();
        private int _totalCollections = 0;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the DataCollector class.
        /// </summary>
        public DataCollector(ILogger<DataCollector> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("DataCollector initialized");
        }

        /// <summary>
        /// Collects metrics from all registered sources.
        /// </summary>
        public async Task<Dictionary<string, double>> CollectMetricsAsync()
        {
            ThrowIfDisposed();
            await _semaphore.WaitAsync();
            try
            {
                var stopwatch = Stopwatch.StartNew();
                var metrics = new Dictionary<string, double>();

                foreach (var source in _metricSources)
                {
                    try
                    {
                        var sourceStopwatch = Stopwatch.StartNew();
                        var value = await source.Value();
                        sourceStopwatch.Stop();
                        
                        metrics[source.Key] = value;
                        _collectionTimes[source.Key] = sourceStopwatch.ElapsedMilliseconds;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Error collecting metric '{source.Key}': {ex.Message}");
                        metrics[source.Key] = 0;
                    }
                }

                stopwatch.Stop();
                _totalCollections++;

                _logger.LogDebug($"Collected {metrics.Count} metrics in {stopwatch.ElapsedMilliseconds}ms");
                return metrics;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Registers a metric source with the collector.
        /// </summary>
        public async Task RegisterMetricSourceAsync(string sourceName, Func<Task<double>> metricFunc)
        {
            if (string.IsNullOrWhiteSpace(sourceName))
                throw new ArgumentException("Source name cannot be null or whitespace", nameof(sourceName));
            if (metricFunc == null)
                throw new ArgumentNullException(nameof(metricFunc));

            ThrowIfDisposed();
            await _semaphore.WaitAsync();
            try
            {
                _metricSources[sourceName] = metricFunc;
                _logger.LogInformation($"Registered metric source: {sourceName}");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Unregisters a metric source.
        /// </summary>
        public async Task UnregisterMetricSourceAsync(string sourceName)
        {
            if (string.IsNullOrWhiteSpace(sourceName))
                throw new ArgumentException("Source name cannot be null or whitespace", nameof(sourceName));

            ThrowIfDisposed();
            await _semaphore.WaitAsync();
            try
            {
                if (_metricSources.Remove(sourceName))
                {
                    _collectionTimes.Remove(sourceName);
                    _logger.LogInformation($"Unregistered metric source: {sourceName}");
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Gets collection statistics.
        /// </summary>
        public async Task<Dictionary<string, object>> GetCollectionStatsAsync()
        {
            ThrowIfDisposed();
            await _semaphore.WaitAsync();
            try
            {
                var stats = new Dictionary<string, object>
                {
                    { "TotalCollections", _totalCollections },
                    { "RegisteredSources", _metricSources.Count },
                    { "AverageSourceTime", _collectionTimes.Count > 0 ? _collectionTimes.Values.Average() : 0 },
                    { "MaxSourceTime", _collectionTimes.Count > 0 ? _collectionTimes.Values.Max() : 0 },
                    { "MinSourceTime", _collectionTimes.Count > 0 ? _collectionTimes.Values.Min() : 0 }
                };

                return stats;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(DataCollector));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _semaphore?.Dispose();
            _disposed = true;
            _logger.LogInformation("DataCollector disposed");
        }
    }
}
