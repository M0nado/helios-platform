using System;
using System.Windows;
using Microsoft.Win32;

namespace MonadoBlade.GUI.Themes
{
    /// <summary>
    /// Manages theme switching and persistence
    /// Supports light, dark, and high contrast modes
    /// </summary>
    public class ThemeManager
    {
        private static ThemeManager _instance;
        private string _currentTheme = "Light";
        private const string ThemeRegistryKey = @"HKEY_CURRENT_USER\Software\MonadoBlade\Theme";
        private const string ThemeValueName = "CurrentTheme";

        // Event for theme changes
        public event EventHandler<ThemeChangedEventArgs> ThemeChanged;

        public static ThemeManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ThemeManager();
                }
                return _instance;
            }
        }

        public string CurrentTheme
        {
            get => _currentTheme;
            set
            {
                if (_currentTheme != value)
                {
                    ApplyTheme(value);
                }
            }
        }

        private ThemeManager()
        {
            LoadThemeFromRegistry();
            DetectSystemTheme();
        }

        /// <summary>
        /// Apply theme to application
        /// </summary>
        public void ApplyTheme(string themeName)
        {
            try
            {
                _currentTheme = themeName;

                // Build XAML resource URI
                string resourceUri = $"pack://application:,,,/Themes/Monado{themeName}.xaml";

                // Clear current theme
                var currentResources = Application.Current.Resources.MergedDictionaries;
                var themeDict = currentResources.FirstOrDefault(d => 
                    d.Source?.OriginalString.Contains("/Themes/") == true);

                if (themeDict != null)
                {
                    currentResources.Remove(themeDict);
                }

                // Apply new theme
                var newTheme = new ResourceDictionary { Source = new Uri(resourceUri) };
                currentResources.Add(newTheme);

                // Persist choice
                SaveThemeToRegistry(themeName);

                // Raise event
                ThemeChanged?.Invoke(this, new ThemeChangedEventArgs { ThemeName = themeName });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Theme error: {ex.Message}");
            }
        }

        /// <summary>
        /// Detect Windows system theme preference
        /// </summary>
        private void DetectSystemTheme()
        {
            try
            {
                // Check Windows registry for app mode (0 = light, 1 = dark)
                var regKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                var appsUseLightTheme = regKey?.GetValue("AppsUseLightTheme");

                if (appsUseLightTheme != null && appsUseLightTheme is int value)
                {
                    string systemTheme = value == 1 ? "Light" : "Dark";
                    if (systemTheme != _currentTheme)
                    {
                        ApplyTheme(systemTheme);
                    }
                }
            }
            catch
            {
                // Fallback to light theme on error
            }
        }

        /// <summary>
        /// Load theme preference from registry
        /// </summary>
        private void LoadThemeFromRegistry()
        {
            try
            {
                var value = Registry.GetValue(ThemeRegistryKey, ThemeValueName, "Light");
                if (value is string theme && !string.IsNullOrEmpty(theme))
                {
                    _currentTheme = theme;
                }
            }
            catch
            {
                // Use default if registry access fails
            }
        }

        /// <summary>
        /// Save theme preference to registry
        /// </summary>
        private void SaveThemeToRegistry(string themeName)
        {
            try
            {
                Registry.SetValue(@"HKEY_CURRENT_USER\Software\MonadoBlade", "Theme", themeName);
            }
            catch
            {
                // Silently fail if registry write fails
            }
        }

        /// <summary>
        /// Get available themes
        /// </summary>
        public string[] GetAvailableThemes()
        {
            return new[] { "Light", "Dark", "HighContrast" };
        }

        /// <summary>
        /// Toggle between light and dark modes
        /// </summary>
        public void ToggleTheme()
        {
            CurrentTheme = _currentTheme == "Light" ? "Dark" : "Light";
        }
    }

    /// <summary>
    /// Event args for theme changes
    /// </summary>
    public class ThemeChangedEventArgs : EventArgs
    {
        public string ThemeName { get; set; }
    }
}
