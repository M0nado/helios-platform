namespace HELIOS.Platform.Core.AdvancedOptimization.Interfaces
{
    /// <summary>
    /// Interface for the Service Mesh Optimizer service.
    /// Optimizes service communication and routing.
    /// </summary>
    public interface IServiceMeshOptimizer : IService
    {
        /// <summary>
        /// Optimizes service-to-service communication.
        /// </summary>
        /// <param name="communicationMetrics">Current communication metrics.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation with optimization results.</returns>
        Task<CommunicationOptimizationResult> OptimizeCommunicationAsync(Dictionary<string, CommunicationMetric> communicationMetrics, CancellationToken cancellationToken = default);

        /// <summary>
        /// Manages circuit breaker settings based on service health.
        /// </summary>
        /// <param name="serviceHealthData">Health metrics for services.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation with circuit breaker updates.</returns>
        Task<CircuitBreakerManagementResult> ManageCircuitBreakersAsync(Dictionary<string, ServiceHealthMetrics> serviceHealthData, CancellationToken cancellationToken = default);

        /// <summary>
        /// Optimizes request routing based on latency and availability.
        /// </summary>
        /// <param name="routingData">Current routing configuration and metrics.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation with routing optimization results.</returns>
        Task<RoutingOptimizationResult> OptimizeRoutingAsync(RoutingConfiguration routingData, CancellationToken cancellationToken = default);

        /// <summary>
        /// Records communication metrics for learning.
        /// </summary>
        /// <param name="metrics">Communication metrics to record.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RecordMetricsAsync(CommunicationMetric metrics);

        /// <summary>
        /// Gets optimization history.
        /// </summary>
        /// <param name="limit">Maximum records to retrieve.</param>
        /// <returns>List of historical optimization results.</returns>
        Task<List<CommunicationOptimizationResult>> GetOptimizationHistoryAsync(int limit = 100);
    }

    /// <summary>
    /// Represents communication metrics between services.
    /// </summary>
    public class CommunicationMetric
    {
        /// <summary>
        /// Source service name.
        /// </summary>
        public string SourceService { get; set; } = string.Empty;

        /// <summary>
        /// Destination service name.
        /// </summary>
        public string DestinationService { get; set; } = string.Empty;

        /// <summary>
        /// Average latency in milliseconds.
        /// </summary>
        public double AverageLatency { get; set; }

        /// <summary>
        /// Error rate (0-1).
        /// </summary>
        public double ErrorRate { get; set; }

        /// <summary>
        /// Request throughput per second.
        /// </summary>
        public double Throughput { get; set; }

        /// <summary>
        /// 95th percentile latency.
        /// </summary>
        public double P95Latency { get; set; }

        /// <summary>
        /// Measurement timestamp.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Represents service health metrics.
    /// </summary>
    public class ServiceHealthMetrics
    {
        /// <summary>
        /// Service name.
        /// </summary>
        public string ServiceName { get; set; } = string.Empty;

        /// <summary>
        /// Service availability percentage (0-100).
        /// </summary>
        public double Availability { get; set; }

        /// <summary>
        /// Response time in milliseconds.
        /// </summary>
        public double ResponseTime { get; set; }

        /// <summary>
        /// Error rate (0-1).
        /// </summary>
        public double ErrorRate { get; set; }

        /// <summary>
        /// Number of active connections.
        /// </summary>
        public int ActiveConnections { get; set; }

        /// <summary>
        /// Health check timestamp.
        /// </summary>
        public DateTime LastHealthCheck { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Represents communication optimization result.
    /// </summary>
    public class CommunicationOptimizationResult
    {
        /// <summary>
        /// Optimization identifier.
        /// </summary>
        public string OptimizationId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Timestamp of optimization.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Latency improvements by service pair.
        /// </summary>
        public Dictionary<string, double> LatencyImprovements { get; set; } = new();

        /// <summary>
        /// Error rate reductions.
        /// </summary>
        public Dictionary<string, double> ErrorRateReductions { get; set; } = new();

        /// <summary>
        /// Overall efficiency score (0-100).
        /// </summary>
        public double EfficiencyScore { get; set; }

        /// <summary>
        /// Recommended configurations.
        /// </summary>
        public List<string> RecommendedConfigurations { get; set; } = new();
    }

    /// <summary>
    /// Represents circuit breaker management result.
    /// </summary>
    public class CircuitBreakerManagementResult
    {
        /// <summary>
        /// Management timestamp.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Updated circuit breaker settings by service.
        /// </summary>
        public Dictionary<string, CircuitBreakerSettings> UpdatedSettings { get; set; } = new();

        /// <summary>
        /// Services with adjusted thresholds.
        /// </summary>
        public List<string> AdjustedServices { get; set; } = new();

        /// <summary>
        /// Total number of circuit breakers managed.
        /// </summary>
        public int TotalManaged { get; set; }

        /// <summary>
        /// Number of currently open circuit breakers.
        /// </summary>
        public int OpenCount { get; set; }
    }

    /// <summary>
    /// Represents circuit breaker settings.
    /// </summary>
    public class CircuitBreakerSettings
    {
        /// <summary>
        /// Failure threshold to open circuit.
        /// </summary>
        public int FailureThreshold { get; set; }

        /// <summary>
        /// Timeout for half-open state in seconds.
        /// </summary>
        public int TimeoutSeconds { get; set; }

        /// <summary>
        /// Minimum required successful calls to close.
        /// </summary>
        public int SuccessThreshold { get; set; }

        /// <summary>
        /// Whether circuit breaker is enabled.
        /// </summary>
        public bool Enabled { get; set; }
    }

    /// <summary>
    /// Represents routing configuration.
    /// </summary>
    public class RoutingConfiguration
    {
        /// <summary>
        /// Current routing rules.
        /// </summary>
        public Dictionary<string, RoutingRule> RoutingRules { get; set; } = new();

        /// <summary>
        /// Service endpoints and their metrics.
        /// </summary>
        public Dictionary<string, ServiceEndpointMetrics> ServiceEndpoints { get; set; } = new();

        /// <summary>
        /// Load balancing strategy.
        /// </summary>
        public string LoadBalancingStrategy { get; set; } = "RoundRobin";
    }

    /// <summary>
    /// Represents a routing rule.
    /// </summary>
    public class RoutingRule
    {
        /// <summary>
        /// Rule identifier.
        /// </summary>
        public string RuleId { get; set; } = string.Empty;

        /// <summary>
        /// Source path pattern.
        /// </summary>
        public string SourcePattern { get; set; } = string.Empty;

        /// <summary>
        /// Destination targets.
        /// </summary>
        public List<string> DestinationTargets { get; set; } = new();

        /// <summary>
        /// Weight distribution for targets (0-100).
        /// </summary>
        public Dictionary<string, int> TargetWeights { get; set; } = new();
    }

    /// <summary>
    /// Represents service endpoint metrics.
    /// </summary>
    public class ServiceEndpointMetrics
    {
        /// <summary>
        /// Endpoint URL.
        /// </summary>
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>
        /// Endpoint latency in milliseconds.
        /// </summary>
        public double Latency { get; set; }

        /// <summary>
        /// Endpoint availability (0-100).
        /// </summary>
        public double Availability { get; set; }

        /// <summary>
        /// Current request count.
        /// </summary>
        public int RequestCount { get; set; }

        /// <summary>
        /// Error count.
        /// </summary>
        public int ErrorCount { get; set; }
    }

    /// <summary>
    /// Represents routing optimization result.
    /// </summary>
    public class RoutingOptimizationResult
    {
        /// <summary>
        /// Optimization timestamp.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Updated routing rules.
        /// </summary>
        public Dictionary<string, RoutingRule> UpdatedRules { get; set; } = new();

        /// <summary>
        /// Expected latency improvement percentage.
        /// </summary>
        public double ExpectedLatencyImprovement { get; set; }

        /// <summary>
        /// Expected availability improvement percentage.
        /// </summary>
        public double ExpectedAvailabilityImprovement { get; set; }

        /// <summary>
        /// Number of rules optimized.
        /// </summary>
        public int OptimizedRuleCount { get; set; }

        /// <summary>
        /// Whether optimization was successful.
        /// </summary>
        public bool Success { get; set; }
    }
}
