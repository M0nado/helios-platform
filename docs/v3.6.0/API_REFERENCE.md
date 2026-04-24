# HELIOS Platform v3.6.0 - API Reference

**Version**: 3.6.0  
**Status**: Production Ready ✅  
**Last Updated**: 2026-05-15

## Table of Contents

1. [CloudSync API](#cloudsync-api)
2. [PluginSystem API](#pluginsystem-api)
3. [MLService API](#mlservice-api)
4. [Dashboard Extension API](#dashboard-extension-api)
5. [Theme API](#theme-api)
6. [Internal Architecture APIs](#internal-architecture-apis)

---

## CloudSync API

### Namespace: `HELIOS.Cloud.Sync`

#### CloudSyncManager

Main orchestrator for cloud synchronization operations.

```csharp
public class CloudSyncManager
{
    // Initialization
    public Task InitializeProviderAsync(CloudProvider provider, IProviderCredentials credentials);
    public Task<bool> IsInitializedAsync();
    
    // Synchronization
    public Task<SyncResult> SyncAsync();
    public Task<SyncResult> SyncAsync(SyncOptions options);
    public Task<SyncResult> SyncFolderAsync(string folderPath);
    public Task<SyncResult> SyncFileAsync(string filePath);
    public Task PushAsync();
    public Task PullAsync();
    
    // Auto-sync
    public Task EnableAutoSyncAsync(SyncOptions options);
    public Task DisableAutoSyncAsync();
    public Task<bool> IsAutoSyncEnabledAsync();
    
    // Conflict Management
    public IConflictResolver GetConflictResolver();
    public Task<List<SyncConflict>> GetConflictsAsync();
    
    // Status and Monitoring
    public Task<SyncStatus> GetStatusAsync();
    public Task<SyncStatistics> GetStatisticsAsync();
    public IObservable<SyncProgress> GetProgress();
    
    // Configuration
    public Task<CloudSyncConfig> GetConfigurationAsync();
    public Task UpdateConfigurationAsync(CloudSyncConfig config);
}
```

#### SyncResult

Results of synchronization operations.

```csharp
public class SyncResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public int FilesSync { get; set; }
    public long BytesTransferred { get; set; }
    public TimeSpan Duration { get; set; }
    public List<SyncError> Errors { get; set; }
}
```

#### IConflictResolver

Handles synchronization conflicts.

```csharp
public interface IConflictResolver
{
    Task ResolveAsync(ConflictResolutionStrategy strategy);
    Task ResolveManuallyAsync(SyncConflict conflict, SyncVersion selectedVersion);
    Task<List<SyncConflict>> GetUnresolvedAsync();
}
```

---

## PluginSystem API

### Namespace: `HELIOS.Plugin`

#### IPlugin

Base interface for all plugins.

```csharp
public interface IPlugin
{
    Task InitializeAsync();
    Task<PluginResult> ExecuteAsync(PluginCommand command);
    Task ShutdownAsync();
    PluginInfo GetInfo();
}
```

#### PluginManager

Central management for plugins.

```csharp
public class PluginManager
{
    // Discovery and Installation
    public Task<List<PluginInfo>> DiscoverAsync();
    public Task<List<PluginInfo>> SearchMarketplaceAsync(string query);
    public Task InstallAsync(string pluginId, string version = null);
    public Task InstallLocalAsync(string packagePath);
    public Task InstallFromUrlAsync(string packageUrl);
    
    // Lifecycle Management
    public Task<IPlugin> LoadPluginAsync(string pluginId);
    public Task UnloadPluginAsync(string pluginId);
    public Task EnablePluginAsync(string pluginId);
    public Task DisablePluginAsync(string pluginId);
    
    // Plugin Execution
    public Task<PluginResult> ExecuteAsync(string pluginId, PluginCommand command);
    public Task<PluginResult> ExecuteAsync(string pluginId, string action, Dictionary<string, object> parameters);
    
    // Information Retrieval
    public Task<IPlugin> GetPluginAsync(string pluginId);
    public Task<List<IPlugin>> GetAllPluginsAsync();
    public Task<List<IPlugin>> GetInstalledPluginsAsync();
    public Task<List<IPlugin>> GetEnabledPluginsAsync();
    
    // Plugin Marketplace
    public IPluginMarketplace GetMarketplace();
}
```

#### PluginCommand

Command execution request.

```csharp
public class PluginCommand
{
    public string Action { get; set; }
    public Dictionary<string, object> Parameters { get; set; }
    public TimeSpan Timeout { get; set; }
    public CancellationToken CancellationToken { get; set; }
}
```

#### PluginResult

Result of plugin command execution.

```csharp
public class PluginResult
{
    public bool Success { get; set; }
    public object Data { get; set; }
    public string ErrorMessage { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    
    public static PluginResult Success(object data = null);
    public static PluginResult Failure(string message);
    public static PluginResult Timeout(TimeSpan duration);
}
```

#### IPluginMarketplace

Plugin marketplace integration.

```csharp
public interface IPluginMarketplace
{
    Task<List<MarketplacePlugin>> SearchAsync(string query);
    Task<List<MarketplacePlugin>> GetFeaturedAsync();
    Task<List<MarketplacePlugin>> GetRecommendedAsync(string category, string compatibility);
    Task<MarketplacePluginDetails> GetDetailsAsync(string pluginId);
    Task<bool> IsCompatibleAsync(string pluginId, string platformVersion);
    Task<bool> IsAvailableAsync(string pluginId);
}
```

---

## MLService API

### Namespace: `HELIOS.ML`

#### MLService

Machine learning service orchestrator.

```csharp
public class MLService
{
    // Model Management
    public Task<Model> RegisterModelAsync(ModelMetadata metadata);
    public Task<Model> GetModelAsync(string modelId);
    public Task<List<Model>> GetAllModelsAsync();
    public Task UnregisterModelAsync(string modelId);
    
    // Predictions
    public Task<Prediction> PredictAsync(string modelId, Dictionary<string, object> input);
    public Task<List<Prediction>> PredictBatchAsync(string modelId, IEnumerable input);
    public Task<Prediction> PredictAsync(string modelId, object input);
    
    // Model Training
    public Task<TrainingJob> TrainModelAsync(TrainingConfig config);
    public Task<TrainingJob> GetTrainingJobAsync(string jobId);
    public Task<List<TrainingJob>> GetTrainingJobsAsync(string modelId);
    public Task CancelTrainingAsync(string jobId);
    
    // Data Management
    public Task<Dataset> PrepareDataAsync(DataPipelineConfig config);
    public Task<Dataset> LoadDataAsync(string dataPath);
    
    // Model Evaluation
    public Task<EvaluationMetrics> EvaluateModelAsync(string modelId, Dataset testData);
    public Task<ModelComparison> CompareModelsAsync(string modelId1, string modelId2, Dataset testData);
    
    // Versioning
    public Task<Model> DeployModelVersionAsync(string modelId, string version);
    public Task<List<ModelVersion>> GetModelVersionsAsync(string modelId);
}
```

#### ModelMetadata

Model registration information.

```csharp
public class ModelMetadata
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Version { get; set; }
    public MLFramework Framework { get; set; }
    public string ModelPath { get; set; }
    public Schema InputSchema { get; set; }
    public Schema OutputSchema { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
}
```

#### Prediction

Prediction result.

```csharp
public class Prediction
{
    public string ModelId { get; set; }
    public Dictionary<string, object> Output { get; set; }
    public float Confidence { get; set; }
    public TimeSpan InferenceTime { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
}
```

#### TrainingConfig

Configuration for model training.

```csharp
public class TrainingConfig
{
    public string ModelId { get; set; }
    public Dataset TrainingData { get; set; }
    public string Algorithm { get; set; }
    public Dictionary<string, object> Hyperparameters { get; set; }
    public int EarlyStoppingPatience { get; set; }
    public string[] ValidationMetrics { get; set; }
    public float TestSplit { get; set; }
}
```

---

## Dashboard Extension API

### Namespace: `HELIOS.Dashboard`

#### DeveloperDashboard

Dashboard management and configuration.

```csharp
public class DeveloperDashboard
{
    public Task StartAsync(string hostAddress, int port, bool enableHttps = false, string certificatePath = null);
    public Task StopAsync();
    public Task<bool> IsRunningAsync();
    
    // View Management
    public Task AddViewAsync(IDashboardView view);
    public Task RemoveViewAsync(string viewId);
    public Task<List<IDashboardView>> GetViewsAsync();
    
    // Configuration
    public Task ConfigureAsync(DashboardConfiguration config);
    public Task<DashboardConfiguration> GetConfigurationAsync();
    
    // Real-time Updates
    public IObservable<DashboardUpdate> GetUpdates();
    public Task PublishUpdateAsync(DashboardUpdate update);
}
```

#### IDashboardView

Custom dashboard view interface.

```csharp
public interface IDashboardView
{
    string ViewId { get; }
    string DisplayName { get; }
    Task InitializeAsync();
    Task RefreshAsync();
    Task<object> GetDataAsync();
}
```

#### DashboardConfiguration

Dashboard settings.

```csharp
public class DashboardConfiguration
{
    public DashboardView[] EnabledViews { get; set; }
    public int RefreshIntervalMs { get; set; }
    public TimeSpan MaxLogRetention { get; set; }
    public bool EnableRealTimeUpdates { get; set; }
    public bool AllowRemoteAccess { get; set; }
    public bool AuthenticationRequired { get; set; }
    public int MaxConcurrentConnections { get; set; }
}
```

#### DashboardView Enumeration

```csharp
public enum DashboardView
{
    Overview,
    Performance,
    Logs,
    Plugins,
    Analytics,
    Security,
    CloudSync,
    Custom
}
```

---

## Theme API

### Namespace: `HELIOS.Theme`

#### ThemeManager

Theme and appearance management.

```csharp
public class ThemeManager
{
    // Theme Operations
    public Task SetThemeAsync(ThemeMode mode);
    public Task<ThemeMode> GetCurrentThemeAsync();
    public Task<ThemeMode> GetSystemThemeAsync();
    
    // Custom Themes
    public Task<CustomTheme> CreateThemeAsync(ThemeDefinition definition);
    public Task<CustomTheme> GetThemeAsync(string themeId);
    public Task<List<CustomTheme>> GetCustomThemesAsync();
    public Task DeleteThemeAsync(string themeId);
    
    // Theme Application
    public Task ApplyThemeAsync(string themeId);
    public Task ApplyColorSchemeAsync(ColorScheme scheme);
    
    // Preferences
    public Task SavePreferencesAsync(ThemePreferences preferences);
    public Task<ThemePreferences> GetPreferencesAsync();
}
```

#### ThemeMode Enumeration

```csharp
public enum ThemeMode
{
    Light,
    Dark,
    Auto,
    Custom
}
```

#### ColorScheme

Color palette definition.

```csharp
public class ColorScheme
{
    public string Primary { get; set; }
    public string Secondary { get; set; }
    public string Background { get; set; }
    public string Surface { get; set; }
    public string Error { get; set; }
    public string Success { get; set; }
    public string Warning { get; set; }
    public string Text { get; set; }
    public string TextSecondary { get; set; }
    public string Border { get; set; }
}
```

---

## Internal Architecture APIs

### Namespace: `HELIOS.Core`

#### IServiceContainer

Dependency injection container.

```csharp
public interface IServiceContainer
{
    void Register<TInterface, TImplementation>(ServiceLifetime lifetime = ServiceLifetime.Singleton) 
        where TImplementation : TInterface;
    void RegisterInstance<TInterface>(TInterface instance);
    void RegisterFactory<TInterface>(Func<IServiceContainer, TInterface> factory);
    
    TInterface Resolve<TInterface>();
    object Resolve(Type type);
    
    bool IsRegistered<TInterface>();
    IEnumerable<(Type Interface, Type Implementation)> GetRegistrations();
}
```

#### ILogger

Logging interface.

```csharp
public interface ILogger
{
    void Debug(string message);
    void Info(string message);
    void Warning(string message);
    void Error(string message, Exception ex = null);
    void Critical(string message, Exception ex = null);
    
    IDisposable BeginScope(string scopeName);
}
```

#### IEventBus

Event publishing and subscription.

```csharp
public interface IEventBus
{
    void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IEvent;
    void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IEvent;
    void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : IEvent;
    
    Task PublishAsync<TEvent>(TEvent @event) where TEvent : IEvent;
    
    IDisposable Subscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : IEvent;
}
```

#### IConfiguration

Configuration management.

```csharp
public interface IConfiguration
{
    T Get<T>(string key, T defaultValue = default);
    Task SetAsync(string key, object value);
    Task<bool> HasAsync(string key);
    Task RemoveAsync(string key);
    
    Task SaveAsync();
    Task LoadAsync();
    
    IObservable<ConfigurationChange> GetChanges();
}
```

---

## Error Handling

### Exception Types

```csharp
// Cloud Sync Exceptions
public class SyncException : Exception { }
public class SyncConflictException : SyncException { }
public class ProviderAuthenticationException : SyncException { }

// Plugin Exceptions
public class PluginException : Exception { }
public class PluginNotFoundException : PluginException { }
public class PluginTimeoutException : PluginException { }

// ML Service Exceptions
public class MLException : Exception { }
public class ModelNotFoundException : MLException { }
public class PredictionException : MLException { }
public class TrainingException : MLException { }

// Dashboard Exceptions
public class DashboardException : Exception { }

// Theme Exceptions
public class ThemeException : Exception { }
```

---

## Async Patterns

All APIs follow Task-based asynchronous patterns:

- Use `async/await` with `Task` and `Task<T>`
- Cancellation supported via `CancellationToken`
- Progress reporting via `IProgress<T>` or `IObservable<T>`
- Timeouts configurable per operation

---

## Versioning Policy

API versioning follows semantic versioning (semver):

- **MAJOR**: Breaking API changes
- **MINOR**: New features, backwards compatible
- **PATCH**: Bug fixes, internal improvements

Deprecation warnings provided for API changes.

---

## Performance Characteristics

| Operation | Typical Latency | Max Latency | Throughput |
|-----------|-----------------|-------------|------------|
| Cloud Sync (100MB) | 5-10s | 30s | 10-50 MB/s |
| Plugin Execution | 10-50ms | 5s | 100+ ops/sec |
| Model Prediction | 5-100ms | 1s | 1000+ ops/sec |
| Dashboard Update | <50ms | 200ms | 60fps |
| Theme Switch | <100ms | 500ms | - |

---

## Security

All APIs implement:

- Input validation and sanitization
- Rate limiting for external APIs
- Encryption for sensitive data
- Token-based authentication
- Audit logging for operations

See [Security Guide](../SECURITY.md) for detailed information.
