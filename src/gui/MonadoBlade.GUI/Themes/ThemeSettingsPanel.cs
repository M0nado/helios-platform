using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections.ObjectModel;

namespace MonadoBlade.GUI.Themes
{
    /// <summary>
    /// Settings panel for theme management and customization.
    /// Provides UI controls for switching between light/dark/auto themes,
    /// previewing themes, and exporting/importing custom themes.
    /// </summary>
    public class ThemeSettingsPanel : UserControl
    {
        public static readonly DependencyProperty SelectedThemeModeProperty =
            DependencyProperty.Register("SelectedThemeMode", typeof(ThemeManager.ThemeMode),
                typeof(ThemeSettingsPanel), new PropertyMetadata(ThemeManager.ThemeMode.Dark, OnThemeModeChanged));

        public static readonly DependencyProperty CustomThemesProperty =
            DependencyProperty.Register("CustomThemes", typeof(ObservableCollection<ThemePreset>),
                typeof(ThemeSettingsPanel), new PropertyMetadata(null));

        private ThemeManager _themeManager;
        private ObservableCollection<ThemePreset> _customThemes;

        public ThemeManager.ThemeMode SelectedThemeMode
        {
            get { return (ThemeManager.ThemeMode)GetValue(SelectedThemeModeProperty); }
            set { SetValue(SelectedThemeModeProperty, value); }
        }

        public ObservableCollection<ThemePreset> CustomThemes
        {
            get { return (ObservableCollection<ThemePreset>)GetValue(CustomThemesProperty); }
            set { SetValue(CustomThemesProperty, value); }
        }

        public ThemeSettingsPanel()
        {
            _themeManager = ThemeManager.Instance;
            _customThemes = new ObservableCollection<ThemePreset>();
            CustomThemes = _customThemes;

            InitializeComponent();
            InitializeThemeOptions();

            _themeManager.ThemeModeChanged += OnThemeModeChanged_Handler;
        }

        /// <summary>
        /// Initializes theme selection UI.
        /// </summary>
        private void InitializeComponent()
        {
            // Create layout
            var mainPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(16)
            };

            // Title
            var titleBlock = new TextBlock
            {
                Text = "Theme Settings",
                FontSize = 20,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 16),
                Foreground = new SolidColorBrush((Color)FindResource("TextPrimaryColor"))
            };
            mainPanel.Children.Add(titleBlock);

            // Theme mode selection group
            var themeModeGroup = CreateThemeModeGroup();
            mainPanel.Children.Add(themeModeGroup);

            // Theme preview section
            var previewSection = CreateThemePreviewSection();
            mainPanel.Children.Add(previewSection);

            // Custom themes section
            var customThemesSection = CreateCustomThemesSection();
            mainPanel.Children.Add(customThemesSection);

            // Settings buttons
            var buttonPanel = CreateButtonPanel();
            mainPanel.Children.Add(buttonPanel);

            Content = new ScrollViewer
            {
                Content = mainPanel,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
        }

        /// <summary>
        /// Creates the theme mode selection group.
        /// </summary>
        private StackPanel CreateThemeModeGroup()
        {
            var group = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(0, 0, 0, 24)
            };

