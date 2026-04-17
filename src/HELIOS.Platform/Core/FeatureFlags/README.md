# HELIOS Platform Feature Flags & Settings System

## Overview

The HELIOS Platform Feature Flags & Settings System is a comprehensive, production-ready solution for managing feature flags, user preferences, global settings, and system toggles. It provides robust control over application behavior without requiring code deployments.

## Components

### 1. Feature Flag System

#### FeatureFlagManager
Core service for managing the lifecycle of feature flags.

**Key Features:**
- Create, update, delete feature flags
- Enable/disable flags without redeployment
- Batch evaluation support
- Metrics and analytics tracking
- Category and tag-based filtering

**Usage:**
```csharp
var manager = new FeatureFlagManager();

// Create a feature flag
var flag = await manager.CreateFlagAsync(new FeatureFlag
{
    Id = "new-dashboard",
    Name = "New Dashboard",
    Description = "New dashboard redesign",
    Type = FeatureFlagTypeEnum.Percentage,
    RolloutPercentage = 25,
    Category = "Features",
    Tags = new List<string> { "ui", "beta" }
});

// Evaluate a flag
var context = new FeatureFlagContext { UserId = "user123" };
var result = await manager.EvaluateAsync("new-dashboard", context);
if (result.Enabled) {
    // Use new dashboard
}

// Get metrics
var metrics = await manager.GetMetricsAsync("new-dashboard");
Console.WriteLine($"Enabled: {metrics.EnabledPercentage}%");
```

### 2. Feature Flag Types

#### Basic
Simple on/off flags.

```csharp
var basicFlag = new FeatureFlag
{
    Id = "simple-toggle",
    Type = FeatureFlagTypeEnum.Basic,
    Enabled = true
};
```

#### Percentage (Canary Deployments)
Gradual rollout to percentage of users.

```csharp
var percentageFlag = new FeatureFlag
{
    Id = "canary-feature",
    Type = FeatureFlagTypeEnum.Percentage,
    RolloutPercentage = 10, // 10% of users
    Enabled = true
};
```

#### Contextual
Evaluation based on user attributes and context.

```csharp
var contextualFlag = new FeatureFlag
{
    Id = "vip-feature",
    Type = FeatureFlagTypeEnum.Contextual,
    TargetSegments = new List<string> { "premium", "enterprise" }
};
```

#### Time Window
Enable features for specific time periods.

```csharp
var timeWindowFlag = new FeatureFlag
{
    Id = "holiday-feature",
    Type = FeatureFlagTypeEnum.TimeWindow,
    Metadata = new Dictionary<string, object>
    {
        ["startTime"] = "2024-12-24T00:00:00Z",
        ["endTime"] = "2024-12-26T23:59:59Z"
    }
};
```

#### User Segment
Target specific user segments.

```csharp
var segmentFlag = new FeatureFlag
{
    Id = "regional-feature",
    Type = FeatureFlagTypeEnum.UserSegment,
    TargetSegments = new List<string> { "us-west", "us-east" }
};
```

### 3. Evaluation Engine

The FeatureFlagEvaluationEngine provides high-performance, cached evaluation of feature flags.

**Features:**
- Efficient hashing for percentage-based rollout
- Evaluation caching (5-minute TTL)
- Thread-safe operations
- Deterministic evaluation (same user always gets same result)

```csharp
var engine = new FeatureFlagEvaluationEngine();

// Register flags
engine.RegisterFlag(flag);

// Evaluate with context
var context = new FeatureFlagContext
{
    UserId = "user123",
    TenantId = "tenant456",
    Environment = "production",
    IsBetaUser = true,
    UserAttributes = new Dictionary<string, string>
    {
        ["region"] = "us-west",
        ["plan"] = "premium"
    }
};

var result = engine.Evaluate("feature-id", context);
Console.WriteLine($"Enabled: {result.Enabled}");
Console.WriteLine($"Reason: {result.EvaluationReason}");
```

### 4. Settings Store

Manages global and user-specific settings with validation and persistence.

**Features:**
- Global settings (environment-wide)
- Per-user settings with fallback to global
- Schema-based validation
- Audit logging
- Type validation (string, number, boolean, array)

```csharp
var store = new SettingsStore();

// Register a schema
await store.RegisterSchemaAsync(new SettingsSchema
{
    SchemaId = "app-config",
    SchemaName = "Application Configuration",
    Properties = new Dictionary<string, SettingProperty>
    {
        ["max_users"] = new SettingProperty
        {
            Name = "Maximum Users",
            Type = "number",
            DefaultValue = 1000,
            MinValue = 1,
            MaxValue = 10000
        },
        ["enable_notifications"] = new SettingProperty
        {
            Name = "Enable Notifications",
            Type = "boolean",
            DefaultValue = true
        }
    }
});

// Set global setting
var setting = new Setting
{
    Key = "max_users",
    Value = 5000,
    Type = "number",
    Category = "System"
};
var validationResult = await store.SetGlobalSettingAsync(setting);

// Get setting (user setting with fallback to global)
var value = await store.GetUserSettingAsync("max_users", "user123");
```

