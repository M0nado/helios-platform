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
    /// Provides geographic load balancing with latency-aware routing and failover orchestration.
    /// </summary>
    public class GlobalLoadBalancer : IGlobalLoadBalancer
    {
        private readonly ILogger<GlobalLoadBalancer> _logger;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly Dictionary<string, double> _regionLatencies;
        private readonly Dictionary<string, int> _regionLoad;
        private readonly Dictionary<string, bool> _regionAvailability;

        /// <summary>
        /// Initializes a new instance of the GlobalLoadBalancer class.
        /// </summary>
        /// <param name="logger">The logger instance for diagnostic output.</param>
        public GlobalLoadBalancer(ILogger<GlobalLoadBalancer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _regionLatencies = new Dictionary<string, double>();
            _regionLoad = new Dictionary<string, int>();
            _regionAvailability = new Dictionary<string, bool>();
            _logger.LogInformation("GlobalLoadBalancer initialized.");
        }

        public async Task<Dictionary<string, int>> BalanceLoadAsync(int requestCount, CancellationToken cancellationToken = default)
        {
            if (requestCount <= 0)
                throw new ArgumentException("Request count must be greater than zero.");

            try
            {
                await _semaphore.WaitAsync(cancellationToken);
                _logger.LogInformation($"Balancing {requestCount} requests across regions.");

                if (_regionLatencies.Count == 0)
                {
                    InitializeRegionalData();
                }

                var balanced = new Dictionary<string, int>();
                var availableRegions = _regionAvailability
                    .Where(x => x.Value)
                    .Select(x => x.Key)
                    .ToList();

                if (availableRegions.Count == 0)
                {
                    _logger.LogWarning("No available regions. Using all regions.");
                    availableRegions = _regionLatencies.Keys.ToList();
                }

                // Score regions by latency and current load (lower is better)
                var scores = availableRegions.ToDictionary(
                    r => r,
                    r => _regionLatencies[r] + (_regionLoad.GetValueOrDefault(r, 0) * 0.1)
                );

                var sortedRegions = scores.OrderBy(x => x.Value).Select(x => x.Key).ToList();
                var requestsPerRegion = requestCount / sortedRegions.Count;
                var remainder = requestCount % sortedRegions.Count;

                for (int i = 0; i < sortedRegions.Count; i++)
                {
                    var requests = requestsPerRegion + (i < remainder ? 1 : 0);
                    balanced[sortedRegions[i]] = requests;
                    _regionLoad[sortedRegions[i]] = requests;
                }

                _logger.LogInformation($"Load balanced across {balanced.Count} regions.");
                return balanced;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<List<string>> CalculateOptimalRoutingAsync(string sourceRegion, string destinationRegion, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sourceRegion))
                throw new ArgumentNullException(nameof(sourceRegion));
            if (string.IsNullOrWhiteSpace(destinationRegion))
                throw new ArgumentNullException(nameof(destinationRegion));

            try
            {
                await _semaphore.WaitAsync(cancellationToken);
                _logger.LogInformation($"Calculating optimal route from {sourceRegion} to {destinationRegion}.");

                if (_regionLatencies.Count == 0)
                {
                    InitializeRegionalData();
                }

                var path = new List<string> { sourceRegion };
                var current = sourceRegion;

                // Greedy algorithm to find low-latency path
                while (current != destinationRegion && path.Count < 10)
                {
                    var next = FindNextHop(current, destinationRegion, path);
                    if (next == null)
                    {
                        path.Add(destinationRegion);
                        break;
                    }
                    path.Add(next);
                    current = next;
                }

                _logger.LogInformation($"Optimal route calculated: {string.Join(" -> ", path)}");
                return path;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<Dictionary<string, int>> HandleFailoverAsync(string failedRegion, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(failedRegion))
                throw new ArgumentNullException(nameof(failedRegion));

            try
            {
                await _semaphore.WaitAsync(cancellationToken);
                _logger.LogWarning($"Handling failover for region: {failedRegion}");

                if (_regionLatencies.Count == 0)
                {
                    InitializeRegionalData();
                }

                // Mark failed region as unavailable
                _regionAvailability[failedRegion] = false;
                var failedLoad = _regionLoad.GetValueOrDefault(failedRegion, 0);

                // Redistribute load from failed region
                var availableRegions = _regionAvailability
                    .Where(x => x.Value && x.Key != failedRegion)
                    .Select(x => x.Key)
                    .ToList();

                var failoverResult = new Dictionary<string, int>();

                if (availableRegions.Count > 0)
                {
                    var additionalLoad = failedLoad / availableRegions.Count;
                    var remainder = failedLoad % availableRegions.Count;

                    for (int i = 0; i < availableRegions.Count; i++)
                    {
                        var additional = additionalLoad + (i < remainder ? 1 : 0);
                        _regionLoad[availableRegions[i]] += additional;
                        failoverResult[availableRegions[i]] = _regionLoad[availableRegions[i]];
                    }
                }

                _regionLoad[failedRegion] = 0;
                _logger.LogInformation($"Failover complete. Redistributed {failedLoad} requests.");
                return failoverResult;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private string FindNextHop(string current, string destination, List<string> visitedRegions)
        {
            var availableRegions = _regionLatencies.Keys
                .Where(r => !visitedRegions.Contains(r) && _regionAvailability.GetValueOrDefault(r, true))
                .ToList();

            if (availableRegions.Count == 0)
                return null;

            // Choose region with lowest latency to destination
            return availableRegions
                .OrderBy(r => _regionLatencies[r])
                .FirstOrDefault();
        }

        private void InitializeRegionalData()
        {
            var regions = new[] { "us-east-1", "us-west-2", "eu-central-1", "ap-northeast-1" };
            var random = new Random(42);

            foreach (var region in regions)
            {
                _regionLatencies[region] = random.NextDouble() * 200; // 0-200ms latency
                _regionLoad[region] = 0;
                _regionAvailability[region] = true;
            }
        }
    }
}
