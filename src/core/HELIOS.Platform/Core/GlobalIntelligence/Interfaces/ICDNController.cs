using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.GlobalIntelligence.Interfaces
{
    /// <summary>
    /// Interface for CDN orchestration, cache management, and edge location management.
    /// </summary>
    public interface ICDNController
    {
        /// <summary>
        /// Configures CDN settings and parameters.
        /// </summary>
        /// <param name="configuration">Configuration dictionary for CDN settings.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>True if configuration was successful; otherwise false.</returns>
        Task<bool> ConfigureCDNAsync(Dictionary<string, string> configuration, System.Threading.CancellationToken cancellationToken = default);

        /// <summary>
        /// Invalidates cached content in the CDN.
        /// </summary>
        /// <param name="cacheKeys">List of cache keys to invalidate.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A list of successfully invalidated cache keys.</returns>
        Task<List<string>> InvalidateCacheAsync(List<string> cacheKeys, System.Threading.CancellationToken cancellationToken = default);

        /// <summary>
        /// Manages edge locations for content distribution.
        /// </summary>
        /// <param name="edgeLocations">List of edge location identifiers to manage.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A dictionary mapping edge locations to their status.</returns>
        Task<Dictionary<string, string>> ManageEdgeLocationsAsync(List<string> edgeLocations, System.Threading.CancellationToken cancellationToken = default);
    }
}