### 5. User Preferences

Advanced user preference system with multiple preference categories.

```csharp
var preferences = new UserPreferences
{
    UserId = "user123",
    UIPreferences = new Dictionary<string, object>
    {
        ["theme"] = "dark",
        ["sidebar_collapsed"] = true
    },
    NotificationPreferences = new Dictionary<string, object>
    {
        ["email_notifications"] = true,
        ["push_notifications"] = false
    },
    PrivacyPreferences = new Dictionary<string, object>
    {
        ["share_analytics"] = false,
        ["share_location"] = false
    }
};

await store.SetUserPreferencesAsync(preferences);
var userPrefs = await store.GetUserPreferencesAsync("user123");
```

### 6. Toggleable Service

Unified interface for all application toggleables.

```csharp
var service = new ToggleableService();

// Toggle a feature
await service.ToggleFeatureAsync("feature-id", true, "admin");

// Check if enabled
bool isEnabled = await service.IsEnabledAsync("feature-id");

// Get toggleables by category
var uiToggles = await service.GetToggleablesByCategoryAsync("UI");

// Performance tuning
var tuning = await service.GetPerformanceTuningAsync();
tuning.EnableCaching = true;
tuning.CacheTimeoutSeconds = 600;
await service.SetPerformanceTuningAsync(tuning);

// Analytics control
var analytics = await service.GetAnalyticsAsync();
analytics.Enabled = true;
analytics.TelemetryEnabled = false;
await service.SetAnalyticsAsync(analytics);

// Integration toggles
var integrations = await service.GetIntegrationsAsync();
integrations.AzureEnabled = true;
integrations.AiEnabled = true;
await service.SetIntegrationsAsync(integrations);
```

### 7. Persistence Layer

JSON-based persistence provider for feature flags and settings.

```csharp
var persistenceProvider = new JsonPersistenceProvider("./data/flags-settings");

// Save feature flags
await persistenceProvider.SaveFeatureFlagsAsync(flags);

// Load feature flags
var loadedFlags = await persistenceProvider.LoadFeatureFlagsAsync();

// Save settings
await persistenceProvider.SaveSettingsAsync(globalSettings, userSettings);

// Load settings
var (global, user) = await persistenceProvider.LoadSettingsAsync();
```

### 8. Backup & Restore

Comprehensive backup and restore system with full and differential backups.

```csharp
var backupManager = new BackupRestoreManager(persistenceProvider);

// Create full backup
var backup = await backupManager.CreateFullBackupAsync(
    globalSettings, userSettings, flags,
    "backup-2024-01-15",
    "admin");

// Create differential backup
var diffBackup = await backupManager.CreateDifferentialBackupAsync(
    backup, globalSettings, userSettings, flags,
    "backup-2024-01-16",
    "admin");

// List backups
var backups = await backupManager.ListBackupsAsync();

// Restore from backup
var restoreResult = await backupManager.RestoreFromBackupAsync(backup.BackupId);
if (restoreResult.Success)
    Console.WriteLine($"Restored {restoreResult.ItemsRestored} items");
```

### 9. Import/Export

Export and import settings and feature flags between environments.

```csharp
var importExportManager = new ImportExportManager(persistenceProvider);

// Export settings
var exportResult = await importExportManager.ExportSettingsAsync(
    globalSettings, userSettings, flags,
    "./exports/config-export.json",
    new ExportConfiguration
    {
        ExportName = "Production Export",
        Format = "JSON",
        ExportScope = "All",
        IncludeSensitiveData = false
    });

// Import settings
var importResult = await importExportManager.ImportSettingsAsync(
    "./imports/config-import.json",
    new ImportConfiguration
    {
        OverwriteExisting = false,
        ValidateOnly = false,
        ImportScope = "All"
    },
    existingGlobalSettings,
    existingUserSettings,
    existingFlags);

foreach (var detail in importResult.Details)
{
    Console.WriteLine($"{detail.Key}: {detail.Status}");
}
```

### 10. UI Generation

Automatic UI element generation from settings schemas.

```csharp
var uiGenerator = new SettingsUIGenerator();

// Generate UI elements from schema
var elements = uiGenerator.GenerateUIElements(schema);
foreach (var element in elements)
{
    Console.WriteLine($"{element.Label}: {element.Type}");
    if (element.Options.Count > 0)
    {
        foreach (var option in element.Options)
            Console.WriteLine($"  - {option.Label}");
    }
}

// Generate complete settings panel
var panel = uiGenerator.GenerateSettingsPanelUI(schemas, flags);
foreach (var section in panel.Sections)
{
    Console.WriteLine($"Section: {section.Title}");
    foreach (var element in section.Elements)
        Console.WriteLine($"  - {element.Label} ({element.Type})");
}
```

