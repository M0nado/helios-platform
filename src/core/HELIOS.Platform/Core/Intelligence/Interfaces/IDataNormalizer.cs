using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Intelligence.Interfaces
{
    /// <summary>
    /// Interface for normalizing metrics for ML processing.
    /// </summary>
    public interface IDataNormalizer
    {
        /// <summary>
        /// Normalizes raw metrics using Z-score normalization.
        /// </summary>
        /// <param name="metrics">Raw metrics to normalize.</param>
        /// <returns>Normalized metrics.</returns>
        Task<Dictionary<string, double>> NormalizeAsync(Dictionary<string, double> metrics);

        /// <summary>
        /// Registers a metric for normalization with min/max bounds.
        /// </summary>
        /// <param name="metricName">Name of the metric.</param>
        /// <param name="minValue">Minimum expected value.</param>
        /// <param name="maxValue">Maximum expected value.</param>
        Task RegisterMetricBoundsAsync(string metricName, double minValue, double maxValue);

        /// <summary>
        /// Gets normalization statistics for a metric.
        /// </summary>
        Task<Dictionary<string, double>> GetNormalizationStatsAsync(string metricName);

        /// <summary>
        /// Clears historical data for normalization recalculation.
        /// </summary>
        Task ClearHistoryAsync();
    }
}
