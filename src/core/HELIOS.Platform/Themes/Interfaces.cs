namespace HELIOS.Platform.Themes;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Theme definition with color and typography settings.
/// </summary>
public class Theme
{
    public string Id { get; set; }
    public string Name { get; set; }
    public bool IsDark { get; set; }
    public ThemeColors Colors { get; set; }
    public ThemeTypography Typography { get; set; }
    public ThemeSpacing Spacing { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Color palette for a theme.
/// </summary>
public class ThemeColors
{
    public string Primary { get; set; }
    public string Secondary { get; set; }
    public string Tertiary { get; set; }
    public string Success { get; set; }
    public string Warning { get; set; }
    public string Error { get; set; }
    public string Info { get; set; }
    public string Background { get; set; }
    public string Surface { get; set; }
    public string OnBackground { get; set; }
    public string OnSurface { get; set; }
    public string Border { get; set; }
    public Dictionary<string, string> Custom { get; set; } = new();
}

/// <summary>
/// Typography settings for a theme.
/// </summary>
public class ThemeTypography
{
    public FontFamily Display { get; set; }
    public FontFamily Headline { get; set; }
    public FontFamily Title { get; set; }
    public FontFamily Body { get; set; }
    public FontFamily Caption { get; set; }
    public int BaseSize { get; set; } = 16;
}

/// <summary>
/// Font family definition.
/// </summary>
public class FontFamily
{
    public string Name { get; set; }
    public int Weight { get; set; } = 400;
    public int Size { get; set; }
    public double LineHeight { get; set; } = 1.5;
}

/// <summary>
/// Spacing scale for a theme.
/// </summary>
public class ThemeSpacing
{
    public int XS { get; set; } = 4;
    public int SM { get; set; } = 8;
    public int MD { get; set; } = 16;
    public int LG { get; set; } = 24;
    public int XL { get; set; } = 32;
    public int XXL { get; set; } = 48;
}

/// <summary>
/// Theme mode (light or dark).
/// </summary>
public enum ThemeMode
{
    Light = 0,
    Dark = 1,
    Auto = 2
}

/// <summary>
/// Theme transition settings.
/// </summary>
public class ThemeTransition
{
    public int DurationMs { get; set; } = 300;
    public string Easing { get; set; } = "ease-in-out";
}

/// <summary>
/// Theme manager interface.
/// </summary>
public interface IThemeManager
{
    Task<Theme> GetCurrentThemeAsync();
    Task<Theme> GetThemeAsync(string themeId);
    Task<List<Theme>> ListThemesAsync();
    Task SetThemeAsync(string themeId);
    Task<Theme> CreateThemeAsync(Theme theme);
    Task UpdateThemeAsync(Theme theme);
    Task DeleteThemeAsync(string themeId);
    Task<ThemeMode> GetThemeModeAsync();
    Task SetThemeModeAsync(ThemeMode mode);
    Task<bool> IsDarkModeEnabledAsync();
    Task SetDarkModeAsync(bool enabled);
}

/// <summary>
/// Theme service interface for applying themes.
/// </summary>
public interface IThemeService
{
    Task ApplyThemeAsync(Theme theme);
    Task ApplySystemThemeAsync();
    Task<Theme> GetSystemPreferenceAsync();
    event EventHandler<ThemeChangedEventArgs> ThemeChanged;
}

/// <summary>
/// Theme changed event args.
/// </summary>
public class ThemeChangedEventArgs : EventArgs
{
    public Theme OldTheme { get; set; }
    public Theme NewTheme { get; set; }
    public DateTime ChangedAt { get; set; }
}

/// <summary>
/// Accessibility-compliant dark mode theme.
/// </summary>
public class DarkModeTheme : Theme
{
    public DarkModeTheme()
    {
        Id = "dark-mode";
        Name = "Dark Mode";
        IsDark = true;
        Colors = new ThemeColors
        {
            Primary = "#00D9FF",      // Cyan
            Secondary = "#00FF41",    // Neon Green
            Tertiary = "#FF0055",     // Pink
            Success = "#00FF41",
            Warning = "#FFB800",      // Amber
            Error = "#FF0055",
            Info = "#00D9FF",
            Background = "#0F0F0F",   // Near black
            Surface = "#1A1A1A",      // Dark gray
            OnBackground = "#E0E0E0", // Light text
            OnSurface = "#FFFFFF",    // White text
            Border = "#333333"        // Dark border
        };
        Typography = new ThemeTypography
        {
            Display = new FontFamily { Name = "Segoe UI", Size = 32, Weight = 700 },
            Headline = new FontFamily { Name = "Segoe UI", Size = 28, Weight = 600 },
            Title = new FontFamily { Name = "Segoe UI", Size = 20, Weight = 600 },
            Body = new FontFamily { Name = "Segoe UI", Size = 16, Weight = 400 },
            Caption = new FontFamily { Name = "Segoe UI", Size = 12, Weight = 400 }
        };
        Spacing = new ThemeSpacing
        {
            XS = 4, SM = 8, MD = 16, LG = 24, XL = 32, XXL = 48
        };
    }
}

/// <summary>
/// Accessibility-compliant light mode theme.
/// </summary>
public class LightModeTheme : Theme
{
    public LightModeTheme()
    {
        Id = "light-mode";
        Name = "Light Mode";
        IsDark = false;
        Colors = new ThemeColors
        {
            Primary = "#0078D4",      // Microsoft Blue
            Secondary = "#107C10",    // Microsoft Green
            Tertiary = "#D13438",     // Microsoft Red
            Success = "#107C10",
            Warning = "#FFB900",      // Amber
            Error = "#D13438",
            Info = "#0078D4",
            Background = "#FFFFFF",   // White
            Surface = "#F3F3F3",      // Light gray
            OnBackground = "#000000", // Black text
            OnSurface = "#333333",    // Dark gray text
            Border = "#D0D0D0"        // Light border
        };
        Typography = new ThemeTypography
        {
            Display = new FontFamily { Name = "Segoe UI", Size = 32, Weight = 700 },
            Headline = new FontFamily { Name = "Segoe UI", Size = 28, Weight = 600 },
            Title = new FontFamily { Name = "Segoe UI", Size = 20, Weight = 600 },
            Body = new FontFamily { Name = "Segoe UI", Size = 16, Weight = 400 },
            Caption = new FontFamily { Name = "Segoe UI", Size = 12, Weight = 400 }
        };
        Spacing = new ThemeSpacing
        {
            XS = 4, SM = 8, MD = 16, LG = 24, XL = 32, XXL = 48
        };
    }
}

/// <summary>
/// High contrast dark theme for accessibility.
/// </summary>
public class HighContrastDarkTheme : Theme
{
    public HighContrastDarkTheme()
    {
        Id = "high-contrast-dark";
        Name = "High Contrast Dark";
        IsDark = true;
        Colors = new ThemeColors
        {
            Primary = "#FFFF00",      // Yellow (max contrast)
            Secondary = "#00FF00",    // Lime (max contrast)
            Tertiary = "#FF00FF",     // Magenta (max contrast)
            Success = "#00FF00",
            Warning = "#FFFF00",
            Error = "#FF0000",
            Info = "#00FFFF",
            Background = "#000000",   // Pure black
            Surface = "#1A1A1A",
            OnBackground = "#FFFFFF", // Pure white
            OnSurface = "#FFFFFF",    // Pure white
            Border = "#FFFFFF"        // White border (max contrast)
        };
        Typography = new ThemeTypography
        {
            Display = new FontFamily { Name = "Arial", Size = 32, Weight = 700 },
            Headline = new FontFamily { Name = "Arial", Size = 28, Weight = 700 },
            Title = new FontFamily { Name = "Arial", Size = 20, Weight = 700 },
            Body = new FontFamily { Name = "Arial", Size = 16, Weight = 500 },
            Caption = new FontFamily { Name = "Arial", Size = 12, Weight = 500 }
        };
        Spacing = new ThemeSpacing
        {
            XS = 4, SM = 8, MD = 16, LG = 24, XL = 32, XXL = 48
        };
    }
}

/// <summary>
/// Theme exception.
/// </summary>
public class ThemeException : Exception
{
    public string ThemeId { get; set; }
    public ThemeException(string message) : base(message) { }
    public ThemeException(string message, Exception innerException) : base(message, innerException) { }
    public ThemeException(string themeId, string message) : base(message) => ThemeId = themeId;
}
