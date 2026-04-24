using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.GlobalIntelligence.Interfaces
{
    /// <summary>
    /// Interface for minimizing global latency through route optimization and path caching.
    /// </summary>
    public interface ILatencyOptimizer
    {
        /// <summary>
        /// Optimizes latency for requests between two regions.
        /// </summary>
        /// <param name="sourceRegion">The source region identifier.</param>
        /// <param name="destinationRegion">The destination region identifier.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>The optimized latency in milliseconds.</returns>
        Task<double> OptimizeLatencyAsync(string sourceRegion, string destinationRegion, System.Threading.CancellationToken cancellationToken = default);

        /// <summary>
        /// Finds the optimal path between regions to minimize latency.
        /// </summary>
        /// <param name="sourceRegion">The source region identifier.</param>
        /// <param name="destinationRegion">The destination region identifier.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A list of regions representing the optimal path.</returns>
        Task<List<string>> FindOptimalPathAsync(string sourceRegion, string destinationRegion, System.Threading.CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs a cache lookup for a previously optimized path.
        /// </summary>
        /// <param name="sourceRegion">The source region identifier.</param>
        /// <param name="destinationRegion">The destination region identifier.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>The cached path if available; otherwise null.</returns>
        Task<List<string>> CacheLookupAsync(string sourceRegion, string destinationRegion, System.Threading.CancellationToken cancellationToken = default);
    }
}
