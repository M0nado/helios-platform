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
    /// Performs multi-region capacity forecasting and resource allocation optimization.
    /// </summary>
    public class GlobalCapacityPlanner : IGlobalCapacityPlanner
    {
        private readonly ILogger<GlobalCapacityPlanner> _logger;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly Dictionary<string, List<int>> _regionCapacityHistory;
        private readonly Dictionary<string, int> _currentCapacity;

        /// <summary>
        /// Initializes a new instance of the GlobalCapacityPlanner class.
        /// </summary>
        /// <param name="logger">The logger instance for diagnostic output.</param>
        public GlobalCapacityPlanner(ILogger<GlobalCapacityPlanner> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _regionCapacityHistory = new Dictionary<string, List<int>>();
            _currentCapacity = new Dictionary<string, int>();
            _logger.LogInformation("GlobalCapacityPlanner initialized.");
        }

        public async Task<Dictionary<string, int>> PlanCapacityAsync(int months, CancellationToken cancellationToken = default)
        {
            if (months < 1 || months > 12)
                throw new ArgumentException("Months must be between 1 and 12.");

            try
            {
                await _semaphore.WaitAsync(cancellationToken);
                _logger.LogInformation($"Planning capacity for {months} months ahead.");

                if (_regionCapacityHistory.Count == 0)
                {
                    InitializeCapacityData();
                }

                var capacityPlan = new Dictionary<string, int>();

                foreach (var region in _regionCapacityHistory)
                {
                    var forecast = ForecastCapacity(region.Value, months);
                    capacityPlan[region.Key] = forecast;
                    _logger.LogDebug($"Region {region.Key}: forecasted capacity {forecast} units");
                }

                _logger.LogInformation($"Capacity planning complete for {capacityPlan.Count} regions.");
                return capacityPlan;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<Dictionary<string, int>> ForecastRequirementsAsync(int months, CancellationToken cancellationToken = default)
        {
            if (months < 1 || months > 12)
                throw new ArgumentException("Months must be between 1 and 12.");

            try
            {
                await _semaphore.WaitAsync(cancellationToken);
                _logger.LogInformation($"Forecasting resource requirements for {months} months.");

                if (_regionCapacityHistory.Count == 0)
                {
                    InitializeCapacityData();
                }

                var resourceForecast = new Dictionary<string, int>();
                var resourceTypes = new[] { "compute", "storage", "network", "memory" };

                foreach (var resourceType in resourceTypes)
                {
                    var totalRequired = 0;
                    foreach (var region in _regionCapacityHistory)
                    {
                        var forecast = ForecastCapacity(region.Value, months);
                        totalRequired += (int)(forecast * GetResourceMultiplier(resourceType));
                    }
                    resourceForecast[resourceType] = totalRequired;
                }

                _logger.LogInformation($"Forecast complete. {resourceForecast.Count} resource types analyzed.");
                return resourceForecast;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<Dictionary<string, int>> AllocateResourcesAsync(int availableResources, CancellationToken cancellationToken = default)
        {
            if (availableResources <= 0)
                throw new ArgumentException("Available resources must be greater than zero.");

            try
            {
                await _semaphore.WaitAsync(cancellationToken);
                _logger.LogInformation($"Allocating {availableResources} resources across regions.");

                if (_regionCapacityHistory.Count == 0)
                {
                    InitializeCapacityData();
                }

                var allocation = new Dictionary<string, int>();
                var totalCapacity = _currentCapacity.Values.Sum();

                if (totalCapacity == 0)
                {
                    // Equal distribution
                    var perRegion = availableResources / _currentCapacity.Count;
                    foreach (var region in _currentCapacity.Keys)
                    {
                        allocation[region] = perRegion;
                    }
                }
                else
                {
                    // Proportional distribution based on current capacity
                    foreach (var region in _currentCapacity)
                    {
                        var proportion = (double)region.Value / totalCapacity;
                        allocation[region.Key] = (int)(availableResources * proportion);
                    }
                }

                _logger.LogInformation($"Resource allocation complete. Total allocated: {allocation.Values.Sum()} units.");
                return allocation;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private int ForecastCapacity(List<int> history, int forecastMonths)
        {
            if (history.Count < 2)
                return history.LastOrDefault();

            // Simple ARIMA-like forecasting using linear trend
            var n = history.Count;
            var sumX = Enumerable.Range(0, n).Sum(i => (double)i);
            var sumY = history.Sum(h => (double)h);
            var sumXY = Enumerable.Range(0, n).Sum(i => i * history[i]);
            var sumX2 = Enumerable.Range(0, n).Sum(i => i * i);

            var slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            var intercept = (sumY - slope * sumX) / n;

            // Project forward by forecast months
            var futureIndex = n + forecastMonths;
            var forecast = (int)(intercept + slope * futureIndex);

            return Math.Max(forecast, history.Last()); // Never forecast below current capacity
        }

        private double GetResourceMultiplier(string resourceType)
        {
            return resourceType.ToLowerInvariant() switch
            {
                "compute" => 1.0,
                "storage" => 2.0,
                "network" => 0.5,
                "memory" => 1.5,
                _ => 1.0
            };
        }

        private void InitializeCapacityData()
        {
            var regions = new[] { "us-east-1", "us-west-2", "eu-central-1", "ap-northeast-1" };
            var random = new Random(42);

            foreach (var region in regions)
            {
                var baseCapacity = random.Next(1000, 5000);
                _currentCapacity[region] = baseCapacity;

                // Generate historical capacity growth
                _regionCapacityHistory[region] = Enumerable.Range(0, 24)
                    .Select(i => (int)(baseCapacity * (1.0 + i * 0.05 + random.NextDouble() * 0.1)))
                    .ToList();
            }
        }
    }
}
