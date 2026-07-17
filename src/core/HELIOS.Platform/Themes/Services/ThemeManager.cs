namespace HELIOS.Platform.Themes;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Theme manager for managing light/dark modes and custom themes.
/// </summary>
public class ThemeManager : IThemeManager
{
    private readonly Dictionary<string, Theme> _themes = new();
    private Theme _currentTheme;
    private ThemeMode _themeMode = ThemeMode.Auto;
    private bool _darkModeEnabled = true;

    public ThemeManager()
    {
        // Initialize default themes
        _themes["light-mode"] = new LightModeTheme();
        _themes["dark-mode"] = new DarkModeTheme();
        _themes["high-contrast-dark"] = new HighContrastDarkTheme();
        _currentTheme = _themes["dark-mode"];
    }

    public async Task<Theme> GetCurrentThemeAsync()
    {
        return await Task.FromResult(_currentTheme);
    }

    public async Task<Theme> GetThemeAsync(string themeId)
    {
        if (!_themes.TryGetValue(themeId, out var theme))
            throw new ThemeException(themeId, "Theme not found");

        return await Task.FromResult(theme);
    }

    public async Task<List<Theme>> ListThemesAsync()
    {
        return await Task.FromResult(_themes.Values.ToList());
    }

    public async Task SetThemeAsync(string themeId)
    {
        if (!_themes.TryGetValue(themeId, out var theme))
            throw new ThemeException(themeId, "Theme not found");

        _currentTheme = theme;
        await Task.CompletedTask;
    }

    public async Task<Theme> CreateThemeAsync(Theme theme)
    {
        if (theme == null) throw new ArgumentNullException(nameof(theme));
        if (string.IsNullOrEmpty(theme.Id)) throw new ArgumentException("Theme ID is required");

        if (_themes.ContainsKey(theme.Id))
            throw new ThemeException(theme.Id, "Theme already exists");

        _themes[theme.Id] = theme;
        return await Task.FromResult(theme);
    }

    public async Task UpdateThemeAsync(Theme theme)
    {
        if (theme == null) throw new ArgumentNullException(nameof(theme));
        if (!_themes.ContainsKey(theme.Id))
            throw new ThemeException(theme.Id, "Theme not found");

        _themes[theme.Id] = theme;
        if (_currentTheme.Id == theme.Id)
            _currentTheme = theme;

        await Task.CompletedTask;
    }

    public async Task DeleteThemeAsync(string themeId)
    {
        if (!_themes.TryGetValue(themeId, out _))
            throw new ThemeException(themeId, "Theme not found");

        _themes.Remove(themeId);

        if (_currentTheme.Id == themeId)
            _currentTheme = _themes["dark-mode"];

        await Task.CompletedTask;
    }

    public async Task<ThemeMode> GetThemeModeAsync()
    {
        return await Task.FromResult(_themeMode);
    }

    public async Task SetThemeModeAsync(ThemeMode mode)
    {
        _themeMode = mode;

        if (mode == ThemeMode.Light)
            await SetThemeAsync("light-mode");
        else if (mode == ThemeMode.Dark)
            await SetThemeAsync("dark-mode");

        await Task.CompletedTask;
    }

    public async Task<bool> IsDarkModeEnabledAsync()
    {
        return await Task.FromResult(_darkModeEnabled);
    }

    public async Task SetDarkModeAsync(bool enabled)
    {
        _darkModeEnabled = enabled;
        if (enabled)
            await SetThemeAsync("dark-mode");
        else
            await SetThemeAsync("light-mode");

        await Task.CompletedTask;
    }
}

/// <summary>
/// Theme service for applying themes to the application.
/// </summary>
public class ThemeService : IThemeService
{
    private Theme _appliedTheme;
    public event EventHandler<ThemeChangedEventArgs> ThemeChanged;

    public async Task ApplyThemeAsync(Theme theme)
    {
        if (theme == null) throw new ArgumentNullException(nameof(theme));

        var oldTheme = _appliedTheme;
        _appliedTheme = theme;

        OnThemeChanged(oldTheme, theme);

        await Task.CompletedTask;
    }

    public async Task ApplySystemThemeAsync()
    {
        // In real implementation, would detect OS dark mode setting
        var isDarkMode = IsSystemDarkModeEnabled();
        Theme theme = isDarkMode ? new DarkModeTheme() : new LightModeTheme();

        await ApplyThemeAsync(theme);
    }

    public async Task<Theme> GetSystemPreferenceAsync()
    {
        var isDarkMode = IsSystemDarkModeEnabled();
        var theme = isDarkMode ? (Theme)new DarkModeTheme() : (Theme)new LightModeTheme();

        return await Task.FromResult(theme);
    }

    private bool IsSystemDarkModeEnabled()
    {
        // Simulate system preference (in real app, check Windows theme setting)
        try
        {
            var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            
            if (key != null)
            {
                var value = key.GetValue("AppsUseLightTheme");
                return value is int intValue && intValue == 0;
            }
        }
        catch { }

        return true; // Default to dark mode
    }

    private void OnThemeChanged(Theme oldTheme, Theme newTheme)
    {
        ThemeChanged?.Invoke(this, new ThemeChangedEventArgs
        {
            OldTheme = oldTheme,
            NewTheme = newTheme,
            ChangedAt = DateTime.UtcNow
        });
    }
}