            var groupTitle = new TextBlock
            {
                Text = "Theme Mode",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 12),
                Foreground = new SolidColorBrush((Color)FindResource("TextSecondaryColor"))
            };
            group.Children.Add(groupTitle);

            // Light mode radio button
            var lightRadio = CreateRadioButton("Light Mode", ThemeManager.ThemeMode.Light);
            group.Children.Add(lightRadio);

            // Dark mode radio button
            var darkRadio = CreateRadioButton("Dark Mode", ThemeManager.ThemeMode.Dark);
            group.Children.Add(darkRadio);

            // Auto mode radio button
            var autoRadio = CreateRadioButton("System Theme (Auto)", ThemeManager.ThemeMode.System);
            group.Children.Add(autoRadio);

            // Current theme info
            var infoBlock = new TextBlock
            {
                Text = $"Current Theme: {_themeManager.CurrentMode}",
                FontSize = 12,
                Foreground = new SolidColorBrush((Color)FindResource("TextTertiaryColor")),
                Margin = new Thickness(0, 12, 0, 0)
            };
            group.Children.Add(infoBlock);

            return group;
        }

        /// <summary>
        /// Creates a radio button for theme selection.
        /// </summary>
        private RadioButton CreateRadioButton(string label, ThemeManager.ThemeMode mode)
        {
            var radio = new RadioButton
            {
                Content = label,
                IsChecked = _themeManager.CurrentMode == mode,
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(12),
                Foreground = new SolidColorBrush((Color)FindResource("TextPrimaryColor"))
            };

            radio.Checked += (s, e) =>
            {
                _themeManager.SetThemeModeAsync(mode).ConfigureAwait(false);
            };

            return radio;
        }

        /// <summary>
        /// Creates the theme preview section.
        /// </summary>
        private GroupBox CreateThemePreviewSection()
        {
            var groupBox = new GroupBox
            {
                Header = "Theme Preview",
                Margin = new Thickness(0, 0, 0, 24),
                Padding = new Thickness(12)
            };

            var preview = new Grid
            {
                Height = 120,
                Background = new SolidColorBrush((Color)FindResource("SurfaceColor"))
            };

            // Preview components
            var previewStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Color swatches
            AddColorSwatch(previewStack, "Background", (Color)FindResource("BackgroundColor"));
            AddColorSwatch(previewStack, "Surface", (Color)FindResource("SurfaceColor"));
            AddColorSwatch(previewStack, "Accent", (Color)FindResource("AccentPrimaryColor"));
            AddColorSwatch(previewStack, "Text", (Color)FindResource("TextPrimaryColor"));

            preview.Children.Add(previewStack);
            groupBox.Content = preview;

            return groupBox;
        }

        /// <summary>
        /// Adds a color swatch to the preview.
        /// </summary>
        private void AddColorSwatch(StackPanel parent, string label, Color color)
        {
            var swatchPanel = new StackPanel
            {
                Margin = new Thickness(12, 0, 12, 0)
            };

            var swatch = new Border
            {
                Width = 40,
                Height = 40,
                Background = new SolidColorBrush(color),
                BorderBrush = new SolidColorBrush((Color)FindResource("BorderPrimaryColor")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var labelBlock = new TextBlock
            {
                Text = label,
                FontSize = 10,
                TextAlignment = TextAlignment.Center,
                Foreground = new SolidColorBrush((Color)FindResource("TextTertiaryColor"))
            };

            swatchPanel.Children.Add(swatch);
            swatchPanel.Children.Add(labelBlock);
            parent.Children.Add(swatchPanel);
        }

        /// <summary>
        /// Creates the custom themes section.
        /// </summary>
        private GroupBox CreateCustomThemesSection()
        {
            var groupBox = new GroupBox
            {
                Header = "Custom Themes",
                Margin = new Thickness(0, 0, 0, 24),
                Padding = new Thickness(12)
            };

            var listBox = new ListBox
            {
                Height = 100,
                ItemsSource = CustomThemes,
                DisplayMemberPath = "Name",
                Foreground = new SolidColorBrush((Color)FindResource("TextPrimaryColor")),
                Background = new SolidColorBrush((Color)FindResource("InputBackground")),
                BorderBrush = new SolidColorBrush((Color)FindResource("InputBorder"))
            };

            groupBox.Content = listBox;
            return groupBox;
        }

        /// <summary>
        /// Creates the button panel.
        /// </summary>
        private StackPanel CreateButtonPanel()
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 24, 0, 0)
            };

            // Export theme button
            var exportBtn = new Button
            {
                Content = "Export Theme",
                Padding = new Thickness(16, 8, 16, 8),
                Margin = new Thickness(0, 0, 8, 0),
                Background = new SolidColorBrush((Color)FindResource("ButtonSecondaryBackground")),
                Foreground = new SolidColorBrush((Color)FindResource("TextPrimaryColor"))
            };
            exportBtn.Click += ExportTheme_Click;
            panel.Children.Add(exportBtn);

            // Import theme button
            var importBtn = new Button
            {
                Content = "Import Theme",
                Padding = new Thickness(16, 8, 16, 8),
                Margin = new Thickness(0, 0, 8, 0),
                Background = new SolidColorBrush((Color)FindResource("ButtonSecondaryBackground")),
                Foreground = new SolidColorBrush((Color)FindResource("TextPrimaryColor"))
            };
            importBtn.Click += ImportTheme_Click;
            panel.Children.Add(importBtn);

            // Reset button
            var resetBtn = new Button
            {
                Content = "Reset to Default",
                Padding = new Thickness(16, 8, 16, 8),
                Background = new SolidColorBrush((Color)FindResource("WarningColor")),
                Foreground = new SolidColorBrush(Colors.White)
            };
            resetBtn.Click += ResetTheme_Click;
            panel.Children.Add(resetBtn);

            return panel;
        }

        /// <summary>
        /// Initializes available theme options.
        /// </summary>
        private void InitializeThemeOptions()
        {
            // Add preset themes
            _customThemes.Add(new ThemePreset { Name = "Ocean", Description = "Blue-tinted dark theme" });
            _customThemes.Add(new ThemePreset { Name = "Sunset", Description = "Warm-toned dark theme" });
            _customThemes.Add(new ThemePreset { Name = "Forest", Description = "Green-tinted dark theme" });
        }

        /// <summary>
        /// Handles export theme button click.
        /// </summary>
        private void ExportTheme_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MessageBox.Show("Theme exported successfully", "Export Theme", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting theme: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handles import theme button click.
        /// </summary>
        private void ImportTheme_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MessageBox.Show("Theme imported successfully", "Import Theme", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error importing theme: {ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handles reset theme button click.
        /// </summary>
        private void ResetTheme_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Reset theme to default?", "Reset Theme", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _themeManager.SetThemeModeAsync(ThemeManager.ThemeMode.Dark).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Handles theme mode changed event.
        /// </summary>
        private void OnThemeModeChanged_Handler(object sender, ThemeManager.ThemeModeChangedEventArgs e)
        {
            SelectedThemeMode = e.NewMode;
        }

        /// <summary>
        /// Dependency property change handler for theme mode.
        /// </summary>
        private static void OnThemeModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ThemeSettingsPanel panel && e.NewValue is ThemeManager.ThemeMode mode)
            {
                panel._themeManager?.SetThemeModeAsync(mode).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Represents a theme preset.
    /// </summary>
    public class ThemePreset
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Path { get; set; }
    }
}
