using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using HELIOS.Platform.Core.API.Interfaces;
using HELIOS.Platform.Core.Performance;

namespace HELIOS.Platform.Core.API.Services;

/// <summary>
/// High-performance theme manager with dynamic theming and caching.
/// </summary>
public class ThemeManager : IThemeManager
{
    private readonly ILogger<ThemeManager> _logger;
    private readonly IL1CacheService? _cache;
    private readonly Dictionary<string, ThemeDefinition> _themes = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private string _currentTheme = "default";
    private long _themeRequests = 0;
    private long _cacheHits = 0;
    private long _cacheMisses = 0;

    private const string ThemeCachePrefix = "theme:";

    public ThemeManager(ILogger<ThemeManager> logger, IL1CacheService? cache = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache;
        _logger.LogInformation("Theme Manager initialized");
        InitializeDefaultTheme();
    }

    private void InitializeDefaultTheme()
    {
        _themes["default"] = new ThemeDefinition
        {
            Name = "default",
            Colors = new Dictionary<string, string>
            {
                { "primary", "#007bff" },
                { "secondary", "#6c757d" },
                { "success", "#28a745" },
                { "danger", "#dc3545" },
                { "warning", "#ffc107" },
                { "info", "#17a2b8" },
                { "light", "#f8f9fa" },
                { "dark", "#343a40" }
            },
            Fonts = new Dictionary<string, string>
            {
                { "body", "Segoe UI, sans-serif" },
                { "heading", "Georgia, serif" },
                { "monospace", "Courier New, monospace" }
            },
            Settings = new Dictionary<string, object>
            {
                { "borderRadius", "4px" },
                { "shadowDepth", "0 2px 8px rgba(0,0,0,0.1)" }
            }
        };
    }

