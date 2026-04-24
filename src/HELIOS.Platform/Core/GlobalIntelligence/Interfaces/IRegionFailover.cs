using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.GlobalIntelligence.Interfaces
{
    /// <summary>
    /// Interface for multi-region failover management with health monitoring and recovery orchestration.
    /// </summary>
    public interface IRegionFailover
    {
        /// <summary>
        /// Monitors the health status of a specific region.
        /// </summary>
        /// <param name="regionId">The identifier of the region to monitor.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A dictionary containing health metrics for the region.</returns>
        Task<Dictionary<string, object>> MonitorRegionHealthAsync(string regionId, System.Threading.CancellationToken cancellationToken = default);

        /// <summary>
        /// Triggers a failover from one region to another.
        /// </summary>
        /// <param name="failedRegionId">The identifier of the failed region.</param>
        /// <param name="targetRegionId">The identifier of the target region for failover.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>True if failover was successful; otherwise false.</returns>
        Task<bool> TriggerFailoverAsync(string failedRegionId, string targetRegionId, System.Threading.CancellationToken cancellationToken = default);

        /// <summary>
        /// Orchestrates automatic recovery of a region.
        /// </summary>
        /// <param name="regionId">The identifier of the region to recover.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>True if recovery was successful; otherwise false.</returns>
        Task<bool> RecoverAsync(string regionId, System.Threading.CancellationToken cancellationToken = default);
    }
}
