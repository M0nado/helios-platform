using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.GlobalIntelligence.Interfaces
{
    /// <summary>
    /// Interface for multi-region capacity forecasting and resource allocation optimization.
    /// </summary>
    public interface IGlobalCapacityPlanner
    {
        /// <summary>
        /// Plans capacity requirements across all regions.
        /// </summary>
        /// <param name="months">Number of months to plan ahead (3-6 month horizon).</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A dictionary mapping regions to their planned capacity requirements.</returns>
        Task<Dictionary<string, int>> PlanCapacityAsync(int months, System.Threading.CancellationToken cancellationToken = default);

        /// <summary>
        /// Forecasts resource requirements using predictive analysis.
        /// </summary>
        /// <param name="months">Number of months to forecast ahead.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A dictionary mapping resource types to their forecasted requirements.</returns>
        Task<Dictionary<string, int>> ForecastRequirementsAsync(int months, System.Threading.CancellationToken cancellationToken = default);

        /// <summary>
        /// Allocates resources optimally across regions.
        /// </summary>
        /// <param name="availableResources">The total available resources to allocate.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A dictionary mapping regions to their allocated resources.</returns>
        Task<Dictionary<string, int>> AllocateResourcesAsync(int availableResources, System.Threading.CancellationToken cancellationToken = default);
    }
}
