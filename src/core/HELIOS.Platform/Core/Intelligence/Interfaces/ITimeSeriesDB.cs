using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Intelligence.Interfaces
{
    /// <summary>
    /// Interface for in-memory time-series database.
    /// </summary>
    public interface ITimeSeriesDB
    {
        /// <summary>
        /// Stores a data point with timestamp.
        /// </summary>
        /// <param name="seriesName">Name of the time series.</param>
        /// <param name="value">Data value.</param>
        /// <param name="timestamp">Timestamp of the data point.</param>
        Task StoreAsync(string seriesName, double value, DateTime? timestamp = null);

        /// <summary>
        /// Retrieves data points within a time range.
        /// </summary>
        /// <param name="seriesName">Name of the time series.</param>
        /// <param name="startTime">Start of time range.</param>
        /// <param name="endTime">End of time range.</param>
        /// <returns>List of data points.</returns>
        Task<List<(DateTime, double)>> QueryAsync(string seriesName, DateTime startTime, DateTime endTime);

        /// <summary>
        /// Gets the latest N data points for a series.
        /// </summary>
        /// <param name="seriesName">Name of the time series.</param>
        /// <param name="count">Number of recent data points to retrieve.</param>
        /// <returns>List of recent data points.</returns>
        Task<List<(DateTime, double)>> GetRecentAsync(string seriesName, int count);

        /// <summary>
        /// Deletes data older than specified age.
        /// </summary>
        /// <param name="olderThan">Age threshold for deletion.</param>
        /// <returns>Number of data points deleted.</returns>
        Task<int> PurgeOldDataAsync(TimeSpan olderThan);

        /// <summary>
        /// Gets aggregate statistics for a series within a time range.
        /// </summary>
        /// <param name="seriesName">Name of the time series.</param>
        /// <param name="startTime">Start of time range.</param>
        /// <param name="endTime">End of time range.</param>
        /// <returns>Dictionary containing min, max, avg, and count statistics.</returns>
        Task<Dictionary<string, double>> GetAggregateStatsAsync(string seriesName, DateTime startTime, DateTime endTime);

        /// <summary>
        /// Gets available series names.
        /// </summary>
        Task<List<string>> GetSeriesNamesAsync();
    }
}
