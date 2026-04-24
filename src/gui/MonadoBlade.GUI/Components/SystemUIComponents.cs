using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace MonadoBlade.GUI.Components
{
    /// <summary>
    /// Complete system UI components - Volume bars, sliders, status indicators, tray icons.
    /// Professional quality, optimized, lightweight.
    /// </summary>

    /// <summary>
    /// Volume control bar with animated slider and visual feedback.
    /// </summary>
    public class VolumeBar : StackPanel
    {
        private Slider _volumeSlider;
        private TextBlock _volumeLabel;
        private Ellipse _volumeIcon;
        private double _currentVolume = 0.7;

        public double Volume
        {
            get => _currentVolume;
            set
            {
                _currentVolume = Math.Clamp(value, 0, 1);
                UpdateVolumeDisplay();
            }
        }

        public VolumeBar()
        {
            Orientation = Orientation.Horizontal;
            Spacing = 8;
            Margin = new Thickness(0, 0, 16, 0);

            // Volume icon
            _volumeIcon = new Ellipse
            {
                Width = 24,
                Height = 24,
                Fill = new SolidColorBrush(Color.FromRgb(0, 217, 255)),
                ToolTip = "Volume Control"
            };

            // Volume slider
            _volumeSlider = new Slider
            {
                Width = 150,
                Height = 24,
                Minimum = 0,
                Maximum = 100,
                Value = _currentVolume * 100,
                Style = (Style)FindResource("SliderStyle")
            };

            _volumeSlider.ValueChanged += (s, e) =>
            {
                _currentVolume = _volumeSlider.Value / 100.0;
                UpdateVolumeDisplay();
            };

            // Volume label
            _volumeLabel = new TextBlock
            {
                Text = "70%",
                Foreground = new SolidColorBrush(Color.FromRgb(168, 176, 184)),
                Width = 40,
                FontSize = 12,
                TextAlignment = TextAlignment.Center
            };

            Children.Add(_volumeIcon);
            Children.Add(_volumeSlider);
            Children.Add(_volumeLabel);
        }

        private void UpdateVolumeDisplay()
        {
            int percent = (int)(_currentVolume * 100);
            _volumeLabel.Text = $"{percent}%";

            // Change icon based on volume level
            if (_currentVolume == 0)
            {
                _volumeIcon.Opacity = 0.5;
            }
            else if (_currentVolume < 0.33)
            {
                _volumeIcon.Opacity = 0.7;
            }
            else if (_currentVolume < 0.67)
            {
                _volumeIcon.Opacity = 0.85;
            }
            else
            {
                _volumeIcon.Opacity = 1.0;
            }
        }
    }

    /// <summary>
    /// Brightness/Backlight control slider.
    /// </summary>
    public class BrightnessControl : StackPanel
    {
        private Slider _brightnessSlider;
        private double _currentBrightness = 0.8;

        public double Brightness
        {
            get => _currentBrightness;
            set
            {
                _currentBrightness = Math.Clamp(value, 0, 1);
                _brightnessSlider.Value = _currentBrightness * 100;
            }
        }

        public BrightnessControl()
        {
            Orientation = Orientation.Horizontal;
            Spacing = 12;
            Margin = new Thickness(0, 12, 0, 0);

            // Brightness icon (sun)
            var icon = new TextBlock
            {
                Text = "☀",
                FontSize = 20,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 0)),
                VerticalAlignment = VerticalAlignment.Center
            };

            _brightnessSlider = new Slider
            {
                Width = 200,
                Height = 24,
                Minimum = 0,
                Maximum = 100,
                Value = _currentBrightness * 100
            };

            _brightnessSlider.ValueChanged += (s, e) =>
            {
                _currentBrightness = _brightnessSlider.Value / 100.0;
            };

            Children.Add(icon);
            Children.Add(_brightnessSlider);
        }
    }

    /// <summary>
    /// System tray icon with tooltip and context menu.
    /// </summary>
    public class TrayIcon : Button
    {
        public string IconLabel { get; set; }
        public string ToolTipText { get; set; }
        public Color IconColor { get; set; } = Color.FromRgb(0, 217, 255);
        public Action OnClick { get; set; }

        public TrayIcon(string label, string tooltip)
        {
            IconLabel = label;
            ToolTipText = tooltip;
            Width = 32;
            Height = 32;
            Background = new SolidColorBrush(Color.FromRgb(26, 40, 56));
            BorderBrush = new SolidColorBrush(Color.FromRgb(58, 90, 120));
            BorderThickness = new Thickness(1);
            Margin = new Thickness(4, 0, 0, 0);

            var content = new TextBlock
            {
                Text = label,
                Foreground = new SolidColorBrush(IconColor),
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            Content = content;

            ToolTip = new ToolTip
            {
                Content = new TextBlock { Text = tooltip, Foreground = new SolidColorBrush(Color.FromRgb(232, 232, 232)) }
            };

            Click += (s, e) => OnClick?.Invoke();

            // Hover effect
            MouseEnter += (s, e) =>
            {
                Background = new SolidColorBrush(Color.FromRgb(37, 53, 72));
                BorderBrush = new SolidColorBrush(Color.FromRgb(0, 217, 255));
            };

            MouseLeave += (s, e) =>
            {
                Background = new SolidColorBrush(Color.FromRgb(26, 40, 56));
                BorderBrush = new SolidColorBrush(Color.FromRgb(58, 90, 120));
            };
        }
    }

    /// <summary>
    /// System tray/notification area.
    /// </summary>
    public class SystemTray : StackPanel
    {
        private ObservableCollection<TrayIcon> _icons = new ObservableCollection<TrayIcon>();

        public SystemTray()
        {
            Orientation = Orientation.Horizontal;
            Background = new SolidColorBrush(Color.FromRgb(10, 20, 40));
            Padding = new Thickness(12, 0);
            Margin = new Thickness(0, 8, 0, 0);
            HorizontalAlignment = HorizontalAlignment.Right;

            // Network icon
            var networkIcon = new TrayIcon("📶", "Network")
            {
                IconColor = Color.FromRgb(0, 255, 65)
            };

            // Audio icon
            var audioIcon = new TrayIcon("🔊", "Audio");

            // Battery icon
            var batteryIcon = new TrayIcon("🔋", "Battery")
            {
                IconColor = Color.FromRgb(255, 215, 0)
            };

            // Clock/Time
            var clockIcon = new TrayIcon("⏰", "Time")
            {
                IconColor = Color.FromRgb(100, 150, 255)
            };

            Children.Add(networkIcon);
            Children.Add(audioIcon);
            Children.Add(batteryIcon);
            Children.Add(clockIcon);
        }
    }

    /// <summary>
    /// Profile selector component - Shows user profiles with avatars.
    /// Used in login and user switching scenarios.
    /// </summary>
    public class ProfileSelector : ItemsControl
    {
        public ObservableCollection<UserProfile> Profiles { get; set; } = new ObservableCollection<UserProfile>();

        public ProfileSelector()
        {
            // Create item template
            var template = new DataTemplate();
            var factory = new FrameworkElementFactory(typeof(ProfileCard));
            template.VisualTree = factory;

            ItemTemplate = template;
            ItemsPanel = new ItemsPanelTemplate(
                new FrameworkElementFactory(typeof(WrapPanel))
                {
                    SetValue(WrapPanel.OrientationProperty, Orientation.Horizontal),
                    SetValue(WrapPanel.HorizontalAlignmentProperty, HorizontalAlignment.Center)
                }
            );

            // Add sample profiles
            Profiles.Add(new UserProfile { Name = "Administrator", Icon = "👤", IsAdmin = true });
            Profiles.Add(new UserProfile { Name = "User", Icon = "👤", IsAdmin = false });
            Profiles.Add(new UserProfile { Name = "Guest", Icon = "👥", IsAdmin = false });

            ItemsSource = Profiles;
        }
    }

    /// <summary>
    /// Individual profile card with avatar and name.
    /// </summary>
    public class ProfileCard : Button
    {
        private UserProfile _profile;

        public ProfileCard()
        {
            Width = 120;
            Height = 140;
            Margin = new Thickness(12);
            Background = new SolidColorBrush(Color.FromRgb(26, 40, 56));
            BorderBrush = new SolidColorBrush(Color.FromRgb(58, 90, 120));
            BorderThickness = new Thickness(2);

            var stackPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Avatar circle
            var avatar = new Ellipse
            {
                Width = 80,
                Height = 80,
                Fill = new SolidColorBrush(Color.FromRgb(0, 217, 255)),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var avatarText = new TextBlock
            {
                Text = "👤",
                FontSize = 40,
                Foreground = new SolidColorBrush(Color.FromRgb(10, 20, 40)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Name
            var nameBlock = new TextBlock
            {
                Text = "User",
                Foreground = new SolidColorBrush(Color.FromRgb(232, 232, 232)),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                TextAlignment = TextAlignment.Center
            };

            stackPanel.Children.Add(avatar);
            stackPanel.Children.Add(nameBlock);
            Content = stackPanel;

            // Hover effect
            MouseEnter += (s, e) =>
            {
                Background = new SolidColorBrush(Color.FromRgb(37, 53, 72));
                BorderBrush = new SolidColorBrush(Color.FromRgb(0, 217, 255));
            };

            MouseLeave += (s, e) =>
            {
                Background = new SolidColorBrush(Color.FromRgb(26, 40, 56));
                BorderBrush = new SolidColorBrush(Color.FromRgb(58, 90, 120));
            };
        }
    }

    /// <summary>
    /// User profile data.
    /// </summary>
    public class UserProfile
    {
        public string Name { get; set; }
        public string Icon { get; set; }
        public bool IsAdmin { get; set; }
        public string AvatarPath { get; set; }
    }

    /// <summary>
    /// Status indicator - Shows system health or status.
    /// </summary>
    public class StatusIndicator : StackPanel
    {
        private Ellipse _statusLight;
        private TextBlock _statusLabel;

        public StatusType Status
        {
            set => SetStatus(value);
        }

        public StatusIndicator()
        {
            Orientation = Orientation.Horizontal;
            Spacing = 8;
            VerticalAlignment = VerticalAlignment.Center;

            _statusLight = new Ellipse
            {
                Width = 12,
                Height = 12,
                Fill = new SolidColorBrush(Color.FromRgb(0, 255, 65))
            };

            _statusLabel = new TextBlock
            {
                Foreground = new SolidColorBrush(Color.FromRgb(232, 232, 232)),
                FontSize = 12
            };

            Children.Add(_statusLight);
            Children.Add(_statusLabel);
        }

        private void SetStatus(StatusType status)
        {
            (_statusLight.Fill as SolidColorBrush).Color = status switch
            {
                StatusType.Healthy => Color.FromRgb(0, 255, 65),
                StatusType.Warning => Color.FromRgb(255, 215, 0),
                StatusType.Critical => Color.FromRgb(255, 0, 85),
                StatusType.Offline => Color.FromRgb(96, 104, 112),
                _ => Color.FromRgb(0, 217, 255)
            };

            _statusLabel.Text = status switch
            {
                StatusType.Healthy => "Healthy",
                StatusType.Warning => "Warning",
                StatusType.Critical => "Critical",
                StatusType.Offline => "Offline",
                _ => "Unknown"
            };
        }
    }

    public enum StatusType
    {
        Healthy,
        Warning,
        Critical,
        Offline
    }

    /// <summary>
    /// Notification card - Appears in toast/notification area.
    /// </summary>
    public class NotificationCard : Border
    {
        public NotificationCard(string title, string message, NotificationType type = NotificationType.Info)
        {
            Height = 80;
            Margin = new Thickness(12);
            CornerRadius = new CornerRadius(8);
            Padding = new Thickness(16);
            BorderThickness = new Thickness(1);

            // Color by type
            var (bgColor, borderColor) = type switch
            {
                NotificationType.Success => (Color.FromRgb(26, 50, 40), Color.FromRgb(0, 255, 65)),
                NotificationType.Warning => (Color.FromRgb(50, 45, 26), Color.FromRgb(255, 215, 0)),
                NotificationType.Error => (Color.FromRgb(50, 26, 40), Color.FromRgb(255, 0, 85)),
                _ => (Color.FromRgb(26, 40, 56), Color.FromRgb(0, 217, 255))
            };

            Background = new SolidColorBrush(bgColor);
            BorderBrush = new SolidColorBrush(borderColor);

            var content = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            content.Children.Add(new TextBlock
            {
                Text = title,
                Foreground = new SolidColorBrush(Color.FromRgb(232, 232, 232)),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold
            });

            content.Children.Add(new TextBlock
            {
                Text = message,
                Foreground = new SolidColorBrush(Color.FromRgb(168, 176, 184)),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 0)
            });

            Child = content;
        }
    }

    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }

    /// <summary>
    /// Optimization progress indicator with phases.
    /// </summary>
    public class OptimizationProgressBar : StackPanel
    {
        private ProgressBar _mainProgress;
        private TextBlock _phaseLabel;

        public OptimizationProgressBar()
        {
            Spacing = 8;

            _phaseLabel = new TextBlock
            {
                Foreground = new SolidColorBrush(Color.FromRgb(0, 217, 255)),
                FontSize = 12,
                Text = "Initializing..."
            };

            _mainProgress = new ProgressBar
            {
                Height = 8,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 217, 255)),
                Background = new SolidColorBrush(Color.FromRgb(26, 42, 58))
            };

            Children.Add(_phaseLabel);
            Children.Add(_mainProgress);
        }

        public void UpdateProgress(double percent, string phase)
        {
            _mainProgress.Value = percent;
            _phaseLabel.Text = phase;
        }
    }
}
