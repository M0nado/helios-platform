using System;
using System.Collections.Generic;

namespace HELIOS.Platform.Core.FeatureFlags.Models
{
    /// <summary>
    /// Settings schema definition for validation
    /// </summary>
    public class SettingsSchema
    {
        public string SchemaId { get; set; }
        public string SchemaName { get; set; }
        public string Description { get; set; }
        public int Version { get; set; }
        public Dictionary<string, SettingProperty> Properties { get; set; } = new();
        public List<string> Required { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastModifiedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Individual setting property definition
    /// </summary>
    public class SettingProperty
    {
        public string Name { get; set; }
        public string Type { get; set; } // string, number, boolean, array, object
        public string Description { get; set; }
        public object DefaultValue { get; set; }
        public object Value { get; set; }
        public bool Required { get; set; }
        public List<object> AllowedValues { get; set; } = new();
        public int? MinValue { get; set; }
        public int? MaxValue { get; set; }
        public int? MinLength { get; set; }
        public int? MaxLength { get; set; }
        public string Pattern { get; set; }
        public string Category { get; set; }
        public bool IsSecret { get; set; }
        public bool IsReadOnly { get; set; }
        public int Priority { get; set; } = 0;
        public List<string> Tags { get; set; } = new();
        public DateTime LastModifiedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Setting with metadata and validation
    /// </summary>
    public class Setting
    {
        public string SettingId { get; set; }
        public string Key { get; set; }
        public object Value { get; set; }
        public string Type { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public bool IsGlobal { get; set; }
        public string UserId { get; set; }
        public string TenantId { get; set; }
        public bool IsSecret { get; set; }
        public bool IsReadOnly { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastModifiedAt { get; set; } = DateTime.UtcNow;
        public int Version { get; set; } = 1;
        public List<string> Tags { get; set; } = new();
    }

    /// <summary>
    /// User preferences container
    /// </summary>
    public class UserPreferences
    {
        public string PreferenceId { get; set; }
        public string UserId { get; set; }
        public Dictionary<string, object> UIPreferences { get; set; } = new();
        public Dictionary<string, object> NotificationPreferences { get; set; } = new();
        public Dictionary<string, object> PrivacyPreferences { get; set; } = new();
        public Dictionary<string, object> PerformancePreferences { get; set; } = new();
        public Dictionary<string, object> AccessibilityPreferences { get; set; } = new();
        public Dictionary<string, object> CustomPreferences { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastModifiedAt { get; set; } = DateTime.UtcNow;
        public int Version { get; set; } = 1;
    }

    /// <summary>
    /// Global settings container
    /// </summary>
    public class GlobalSettings
    {
        public string GlobalSettingsId { get; set; }
        public string Environment { get; set; }
        public Dictionary<string, object> System { get; set; } = new();
        public Dictionary<string, object> Security { get; set; } = new();
        public Dictionary<string, object> Performance { get; set; } = new();
        public Dictionary<string, object> Analytics { get; set; } = new();
        public Dictionary<string, object> Integration { get; set; } = new();
        public Dictionary<string, object> UI { get; set; } = new();
        public Dictionary<string, object> Custom { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastModifiedAt { get; set; } = DateTime.UtcNow;
        public int Version { get; set; } = 1;
    }

    /// <summary>
    /// Settings validation result
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<ValidationError> Errors { get; set; } = new();
        public List<ValidationWarning> Warnings { get; set; } = new();

        public ValidationResult()
        {
            IsValid = true;
        }

        public ValidationResult(bool isValid)
        {
            IsValid = isValid;
        }
    }

    /// <summary>
    /// Validation error details
    /// </summary>
    public class ValidationError
    {
        public string PropertyName { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public object AttemptedValue { get; set; }
    }

    /// <summary>
    /// Validation warning details
    /// </summary>
    public class ValidationWarning
    {
        public string PropertyName { get; set; }
        public string WarningCode { get; set; }
        public string WarningMessage { get; set; }
    }
}
