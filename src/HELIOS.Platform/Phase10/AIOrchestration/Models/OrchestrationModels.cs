using System;
using System.Collections.Generic;

namespace HELIOS.Platform.Phase10.AIOrchestration.Models
{
    /// <summary>
    /// Profile types for automatic switching
    /// </summary>
    public enum OrchestrationProfile
    {
        Gaming,
        Development,
        Work,
        Secure
    }

    /// <summary>
    /// Tool conflict information
    /// </summary>
    public class ToolConflict
    {
        public string ConflictId { get; set; }
        public string Tool1 { get; set; }
        public string Tool2 { get; set; }
        public ConflictType Type { get; set; }
        public string Description { get; set; }
        public ConflictSeverity Severity { get; set; }
        public bool AutoResolved { get; set; }
        public string ResolutionStrategy { get; set; }
        public DateTime DetectedAt { get; set; }

        public ToolConflict()
        {
            ConflictId = Guid.NewGuid().ToString();
            DetectedAt = DateTime.UtcNow;
        }
    }

    public enum ConflictType
    {
        ResourceContention,
        DependencyMissing,
        VersionIncompatibility,
        CommunicationFailure,
        StateInconsistency,
        PermissionDenied
    }

    public enum ConflictSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Tool optimization recommendation
    /// </summary>
    public class OptimizationRecommendation
    {
        public string RecommendationId { get; set; }
        public string ToolId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public OptimizationCategory Category { get; set; }
        public double ExpectedImprovement { get; set; } // 0-100%
        public int Priority { get; set; } // 1-10
        public Dictionary<string, object> Changes { get; set; }
        public bool Applied { get; set; }
        public DateTime CreatedAt { get; set; }

        public OptimizationRecommendation()
        {
            RecommendationId = Guid.NewGuid().ToString();
            Changes = new Dictionary<string, object>();
            CreatedAt = DateTime.UtcNow;
        }
    }

    public enum OptimizationCategory
    {
        CpuUsage,
        MemoryUsage,
        DiskIO,
        GpuUsage,
        Latency,
        Throughput,
        Reliability,
        Security
    }

    /// <summary>
    /// Maintenance prediction
    /// </summary>
    public class MaintenancePrediction
    {
        public string PredictionId { get; set; }
        public string ToolId { get; set; }
        public MaintenanceType Type { get; set; }
        public string Description { get; set; }
        public double Confidence { get; set; } // 0-100%
        public DateTime PredictedDate { get; set; }
        public string RecommendedAction { get; set; }
        public DateTime CreatedAt { get; set; }

        public MaintenancePrediction()
        {
            PredictionId = Guid.NewGuid().ToString();
            CreatedAt = DateTime.UtcNow;
        }
    }

    public enum MaintenanceType
    {
        Update,
        Restart,
        MemoryCleanup,
        CacheReset,
        ConfigUpdate,
        DependencyUpdate
    }

    /// <summary>
    /// Orchestration event for logging and tracking
    /// </summary>
    public class OrchestrationEvent
    {
        public string EventId { get; set; }
        public string ToolId { get; set; }
        public EventType Type { get; set; }
        public string Message { get; set; }
        public Dictionary<string, object> Data { get; set; }
        public EventSeverity Severity { get; set; }
        public DateTime Timestamp { get; set; }

        public OrchestrationEvent()
        {
            EventId = Guid.NewGuid().ToString();
            Data = new Dictionary<string, object>();
            Timestamp = DateTime.UtcNow;
        }
    }

    public enum EventType
    {
        ToolInitialized,
        ToolStarted,
        ToolStopped,
        ToolFailed,
        ToolRestarted,
        HealthCheckFailed,
        ConflictDetected,
        ConflictResolved,
        OptimizationApplied,
        ProfileSwitched,
        ResourceAllocated,
        MaintenanceScheduled
    }

    public enum EventSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }

    /// <summary>
    /// Tool interaction dependency graph
    /// </summary>
    public class ToolDependencyGraph
    {
        public Dictionary<string, ToolNode> Nodes { get; set; }
        public List<ToolEdge> Edges { get; set; }
        public DateTime LastUpdated { get; set; }

        public ToolDependencyGraph()
        {
            Nodes = new Dictionary<string, ToolNode>();
            Edges = new List<ToolEdge>();
            LastUpdated = DateTime.UtcNow;
        }
    }

    public class ToolNode
    {
        public string ToolId { get; set; }
        public string ToolName { get; set; }
        public int Priority { get; set; }
        public List<string> Dependencies { get; set; }

        public ToolNode()
        {
            Dependencies = new List<string>();
        }
    }

    public class ToolEdge
    {
        public string SourceToolId { get; set; }
        public string TargetToolId { get; set; }
        public DependencyType Type { get; set; }
        public string Description { get; set; }
    }

    public enum DependencyType
    {
        Requires,
        RequiredBy,
        Conflicts,
        Communicates,
        Shares
    }

    /// <summary>
    /// Statistics for orchestration performance
    /// </summary>
    public class OrchestrationStats
    {
        public int TotalToolsManaged { get; set; }
        public int ToolsRunning { get; set; }
        public int ToolsFailed { get; set; }
        public int ConflictsDetected { get; set; }
        public int ConflictsResolved { get; set; }
        public int OptimizationsApplied { get; set; }
        public double AverageSystemHealth { get; set; }
        public double AverageCpuUtilization { get; set; }
        public double AverageMemoryUtilization { get; set; }
        public DateTime CalculatedAt { get; set; }

        public OrchestrationStats()
        {
            CalculatedAt = DateTime.UtcNow;
        }
    }
}
