using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.GlobalIntelligence.Interfaces
{
    /// <summary>
    /// Interface for aggregating metrics from multiple regions with real-time consolidation
    /// and cross-region trend analysis capabilities.
    /// </summary>
    public interface IGlobalMetricsAggregator
    {
        /// <summary>
        /// Aggregates metrics from all regions in real-time.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A dictionary mapping region identifiers to aggregated metric values.</returns>
        Task<Dictionary<string, double>> AggregateMetricsAsync(System.Threading.CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves metrics for a specific region.
        /// </summary>
        /// <param name="regionId">The identifier of the region.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A dictionary containing the metrics for the specified region.</returns>
        Task<Dictionary<string, double>> GetRegionalMetricsAsync(string regionId, System.Threading.CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyzes trends across regions over a specified time period.
        /// </summary>
        /// <param name="startTime">The start time for the analysis period.</param>
        /// <param name="endTime">The end time for the analysis period.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A dictionary mapping metrics to their trend values.</returns>
        Task<Dictionary<string, double>> AnalyzeTrendsAsync(DateTime startTime, DateTime endTime, System.Threading.CancellationToken cancellationToken = default);
    }
}
