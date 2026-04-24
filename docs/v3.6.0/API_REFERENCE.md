# HELIOS Platform v3.6.0 - API Reference

**Version**: 3.6.0

## CloudSync API

```csharp
public class CloudSyncManager
{
    // Initialization
    public Task InitializeProviderAsync(CloudProvider provider, IProviderCredentials credentials);
    
    // Synchronization
    public Task<SyncResult> SyncAsync();
    public Task<SyncResult> SyncFolderAsync(string folderPath);
    public Task<SyncResult> PushAsync();
    public Task<SyncResult> PullAsync();
    
    // Auto-sync
    public Task EnableAutoSyncAsync(SyncOptions options);
    public Task DisableAutoSyncAsync();
    
    // Conflict Management
    public IConflictResolver GetConflictResolver();
    public Task<List<SyncConflict>> GetConflictsAsync();
    
    // Status
    public Task<SyncStatus> GetStatusAsync();
    public Task<SyncStatistics> GetStatisticsAsync();
}
```

## PluginSystem API

```csharp
public interface IPlugin
{
    Task InitializeAsync();
    Task<PluginResult> ExecuteAsync(PluginCommand command);
    Task ShutdownAsync();
    PluginInfo GetInfo();
}

public class PluginManager
{
    public Task<List<PluginInfo>> DiscoverAsync();
    public Task InstallAsync(string pluginId, string version = null);
    public Task<PluginResult> ExecuteAsync(string pluginId, PluginCommand command);
    public Task<List<IPlugin>> GetInstalledPluginsAsync();
    public Task UnloadPluginAsync(string pluginId);
}
```

## MLService API

```csharp
public class MLService
{
    public Task<Model> RegisterModelAsync(ModelMetadata metadata);
    public Task<Prediction> PredictAsync(string modelId, Dictionary<string, object> input);
    public Task<List<Prediction>> PredictBatchAsync(string modelId, IEnumerable input);
    public Task<TrainingJob> TrainModelAsync(TrainingConfig config);
    public Task<EvaluationMetrics> EvaluateModelAsync(string modelId, Dataset testData);
}
```

## Dashboard Extension API

```csharp
public interface IDashboardView
{
    string ViewId { get; }
    string DisplayName { get; }
    Task InitializeAsync();
    Task RefreshAsync();
    Task<object> GetDataAsync();
}

public class DeveloperDashboard
{
    public Task StartAsync(string hostAddress, int port, bool enableHttps = false);
    public Task AddViewAsync(IDashboardView view);
    public Task ConfigureAsync(DashboardConfiguration config);
}
```

## Theme API

```csharp
public class ThemeManager
{
    public Task SetThemeAsync(ThemeMode mode);  // Light/Dark/Auto
    public Task<ThemeMode> GetCurrentThemeAsync();
    public Task<CustomTheme> CreateThemeAsync(ThemeDefinition definition);
    public Task ApplyThemeAsync(string themeId);
}
```

---

All APIs use async/await patterns. See FEATURES_GUIDE.md for code examples.