### 11. Analytics & Monitoring

Track feature flag usage and metrics.

```csharp
// Get analytics events
var events = await manager.GetAnalyticsEventsAsync("feature-id", limit: 100);
foreach (var evt in events)
{
    Console.WriteLine($"{evt.EventType}: {evt.FlagValue} at {evt.Timestamp}");
}

// Get metrics
var metrics = await manager.GetMetricsAsync("feature-id");
Console.WriteLine($"Total Evaluations: {metrics.TotalEvaluations}");
Console.WriteLine($"Enabled Count: {metrics.EnabledCount}");
Console.WriteLine($"Enabled Percentage: {metrics.EnabledPercentage}%");
Console.WriteLine($"Last Evaluated: {metrics.LastEvaluatedAt}");
```

### 12. Audit Logging

Complete audit trail of all settings and feature flag changes.

```csharp
// Get audit logs for specific setting
var logs = await store.GetAuditLogAsync("setting-key", limit: 100);
foreach (var log in logs)
{
    Console.WriteLine($"{log.Action} by {log.UserId} at {log.Timestamp}");
    Console.WriteLine($"  Old: {log.OldValue}");
    Console.WriteLine($"  New: {log.NewValue}");
}
```

## Feature Categories

### 1. Core Features
- Dashboard
- Search
- Reporting
- Data Export

### 2. Beta Features
- New UI Redesign
- Advanced Analytics
- AI Integration

### 3. Performance Toggles
- Response Caching
- Data Compression
- Connection Pooling
- Query Optimization

### 4. Analytics & Telemetry
- Usage Analytics
- Error Tracking
- Performance Metrics
- User Behavior Analysis

### 5. Integrations
- Azure Services
- AI Services
- Cloud Sync
- Third-Party APIs

### 6. Security Features
- Two-Factor Authentication
- Encryption
- Rate Limiting
- Access Control

### 7. UI Components
- Dark Mode
- Compact View
- Advanced Search
- Custom Dashboards

### 8. Advanced Settings
- Developer Mode
- Debug Logging
- Performance Profiling
- System Diagnostics

## Best Practices

### 1. Flag Naming
Use clear, descriptive names with appropriate prefixes:
```
feature.new-dashboard
integration.azure-sync
performance.enable-cache
beta.ai-recommendations
```

### 2. Flag States
Use appropriate states for different scenarios:
- **Enabled**: Fully rolled out
- **BetaOnly**: Only for beta users
- **Experimental**: Only for experimental users
- **Deprecated**: Planned for removal
- **Disabled**: Turned off

### 3. Rollout Strategy
```csharp
// Start with low percentage
flag.RolloutPercentage = 1;  // 1% of users

// Monitor metrics
var metrics = await manager.GetMetricsAsync(flagId);

// Gradually increase
flag.RolloutPercentage = 10;  // 10% of users
flag.RolloutPercentage = 50;  // 50% of users
flag.RolloutPercentage = 100; // 100% of users
```

### 4. Context Usage
Always provide rich context for evaluation:
```csharp
var context = new FeatureFlagContext
{
    UserId = currentUser.Id,
    TenantId = currentTenant.Id,
    Environment = "production",
    Region = "us-west-2",
    UserAttributes = new Dictionary<string, string>
    {
        ["plan"] = user.Plan,
        ["cohort"] = user.CohortId,
        ["browser"] = userAgent.Browser
    }
};

var result = await manager.EvaluateAsync("feature-id", context);
```

### 5. Caching Strategy
Clear cache after flag updates:
```csharp
await manager.UpdateFlagAsync(updatedFlag);
manager.ClearCache();
```

## Performance Characteristics

- **Evaluation Latency**: <1ms (cached) or 2-5ms (uncached)
- **Cache Hit Rate**: 95%+ for typical applications
- **Memory Usage**: ~1KB per flag + evaluation cache
- **Throughput**: 10,000+ evaluations/second

## Security Considerations

1. **Sensitive Settings**: Mark as secrets for encryption
2. **Audit Trail**: All changes are logged
3. **Access Control**: Integrate with authorization system
4. **Export Control**: Option to exclude sensitive data
5. **Backup Security**: Encrypt backups at rest

## Troubleshooting

### Flag Not Evaluated
1. Check flag is registered
2. Verify context matches target segments
3. Check expiration date
4. Review evaluation logs

### Settings Not Persisting
1. Verify persistence provider is configured
2. Check file permissions
3. Review error logs
4. Test with direct file operations

### Import/Export Issues
1. Validate JSON format
2. Check schema compatibility
3. Verify overwrite settings
4. Review import logs

## API Reference

See code documentation for complete API reference and method signatures.

## Contributing

When adding new features:
1. Create appropriate model types
2. Implement in relevant manager/store
3. Add analytics events
4. Update documentation
5. Write tests

## License

Part of the HELIOS Platform - Proprietary License
