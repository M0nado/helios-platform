using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.GlobalIntelligence.Interfaces
{
    /// <summary>
    /// Interface for geographic load balancing with latency-aware routing and failover orchestration.
    /// </summary>
    public interface IGlobalLoadBalancer
    {
        /// <summary>
        /// Balances load across regions based on current metrics and latency.
        /// </summary>
        /// <param name="requestCount">The number of requests to balance.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A dictionary mapping regions to their assigned request counts.</returns>
        Task<Dictionary<string, int>> BalanceLoadAsync(int requestCount, System.Threading.CancellationToken cancellationToken = default);

        /// <summary>
        /// Calculates optimal routing paths based on latency and availability.
        /// </summary>
        /// <param name="sourceRegion">The source region identifier.</param>
        /// <param name="destinationRegion">The destination region identifier.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A list of regions representing the optimal routing path.</returns>
        Task<List<string>> CalculateOptimalRoutingAsync(string sourceRegion, string destinationRegion, System.Threading.CancellationToken cancellationToken = default);

        /// <summary>
        /// Handles failover orchestration when a region becomes unavailable.
        /// </summary>
        /// <param name="failedRegion">The region that has failed.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A dictionary with the failover results and new load distribution.</returns>
        Task<Dictionary<string, int>> HandleFailoverAsync(string failedRegion, System.Threading.CancellationToken cancellationToken = default);
    }
}