    /// <summary>
    /// Gets a theme by name with caching.
    /// </summary>
    public async Task<string> GetThemeAsync(string themeName)
    {
        if (string.IsNullOrWhiteSpace(themeName))
            throw new ArgumentException("Theme name cannot be empty", nameof(themeName));

        var startTime = DateTime.UtcNow;
        var cacheKey = ThemeCachePrefix + themeName;

        try
        {
            Interlocked.Increment(ref _themeRequests);

            // Try cache
            if (_cache != null && _cache.TryGet(cacheKey, out string cachedTheme))
            {
                Interlocked.Increment(ref _cacheHits);
                _logger.LogDebug("Theme cache hit: {ThemeName} (<5ms)", themeName);
                return cachedTheme;
            }

            Interlocked.Increment(ref _cacheMisses);

            await _semaphore.WaitAsync();
            try
            {
                if (!_themes.TryGetValue(themeName, out var theme))
                {
                    _logger.LogWarning("Theme not found: {ThemeName}, using default", themeName);
                    theme = _themes["default"];
                }

                var themeJson = SerializeTheme(theme);

                // Cache the result
                if (_cache != null)
                    _cache.Set(cacheKey, themeJson, TimeSpan.FromMinutes(60));

                var elapsed = DateTime.UtcNow - startTime;
                _logger.LogDebug("Theme retrieved: {ThemeName} ({Latency}ms)", themeName, elapsed.TotalMilliseconds);

                return themeJson;
            }
            finally
            {
                _semaphore.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving theme: {ThemeName}", themeName);
            throw;
        }
    }

    private string SerializeTheme(ThemeDefinition theme)
    {
        var json = new System.Text.StringBuilder();
        json.AppendLine("{");
        json.AppendLine($"  \"name\": \"{theme.Name}\",");
        json.AppendLine("  \"colors\": {");

        var colorsList = theme.Colors.ToList();
        for (int i = 0; i < colorsList.Count; i++)
        {
            json.Append($"    \"{colorsList[i].Key}\": \"{colorsList[i].Value}\"");
            if (i < colorsList.Count - 1) json.AppendLine(",");
            else json.AppendLine();
        }

        json.AppendLine("  }");
        json.AppendLine("}");
        return json.ToString();
    }

    /// <summary>
    /// Registers a new theme.
    /// </summary>
    public async Task<bool> RegisterThemeAsync(string themeName, ThemeDefinition theme)
    {
        if (string.IsNullOrWhiteSpace(themeName))
            throw new ArgumentException("Theme name cannot be empty", nameof(themeName));
        if (theme == null)
            throw new ArgumentNullException(nameof(theme));

        await _semaphore.WaitAsync();
        try
        {
            _themes[themeName] = theme;
            _cache?.Remove(ThemeCachePrefix + themeName); // Invalidate cache
            _logger.LogInformation("Theme registered: {ThemeName}", themeName);
            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Lists all registered themes.
    /// </summary>
    public async Task<List<string>> ListThemesAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            var themes = _themes.Keys.ToList();
            _logger.LogDebug("Listed {Count} themes", themes.Count);
            return themes;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Sets the default theme.
    /// </summary>
    public async Task<bool> SetDefaultThemeAsync(string themeName)
    {
        if (string.IsNullOrWhiteSpace(themeName))
            throw new ArgumentException("Theme name cannot be empty", nameof(themeName));

        await _semaphore.WaitAsync();
        try
        {
            if (!_themes.ContainsKey(themeName))
            {
                _logger.LogWarning("Theme not found: {ThemeName}", themeName);
                return false;
            }

            _currentTheme = themeName;
            _logger.LogInformation("Default theme set to: {ThemeName}", themeName);
            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Gets the current theme.
    /// </summary>
    public async Task<ThemeDefinition> GetCurrentThemeAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_themes.TryGetValue(_currentTheme, out var theme))
                return theme;

            _logger.LogWarning("Current theme not found: {ThemeName}, using default", _currentTheme);
            return _themes["default"];
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Deletes a theme.
    /// </summary>
    public async Task<bool> DeleteThemeAsync(string themeName)
    {
        if (string.IsNullOrWhiteSpace(themeName))
            throw new ArgumentException("Theme name cannot be empty", nameof(themeName));

        if (themeName == "default")
        {
            _logger.LogWarning("Cannot delete default theme");
            return false;
        }

        await _semaphore.WaitAsync();
        try
        {
            if (_themes.Remove(themeName))
            {
                _cache?.Remove(ThemeCachePrefix + themeName); // Invalidate cache

                if (_currentTheme == themeName)
                    _currentTheme = "default";

                _logger.LogInformation("Theme deleted: {ThemeName}", themeName);
                return true;
            }

            _logger.LogWarning("Theme not found: {ThemeName}", themeName);
            return false;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Gets theme manager metrics.
    /// </summary>
    public async Task<ThemeMetrics> GetMetricsAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            return await Task.FromResult(new ThemeMetrics
            {
                TotalThemes = _themes.Count,
                CurrentTheme = _currentTheme,
                CacheHits = Interlocked.Read(ref _cacheHits),
                CacheMisses = Interlocked.Read(ref _cacheMisses)
            });
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Clears all caches.
    /// </summary>
    public void ClearCache()
    {
        _cache?.Clear();
        _logger.LogInformation("Theme cache cleared");
    }

    /// <summary>
    /// Gets the theme JSON CSS.
    /// </summary>
    public async Task<string> GetThemeCssAsync(string themeName)
    {
        if (string.IsNullOrWhiteSpace(themeName))
            throw new ArgumentException("Theme name cannot be empty", nameof(themeName));

        await _semaphore.WaitAsync();
        try
        {
            if (!_themes.TryGetValue(themeName, out var theme))
                return "/* Theme not found */";

            var css = GenerateCss(theme);
            return css;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private string GenerateCss(ThemeDefinition theme)
    {
        var css = new System.Text.StringBuilder();
        css.AppendLine(":root {");

        foreach (var color in theme.Colors)
            css.AppendLine($"  --color-{color.Key}: {color.Value};");

        foreach (var font in theme.Fonts)
            css.AppendLine($"  --font-{font.Key}: {font.Value};");

        css.AppendLine("}");
        return css.ToString();
    }
}
