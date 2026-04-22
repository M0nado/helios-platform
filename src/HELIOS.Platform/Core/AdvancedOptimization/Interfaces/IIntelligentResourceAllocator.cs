namespace HELIOS.Platform.Core.AdvancedOptimization.Interfaces
{
    /// <summary>
    /// Interface for the Intelligent Resource Allocator service.
    /// Provides AI-driven resource allocation with predictive sizing.
    /// </summary>
    public interface IIntelligentResourceAllocator : IService
    {
        /// <summary>
        /// Allocates resources based on current demand and historical patterns.
        /// </summary>
        /// <param name="currentLoad">Current system load (0-100).</param>
        /// <param name="resourceRequirements">Required resources by type.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation with allocation results.</returns>
        Task<ResourceAllocationResult> AllocateResourcesAsync(double currentLoad, Dictionary<string, double> resourceRequirements, CancellationToken cancellationToken = default);

        /// <summary>
        /// Predicts future resource requirements based on patterns and trends.
        /// </summary>
        /// <param name="historicalData">Historical resource usage data.</param>
        /// <param name="forecastPeriodMinutes">Number of minutes to forecast ahead.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation with predictions.</returns>
        Task<ResourceRequirementsPrediction> PredictRequirementsAsync(List<ResourceUsagePoint> historicalData, int forecastPeriodMinutes, CancellationToken cancellationToken = default);

        /// <summary>
        /// Rebalances resource allocations across the system.
        /// </summary>
        /// <param name="currentAllocations">Current resource allocations.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation with rebalancing results.</returns>
        Task<RebalancingResult> RebalanceAsync(Dictionary<string, ResourceAllocation> currentAllocations, CancellationToken cancellationToken = default);

        /// <summary>
        /// Records resource usage data for learning.
        /// </summary>
        /// <param name="usageData">Resource usage data point.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RecordUsageAsync(ResourceUsagePoint usageData);

        /// <summary>
        /// Gets allocation history.
        /// </summary>
        /// <param name="limit">Maximum records to retrieve.</param>
        /// <returns>List of historical allocations.</returns>
        Task<List<ResourceAllocationResult>> GetAllocationHistoryAsync(int limit = 100);
    }

    /// <summary>
    /// Represents resource allocation result.
    /// </summary>
    public class ResourceAllocationResult
    {
        /// <summary>
        /// Allocation identifier.
        /// </summary>
        public string AllocationId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Timestamp of allocation.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Allocated resources by type.
        /// </summary>
        public Dictionary<string, double> AllocatedResources { get; set; } = new();

        /// <summary>
        /// Efficiency score (0-100).
        /// </summary>
        public double EfficiencyScore { get; set; }

        /// <summary>
        /// Whether allocation was successful.
        /// </summary>
        public bool Success { get; set; }
    }

    /// <summary>
    /// Represents a resource usage data point.
    /// </summary>
    public class ResourceUsagePoint
    {
        /// <summary>
        /// Timestamp of measurement.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Resource usage by type.
        /// </summary>
        public Dictionary<string, double> Resources { get; set; } = new();

        /// <summary>
        /// System load at this point.
        /// </summary>
        public double SystemLoad { get; set; }
    }

    /// <summary>
    /// Represents resource requirements prediction.
    /// </summary>
    public class ResourceRequirementsPrediction
    {
        /// <summary>
        /// Prediction timestamp.
        /// </summary>
        public DateTime PredictionTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Forecast period in minutes.
        /// </summary>
        public int ForecastPeriodMinutes { get; set; }

        /// <summary>
        /// Predicted resource requirements.
        /// </summary>
        public Dictionary<string, double> PredictedRequirements { get; set; } = new();

        /// <summary>
        /// Confidence scores for predictions (0-1).
        /// </summary>
        public Dictionary<string, double> ConfidenceScores { get; set; } = new();

        /// <summary>
        /// Trends detected for each resource.
        /// </summary>
        public Dictionary<string, string> Trends { get; set; } = new();
    }

    /// <summary>
    /// Represents a resource allocation.
    /// </summary>
    public class ResourceAllocation
    {
        /// <summary>
        /// Resource type.
        /// </summary>
        public string ResourceType { get; set; } = string.Empty;

        /// <summary>
        /// Current allocation amount.
        /// </summary>
        public double CurrentAllocation { get; set; }

        /// <summary>
        /// Maximum available amount.
        /// </summary>
        public double MaxAvailable { get; set; }

        /// <summary>
        /// Current utilization percentage.
        /// </summary>
        public double UtilizationPercentage { get; set; }
    }

    /// <summary>
    /// Represents rebalancing result.
    /// </summary>
    public class RebalancingResult
    {
        /// <summary>
        /// Rebalancing timestamp.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// New allocations after rebalancing.
        /// </summary>
        public Dictionary<string, double> NewAllocations { get; set; } = new();

        /// <summary>
        /// Changes made for each resource.
        /// </summary>
        public Dictionary<string, double> Changes { get; set; } = new();

        /// <summary>
        /// Overall efficiency improvement percentage.
        /// </summary>
        public double EfficiencyImprovement { get; set; }

        /// <summary>
        /// Whether rebalancing was successful.
        /// </summary>
        public bool Success { get; set; }
    }
}
