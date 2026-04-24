using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Intelligence.Interfaces
{
    /// <summary>
    /// Interface for collecting metrics from various services in real-time.
    /// </summary>
    public interface IDataCollector
    {
        /// <summary>
        /// Collects metrics from all registered sources.
        /// </summary>
        /// <returns>Dictionary of metric names and their values.</returns>
        Task<Dictionary<string, double>> CollectMetricsAsync();

        /// <summary>
        /// Registers a metric source with the collector.
        /// </summary>
        /// <param name="sourceName">Unique name for the metric source.</param>
        /// <param name="metricFunc">Function that returns metric value.</param>
        Task RegisterMetricSourceAsync(string sourceName, Func<Task<double>> metricFunc);

        /// <summary>
        /// Unregisters a metric source.
        /// </summary>
        Task UnregisterMetricSourceAsync(string sourceName);

        /// <summary>
        /// Gets collection statistics.
        /// </summary>
        Task<Dictionary<string, object>> GetCollectionStatsAsync();
    }
}
