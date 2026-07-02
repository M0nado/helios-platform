namespace MonadoBlade.Core.Services;

/// <summary>
/// Implementation of ISettingsService providing configuration management.
/// Segregation Pattern: Focused on user preferences and system configuration.
/// </summary>
public class SettingsService : ServiceBase, ISettingsService
{
    private readonly Dictionary<string, object> _settings = new();
    private readonly Dictionary<string, Action<object>> _subscribers = new();

    public SettingsService(ILogger logger) : base(logger)
    {
        InitializeDefaultSettings();
    }

    private void InitializeDefaultSettings()
    {
        _settings["Theme"] = "Light";
        _settings["Language"] = "en-US";
        _settings["NotificationsEnabled"] = true;
    }

    public async Task<T?> GetSettingAsync<T>(string key) where T : class
    {
        LogInfo("Getting setting {SettingKey}", key);
        
        if (_settings.TryGetValue(key, out var value))
            return (T?)value;

        return await Task.FromResult<T?>(null);
    }

    public async Task<T> GetSettingAsync<T>(string key, T defaultValue) where T : class
    {
        LogInfo("Getting setting {SettingKey} with default", key);
        
        if (_settings.TryGetValue(key, out var value))
            return (T?)value ?? defaultValue;

        return await Task.FromResult(defaultValue);
    }

    public async Task SetSettingAsync<T>(string key, T value) where T : class
    {
        LogInfo("Setting {SettingKey} to {Value}", key, value);
        
        _settings[key] = value ?? throw new ArgumentNullException(nameof(value));
        
        // Notify subscribers
        if (_subscribers.TryGetValue(key, out var handler))
            handler(value);

        await Task.CompletedTask;
    }

    public async Task<Dictionary<string, object>> GetAllSettingsAsync()
    {
        LogInfo("Getting all settings");
        return await Task.FromResult(new Dictionary<string, object>(_settings));
    }

    public async Task<Dictionary<string, object>> GetSettingsCategoryAsync(string category)
    {
        LogInfo("Getting settings for category {Category}", category);
        
        var categorySettings = _settings
            .Where(kvp => kvp.Key.StartsWith($"{category}.", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        return await Task.FromResult(categorySettings);
    }

    public async Task SetMultipleSettingsAsync(Dictionary<string, object> settings)
    {
        LogInfo("Setting multiple settings: {Count} items", settings.Count);
        
        foreach (var kvp in settings)
            _settings[kvp.Key] = kvp.Value;

        await Task.CompletedTask;
    }

    public async Task ResetSettingAsync(string key)
    {
        LogInfo("Resetting setting {SettingKey}", key);
        _settings.Remove(key);
        await Task.CompletedTask;
    }

    public async Task ResetAllSettingsAsync()
    {
        LogInfo("Resetting all settings");
        _settings.Clear();
        InitializeDefaultSettings();
        await Task.CompletedTask;
    }

    public IDisposable SubscribeToSettingChanges<T>(string key, Action<T> handler) where T : class
    {
        LogInfo("Subscribing to changes for setting {SettingKey}", key);
        
        Action<object> wrappedHandler = obj => handler((T?)obj as T ?? throw new InvalidCastException());
        _subscribers[key] = wrappedHandler;

        return new SettingSubscription(() =>
        {
            _subscribers.Remove(key);
        });
    }

    public async Task<string> ExportSettingsAsync(string format = "json")
    {
        LogInfo("Exporting settings in format {Format}", format);
        
        if (format.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            var json = System.Text.Json.JsonSerializer.Serialize(_settings);
            return await Task.FromResult(json);
        }

        return await Task.FromResult("");
    }

    public async Task ImportSettingsAsync(string data, string format = "json", bool merge = true)
    {
        LogInfo("Importing settings in format {Format}, merge={Merge}", format, merge);
        
        if (format.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            var imported = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(data) ?? new();
            
            if (!merge)
                _settings.Clear();

            foreach (var kvp in imported)
                _settings[kvp.Key] = kvp.Value;
        }

        await Task.CompletedTask;
    }

    public async Task<ICollection<string>> ValidateSettingAsync(string key, object? value)
    {
        LogInfo("Validating setting {SettingKey}", key);
        
        var errors = new List<string>();
        
        // Placeholder validation
        if (value == null)
            errors.Add($"Setting '{key}' cannot be null");

        return await Task.FromResult(errors);
    }

    private class SettingSubscription : IDisposable
    {
        private readonly Action _onDispose;

        public SettingSubscription(Action onDispose)
        {
            _onDispose = onDispose;
        }

        public void Dispose()
        {
            _onDispose();
        }
    }
}
