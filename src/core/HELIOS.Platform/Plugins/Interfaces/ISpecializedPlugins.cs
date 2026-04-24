// ═══════════════════════════════════════════════════════════════════════════
// Specialized Plugin Interfaces for Common Plugin Types
// ═══════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HELIOS.Platform.Plugins.Interfaces
{
    /// <summary>
    /// Interface for UI extension plugins
    /// Allows plugins to extend the user interface with custom components
    /// </summary>
    public interface IUIExtension : IPlugin
    {
        /// <summary>
        /// Gets the UI extension type (panel, dialog, ribbon, menu, etc.)
        /// </summary>
        string ExtensionType { get; }

        /// <summary>
        /// Gets the parent UI container identifier
        /// </summary>
        string ParentContainerId { get; }

        /// <summary>
        /// Loads the UI component asynchronously
        /// </summary>
        Task<object> LoadUIComponentAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Unloads the UI component
        /// </summary>
        Task UnloadUIComponentAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Handles UI interaction events
        /// </summary>
        Task<object> HandleUIEventAsync(string eventName, object eventData, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Interface for analyzer plugins
    /// Plugins that analyze system state or data
    /// </summary>
    public interface IAnalyzer : IPlugin
    {
        /// <summary>
        /// Gets the analysis scope
        /// </summary>
        string AnalysisScope { get; }

        /// <summary>
        /// Executes analysis on the specified target
        /// </summary>
        Task<AnalysisResult> AnalyzeAsync(
            object target,
            IAnalysisOptions options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets supported analysis options
        /// </summary>
        IReadOnlyList<string> GetSupportedOptions();

        /// <summary>
        /// Validates the target for analysis capability
        /// </summary>
        Task<bool> CanAnalyzeAsync(object target, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Interface for optimizer plugins
    /// Plugins that optimize system or application performance
    /// </summary>
    public interface IOptimizer : IPlugin
    {
        /// <summary>
        /// Gets the optimization area
        /// </summary>
        string OptimizationArea { get; }

        /// <summary>
        /// Performs optimization analysis and returns recommendations
        /// </summary>
        Task<OptimizationResult> OptimizeAsync(
            object target,
            IOptimizationOptions options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets supported optimization levels
        /// </summary>
        IReadOnlyList<OptimizationLevel> GetSupportedLevels();

        /// <summary>
        /// Applies an optimization recommendation
        /// </summary>
        Task<bool> ApplyOptimizationAsync(
            string recommendationId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Reverts an applied optimization
        /// </summary>
        Task<bool> RollbackOptimizationAsync(
            string recommendationId,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Analyzer result containing findings
    /// </summary>
    public class AnalysisResult
    {
        public string AnalyzerId { get; set; }
        public DateTime AnalysisTime { get; set; }
        public bool IsSuccessful { get; set; }
        public string ScopeAnalyzed { get; set; }
        public List<AnalysisFinding> Findings { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
        public TimeSpan ExecutionTime { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Individual finding from an analysis
    /// </summary>
    public class AnalysisFinding
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public FindingSeverity Severity { get; set; }
        public string Category { get; set; }
        public object AffectedResource { get; set; }
        public Dictionary<string, object> Details { get; set; } = new();
    }

    /// <summary>
    /// Severity level for findings
    /// </summary>
    public enum FindingSeverity
    {
        Information = 0,
        Warning = 1,
        Error = 2,
        Critical = 3
    }

    /// <summary>
    /// Optimization result with recommendations
    /// </summary>
    public class OptimizationResult
    {
        public string OptimizerId { get; set; }
        public DateTime OptimizationTime { get; set; }
        public bool IsSuccessful { get; set; }
        public string AreaOptimized { get; set; }
        public List<OptimizationRecommendation> Recommendations { get; set; } = new();
        public double EstimatedImprovementPercent { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Individual optimization recommendation
    /// </summary>
    public class OptimizationRecommendation
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public OptimizationLevel Level { get; set; }
        public double EstimatedImpactPercent { get; set; }
        public bool IsAutoApplicable { get; set; }
        public string RiskLevel { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
        public DateTime RecommendedApplyTime { get; set; }
    }

    /// <summary>
    /// Optimization level intensity
    /// </summary>
    public enum OptimizationLevel
    {
        Conservative = 1,
        Moderate = 2,
        Aggressive = 3,
        Experimental = 4
    }

    /// <summary>
    /// Analysis options interface
    /// </summary>
    public interface IAnalysisOptions
    {
        bool IncludeDetailedMetrics { get; set; }
        bool EnableComparisonWithBaseline { get; set; }
        int TimeoutSeconds { get; set; }
        Dictionary<string, object> CustomParameters { get; set; }
    }

    /// <summary>
    /// Optimization options interface
    /// </summary>
    public interface IOptimizationOptions
    {
        OptimizationLevel Level { get; set; }
        bool DryRun { get; set; }
        bool AllowAutoApply { get; set; }
        int TimeoutSeconds { get; set; }
        Dictionary<string, object> CustomParameters { get; set; }
    }

    /// <summary>
    /// Default implementations for options
    /// </summary>
    public class AnalysisOptions : IAnalysisOptions
    {
        public bool IncludeDetailedMetrics { get; set; } = true;
        public bool EnableComparisonWithBaseline { get; set; } = false;
        public int TimeoutSeconds { get; set; } = 300;
        public Dictionary<string, object> CustomParameters { get; set; } = new();
    }

    public class OptimizationOptions : IOptimizationOptions
    {
        public OptimizationLevel Level { get; set; } = OptimizationLevel.Moderate;
        public bool DryRun { get; set; } = true;
        public bool AllowAutoApply { get; set; } = false;
        public int TimeoutSeconds { get; set; } = 600;
        public Dictionary<string, object> CustomParameters { get; set; } = new();
    }
}
