using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace MonadoBlade.GUI.Components.Helpers
{
    /// <summary>
    /// Theme manager for consistent styling across all components (120 LOC)
    /// Handles light/dark theme switching, color schemes, and style application.
    /// </summary>
    public class ThemeManager
    {
        private static ThemeManager _instance;
        private string _currentTheme = "Light";
        private readonly Dictionary<string, ThemeColors> _themes = new();

        public event Action<string> ThemeChanged;

        private ThemeManager()
        {
            InitializeThemes();
        }

        public static ThemeManager Instance => _instance ??= new ThemeManager();

        /// <summary>
        /// Get current theme name
        /// </summary>
        public string CurrentTheme => _currentTheme;

        /// <summary>
        /// Set active theme
        /// </summary>
        public void SetTheme(string themeName)
        {
            if (_themes.ContainsKey(themeName) && _currentTheme != themeName)
            {
                _currentTheme = themeName;
                ApplyTheme(themeName);
                ThemeChanged?.Invoke(themeName);
            }
        }

        /// <summary>
        /// Get current theme colors
        /// </summary>
        public ThemeColors GetCurrentColors() => _themes[_currentTheme];

        /// <summary>
        /// Get specific theme colors
        /// </summary>
        public ThemeColors GetThemeColors(string themeName)
        {
            return _themes.ContainsKey(themeName) ? _themes[themeName] : GetCurrentColors();
        }

        /// <summary>
        /// Register custom theme
        /// </summary>
        public void RegisterTheme(string name, ThemeColors colors)
        {
            _themes[name] = colors;
        }

        /// <summary>
        /// Get available themes
        /// </summary>
        public IEnumerable<string> GetAvailableThemes() => _themes.Keys;

        /// <summary>
        /// Apply theme to application resources
        /// </summary>
        private void ApplyTheme(string themeName)
        {
            if (!_themes.ContainsKey(themeName))
                return;

            var theme = _themes[themeName];
            var resources = Application.Current.Resources;

            resources["BackgroundColor"] = theme.BackgroundColor;
            resources["ForegroundColor"] = theme.ForegroundColor;
            resources["AccentColor"] = theme.AccentColor;
            resources["BorderColor"] = theme.BorderColor;
            resources["HoverColor"] = theme.HoverColor;
            resources["DisabledColor"] = theme.DisabledColor;
            resources["ErrorColor"] = theme.ErrorColor;
            resources["SuccessColor"] = theme.SuccessColor;
            resources["WarningColor"] = theme.WarningColor;
        }

        /// <summary>
        /// Initialize default themes
        /// </summary>
        private void InitializeThemes()
        {
            // Light theme
            _themes["Light"] = new ThemeColors
            {
                Name = "Light",
                BackgroundColor = Color.FromRgb(255, 255, 255),
                ForegroundColor = Color.FromRgb(0, 0, 0),
                AccentColor = Color.FromRgb(0, 120, 215),
                BorderColor = Color.FromRgb(224, 224, 224),
                HoverColor = Color.FromRgb(240, 240, 240),
                DisabledColor = Color.FromRgb(204, 204, 204),
                ErrorColor = Color.FromRgb(255, 0, 0),
                SuccessColor = Color.FromRgb(0, 128, 0),
                WarningColor = Color.FromRgb(255, 165, 0)
            };

            // Dark theme
            _themes["Dark"] = new ThemeColors
            {
                Name = "Dark",
                BackgroundColor = Color.FromRgb(32, 32, 32),
                ForegroundColor = Color.FromRgb(255, 255, 255),
                AccentColor = Color.FromRgb(0, 176, 240),
                BorderColor = Color.FromRgb(64, 64, 64),
                HoverColor = Color.FromRgb(48, 48, 48),
                DisabledColor = Color.FromRgb(102, 102, 102),
                ErrorColor = Color.FromRgb(255, 100, 100),
                SuccessColor = Color.FromRgb(100, 200, 100),
                WarningColor = Color.FromRgb(255, 200, 100)
            };

            // High Contrast theme
            _themes["HighContrast"] = new ThemeColors
            {
                Name = "HighContrast",
                BackgroundColor = Color.FromRgb(0, 0, 0),
                ForegroundColor = Color.FromRgb(255, 255, 255),
                AccentColor = Color.FromRgb(255, 255, 0),
                BorderColor = Color.FromRgb(255, 255, 255),
                HoverColor = Color.FromRgb(64, 64, 64),
                DisabledColor = Color.FromRgb(128, 128, 128),
                ErrorColor = Color.FromRgb(255, 0, 0),
                SuccessColor = Color.FromRgb(0, 255, 0),
                WarningColor = Color.FromRgb(255, 255, 0)
            };
        }
    }

    /// <summary>
    /// Theme color definition
    /// </summary>
    public class ThemeColors
    {
        public string Name { get; set; }
        public Color BackgroundColor { get; set; }
        public Color ForegroundColor { get; set; }
        public Color AccentColor { get; set; }
        public Color BorderColor { get; set; }
        public Color HoverColor { get; set; }
        public Color DisabledColor { get; set; }
        public Color ErrorColor { get; set; }
        public Color SuccessColor { get; set; }
        public Color WarningColor { get; set; }
    }
}
