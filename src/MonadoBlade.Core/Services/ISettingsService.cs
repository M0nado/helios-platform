namespace MonadoBlade.Core.Services;

/// <summary>
/// Settings service managing application configuration and user preferences.
/// Responsible for reading, writing, and broadcasting setting changes.
/// </summary>
public interface ISettingsService : IService
{
    /// <summary>
    /// Gets a setting value.
    /// </summary>
    /// <typeparam name="T">The expected type of the setting.</typeparam>
    /// <param name="key">The setting key.</param>
    /// <returns>The setting value; default(T) if not found.</returns>
    /// <exception cref="OperationFailedException">Thrown when retrieval fails.</exception>
    Task<T?> GetSettingAsync<T>(string key) where T : class;

    /// <summary>
    /// Gets a setting value with a default.
    /// </summary>
    /// <typeparam name="T">The expected type of the setting.</typeparam>
    /// <param name="key">The setting key.</param>
    /// <param name="defaultValue">The default value if setting not found.</param>
    /// <returns>The setting value or the default.</returns>
    Task<T> GetSettingAsync<T>(string key, T defaultValue) where T : class;

    /// <summary>
    /// Sets a setting value.
    /// </summary>
    /// <typeparam name="T">The type of the setting.</typeparam>
    /// <param name="key">The setting key.</param>
    /// <param name="value">The setting value.</param>
    /// <returns>A task representing the save operation.</returns>
    /// <exception cref="ValidationFailedException">Thrown when value is invalid.</exception>
    /// <exception cref="OperationFailedException">Thrown when save fails.</exception>
    Task SetSettingAsync<T>(string key, T value) where T : class;

    /// <summary>
    /// Gets all settings as a dictionary.
    /// </summary>
    /// <returns>A dictionary of all setting keys and values.</returns>
    /// <exception cref="OperationFailedException">Thrown when retrieval fails.</exception>
    Task<Dictionary<string, object>> GetAllSettingsAsync();

    /// <summary>
    /// Gets settings for a specific category/section.
    /// </summary>
    /// <param name="category">The settings category (e.g., "Display", "Network").</param>
    /// <returns>Settings in the category.</returns>
    Task<Dictionary<string, object>> GetSettingsCategoryAsync(string category);

    /// <summary>
    /// Sets multiple settings at once.
    /// </summary>
    /// <param name="settings">Dictionary of key-value pairs to set.</param>
    /// <returns>A task representing the operation.</returns>
    Task SetMultipleSettingsAsync(Dictionary<string, object> settings);

    /// <summary>
    /// Resets a setting to its default value.
    /// </summary>
    /// <param name="key">The setting key.</param>
    /// <returns>A task representing the operation.</returns>
    Task ResetSettingAsync(string key);

    /// <summary>
    /// Resets all settings to their defaults.
    /// </summary>
    /// <returns>A task representing the operation.</returns>
    Task ResetAllSettingsAsync();

    /// <summary>
    /// Subscribes to changes of a specific setting.
    /// </summary>
    /// <typeparam name="T">The setting type.</typeparam>
    /// <param name="key">The setting key.</param>
    /// <param name="handler">Handler to invoke when setting changes.</param>
    /// <returns>A disposable subscription.</returns>
    IDisposable SubscribeToSettingChanges<T>(string key, Action<T> handler) where T : class;

    /// <summary>
    /// Exports settings to a file.
    /// </summary>
    /// <param name="format">Export format (json, xml, ini, etc).</param>
    /// <returns>The exported settings as string.</returns>
    Task<string> ExportSettingsAsync(string format = "json");

    /// <summary>
    /// Imports settings from a file.
    /// </summary>
    /// <param name="data">The settings data to import.</param>
    /// <param name="format">The format of the data.</param>
    /// <param name="merge">Whether to merge with existing settings or replace.</param>
    /// <returns>A task representing the import operation.</returns>
    Task ImportSettingsAsync(string data, string format = "json", bool merge = true);

    /// <summary>
    /// Validates a setting value against its constraints.
    /// </summary>
    /// <param name="key">The setting key.</param>
    /// <param name="value">The value to validate.</param>
    /// <returns>A collection of validation errors; empty if valid.</returns>
    Task<ICollection<string>> ValidateSettingAsync(string key, object? value);
}
