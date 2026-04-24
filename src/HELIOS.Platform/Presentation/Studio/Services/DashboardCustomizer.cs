using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace HELIOS.Platform.Presentation.Studio.Services
{
    /// <summary>
    /// Dashboard customization service for layouts, widgets, and themes
    /// </summary>
    public class DashboardCustomizer
    {
        private readonly Dictionary<string, DashboardLayout> _layouts;
        private readonly List<DashboardTheme> _themes;
        private DashboardLayout _currentLayout;
        private DashboardTheme _currentTheme;

        public event EventHandler<DashboardLayout> LayoutChanged;
        public event EventHandler<DashboardTheme> ThemeChanged;

        public DashboardCustomizer()
        {
            _layouts = new Dictionary<string, DashboardLayout>();
            _themes = new List<DashboardTheme>();
            InitializeDefaultThemes();
        }

        /// <summary>
        /// Create a new dashboard layout
        /// </summary>
        public DashboardLayout CreateLayout(string name, string description = null)
        {
            var layout = new DashboardLayout
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = name,
                Description = description,
                CreatedAt = DateTime.UtcNow,
                Widgets = new List<DashboardWidget>(),
                IsDefault = false
            };

            _layouts[layout.Id] = layout;
            return layout;
        }

        /// <summary>
        /// Add widget to layout
        /// </summary>
        public void AddWidgetToLayout(string layoutId, DashboardWidget widget)
        {
            if (_layouts.TryGetValue(layoutId, out var layout))
            {
                layout.Widgets.Add(widget);
            }
        }

        /// <summary>
        /// Remove widget from layout
        /// </summary>
        public void RemoveWidgetFromLayout(string layoutId, string widgetId)
        {
            if (_layouts.TryGetValue(layoutId, out var layout))
            {
                layout.Widgets.RemoveAll(w => w.Id == widgetId);
            }
        }

        /// <summary>
        /// Update widget position and size
        /// </summary>
        public void UpdateWidgetPosition(string layoutId, string widgetId, int row, int column, int width, int height)
        {
            if (_layouts.TryGetValue(layoutId, out var layout))
            {
                var widget = layout.Widgets.FirstOrDefault(w => w.Id == widgetId);
                if (widget != null)
                {
                    widget.Row = row;
                    widget.Column = column;
                    widget.Width = width;
                    widget.Height = height;
                }
            }
        }

        /// <summary>
        /// Set layout as active
        /// </summary>
        public void SetActiveLayout(string layoutId)
        {
            if (_layouts.TryGetValue(layoutId, out var layout))
            {
                _currentLayout = layout;
                LayoutChanged?.Invoke(this, layout);
            }
        }

        /// <summary>
        /// Get current active layout
        /// </summary>
        public DashboardLayout GetCurrentLayout()
        {
            return _currentLayout;
        }

        /// <summary>
        /// Get all layouts
        /// </summary>
        public IEnumerable<DashboardLayout> GetAllLayouts()
        {
            return _layouts.Values;
        }

        /// <summary>
        /// Delete a layout
        /// </summary>
        public bool DeleteLayout(string layoutId)
        {
            if (_currentLayout?.Id == layoutId)
                _currentLayout = null;

            return _layouts.Remove(layoutId);
        }

        /// <summary>
        /// Export layout to JSON
        /// </summary>
        public string ExportLayoutJson(string layoutId)
        {
            if (!_layouts.TryGetValue(layoutId, out var layout))
                return null;

            var options = new JsonSerializerOptions { WriteIndented = true };
            return JsonSerializer.Serialize(layout, options);
        }

        /// <summary>
        /// Import layout from JSON
        /// </summary>
        public DashboardLayout ImportLayoutJson(string json)
        {
            try
            {
                var layout = JsonSerializer.Deserialize<DashboardLayout>(json);
                if (layout != null && !string.IsNullOrEmpty(layout.Id))
                {
                    _layouts[layout.Id] = layout;
                    return layout;
                }
            }
            catch (JsonException)
            {
                // Invalid JSON
            }

            return null;
        }

        /// <summary>
        /// Set active theme
        /// </summary>
        public void SetTheme(string themeName)
        {
            var theme = _themes.FirstOrDefault(t => t.Name == themeName);
            if (theme != null)
            {
                _currentTheme = theme;
                ThemeChanged?.Invoke(this, theme);
            }
        }

        /// <summary>
        /// Get current theme
        /// </summary>
        public DashboardTheme GetCurrentTheme()
        {
            return _currentTheme;
        }

        /// <summary>
        /// Get all available themes
        /// </summary>
        public IEnumerable<DashboardTheme> GetAvailableThemes()
        {
            return _themes;
        }

        /// <summary>
        /// Create custom theme
        /// </summary>
        public DashboardTheme CreateCustomTheme(string name, ThemeColors colors)
        {
            var theme = new DashboardTheme
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = name,
                IsCustom = true,
                Colors = colors
            };

            _themes.Add(theme);
            return theme;
        }

        /// <summary>
        /// Delete custom theme
        /// </summary>
        public bool DeleteCustomTheme(string themeName)
        {
            var theme = _themes.FirstOrDefault(t => t.Name == themeName && t.IsCustom);
            if (theme != null)
            {
                if (_currentTheme?.Id == theme.Id)
                    _currentTheme = _themes.FirstOrDefault(t => !t.IsCustom);

                return _themes.Remove(theme);
            }

            return false;
        }

        /// <summary>
        /// Save layout configuration
        /// </summary>
        public LayoutConfiguration SaveLayoutConfiguration(string layoutId)
        {
            if (!_layouts.TryGetValue(layoutId, out var layout))
                return null;

            var config = new LayoutConfiguration
            {
                LayoutId = layoutId,
                LayoutName = layout.Name,
                Widgets = layout.Widgets.Select(w => new WidgetConfig
                {
                    Id = w.Id,
                    Type = w.Type,
                    Row = w.Row,
                    Column = w.Column,
                    Width = w.Width,
                    Height = w.Height,
                    IsVisible = w.IsVisible,
                    Settings = w.Settings
                }).ToList(),
                SavedAt = DateTime.UtcNow
            };

            return config;
        }

        /// <summary>
        /// Load layout configuration
        /// </summary>
        public void LoadLayoutConfiguration(LayoutConfiguration config)
        {
            if (config == null)
                return;

            var layout = CreateLayout(config.LayoutName);

            foreach (var widgetConfig in config.Widgets)
            {
                var widget = new DashboardWidget
                {
                    Id = widgetConfig.Id,
                    Type = widgetConfig.Type,
                    Row = widgetConfig.Row,
                    Column = widgetConfig.Column,
                    Width = widgetConfig.Width,
                    Height = widgetConfig.Height,
                    IsVisible = widgetConfig.IsVisible,
                    Settings = widgetConfig.Settings
                };

                AddWidgetToLayout(layout.Id, widget);
            }

            SetActiveLayout(layout.Id);
        }

        private void InitializeDefaultThemes()
        {
            // Light theme
            var lightTheme = new DashboardTheme
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = "Light",
                IsCustom = false,
                Colors = new ThemeColors
                {
                    Background = "#FFFFFF",
                    Foreground = "#333333",
                    Primary = "#007BFF",
                    Secondary = "#6C757D",
                    Success = "#28A745",
                    Warning = "#FFC107",
                    Danger = "#DC3545",
                    Info = "#17A2B8"
                }
            };

            // Dark theme
            var darkTheme = new DashboardTheme
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = "Dark",
                IsCustom = false,
                Colors = new ThemeColors
                {
                    Background = "#1E1E1E",
                    Foreground = "#E0E0E0",
                    Primary = "#0D47A1",
                    Secondary = "#455A64",
                    Success = "#2E7D32",
                    Warning = "#F57F17",
                    Danger = "#C62828",
                    Info = "#00838F"
                }
            };

            _themes.Add(lightTheme);
            _themes.Add(darkTheme);
            _currentTheme = lightTheme;
        }
    }

    /// <summary>
    /// Dashboard layout with widgets
    /// </summary>
    public class DashboardLayout
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsDefault { get; set; }
        public List<DashboardWidget> Widgets { get; set; }
    }

    /// <summary>
    /// Dashboard widget
    /// </summary>
    public class DashboardWidget
    {
        public string Id { get; set; }
        public string Type { get; set; } // CPU, Memory, Disk, Network, GPU, etc.
        public int Row { get; set; }
        public int Column { get; set; }
        public int Width { get; set; } = 1;
        public int Height { get; set; } = 1;
        public bool IsVisible { get; set; } = true;
        public Dictionary<string, object> Settings { get; set; } = new();
    }

    /// <summary>
    /// Dashboard theme
    /// </summary>
    public class DashboardTheme
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool IsCustom { get; set; }
        public ThemeColors Colors { get; set; }
    }

    /// <summary>
    /// Theme color scheme
    /// </summary>
    public class ThemeColors
    {
        public string Background { get; set; }
        public string Foreground { get; set; }
        public string Primary { get; set; }
        public string Secondary { get; set; }
        public string Success { get; set; }
        public string Warning { get; set; }
        public string Danger { get; set; }
        public string Info { get; set; }
    }

    /// <summary>
    /// Layout configuration for saving/loading
    /// </summary>
    public class LayoutConfiguration
    {
        public string LayoutId { get; set; }
        public string LayoutName { get; set; }
        public List<WidgetConfig> Widgets { get; set; }
        public DateTime SavedAt { get; set; }
    }

    /// <summary>
    /// Widget configuration
    /// </summary>
    public class WidgetConfig
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public int Row { get; set; }
        public int Column { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool IsVisible { get; set; }
        public Dictionary<string, object> Settings { get; set; }
    }
}
