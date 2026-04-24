using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Text.Json;
using System.Threading.Tasks;

namespace MonadoBlade.GUI.Themes
{
    /// <summary>
    /// Manages application-wide theme switching, persistence, and system theme detection.
    /// Supports light and dark modes with smooth transitions and persistent user preferences.
    /// </summary>
    public class ThemeManager : IDisposable
    {
        public enum ThemeMode
        {
            Light,
            Dark,
            System
        }

        private static ThemeManager _instance;
        private ThemeMode _currentMode = ThemeMode.Dark;
        private DarkModeThemeDefinition _darkTheme;
        private Dictionary<string, ResourceDictionary> _themeDictionaries;
        private string _themePreferencePath;
        private bool _isTransitioning = false;
        private const int THEME_TRANSITION_MS = 250;

        public event EventHandler<ThemeModeChangedEventArgs> ThemeModeChanged;

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

        private ThemeManager()
        {
            _darkTheme = new DarkModeThemeDefinition();
            _themeDictionaries = new Dictionary<string, ResourceDictionary>();
            _themePreferencePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "HeliosPlatform",
                "theme-preference.json"
            );

            InitializeThemeDictionaries();
            LoadPersistedThemePreference();
            RegisterSystemThemeChanges();
        }

        /// <summary>
        /// Initializes all theme resource dictionaries.
        /// </summary>
        private void InitializeThemeDictionaries()
        {
            try
            {
                // Create dark theme resource dictionary
                var darkDict = new ResourceDictionary();
                ApplyDarkThemeResources(darkDict);
                _themeDictionaries["dark"] = darkDict;

                // Create light theme resource dictionary
                var lightDict = new ResourceDictionary();
                ApplyLightThemeResources(lightDict);
                _themeDictionaries["light"] = lightDict;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing theme dictionaries: {ex.Message}");
            }
        }

        /// <summary>
        /// Applies dark theme colors and styles to resource dictionary.
        /// </summary>
        private void ApplyDarkThemeResources(ResourceDictionary dict)
        {
            var palette = DarkModeThemeDefinition.DarkPalette;
            var colors = _darkTheme.ComponentColors;
            var spacing = _darkTheme.Spacing;
            var typography = _darkTheme.Typography;

            // Background colors
            dict["BackgroundColor"] = palette.DarkBackground;
            dict["SurfaceColor"] = palette.DarkSurface;
            dict["SurfaceVariantColor"] = palette.DarkSurfaceVariant;

            // Text colors
            dict["TextPrimaryColor"] = palette.TextPrimary;
            dict["TextSecondaryColor"] = palette.TextSecondary;
            dict["TextTertiaryColor"] = palette.TextTertiary;
            dict["TextDisabledColor"] = palette.TextDisabled;

            // Accent colors
            dict["AccentPrimaryColor"] = palette.AccentPrimary;
            dict["AccentSecondaryColor"] = palette.AccentSecondary;
            dict["AccentTertiaryColor"] = palette.AccentTertiary;

            // Semantic colors
            dict["SuccessColor"] = palette.Success;
            dict["WarningColor"] = palette.Warning;
            dict["ErrorColor"] = palette.Error;
            dict["InfoColor"] = palette.Info;

            // Button colors
            dict["ButtonPrimaryBackground"] = colors.ButtonPrimaryBackground;
            dict["ButtonPrimaryText"] = colors.ButtonPrimaryText;
            dict["ButtonSecondaryBackground"] = colors.ButtonSecondaryBackground;
            dict["ButtonSecondaryBorder"] = colors.ButtonSecondaryBorder;

            // Input colors
            dict["InputBackground"] = palette.InputBackground;
            dict["InputBorder"] = palette.InputBorder;
            dict["InputFocus"] = palette.InputFocus;
            dict["InputText"] = palette.InputText;

            // Border and divider colors
            dict["BorderPrimaryColor"] = palette.BorderPrimary;
            dict["DividerColor"] = palette.DividerLight;

            // Spacing values
            dict["Spacing2"] = spacing.Spacing2;
            dict["Spacing4"] = spacing.Spacing4;
            dict["Spacing8"] = spacing.Spacing8;
            dict["Spacing16"] = spacing.Spacing16;
            dict["Spacing24"] = spacing.Spacing24;
            dict["CornerRadius"] = spacing.CornerRadiusMedium;

            // Typography
            dict["FontFamilyPrimary"] = typography.PrimaryFontFamily;
            dict["FontSizeRegular"] = typography.FontSizeRegular;
            dict["FontSizeSmall"] = typography.FontSizeSmall;
            dict["HeadingSize"] = typography.HeadingSize;

            // Create brushes for easier binding
            dict["BackgroundBrush"] = new SolidColorBrush(palette.DarkBackground);
            dict["SurfaceBrush"] = new SolidColorBrush(palette.DarkSurface);
            dict["TextBrush"] = new SolidColorBrush(palette.TextPrimary);
            dict["AccentBrush"] = new SolidColorBrush(palette.AccentPrimary);
        }

