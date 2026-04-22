namespace HELIOS.Platform.Core.AdvancedOptimization.Interfaces
{
    /// <summary>
    /// Interface for the Complex Event Processor service.
    /// Provides event stream analysis and pattern detection.
    /// </summary>
    public interface IComplexEventProcessor : IService
    {
        /// <summary>
        /// Processes an event from the stream.
        /// </summary>
        /// <param name="complexEvent">Event to process.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation with processing results.</returns>
        Task<EventProcessingResult> ProcessEventAsync(ComplexEvent complexEvent, CancellationToken cancellationToken = default);

        /// <summary>
        /// Matches event patterns from a stream.
        /// </summary>
        /// <param name="events">Stream of events to analyze.</param>
        /// <param name="patterns">Patterns to match.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation with pattern matches.</returns>
        Task<PatternMatchingResult> MatchPatternsAsync(List<ComplexEvent> events, List<EventPattern> patterns, CancellationToken cancellationToken = default);

        /// <summary>
        /// Detects correlations between events.
        /// </summary>
        /// <param name="events">Events to analyze for correlations.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation with correlation analysis.</returns>
        Task<CorrelationAnalysis> DetectCorrelationsAsync(List<ComplexEvent> events, CancellationToken cancellationToken = default);

        /// <summary>
        /// Aggregates events over a time window.
        /// </summary>
        /// <param name="events">Events to aggregate.</param>
        /// <param name="windowSizeSeconds">Window size in seconds.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation with aggregated results.</returns>
        Task<EventAggregationResult> AggregateEventsAsync(List<ComplexEvent> events, int windowSizeSeconds, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets event processing history.
        /// </summary>
        /// <param name="limit">Maximum records to retrieve.</param>
        /// <returns>List of historical processing results.</returns>
        Task<List<EventProcessingResult>> GetProcessingHistoryAsync(int limit = 100);
    }

    /// <summary>
    /// Represents a complex event.
    /// </summary>
    public class ComplexEvent
    {
        /// <summary>
        /// Event identifier.
        /// </summary>
        public string EventId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Event timestamp.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Event type.
        /// </summary>
        public string EventType { get; set; } = string.Empty;

        /// <summary>
        /// Event source.
        /// </summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// Event severity (Low, Medium, High, Critical).
        /// </summary>
        public string Severity { get; set; } = "Medium";

        /// <summary>
        /// Event properties.
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new();

        /// <summary>
        /// Correlated event IDs.
        /// </summary>
        public List<string> CorrelatedEventIds { get; set; } = new();

        /// <summary>
        /// Tags for categorization.
        /// </summary>
        public List<string> Tags { get; set; } = new();
    }

    /// <summary>
    /// Represents event processing result.
    /// </summary>
    public class EventProcessingResult
    {
        /// <summary>
        /// Result identifier.
        /// </summary>
        public string ResultId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Processing timestamp.
        /// </summary>
        public DateTime ProcessingTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Processed event ID.
        /// </summary>
        public string ProcessedEventId { get; set; } = string.Empty;

        /// <summary>
        /// Number of matched patterns.
        /// </summary>
        public int PatternsMatched { get; set; }

        /// <summary>
        /// Detected correlations.
        /// </summary>
        public List<EventCorrelation> CorrelationsDetected { get; set; } = new();

        /// <summary>
        /// Generated alerts.
        /// </summary>
        public List<string> GeneratedAlerts { get; set; } = new();

        /// <summary>
        /// Processing status (Success, Partial, Failed).
        /// </summary>
        public string Status { get; set; } = "Success";

        /// <summary>
        /// Processing duration in milliseconds.
        /// </summary>
        public long ProcessingDurationMs { get; set; }
    }

    /// <summary>
    /// Represents an event pattern.
    /// </summary>
    public class EventPattern
    {
        /// <summary>
        /// Pattern identifier.
        /// </summary>
        public string PatternId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Pattern name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Pattern description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Event types in the pattern.
        /// </summary>
        public List<string> EventTypes { get; set; } = new();

        /// <summary>
        /// Time window in seconds.
        /// </summary>
        public int TimeWindowSeconds { get; set; }

        /// <summary>
        /// Minimum occurrences to match.
        /// </summary>
        public int MinOccurrences { get; set; }

        /// <summary>
        /// Property conditions.
        /// </summary>
        public Dictionary<string, string> PropertyConditions { get; set; } = new();

        /// <summary>
        /// Severity when pattern matches.
        /// </summary>
        public string AlertSeverity { get; set; } = "Medium";

        /// <summary>
        /// Priority for pattern matching.
        /// </summary>
        public int Priority { get; set; }
    }

    /// <summary>
    /// Represents pattern matching result.
    /// </summary>
    public class PatternMatchingResult
    {
        /// <summary>
        /// Result identifier.
        /// </summary>
        public string ResultId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Analysis timestamp.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Number of events analyzed.
        /// </summary>
        public int EventsAnalyzed { get; set; }

        /// <summary>
        /// Matched patterns.
        /// </summary>
        public List<PatternMatch> PatternMatches { get; set; } = new();

        /// <summary>
        /// Total pattern matches found.
        /// </summary>
        public int TotalMatches { get; set; }

        /// <summary>
        /// Critical patterns matched.
        /// </summary>
        public List<string> CriticalMatches { get; set; } = new();
    }

    /// <summary>
    /// Represents a pattern match.
    /// </summary>
    public class PatternMatch
    {
        /// <summary>
        /// Pattern ID that matched.
        /// </summary>
        public string PatternId { get; set; } = string.Empty;

        /// <summary>
        /// Pattern name.
        /// </summary>
        public string PatternName { get; set; } = string.Empty;

        /// <summary>
        /// Matching event IDs.
        /// </summary>
        public List<string> MatchingEventIds { get; set; } = new();

        /// <summary>
        /// Match confidence (0-1).
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// Match timestamp.
        /// </summary>
        public DateTime MatchTime { get; set; }

        /// <summary>
        /// Match details.
        /// </summary>
        public string Details { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents correlation analysis.
    /// </summary>
    public class CorrelationAnalysis
    {
        /// <summary>
        /// Analysis identifier.
        /// </summary>
        public string AnalysisId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Analysis timestamp.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Detected correlations.
        /// </summary>
        public List<EventCorrelation> Correlations { get; set; } = new();

        /// <summary>
        /// Total events analyzed.
        /// </summary>
        public int EventsAnalyzed { get; set; }

        /// <summary>
        /// Correlation strength threshold used.
        /// </summary>
        public double CorrelationThreshold { get; set; }

        /// <summary>
        /// Strong correlations found.
        /// </summary>
        public int StrongCorrelationCount { get; set; }

        /// <summary>
        /// Average correlation strength.
        /// </summary>
        public double AverageCorrelationStrength { get; set; }
    }

    /// <summary>
    /// Represents correlation between events.
    /// </summary>
    public class EventCorrelation
    {
        /// <summary>
        /// First event ID.
        /// </summary>
        public string EventId1 { get; set; } = string.Empty;

        /// <summary>
        /// Second event ID.
        /// </summary>
        public string EventId2 { get; set; } = string.Empty;

        /// <summary>
        /// Correlation strength (0-1).
        /// </summary>
        public double CorrelationStrength { get; set; }

        /// <summary>
        /// Correlation type (Temporal, Causal, Semantic, etc.).
        /// </summary>
        public string CorrelationType { get; set; } = string.Empty;

        /// <summary>
        /// Time gap between events in seconds.
        /// </summary>
        public int TimeGapSeconds { get; set; }
    }

    /// <summary>
    /// Represents event aggregation result.
    /// </summary>
    public class EventAggregationResult
    {
        /// <summary>
        /// Result identifier.
        /// </summary>
        public string ResultId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Aggregation timestamp.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Window size in seconds.
        /// </summary>
        public int WindowSizeSeconds { get; set; }

        /// <summary>
        /// Total events in window.
        /// </summary>
        public int TotalEventsInWindow { get; set; }

        /// <summary>
        /// Aggregated event counts by type.
        /// </summary>
        public Dictionary<string, int> EventCountsByType { get; set; } = new();

        /// <summary>
        /// Aggregated event counts by severity.
        /// </summary>
        public Dictionary<string, int> EventCountsBySeverity { get; set; } = new();

        /// <summary>
        /// Event sources in window.
        /// </summary>
        public List<string> EventSources { get; set; } = new();

        /// <summary>
        /// Window start time.
        /// </summary>
        public DateTime WindowStart { get; set; }

        /// <summary>
        /// Window end time.
        /// </summary>
        public DateTime WindowEnd { get; set; }

        /// <summary>
        /// Aggregated summary.
        /// </summary>
        public string Summary { get; set; } = string.Empty;
    }
}
