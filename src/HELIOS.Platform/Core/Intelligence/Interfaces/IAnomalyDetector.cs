using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Intelligence.Interfaces
{
    /// <summary>
    /// Interface for detecting anomalies in data.
    /// </summary>
    public interface IAnomalyDetector
    {
        /// <summary>
        /// Detects anomalies in a data point based on historical context.
        /// </summary>
        /// <param name="seriesName">Name of the time series.</param>
        /// <param name="value">Current value to check.</param>
        /// <param name="sensitivity">Sensitivity level (1-10, higher = more sensitive).</param>
        /// <returns>Anomaly score (0-1, where 1 is certain anomaly).</returns>
        Task<double> DetectAnomalyAsync(string seriesName, double value, int sensitivity = 5);

        /// <summary>
        /// Batch detects anomalies in multiple values.
        /// </summary>
        /// <param name="seriesName">Name of the time series.</param>
        /// <param name="values">Values to check for anomalies.</param>
        /// <returns>List of anomaly scores corresponding to each value.</returns>
        Task<List<double>> DetectBatchAnomaliesAsync(string seriesName, List<double> values);

        /// <summary>
        /// Sets the anomaly detection model for a series.
        /// </summary>
        /// <param name="seriesName">Name of the time series.</param>
        /// <param name="historicalData">Historical data for model training.</param>
        Task TrainModelAsync(string seriesName, List<double> historicalData);

        /// <summary>
        /// Gets anomaly detection statistics.
        /// </summary>
        Task<Dictionary<string, object>> GetDetectionStatsAsync();
    }
}
