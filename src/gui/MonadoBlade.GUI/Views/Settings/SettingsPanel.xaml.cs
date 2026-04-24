using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MonadoBlade.GUI.Views.Settings
{
    public partial class SettingsPanel : UserControl
    {
        private SettingsPanelViewModel _viewModel;

        public event EventHandler<SettingsChangedEventArgs> SettingsChanged;

        public SettingsPanel()
        {
            InitializeComponent();
            _viewModel = new SettingsPanelViewModel();
            DataContext = _viewModel;
            LoadSettings();
        }

        private void LoadSettings()
        {
            try
            {
                // Load saved settings
                ThemeComboBox.SelectedIndex = (int)_viewModel.CurrentTheme;
                TransparencySlider.Value = _viewModel.WindowTransparency * 100;
                GpuAccelCheckBox.IsChecked = _viewModel.IsGpuAccelerationEnabled;
                StartupCheckBox.IsChecked = _viewModel.StartAtSystemStartup;
                MinimizedCheckBox.IsChecked = _viewModel.StartMinimized;
                GlobalHotkeysCheckBox.IsChecked = _viewModel.GlobalHotkeysEnabled;
                HighContrastCheckBox.IsChecked = _viewModel.HighContrastMode;
                ReduceAnimationCheckBox.IsChecked = _viewModel.ReduceAnimations;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading settings: {ex.Message}", "Settings Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SettingsCategoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsCategoryListBox.SelectedItem is ListBoxItem item)
            {
                string tag = item.Tag?.ToString() ?? "Appearance";
                ShowSettingsPanel(tag);
            }
        }

        private void ShowSettingsPanel(string category)
        {
            // Hide all panels
            AppearancePanel.Visibility = Visibility.Collapsed;
            PerformancePanel.Visibility = Visibility.Collapsed;
            SystemPanel.Visibility = Visibility.Collapsed;
            AccessibilityPanel.Visibility = Visibility.Collapsed;
            AboutPanel.Visibility = Visibility.Collapsed;

            // Show selected panel
            switch (category)
            {
                case "Appearance":
                    AppearancePanel.Visibility = Visibility.Visible;
                    break;
                case "Performance":
                    PerformancePanel.Visibility = Visibility.Visible;
                    break;
                case "System":
                    SystemPanel.Visibility = Visibility.Visible;
                    break;
                case "Accessibility":
                    AccessibilityPanel.Visibility = Visibility.Visible;
                    break;
                case "About":
                    AboutPanel.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ThemeComboBox.SelectedIndex >= 0)
            {
                var theme = (AppTheme)ThemeComboBox.SelectedIndex;
                _viewModel.CurrentTheme = theme;
                _viewModel.SaveSettings();
                
                SettingsChanged?.Invoke(this, new SettingsChangedEventArgs { 
                    SettingName = "Theme", 
                    Value = theme.ToString() 
                });
            }
        }

        private void AccentColor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                string colorName = button.Content?.ToString() ?? "Cyan";
                _viewModel.AccentColor = colorName;
                _viewModel.SaveSettings();
                
                SettingsChanged?.Invoke(this, new SettingsChangedEventArgs { 
                    SettingName = "AccentColor", 
                    Value = colorName 
                });
            }
        }

        private void TransparencySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double transparency = TransparencySlider.Value / 100.0;
            _viewModel.WindowTransparency = transparency;
            TransparencyValueText.Text = $"{(int)TransparencySlider.Value}%";
            _viewModel.SaveSettings();
            
            SettingsChanged?.Invoke(this, new SettingsChangedEventArgs { 
                SettingName = "Transparency", 
                Value = transparency.ToString() 
            });
        }

        private void GpuAccelCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _viewModel.IsGpuAccelerationEnabled = GpuAccelCheckBox.IsChecked ?? false;
            _viewModel.SaveSettings();
            
            SettingsChanged?.Invoke(this, new SettingsChangedEventArgs { 
                SettingName = "GpuAcceleration", 
                Value = _viewModel.IsGpuAccelerationEnabled.ToString() 
            });
        }

        private void StartupCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _viewModel.StartAtSystemStartup = StartupCheckBox.IsChecked ?? false;
            _viewModel.SaveSettings();
        }

        private void TextScaleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _viewModel.TextScale = TextScaleSlider.Value;
            _viewModel.SaveSettings();
            
            SettingsChanged?.Invoke(this, new SettingsChangedEventArgs { 
                SettingName = "TextScale", 
                Value = _viewModel.TextScale.ToString() 
            });
        }

        private void HighContrastCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _viewModel.HighContrastMode = HighContrastCheckBox.IsChecked ?? false;
            _viewModel.SaveSettings();
            
            SettingsChanged?.Invoke(this, new SettingsChangedEventArgs { 
                SettingName = "HighContrast", 
                Value = _viewModel.HighContrastMode.ToString() 
            });
        }
    }

    public enum AppTheme
    {
        MonadoDark = 0,
        MonadoLight = 1,
        HighContrast = 2,
        Classic = 3
    }

    public class SettingsPanelViewModel
    {
        public AppTheme CurrentTheme { get; set; } = AppTheme.MonadoDark;
        public double WindowTransparency { get; set; } = 0.85;
        public string AccentColor { get; set; } = "Cyan";
        public bool IsGpuAccelerationEnabled { get; set; } = true;
        public bool StartAtSystemStartup { get; set; } = false;
        public bool StartMinimized { get; set; } = false;
        public bool GlobalHotkeysEnabled { get; set; } = true;
        public bool HighContrastMode { get; set; } = false;
        public bool ReduceAnimations { get; set; } = false;
        public double TextScale { get; set; } = 1.0;

        public void SaveSettings()
        {
            try
            {
                var settings = Properties.Settings.Default;
                settings.Theme = CurrentTheme.ToString();
                settings.WindowTransparency = WindowTransparency;
                settings.AccentColor = AccentColor;
                settings.GpuAcceleration = IsGpuAccelerationEnabled;
                settings.StartupBehavior = StartAtSystemStartup;
                settings.GlobalHotkeys = GlobalHotkeysEnabled;
                settings.HighContrast = HighContrastMode;
                settings.TextScale = TextScale;
                settings.Save();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        public void LoadSettings()
        {
            try
            {
                var settings = Properties.Settings.Default;
                if (Enum.TryParse<AppTheme>(settings.Theme, out var theme))
                    CurrentTheme = theme;
                WindowTransparency = settings.WindowTransparency;
                AccentColor = settings.AccentColor;
                IsGpuAccelerationEnabled = settings.GpuAcceleration;
                StartAtSystemStartup = settings.StartupBehavior;
                GlobalHotkeysEnabled = settings.GlobalHotkeys;
                HighContrastMode = settings.HighContrast;
                TextScale = settings.TextScale;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
            }
        }
    }

    public class SettingsChangedEventArgs : EventArgs
    {
        public string SettingName { get; set; }
        public object Value { get; set; }
    }
}
