using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace HELIOS.Platform.Core.AdvancedOptimization
{
    /// <summary>
    /// Intelligent Resource Allocator implementation.
    /// Provides AI-driven resource allocation with predictive sizing.
    /// </summary>
    public class IntelligentResourceAllocator : IIntelligentResourceAllocator
    {
        private readonly ILogger<IntelligentResourceAllocator> _logger;
        private readonly SemaphoreSlim _semaphore;
        private readonly ConcurrentQueue<ResourceUsagePoint> _usageHistory;
        private readonly ConcurrentQueue<ResourceAllocationResult> _allocationHistory;
        private bool _isRunning;

        /// <summary>
        /// Initializes a new instance of the IntelligentResourceAllocator class.
        /// </summary>
        public IntelligentResourceAllocator(ILogger<IntelligentResourceAllocator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _semaphore = new SemaphoreSlim(1, 1);
            _usageHistory = new ConcurrentQueue<ResourceUsagePoint>();
            _allocationHistory = new ConcurrentQueue<ResourceAllocationResult>();
            _isRunning = false;
        }

        /// <inheritdoc/>
        public string ServiceName => nameof(IntelligentResourceAllocator);

        /// <inheritdoc/>
        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                _logger.LogInformation("{ServiceName} initializing", ServiceName);
                await Task.CompletedTask;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                _isRunning = true;
                _logger.LogInformation("{ServiceName} started", ServiceName);
                await Task.CompletedTask;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                _isRunning = false;
                _logger.LogInformation("{ServiceName} stopped", ServiceName);
                await Task.CompletedTask;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public bool IsRunning() => _isRunning;

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            _semaphore?.Dispose();
            await Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task<ResourceAllocationResult> AllocateResourcesAsync(double currentLoad, Dictionary<string, double> resourceRequirements, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                var result = new ResourceAllocationResult { Timestamp = DateTime.UtcNow, Success = true };

                if (resourceRequirements == null || resourceRequirements.Count == 0)
                {
                    result.EfficiencyScore = 100;
                    _allocationHistory.Enqueue(result);
                    return result;
                }

                double totalAllocated = 0;
                foreach (var req in resourceRequirements)
                {
                    double allocatedAmount = req.Value * (1 + (currentLoad / 100.0) * 0.5);
                    result.AllocatedResources[req.Key] = allocatedAmount;
                    totalAllocated += allocatedAmount;
                }

                result.EfficiencyScore = Math.Max(0, 100 - (currentLoad * 0.5));

                _allocationHistory.Enqueue(result);
                _logger.LogInformation("Resource allocation completed with efficiency score: {Score}", result.EfficiencyScore);

                return result;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<ResourceRequirementsPrediction> PredictRequirementsAsync(List<ResourceUsagePoint> historicalData, int forecastPeriodMinutes, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                var prediction = new ResourceRequirementsPrediction
                {
                    PredictionTime = DateTime.UtcNow,
                    ForecastPeriodMinutes = forecastPeriodMinutes
                };

                if (historicalData == null || historicalData.Count == 0)
                {
                    return prediction;
                }

                var lastPoint = historicalData.LastOrDefault();
                if (lastPoint?.Resources != null)
                {
                    foreach (var resource in lastPoint.Resources)
                    {
                        double trend = CalculateTrend(historicalData.Select(p => p.Resources.ContainsKey(resource.Key) ? p.Resources[resource.Key] : 0).ToList());
                        double predicted = resource.Value * (1 + trend * (forecastPeriodMinutes / 60.0));
                        prediction.PredictedRequirements[resource.Key] = Math.Max(0, predicted);
                        prediction.ConfidenceScores[resource.Key] = 0.8;
                        prediction.Trends[resource.Key] = trend > 0 ? "Increasing" : (trend < 0 ? "Decreasing" : "Stable");
                    }
                }

                _logger.LogInformation("Predicted resource requirements for {Minutes} minutes", forecastPeriodMinutes);

                return prediction;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<RebalancingResult> RebalanceAsync(Dictionary<string, ResourceAllocation> currentAllocations, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                var result = new RebalancingResult { Timestamp = DateTime.UtcNow, Success = true };

                if (currentAllocations == null || currentAllocations.Count == 0)
                {
                    result.EfficiencyImprovement = 0;
                    return result;
                }

                double totalUtilization = 0;
                int allocationCount = 0;

                foreach (var alloc in currentAllocations.Values)
                {
                    totalUtilization += alloc.UtilizationPercentage;
                    allocationCount++;
                }

                double averageUtilization = allocationCount > 0 ? totalUtilization / allocationCount : 0;

                foreach (var alloc in currentAllocations)
                {
                    double targetUtilization = averageUtilization;
                    double currentUtilization = alloc.Value.UtilizationPercentage;
                    double adjustmentFactor = targetUtilization / (currentUtilization + 0.1);
                    double newAllocation = alloc.Value.CurrentAllocation * adjustmentFactor;

                    result.NewAllocations[alloc.Key] = Math.Min(newAllocation, alloc.Value.MaxAvailable);
                    result.Changes[alloc.Key] = newAllocation - alloc.Value.CurrentAllocation;
                }

                result.EfficiencyImprovement = Math.Abs(averageUtilization - 50) / 50 * 100;

                _logger.LogInformation("Resource rebalancing completed with {Improvement}% efficiency improvement", result.EfficiencyImprovement);

                return result;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public async Task RecordUsageAsync(ResourceUsagePoint usageData)
        {
            await _semaphore.WaitAsync();
            try
            {
                _usageHistory.Enqueue(usageData);
                if (_usageHistory.Count > 1000)
                {
                    _usageHistory.TryDequeue(out _);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<List<ResourceAllocationResult>> GetAllocationHistoryAsync(int limit = 100)
        {
            var results = new List<ResourceAllocationResult>();
            int count = 0;

            foreach (var item in _allocationHistory.Reverse())
            {
                if (count >= limit) break;
                results.Add(item);
                count++;
            }

            return await Task.FromResult(results);
        }

        private double CalculateTrend(List<double> values)
        {
            if (values.Count < 2) return 0;

            double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;
            int n = values.Count;

            for (int i = 0; i < n; i++)
            {
                sumX += i;
                sumY += values[i];
                sumXY += i * values[i];
                sumX2 += i * i;
            }

            double slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            return slope / (sumY / n + 0.0001);
        }
    }
}
