using System;
using System.Collections.Generic;

namespace HELIOS.Platform.Core.FeatureFlags.Models
{
    /// <summary>
    /// Enumeration of supported feature flag types
    /// </summary>
    public enum FeatureFlagTypeEnum
    {
        Basic = 0,
        Percentage = 1,
        Contextual = 2,
        TimeWindow = 3,
        UserSegment = 4
    }

    /// <summary>
    /// Enumeration of feature flag states
    /// </summary>
    public enum FeatureFlagState
    {
        Disabled = 0,
        Enabled = 1,
        BetaOnly = 2,
        Experimental = 3,
        Deprecated = 4
    }

    /// <summary>
    /// Feature flag definition
    /// </summary>
    public class FeatureFlag
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public FeatureFlagTypeEnum Type { get; set; }
        public FeatureFlagState State { get; set; }
        public bool Enabled { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastModifiedAt { get; set; }
        public string CreatedBy { get; set; }
        public string LastModifiedBy { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
        public List<string> Tags { get; set; } = new();
        public List<string> DependsOn { get; set; } = new();
        public int? RolloutPercentage { get; set; }
        public List<string> TargetSegments { get; set; } = new();
        public DateTime? ExpiresAt { get; set; }
        public string Category { get; set; }
        public int Priority { get; set; } = 0;
    }

    /// <summary>
    /// Contextual feature flag evaluation context
    /// </summary>
    public class FeatureFlagContext
    {
        public string UserId { get; set; }
        public Dictionary<string, string> UserAttributes { get; set; } = new();
        public Dictionary<string, object> RequestContext { get; set; } = new();
        public DateTime EvaluationTime { get; set; } = DateTime.UtcNow;
        public string Environment { get; set; }
        public string Region { get; set; }
        public string TenantId { get; set; }
        public bool IsBetaUser { get; set; }
        public bool IsExperimentalUser { get; set; }
    }

    /// <summary>
    /// Feature flag evaluation result
    /// </summary>
    public class FeatureFlagEvaluationResult
    {
        public string FlagId { get; set; }
        public bool Enabled { get; set; }
        public FeatureFlagTypeEnum FlagType { get; set; }
        public string EvaluationReason { get; set; }
        public Dictionary<string, object> VariationData { get; set; } = new();
        public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;
        public string ContextId { get; set; }
    }
}