        /// <summary>
        /// Applies light theme colors and styles to resource dictionary.
        /// </summary>
        private void ApplyLightThemeResources(ResourceDictionary dict)
        {
            var palette = DarkModeThemeDefinition.LightPalette;
            var spacing = _darkTheme.Spacing;
            var typography = _darkTheme.Typography;

            // Background colors
            dict["BackgroundColor"] = palette.LightBackground;
            dict["SurfaceColor"] = palette.LightSurface;
            dict["SurfaceVariantColor"] = palette.LightSurfaceVariant;

            // Text colors
            dict["TextPrimaryColor"] = palette.TextPrimary;
            dict["TextSecondaryColor"] = palette.TextSecondary;
            dict["TextTertiaryColor"] = palette.TextTertiary;
            dict["TextDisabledColor"] = palette.TextDisabled;

            // Accent colors
            dict["AccentPrimaryColor"] = palette.AccentPrimary;
            dict["AccentSecondaryColor"] = palette.AccentSecondary;

            // Semantic colors (same as dark mode)
            dict["SuccessColor"] = DarkModeThemeDefinition.DarkPalette.Success;
            dict["WarningColor"] = DarkModeThemeDefinition.DarkPalette.Warning;
            dict["ErrorColor"] = DarkModeThemeDefinition.DarkPalette.Error;

            // Spacing values
            dict["Spacing2"] = spacing.Spacing2;
            dict["Spacing4"] = spacing.Spacing4;
            dict["Spacing8"] = spacing.Spacing8;
            dict["Spacing16"] = spacing.Spacing16;
            dict["Spacing24"] = spacing.Spacing24;

            // Typography
            dict["FontFamilyPrimary"] = typography.PrimaryFontFamily;
            dict["FontSizeRegular"] = typography.FontSizeRegular;

            // Create brushes
            dict["BackgroundBrush"] = new SolidColorBrush(palette.LightBackground);
            dict["SurfaceBrush"] = new SolidColorBrush(palette.LightSurface);
            dict["TextBrush"] = new SolidColorBrush(palette.TextPrimary);
            dict["AccentBrush"] = new SolidColorBrush(palette.AccentPrimary);
        }

        /// <summary>
        /// Changes the application theme with smooth transition.
        /// </summary>
        public async Task SetThemeModeAsync(ThemeMode mode)
        {
            if (_isTransitioning || _currentMode == mode)
                return;

            _isTransitioning = true;

            try
            {
                await Task.Delay(50); // Allow current frame to complete

                string themeName = mode == ThemeMode.Dark ? "dark" : "light";

                if (_themeDictionaries.ContainsKey(themeName))
                {
                    var app = Application.Current;
                    var merged = app.Resources.MergedDictionaries;

                    // Remove old theme dictionaries
                    for (int i = merged.Count - 1; i >= 0; i--)
                    {
                        if (merged[i] == _themeDictionaries["dark"] ||
                            merged[i] == _themeDictionaries["light"])
                        {
                            merged.RemoveAt(i);
                        }
                    }

                    // Apply new theme
                    merged.Add(_themeDictionaries[themeName]);

                    _currentMode = mode;

                    // Persist preference
                    await PersistThemePreferenceAsync(mode);

                    // Raise event
                    ThemeModeChanged?.Invoke(this, new ThemeModeChangedEventArgs { NewMode = mode });
                }

                await Task.Delay(THEME_TRANSITION_MS - 50);
            }
            finally
            {
                _isTransitioning = false;
            }
        }

        /// <summary>
        /// Gets the currently active theme mode.
        /// </summary>
        public ThemeMode CurrentMode => _currentMode;

        /// <summary>
        /// Gets whether a theme transition is in progress.
        /// </summary>
        public bool IsTransitioning => _isTransitioning;

        /// <summary>
        /// Persists theme preference to disk.
        /// </summary>
        private async Task PersistThemePreferenceAsync(ThemeMode mode)
        {
            try
            {
                var directory = Path.GetDirectoryName(_themePreferencePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var preference = new { ThemeMode = mode.ToString() };
                var json = JsonSerializer.Serialize(preference);

                await File.WriteAllTextAsync(_themePreferencePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error persisting theme preference: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads persisted theme preference from disk.
        /// </summary>
        private void LoadPersistedThemePreference()
        {
            try
            {
                if (File.Exists(_themePreferencePath))
                {
                    var json = File.ReadAllText(_themePreferencePath);
                    using (JsonDocument doc = JsonDocument.Parse(json))
                    {
                        var root = doc.RootElement;
                        if (root.TryGetProperty("ThemeMode", out var modeElement))
                        {
                            string modeStr = modeElement.GetString();
                            if (Enum.TryParse<ThemeMode>(modeStr, out var mode))
                            {
                                _currentMode = mode;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading theme preference: {ex.Message}");
            }
        }

        /// <summary>
        /// Registers for Windows system theme changes.
        /// </summary>
        private void RegisterSystemThemeChanges()
        {
            try
            {
                var registryPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
                var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(registryPath);

                if (key != null)
                {
                    var value = key.GetValue("AppsUseLightTheme");
                    if (value is int)
                    {
                        int lightTheme = (int)value;
                        if (_currentMode == ThemeMode.System)
                        {
                            var mode = lightTheme == 1 ? ThemeMode.Light : ThemeMode.Dark;
                            SetThemeModeAsync(mode).ConfigureAwait(false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error registering system theme changes: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the dark theme definition.
        /// </summary>
        public DarkModeThemeDefinition GetDarkTheme() => _darkTheme;

        /// <summary>
        /// Event arguments for theme mode changed event.
        /// </summary>
        public class ThemeModeChangedEventArgs : EventArgs
        {
            public ThemeMode NewMode { get; set; }
        }

        public void Dispose()
        {
            _themeDictionaries?.Clear();
            ThemeModeChanged = null;
        }
    }
}
