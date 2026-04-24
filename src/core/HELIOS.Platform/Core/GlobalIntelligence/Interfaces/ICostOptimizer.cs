using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.GlobalIntelligence.Interfaces
{
    /// <summary>
    /// Interface for analyzing costs across all regions and providing optimization recommendations.
    /// </summary>
    public interface ICostOptimizer
    {
        /// <summary>
        /// Analyzes costs across all regions.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A dictionary mapping regions to their current costs.</returns>
        Task<Dictionary<string, decimal>> AnalyzeCostsAsync(System.Threading.CancellationToken cancellationToken = default);

        /// <summary>
        /// Provides optimization recommendations for cost reduction.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A list of optimization recommendations with projected savings.</returns>
        Task<List<string>> OptimizeAsync(System.Threading.CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves ROI improvement recommendations.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A dictionary mapping recommendations to their projected ROI percentages.</returns>
        Task<Dictionary<string, double>> GetRecommendationsAsync(System.Threading.CancellationToken cancellationToken = default);
    }
}
