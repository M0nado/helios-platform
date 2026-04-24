using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MonadoBlade.GUI.Controls
{
    /// <summary>
    /// Advanced Settings System - Intelligent layering of Simple → Advanced controls.
    /// Enables users to start simple and progressively unlock more powerful options.
    /// </summary>
    public class AdvancedSettingsPanel : UserControl
    {
        private bool _isSimpleMode = true;
        private Dictionary<string, SettingItem> _settings;
        private StackPanel _settingsContainer;

        public AdvancedSettingsPanel(bool simpleMode = true)
        {
            _isSimpleMode = simpleMode;
            _settings = new Dictionary<string, SettingItem>();

            InitializeSettings();
            BuildUI();
        }

        private void InitializeSettings()
        {
            // SIMPLE MODE SETTINGS (Always visible)

            _settings.Add("dark_mode", new SettingItem
            {
                Key = "dark_mode",
                Label = "🌙 Dark Mode",
                SimpleDescription = "Darker colors for night use",
                AdvancedDescription = "Toggle dark theme across entire interface",
                Category = "Display",
                Type = SettingType.Toggle,
                DefaultValue = true,
                MinimumMode = DisplayMode.Simple
            });

            _settings.Add("sound_enabled", new SettingItem
            {
                Key = "sound_enabled",
                Label = "🔊 Sound Effects",
                SimpleDescription = "Enable sound feedback",
                AdvancedDescription = "Play audio feedback for interactions",
                Category = "Audio",
                Type = SettingType.Toggle,
                DefaultValue = true,
                MinimumMode = DisplayMode.Simple
            });

            _settings.Add("effect_quality", new SettingItem
            {
                Key = "effect_quality",
                Label = "✨ Effect Quality",
                SimpleDescription = "Quality of visual effects",
                AdvancedDescription = "Visual effect rendering quality (Low/Medium/High)",
                Category = "Display",
                Type = SettingType.Dropdown,
                DefaultValue = "High",
                Options = new[] { "Low", "Medium", "High" },
                MinimumMode = DisplayMode.Simple
            });

            _settings.Add("auto_update", new SettingItem
            {
                Key = "auto_update",
                Label = "🔄 Auto-Update",
                SimpleDescription = "Automatically update system",
                AdvancedDescription = "Automatically download and apply system updates",
                Category = "System",
                Type = SettingType.Toggle,
                DefaultValue = true,
                MinimumMode = DisplayMode.Simple
            });

            // ADVANCED MODE SETTINGS (Visible only in Advanced)

            _settings.Add("resolution", new SettingItem
            {
                Key = "resolution",
                Label = "Resolution",
                SimpleDescription = null,
                AdvancedDescription = "Display resolution and scaling",
                Category = "Display",
                Type = SettingType.Dropdown,
                DefaultValue = "1920x1080",
                Options = new[] { "1280x720", "1600x900", "1920x1080", "2560x1440", "3840x2160" },
                MinimumMode = DisplayMode.Advanced
            });

            _settings.Add("refresh_rate", new SettingItem
            {
                Key = "refresh_rate",
                Label = "Refresh Rate",
                SimpleDescription = null,
                AdvancedDescription = "Display refresh rate in Hz",
                Category = "Display",
                Type = SettingType.Dropdown,
                DefaultValue = "144Hz",
                Options = new[] { "60Hz", "75Hz", "120Hz", "144Hz", "240Hz" },
                MinimumMode = DisplayMode.Advanced
            });

            _settings.Add("gpu_acceleration", new SettingItem
            {
                Key = "gpu_acceleration",
                Label = "GPU Acceleration",
                SimpleDescription = null,
                AdvancedDescription = "Enable hardware acceleration for UI rendering",
                Category = "Performance",
                Type = SettingType.Toggle,
                DefaultValue = true,
                MinimumMode = DisplayMode.Advanced
            });

            _settings.Add("particle_limit", new SettingItem
            {
                Key = "particle_limit",
                Label = "Particle Limit",
                SimpleDescription = null,
                AdvancedDescription = "Maximum number of simultaneous particles (50-1000)",
                Category = "Performance",
                Type = SettingType.Slider,
                DefaultValue = 500,
                Min = 50,
                Max = 1000,
                Step = 50,
                MinimumMode = DisplayMode.Advanced
            });

            _settings.Add("animation_fps", new SettingItem
            {
                Key = "animation_fps",
                Label = "Animation FPS Target",
                SimpleDescription = null,
                AdvancedDescription = "Target framerate for animations (30-240)",
                Category = "Performance",
                Type = SettingType.Slider,
                DefaultValue = 60,
                Min = 30,
                Max = 240,
                Step = 10,
                MinimumMode = DisplayMode.Advanced
            });

            _settings.Add("master_volume", new SettingItem
            {
                Key = "master_volume",
                Label = "Master Volume",
                SimpleDescription = null,
                AdvancedDescription = "Overall audio volume level (0-100%)",
                Category = "Audio",
                Type = SettingType.Slider,
                DefaultValue = 80,
                Min = 0,
                Max = 100,
                Step = 5,
                MinimumMode = DisplayMode.Advanced
            });

            _settings.Add("kanji_frequencies", new SettingItem
            {
                Key = "kanji_frequencies",
                Label = "Kanji Frequencies",
                SimpleDescription = null,
                AdvancedDescription = "Audio frequency generation for kanji (Auto/Custom)",
                Category = "Audio",
                Type = SettingType.Dropdown,
                DefaultValue = "Auto",
                Options = new[] { "Auto", "Custom" },
                MinimumMode = DisplayMode.Advanced
            });

            _settings.Add("spatial_audio", new SettingItem
            {
                Key = "spatial_audio",
                Label = "Spatial Audio",
                SimpleDescription = null,
                AdvancedDescription = "Enable 3D positional audio for immersive experience",
                Category = "Audio",
                Type = SettingType.Toggle,
                DefaultValue = true,
                MinimumMode = DisplayMode.Advanced
            });

            _settings.Add("network_mode", new SettingItem
            {
                Key = "network_mode",
                Label = "Network Mode",
                SimpleDescription = null,
                AdvancedDescription = "Network connection type (Local/Cloud/Hybrid)",
                Category = "Network",
                Type = SettingType.Dropdown,
                DefaultValue = "Hybrid",
                Options = new[] { "Local", "Cloud", "Hybrid" },
                MinimumMode = DisplayMode.Advanced
            });

            _settings.Add("debug_mode", new SettingItem
            {
                Key = "debug_mode",
                Label = "Debug Mode",
                SimpleDescription = null,
                AdvancedDescription = "Enable developer debugging tools and logs",
                Category = "Developer",
                Type = SettingType.Toggle,
                DefaultValue = false,
                MinimumMode = DisplayMode.Advanced
            });
        }

        private void BuildUI()
        {
            var mainPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 16
            };

            // Title
            var title = new TextBlock
            {
                Text = _isSimpleMode ? "QUICK SETTINGS" : "ADVANCED SETTINGS",
                Foreground = new SolidColorBrush(Color.FromRgb(0, 217, 255)),
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 16)
            };

            mainPanel.Children.Add(title);

            // Settings container
            _settingsContainer = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 8
            };

            // Group settings by category
            var categories = new Dictionary<string, List<SettingItem>>();

            foreach (var setting in _settings.Values)
            {
                // Only show settings for current mode
                if ((int)setting.MinimumMode > (_isSimpleMode ? 0 : 1))
                    continue;

                if (!categories.ContainsKey(setting.Category))
                    categories[setting.Category] = new List<SettingItem>();

                categories[setting.Category].Add(setting);
            }

            // Add settings grouped by category
            foreach (var category in categories)
            {
                var categoryLabel = new TextBlock
                {
                    Text = category.Key.ToUpper(),
                    Foreground = new SolidColorBrush(Color.FromRgb(0, 217, 255)),
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 12, 0, 8)
                };

                _settingsContainer.Children.Add(categoryLabel);

                foreach (var setting in category.Value)
                {
                    _settingsContainer.Children.Add(CreateSettingControl(setting));
                }
            }

            mainPanel.Children.Add(_settingsContainer);

            Content = new ScrollViewer
            {
                Content = mainPanel,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
        }

        private Control CreateSettingControl(SettingItem setting)
        {
            var container = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(26, 40, 56)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(58, 90, 120)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(12, 10),
                Margin = new Thickness(0, 0, 0, 6)
            };

            var mainPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 4 };

            // Label
            var label = new TextBlock
            {
                Text = setting.Label,
                Foreground = new SolidColorBrush(Color.FromRgb(232, 232, 232)),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold
            };

            mainPanel.Children.Add(label);

            // Description
            var description = _isSimpleMode ? setting.SimpleDescription : setting.AdvancedDescription;
            if (!string.IsNullOrEmpty(description))
            {
                var desc = new TextBlock
                {
                    Text = description,
                    Foreground = new SolidColorBrush(Color.FromRgb(168, 176, 184)),
                    FontSize = 11,
                    TextWrapping = TextWrapping.Wrap
                };

                mainPanel.Children.Add(desc);
            }

            // Control (toggle, slider, dropdown)
            var controlPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 6, 0, 0) };

            switch (setting.Type)
            {
                case SettingType.Toggle:
                    var checkbox = new CheckBox
                    {
                        IsChecked = (bool)setting.DefaultValue,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    controlPanel.Children.Add(checkbox);
                    break;

                case SettingType.Slider:
                    var slider = new Slider
                    {
                        Width = 300,
                        Height = 24,
                        Minimum = setting.Min,
                        Maximum = setting.Max,
                        Value = (int)setting.DefaultValue,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    var sliderValue = new TextBlock
                    {
                        Text = $"{setting.DefaultValue}",
                        Foreground = new SolidColorBrush(Color.FromRgb(0, 217, 255)),
                        FontSize = 11,
                        Width = 40
                    };

                    slider.ValueChanged += (s, e) =>
                    {
                        sliderValue.Text = $"{(int)e.NewValue}";
                    };

                    controlPanel.Children.Add(slider);
                    controlPanel.Children.Add(sliderValue);
                    break;

                case SettingType.Dropdown:
                    var dropdown = new ComboBox
                    {
                        Width = 200,
                        Height = 28,
                        Foreground = new SolidColorBrush(Color.FromRgb(232, 232, 232)),
                        Background = new SolidColorBrush(Color.FromRgb(10, 20, 40))
                    };

                    foreach (var option in setting.Options)
                    {
                        dropdown.Items.Add(option);
                    }

                    dropdown.SelectedItem = setting.DefaultValue;

                    controlPanel.Children.Add(dropdown);
                    break;
            }

            mainPanel.Children.Add(controlPanel);
            container.Child = mainPanel;

            return container;
        }

        public void SwitchMode(bool simpleMode)
        {
            _isSimpleMode = simpleMode;
            BuildUI();
        }

        private enum DisplayMode { Simple = 0, Advanced = 1 }
        private enum SettingType { Toggle, Slider, Dropdown }

        private class SettingItem
        {
            public string Key { get; set; }
            public string Label { get; set; }
            public string SimpleDescription { get; set; }
            public string AdvancedDescription { get; set; }
            public string Category { get; set; }
            public SettingType Type { get; set; }
            public object DefaultValue { get; set; }
            public string[] Options { get; set; }
            public int Min { get; set; }
            public int Max { get; set; }
            public int Step { get; set; }
            public DisplayMode MinimumMode { get; set; }
        }
    }

    /// <summary>
    /// Smart Control Panel - Context-aware quick actions based on system state.
    /// Simplifies common operations into visual, touchable controls.
    /// </summary>
    public class SmartControlPanel : UserControl
    {
        public SmartControlPanel()
        {
            BuildUI();
        }

        private void BuildUI()
        {
            var mainPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 12,
                Padding = new Thickness(16)
            };

            var title = new TextBlock
            {
                Text = "QUICK ACTIONS",
                Foreground = new SolidColorBrush(Color.FromRgb(0, 217, 255)),
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 12)
            };

            mainPanel.Children.Add(title);

            // Organize actions by priority
            var systemActions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            systemActions.Children.Add(CreateQuickAction("🔄 Restart", "Restart system", Color.FromRgb(255, 215, 0)));
            systemActions.Children.Add(CreateQuickAction("⚡ Optimize", "Run optimization", Color.FromRgb(0, 255, 65)));
            systemActions.Children.Add(CreateQuickAction("🛡 Update", "Check for updates", Color.FromRgb(0, 217, 255)));

            var performanceActions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            performanceActions.Children.Add(CreateQuickAction("🧹 Clean Cache", "Clear system cache", Color.FromRgb(255, 102, 0)));
            performanceActions.Children.Add(CreateQuickAction("💾 Backup", "Create backup", Color.FromRgb(102, 217, 255)));

            mainPanel.Children.Add(systemActions);
            mainPanel.Children.Add(performanceActions);

            Content = mainPanel;
        }

        private Border CreateQuickAction(string label, string tooltip, Color color)
        {
            var action = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(26, 40, 56)),
                BorderBrush = new SolidColorBrush(color),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(16, 12),
                Cursor = System.Windows.Input.Cursors.Hand,
                Width = 120,
                Height = 100
            };

            var panel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Center };

            var labelText = new TextBlock
            {
                Text = label,
                Foreground = new SolidColorBrush(color),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center
            };

            panel.Children.Add(labelText);
            action.Child = panel;

            action.MouseEnter += (s, e) =>
            {
                action.Background = new SolidColorBrush(Color.FromRgb(37, 53, 72));
                action.BorderThickness = new Thickness(3);
            };

            action.MouseLeave += (s, e) =>
            {
                action.Background = new SolidColorBrush(Color.FromRgb(26, 40, 56));
                action.BorderThickness = new Thickness(2);
            };

            return action;
        }
    }
}
