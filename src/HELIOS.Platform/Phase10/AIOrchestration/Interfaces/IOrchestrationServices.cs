using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HELIOS.Platform.Phase10.AIOrchestration.Models;

namespace HELIOS.Platform.Phase10.AIOrchestration.Interfaces
{
    /// <summary>
    /// Core interface for tool orchestration engine
    /// </summary>
    public interface IToolOrchestratorEngine
    {
        Task InitializeAsync();
        Task ShutdownAsync();
        Task<bool> RegisterToolAsync(ToolInfo tool);
        Task<bool> UnregisterToolAsync(string toolId);
        Task<ToolInfo> GetToolInfoAsync(string toolId);
        Task<IEnumerable<ToolInfo>> GetAllToolsAsync();
        Task<bool> StartToolAsync(string toolId);
        Task<bool> StopToolAsync(string toolId);
        Task SwitchProfileAsync(OrchestrationProfile profile);
        Task<OrchestrationProfile> GetCurrentProfileAsync();
        Task<OrchestrationStats> GetStatsAsync();
        Task<bool> IsHealthyAsync();
    }

    /// <summary>
    /// Interface for tool optimization profiler
    /// </summary>
    public interface IToolOptimizationProfiler
    {
        Task InitializeAsync();
        Task ProfileToolAsync(string toolId);
        Task<ToolPerformanceMetrics> GetPerformanceMetricsAsync(string toolId);
        Task<List<OptimizationRecommendation>> AnalyzeToolAsync(string toolId);
        Task<bool> ApplyOptimizationAsync(string toolId, OptimizationRecommendation recommendation);
        Task<ToolResourceAllocation> OptimizeAllocationAsync(string toolId, OrchestrationProfile profile);
        Task SaveProfileConfigAsync(string toolId, ToolProfileConfig config);
        Task<ToolProfileConfig> LoadProfileConfigAsync(string toolId, string profileName);
        Task<Dictionary<string, ToolProfileConfig>> GetAllProfilesAsync(string toolId);
        Task UpdateResourceAllocationAsync(string toolId, ToolResourceAllocation allocation);
    }

    /// <summary>
    /// Interface for tool health monitoring and coordination
    /// </summary>
    public interface IToolHealthMonitorCoordinator
    {
        Task InitializeAsync();
        Task ShutdownAsync();
        Task<ToolHealthMetrics> GetHealthMetricsAsync(string toolId);
        Task<bool> IsToolHealthyAsync(string toolId);
        Task<List<ToolConflict>> DetectConflictsAsync();
        Task<bool> ResolveConflictAsync(string conflictId);
        Task<bool> RestartToolAsync(string toolId);
        Task<List<MaintenancePrediction>> GetMaintenancePredictionsAsync();
        Task ScheduleMaintenanceAsync(string toolId, MaintenanceType type);
        Task<ToolDependencyGraph> GetDependencyGraphAsync();
        Task MonitorToolAsync(string toolId);
        Task<List<OrchestrationEvent>> GetRecentEventsAsync(int count = 100);
    }

    /// <summary>
    /// Interface for AI-based optimization learning
    /// </summary>
    public interface IAIOptimizationLearner
    {
        Task TrainAsync(string toolId, ToolPerformanceMetrics metrics, OrchestrationProfile profile);
        Task<Dictionary<string, object>> PredictOptimalSettingsAsync(string toolId, OrchestrationProfile profile);
        Task<double> PredictPerformanceAsync(string toolId, Dictionary<string, object> settings);
        Task SaveModelAsync(string toolId);
        Task LoadModelAsync(string toolId);
    }

    /// <summary>
    /// Interface for inter-tool communication coordination
    /// </summary>
    public interface IToolCommunicationCoordinator
    {
        Task<bool> RegisterCommunicationAsync(string sourceTool, string targetTool, string protocol);
        Task<bool> SendMessageAsync(string sourceTool, string targetTool, object message);
        Task<object> RequestResponseAsync(string sourceTool, string targetTool, object request, int timeoutMs = 5000);
        Task<bool> BroadcastAsync(string sourceTool, object message, IEnumerable<string> recipients);
        Task<bool> UnregisterCommunicationAsync(string sourceTool, string targetTool);
    }

    /// <summary>
    /// Interface for tool conflict resolution
    /// </summary>
    public interface IToolConflictResolver
    {
        Task<ToolConflict> DetectConflictAsync(string tool1Id, string tool2Id);
        Task<bool> ResolveAsync(ToolConflict conflict);
        Task<List<string>> GetResolutionStrategiesAsync(ConflictType type);
    }

    /// <summary>
    /// Interface for profile management
    /// </summary>
    public interface IProfileManager
    {
        Task<OrchestrationProfile> GetCurrentProfileAsync();
        Task SwitchProfileAsync(OrchestrationProfile profile);
        Task<ToolProfileConfig> GetProfileConfigAsync(string toolId, OrchestrationProfile profile);
        Task SaveProfileConfigAsync(string toolId, OrchestrationProfile profile, ToolProfileConfig config);
        Task<Dictionary<string, ToolProfileConfig>> GetAllProfilesAsync();
    }

    /// <summary>
    /// Interface for performance monitoring
    /// </summary>
    public interface IPerformanceMonitor
    {
        Task StartMonitoringAsync(string toolId);
        Task StopMonitoringAsync(string toolId);
        Task<ToolPerformanceMetrics> GetMetricsAsync(string toolId);
        Task<List<ToolPerformanceMetrics>> GetAllMetricsAsync();
        Task<bool> ExceedsThresholdAsync(string toolId, string metric, double threshold);
    }
}
