using System;
using System.Collections.Generic;

namespace HELIOS.Platform.Core.FeatureFlags.Models
{
    /// <summary>
    /// Backup/restore snapshot metadata
    /// </summary>
    public class SettingsBackup
    {
        public string BackupId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime RestoredAt { get; set; }
        public string CreatedBy { get; set; }
        public Dictionary<string, object> GlobalSettings { get; set; } = new();
        public Dictionary<string, Dictionary<string, object>> UserSettings { get; set; } = new();
        public Dictionary<string, object> FeatureFlags { get; set; } = new();
        public int Version { get; set; }
        public string BackupType { get; set; } // Full, Partial, Differential
        public long SizeBytes { get; set; }
    }

    /// <summary>
    /// Import/export data container
    /// </summary>
    public class SettingsExport
    {
        public string ExportId { get; set; }
        public string ExportName { get; set; }
        public DateTime ExportedAt { get; set; }
        public string ExportedBy { get; set; }
        public string Format { get; set; } // JSON, XML, YAML, CSV
        public int Version { get; set; }
        public Dictionary<string, object> GlobalSettings { get; set; } = new();
        public Dictionary<string, Dictionary<string, object>> UserSettings { get; set; } = new();
        public Dictionary<string, object> FeatureFlags { get; set; } = new();
        public List<string> IncludedCategories { get; set; } = new();
        public string ExportScope { get; set; } // All, GlobalOnly, FeatureFlagsOnly
        public bool IncludeSensitiveData { get; set; }
    }

    /// <summary>
    /// Import configuration
    /// </summary>
    public class ImportConfiguration
    {
        public string SourcePath { get; set; }
        public string Format { get; set; }
        public bool OverwriteExisting { get; set; }
        public bool ValidateOnly { get; set; }
        public List<string> TargetCategories { get; set; } = new();
        public string ImportScope { get; set; } // All, GlobalOnly, FeatureFlagsOnly
        public bool ImportSensitiveData { get; set; }
        public Dictionary<string, string> MappingRules { get; set; } = new();
    }

    /// <summary>
    /// Import result with detailed status
    /// </summary>
    public class ImportResult
    {
        public bool Success { get; set; }
        public int ItemsImported { get; set; }
        public int ItemsSkipped { get; set; }
        public int ItemsFailed { get; set; }
        public List<ImportResultItem> Details { get; set; } = new();
        public string Message { get; set; }
        public DateTime ImportedAt { get; set; }
    }

    /// <summary>
    /// Individual import result item
    /// </summary>
    public class ImportResultItem
    {
        public string Key { get; set; }
        public bool Success { get; set; }
        public string Status { get; set; }
        public string ErrorMessage { get; set; }
        public object OriginalValue { get; set; }
        public object ImportedValue { get; set; }
    }

    /// <summary>
    /// Analytics event for feature flags
    /// </summary>
    public class FeatureFlagAnalyticsEvent
    {
        public string EventId { get; set; }
        public string FlagId { get; set; }
        public string UserId { get; set; }
        public string TenantId { get; set; }
        public string EventType { get; set; } // Evaluation, Toggle, Create, Update, Delete
        public bool FlagValue { get; set; }
        public Dictionary<string, object> Context { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Environment { get; set; }
        public string Region { get; set; }
    }

    /// <summary>
    /// Analytics metrics
    /// </summary>
    public class FeatureFlagMetrics
    {
        public string FlagId { get; set; }
        public long TotalEvaluations { get; set; }
        public long EnabledCount { get; set; }
        public long DisabledCount { get; set; }
        public double EnabledPercentage { get; set; }
        public Dictionary<string, long> UserSegmentMetrics { get; set; } = new();
        public Dictionary<string, long> EnvironmentMetrics { get; set; } = new();
        public DateTime LastEvaluatedAt { get; set; }
        public DateTime CalculatedAt { get; set; }
        public double AverageEvaluationTimeMs { get; set; }
    }

    /// <summary>
    /// Settings audit log entry
    /// </summary>
    public class SettingsAuditLog
    {
        public string AuditId { get; set; }
        public string SettingKey { get; set; }
        public string UserId { get; set; }
        public string Action { get; set; } // Create, Update, Delete, Restore
        public object OldValue { get; set; }
        public object NewValue { get; set; }
        public Dictionary<string, object> Changes { get; set; } = new();
        public string Reason { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Environment { get; set; }
        public string IpAddress { get; set; }
    }
}
