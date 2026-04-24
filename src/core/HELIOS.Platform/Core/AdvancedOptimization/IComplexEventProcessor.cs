using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.AdvancedOptimization
{
    /// <summary>
    /// Complex event processor that analyzes event streams, detects patterns,
    /// and generates alerts for critical event sequences.
    /// </summary>
    public interface IComplexEventProcessor
    {
        Task<bool> InitializeAsync();
        Task<bool> ProcessEventAsync(SystemEvent systemEvent);
        Task<EventPattern[]> DetectPatternsAsync();
        Task<EventAlert[]> GetCriticalAlertsAsync();
        Task<EventProcessingMetrics> GetMetricsAsync();
        Task<EventAggregationResult> AggregateEventsAsync(TimeSpan window);
    }

    /// <summary>Represents a system event.</summary>
    public class SystemEvent
    {
        public string EventId { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string EventType { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public int Severity { get; set; } = 1;
        public Dictionary<string, object> Data { get; set; } = new();
        public string CorrelationId { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
    }

    /// <summary>Detected event pattern.</summary>
    public class EventPattern
    {
        public string PatternId { get; set; } = Guid.NewGuid().ToString();
        public string PatternName { get; set; } = string.Empty;
        public List<string> EventSequence { get; set; } = new();
        public int Occurrences { get; set; }
        public double Confidence { get; set; }
        public TimeSpan AverageTimespan { get; set; }
        public DateTime LastOccurrence { get; set; }
        public int Severity { get; set; }
        public bool IsCritical { get; set; }
    }

    /// <summary>Alert for critical event patterns.</summary>
    public class EventAlert
    {
        public string AlertId { get; set; } = Guid.NewGuid().ToString();
        public string PatternId { get; set; } = string.Empty;
        public string AlertMessage { get; set; } = string.Empty;
        public int SeverityLevel { get; set; }
        public DateTime AlertTime { get; set; } = DateTime.UtcNow;
        public List<string> InvolvedEventIds { get; set; } = new();
        public string RecommendedAction { get; set; } = string.Empty;
        public bool RequiresImmediateAction { get; set; }
    }

    /// <summary>Event processing metrics and statistics.</summary>
    public class EventProcessingMetrics
    {
        public long TotalEventsProcessed { get; set; }
        public int PatternsDetected { get; set; }
        public int CriticalAlertsGenerated { get; set; }
        public int EventsAggregated { get; set; }
        public double AverageProcessingTimeMs { get; set; }
        public double EventsPerSecond { get; set; }
        public long EventQueueDepth { get; set; }
        public DateTime LastProcessedTime { get; set; }
        public Dictionary<string, int> EventTypeDistribution { get; set; } = new();
        public double PatternDetectionAccuracy { get; set; }
    }

    /// <summary>Result of event aggregation within time window.</summary>
    public class EventAggregationResult
    {
        public string AggregationId { get; set; } = Guid.NewGuid().ToString();
        public TimeSpan AggregationWindow { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int TotalEventCount { get; set; }
        public Dictionary<string, int> EventCountByType { get; set; } = new();
        public Dictionary<string, int> EventCountBySource { get; set; } = new();
        public int CriticalEventCount { get; set; }
        public List<EventPattern> PatternsInWindow { get; set; } = new();
        public double AggregationCompleteness { get; set; }
    }
}
