using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HELIOS.Platform.Core.GlobalIntelligence.Interfaces;

namespace HELIOS.Platform.Core.GlobalIntelligence
{
    /// <summary>
    /// Minimizes global latency through route optimization and path caching.
    /// </summary>
    public class LatencyOptimizer : ILatencyOptimizer
    {
        private const double TargetLatencyMs = 100.0;
        private readonly ILogger<LatencyOptimizer> _logger;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly Dictionary<string, double> _regionLatencies;
        private readonly Dictionary<string, List<string>> _pathCache;

        /// <summary>
        /// Initializes a new instance of the LatencyOptimizer class.
        /// </summary>
        /// <param name="logger">The logger instance for diagnostic output.</param>
        public LatencyOptimizer(ILogger<LatencyOptimizer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _regionLatencies = new Dictionary<string, double>();
            _pathCache = new Dictionary<string, List<string>>();
            _logger.LogInformation("LatencyOptimizer initialized.");
        }

        public async Task<double> OptimizeLatencyAsync(string sourceRegion, string destinationRegion, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sourceRegion))
                throw new ArgumentNullException(nameof(sourceRegion));
            if (string.IsNullOrWhiteSpace(destinationRegion))
                throw new ArgumentNullException(nameof(destinationRegion));

            try
            {
                await _semaphore.WaitAsync(cancellationToken);
                _logger.LogInformation($"Optimizing latency from {sourceRegion} to {destinationRegion}");

                if (_regionLatencies.Count == 0)
                {
                    InitializeLatencyData();
                }

                // Try to get cached path
                var cacheKey = $"{sourceRegion}-{destinationRegion}";
                var path = await CacheLookupAsync(sourceRegion, destinationRegion, cancellationToken);

                if (path == null)
                {
                    // Calculate new optimal path
                    path = await FindOptimalPathAsync(sourceRegion, destinationRegion, cancellationToken);
                    _pathCache[cacheKey] = path;
                }

                // Calculate total latency for the path
                var totalLatency = CalculatePathLatency(path);
                var optimizationPercentage = ((TargetLatencyMs - totalLatency) / TargetLatencyMs) * 100;

                _logger.LogInformation($"Optimized latency: {totalLatency:F2}ms (Target: {TargetLatencyMs}ms, Optimization: {optimizationPercentage:F1}%)");
                return totalLatency;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<List<string>> FindOptimalPathAsync(string sourceRegion, string destinationRegion, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sourceRegion))
                throw new ArgumentNullException(nameof(sourceRegion));
            if (string.IsNullOrWhiteSpace(destinationRegion))
                throw new ArgumentNullException(nameof(destinationRegion));

            try
            {
                await _semaphore.WaitAsync(cancellationToken);
                _logger.LogInformation($"Finding optimal path from {sourceRegion} to {destinationRegion}");

                if (_regionLatencies.Count == 0)
                {
                    InitializeLatencyData();
                }

                // Dijkstra-like algorithm for finding lowest latency path
                var path = new List<string> { sourceRegion };
                var visited = new HashSet<string> { sourceRegion };
                var current = sourceRegion;

                while (current != destinationRegion && visited.Count < _regionLatencies.Count)
                {
                    var neighbors = _regionLatencies.Keys
                        .Where(r => !visited.Contains(r))
                        .OrderBy(r => _regionLatencies[r])
                        .ToList();

                    if (neighbors.Count == 0)
                        break;

                    current = neighbors.First();
                    path.Add(current);
                    visited.Add(current);

                    if (current == destinationRegion)
                        break;
                }

                if (current != destinationRegion)
                {
                    path.Add(destinationRegion);
                }

                _logger.LogInformation($"Optimal path found: {string.Join(" -> ", path)}");
                return path;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<List<string>> CacheLookupAsync(string sourceRegion, string destinationRegion, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sourceRegion))
                throw new ArgumentNullException(nameof(sourceRegion));
            if (string.IsNullOrWhiteSpace(destinationRegion))
                throw new ArgumentNullException(nameof(destinationRegion));

            try
            {
                await _semaphore.WaitAsync(cancellationToken);
                var cacheKey = $"{sourceRegion}-{destinationRegion}";

                if (_pathCache.TryGetValue(cacheKey, out var cachedPath))
                {
                    _logger.LogDebug($"Cache hit for path {cacheKey}");
                    return new List<string>(cachedPath);
                }

                _logger.LogDebug($"Cache miss for path {cacheKey}");
                return null;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private double CalculatePathLatency(List<string> path)
        {
            if (path == null || path.Count < 2)
                return 0;

            double totalLatency = 0;
            for (int i = 0; i < path.Count - 1; i++)
            {
                var from = path[i];
                var to = path[i + 1];

                // Simulate latency between regions
                if (_regionLatencies.TryGetValue(from, out var fromLatency) &&
                    _regionLatencies.TryGetValue(to, out var toLatency))
                {
                    totalLatency += (fromLatency + toLatency) / 2;
                }
            }

            return Math.Min(totalLatency, TargetLatencyMs);
        }

        private void InitializeLatencyData()
        {
            var regions = new[] { "us-east-1", "us-west-2", "eu-central-1", "ap-northeast-1" };
            var random = new Random(42);

            foreach (var region in regions)
            {
                _regionLatencies[region] = random.NextDouble() * 150; // 0-150ms baseline
            }
        }
    }
}
