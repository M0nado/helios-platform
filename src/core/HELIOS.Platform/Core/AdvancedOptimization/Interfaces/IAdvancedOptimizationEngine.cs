namespace HELIOS.Platform.Core.AdvancedOptimization.Interfaces
{
    /// <summary>
    /// Interface for the Advanced Optimization Engine service.
    /// Provides system-wide optimization orchestration with multi-metric analysis.
    /// </summary>
    public interface IAdvancedOptimizationEngine : IService
    {
        /// <summary>
        /// Optimizes the system based on current metrics and historical data.
        /// </summary>
        /// <param name="systemMetrics">Current system metrics dictionary.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation with optimization results.</returns>
        Task<OptimizationResult> OptimizeSystemAsync(Dictionary<string, double> systemMetrics, CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyzes system bottlenecks and identifies optimization opportunities.
        /// </summary>
        /// <param name="systemMetrics">Current system metrics dictionary.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation with bottleneck analysis.</returns>
        Task<BottleneckAnalysis> AnalyzeBottlenecksAsync(Dictionary<string, double> systemMetrics, CancellationToken cancellationToken = default);

        /// <summary>
        /// Applies optimizations to the system.
        /// </summary>
        /// <param name="optimizations">List of optimizations to apply.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation with application results.</returns>
        Task<ApplyOptimizationResult> ApplyOptimizationsAsync(List<OptimizationAction> optimizations, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the optimization history.
        /// </summary>
        /// <param name="limit">Maximum number of historical records to retrieve.</param>
        /// <returns>List of historical optimization results.</returns>
        Task<List<OptimizationResult>> GetHistoryAsync(int limit = 100);

        /// <summary>
        /// Clears the optimization history.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ClearHistoryAsync();
    }

    /// <summary>
    /// Represents an optimization result.
    /// </summary>
    public class OptimizationResult
    {
        /// <summary>
        /// Unique identifier for this optimization.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Timestamp of the optimization.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Overall optimization score (0-100).
        /// </summary>
        public double OptimizationScore { get; set; }

        /// <summary>
        /// Improvements made by metric.
        /// </summary>
        public Dictionary<string, double> Improvements { get; set; } = new();

        /// <summary>
        /// Recommended actions.
        /// </summary>
        public List<string> RecommendedActions { get; set; } = new();

        /// <summary>
        /// Whether the optimization was successful.
        /// </summary>
        public bool Success { get; set; }
    }

    /// <summary>
    /// Represents bottleneck analysis results.
    /// </summary>
    public class BottleneckAnalysis
    {
        /// <summary>
        /// Identified bottlenecks by severity.
        /// </summary>
        public Dictionary<string, double> Bottlenecks { get; set; } = new();

        /// <summary>
        /// Impact scores for each bottleneck.
        /// </summary>
        public Dictionary<string, double> ImpactScores { get; set; } = new();

        /// <summary>
        /// Critical bottlenecks requiring immediate attention.
        /// </summary>
        public List<string> CriticalBottlenecks { get; set; } = new();

        /// <summary>
        /// Analysis timestamp.
        /// </summary>
        public DateTime AnalysisTime { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Represents an optimization action.
    /// </summary>
    public class OptimizationAction
    {
        /// <summary>
        /// Action identifier.
        /// </summary>
        public string ActionId { get; set; } = string.Empty;

        /// <summary>
        /// Action description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Priority level (1-5).
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Expected impact score.
        /// </summary>
        public double ExpectedImpact { get; set; }

        /// <summary>
        /// Action parameters.
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Represents the result of applying optimizations.
    /// </summary>
    public class ApplyOptimizationResult
    {
        /// <summary>
        /// Total optimizations processed.
        /// </summary>
        public int TotalProcessed { get; set; }

        /// <summary>
        /// Number of successful optimizations.
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// Number of failed optimizations.
        /// </summary>
        public int FailureCount { get; set; }

        /// <summary>
        /// Overall success rate percentage.
        /// </summary>
        public double SuccessRate { get; set; }

        /// <summary>
        /// Total improvement achieved.
        /// </summary>
        public double TotalImprovement { get; set; }

        /// <summary>
        /// Detailed results for each optimization.
        /// </summary>
        public List<string> Details { get; set; } = new();
    }
}
