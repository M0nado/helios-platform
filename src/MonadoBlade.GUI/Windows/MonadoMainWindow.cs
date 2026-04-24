using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;

namespace MonadoBlade.GUI.Windows
{
    /// <summary>
    /// Main application window - Complete Xenoblade-inspired UI/UX system.
    /// Seamless integration of kanji, blade, sounds, and settings.
    /// Simple → Advanced controls with intuitive navigation.
    /// </summary>
    public partial class MonadoMainWindow : Window
    {
        private Grid _mainGrid;
        private StackPanel _topBar;
        private StackPanel _sidePanel;
        private ContentControl _mainContent;
        private StackPanel _bottomBar;
        private MonadoSoundManager _soundManager;
        private KanjiGlowSystem _glowSystem;
        private XenobladeMondo _monado;
        private bool _isSimpleMode = true;

        public MonadoMainWindow()
        {
            InitializeComponent();
            _soundManager = new MonadoSoundManager();
            _glowSystem = new KanjiGlowSystem();

            BuildMainInterface();
            ApplyTheme();
            LoadDashboard();
        }

        private void BuildMainInterface()
        {
            Width = 1400;
            Height = 900;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Title = "MONADO BLADE v3.4.0 - PREMIUM EDITION";
            Background = new SolidColorBrush(Color.FromRgb(10, 20, 40));

            _mainGrid = new Grid();
            _mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(60) });      // Top bar
            _mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Content
            _mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50) });      // Bottom bar

            _mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(280) }); // Side panel
            _mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Main content

            // Top navigation bar
            _topBar = BuildTopBar();
            Grid.SetRow(_topBar, 0);
            Grid.SetColumnSpan(_topBar, 2);
            _mainGrid.Children.Add(_topBar);

            // Side navigation panel
            _sidePanel = BuildSidePanel();
            Grid.SetRow(_sidePanel, 1);
            Grid.SetColumn(_sidePanel, 0);
            _mainGrid.Children.Add(_sidePanel);

            // Main content area
            _mainContent = new ContentControl
            {
                Background = new SolidColorBrush(Color.FromRgb(15, 30, 55)),
                Margin = new Thickness(12)
            };
            Grid.SetRow(_mainContent, 1);
            Grid.SetColumn(_mainContent, 1);
            _mainGrid.Children.Add(_mainContent);

            // Bottom status bar
            _bottomBar = BuildBottomBar();
            Grid.SetRow(_bottomBar, 2);
            Grid.SetColumnSpan(_bottomBar, 2);
            _mainGrid.Children.Add(_bottomBar);

            Content = _mainGrid;
        }

        private StackPanel BuildTopBar()
        {
            var topBar = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Background = new SolidColorBrush(Color.FromRgb(10, 20, 40)),
                Padding = new Thickness(16, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Spacing = 20
            };

            // Logo/Title
            var logoText = new TextBlock
            {
                Text = "⚔ MONADO BLADE",
                Foreground = new SolidColorBrush(Color.FromRgb(0, 217, 255)),
                FontSize = 18,
                FontWeight = FontWeights.Bold
            };

            // Quick access buttons
            var quickButtons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 12,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 0)
            };

            quickButtons.Children.Add(CreateQuickButton("🏠 Home", OpenDashboard));
            quickButtons.Children.Add(CreateQuickButton("📊 Monitor", OpenMonitor));
            quickButtons.Children.Add(CreateQuickButton("⚙ Settings", OpenSettings));
            quickButtons.Children.Add(CreateQuickButton("🎓 Help", OpenHelp));

            // Right-side info
            var infoPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 0)
            };

            var modeToggle = new ToggleButton
            {
                Content = "SIMPLE / ADVANCED",
                Width = 180,
                Height = 32,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 217, 255)),
                Background = new SolidColorBrush(Color.FromRgb(26, 40, 56)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0, 217, 255)),
                BorderThickness = new Thickness(1),
                FontSize = 11,
                FontWeight = FontWeights.Bold
            };

            modeToggle.Click += (s, e) =>
            {
                _isSimpleMode = !_isSimpleMode;
                _soundManager.PlayBladeSound("hover");
                RefreshCurrentView();
            };

            infoPanel.Children.Add(modeToggle);

            topBar.Children.Add(logoText);
            topBar.Children.Add(quickButtons);
            topBar.Children.Add(infoPanel);

            return topBar;
        }

        private Button CreateQuickButton(string text, Action onClicked)
        {
            var button = new Button
            {
                Content = text,
                Padding = new Thickness(12, 6),
                Foreground = new SolidColorBrush(Color.FromRgb(232, 232, 232)),
                Background = new SolidColorBrush(Color.FromRgb(26, 40, 56)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(58, 90, 120)),
                BorderThickness = new Thickness(1),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Cursor = System.Windows.Input.Cursors.Hand
            };

            button.MouseEnter += (s, e) =>
            {
                button.Background = new SolidColorBrush(Color.FromRgb(37, 53, 72));
                button.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 217, 255));
            };

            button.MouseLeave += (s, e) =>
            {
                button.Background = new SolidColorBrush(Color.FromRgb(26, 40, 56));
                button.BorderBrush = new SolidColorBrush(Color.FromRgb(58, 90, 120));
            };

            button.Click += (s, e) => onClicked?.Invoke();

            return button;
        }

        private StackPanel BuildSidePanel()
        {
            var sidePanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Background = new SolidColorBrush(Color.FromRgb(10, 20, 40)),
                Padding = new Thickness(12),
                Spacing = 8
            };

            var title = new TextBlock
            {
                Text = "QUICK ACCESS",
                Foreground = new SolidColorBrush(Color.FromRgb(0, 217, 255)),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 12)
            };

            sidePanel.Children.Add(title);

            // Navigation items
            sidePanel.Children.Add(CreateNavItem("🏠 Dashboard", OpenDashboard));
            sidePanel.Children.Add(CreateNavItem("📊 System Monitor", OpenMonitor));
            sidePanel.Children.Add(CreateNavItem("🎨 Themes", OpenThemes));
            sidePanel.Children.Add(CreateNavItem("🔊 Audio", OpenAudio));
            sidePanel.Children.Add(CreateNavItem("⚙ Settings", OpenSettings));
            sidePanel.Children.Add(CreateNavItem("🎓 Tutorials", OpenHelp));
            sidePanel.Children.Add(CreateNavItem("👤 Profile", OpenProfile));

            // Separator
            var separator = new Line
            {
                X1 = 0,
                X2 = 256,
                Stroke = new SolidColorBrush(Color.FromRgb(58, 90, 120)),
                StrokeThickness = 1,
                Margin = new Thickness(0, 12, 0, 12)
            };

            sidePanel.Children.Add(separator);

            // System info
            var sysInfoTitle = new TextBlock
            {
                Text = "SYSTEM INFO",
                Foreground = new SolidColorBrush(Color.FromRgb(0, 217, 255)),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 8)
            };

            sidePanel.Children.Add(sysInfoTitle);

            var cpuInfo = new TextBlock
            {
                Text = "CPU: 45%",
                Foreground = new SolidColorBrush(Color.FromRgb(168, 176, 184)),
                FontSize = 11
            };

            var gpuInfo = new TextBlock
            {
                Text = "GPU: 32%",
                Foreground = new SolidColorBrush(Color.FromRgb(168, 176, 184)),
                FontSize = 11
            };

            var memInfo = new TextBlock
            {
                Text = "MEM: 4.2 GB",
                Foreground = new SolidColorBrush(Color.FromRgb(168, 176, 184)),
                FontSize = 11
            };

            sidePanel.Children.Add(cpuInfo);
            sidePanel.Children.Add(gpuInfo);
            sidePanel.Children.Add(memInfo);

            return sidePanel;
        }

        private Border CreateNavItem(string text, Action onClicked)
        {
            var navItem = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(26, 40, 56)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(58, 90, 120)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(12, 10),
                Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new Thickness(0, 0, 0, 4)
            };

            var textBlock = new TextBlock
            {
                Text = text,
                Foreground = new SolidColorBrush(Color.FromRgb(232, 232, 232)),
                FontSize = 12
            };

            navItem.Child = textBlock;

            navItem.MouseEnter += (s, e) =>
            {
                navItem.Background = new SolidColorBrush(Color.FromRgb(37, 53, 72));
                navItem.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 217, 255));
            };

            navItem.MouseLeave += (s, e) =>
            {
                navItem.Background = new SolidColorBrush(Color.FromRgb(26, 40, 56));
                navItem.BorderBrush = new SolidColorBrush(Color.FromRgb(58, 90, 120));
            };

            navItem.MouseDown += (s, e) => onClicked?.Invoke();

            return navItem;
        }

        private StackPanel BuildBottomBar()
        {
            var bottomBar = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Background = new SolidColorBrush(Color.FromRgb(10, 20, 40)),
                Padding = new Thickness(16),
                VerticalAlignment = VerticalAlignment.Center,
                Spacing = 20
            };

            // Status indicator
            var statusLight = new Ellipse
            {
                Width = 12,
                Height = 12,
                Fill = new SolidColorBrush(Color.FromRgb(0, 255, 65)),
                VerticalAlignment = VerticalAlignment.Center
            };

            var statusText = new TextBlock
            {
                Text = "● System Healthy",
                Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 65)),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Version info
            var versionText = new TextBlock
            {
                Text = "v3.4.0 Premium Edition",
                Foreground = new SolidColorBrush(Color.FromRgb(168, 176, 184)),
                FontSize = 11,
                Margin = new Thickness(0, 0, 0, 0)
            };

            // Right-aligned info
            var rightPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Spacing = 16,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 0)
            };

            var uptime = new TextBlock
            {
                Text = "Uptime: 23h 45m",
                Foreground = new SolidColorBrush(Color.FromRgb(168, 176, 184)),
                FontSize = 11
            };

            var time = new TextBlock
            {
                Text = DateTime.Now.ToString("HH:mm:ss"),
                Foreground = new SolidColorBrush(Color.FromRgb(168, 176, 184)),
                FontSize = 11
            };

            bottomBar.Children.Add(statusLight);
            bottomBar.Children.Add(statusText);
            bottomBar.Children.Add(versionText);
            bottomBar.Children.Add(rightPanel);
            rightPanel.Children.Add(uptime);
            rightPanel.Children.Add(time);

            return bottomBar;
        }

        private void LoadDashboard()
        {
            OpenDashboard();
        }

        private void OpenDashboard()
        {
            _soundManager.PlayBladeSound("hover");

            var dashboard = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 16
            };

            // Welcome section
            var welcomePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 20
            };

            // Monado blade display
            _monado = new XenobladeMondo(_soundManager, _glowSystem);
            _monado.Width = 250;
            _monado.Height = 300;
            _monado.HorizontalAlignment = HorizontalAlignment.Center;

            welcomePanel.Children.Add(_monado);

            // Info section
            var infoPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 12
            };

            var title = new TextBlock
            {
                Text = "WELCOME TO MONADO BLADE",
                Foreground = new SolidColorBrush(Color.FromRgb(0, 217, 255)),
                FontSize = 22,
                FontWeight = FontWeights.Bold
            };

            var subtitle = new TextBlock
            {
                Text = "Premium OS Enhancement System",
                Foreground = new SolidColorBrush(Color.FromRgb(168, 176, 184)),
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 12)
            };

            var description = new TextBlock
            {
                Text = "Explore the Xenoblade-inspired interface. Click the kanji to interact with the system. Use Simple mode for quick access, or Advanced mode for full control.",
                Foreground = new SolidColorBrush(Color.FromRgb(232, 232, 232)),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Width = 400,
                Margin = new Thickness(0, 0, 0, 12)
            };

            infoPanel.Children.Add(title);
            infoPanel.Children.Add(subtitle);
            infoPanel.Children.Add(description);

            welcomePanel.Children.Add(infoPanel);

            dashboard.Children.Add(welcomePanel);

            // Quick stats
            var statsPanel = BuildStatsPanel();
            dashboard.Children.Add(statsPanel);

            _mainContent.Content = dashboard;
        }

        private Panel BuildStatsPanel()
        {
            var grid = new Grid
            {
                Margin = new Thickness(0, 20, 0, 0)
            };

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            grid.Children.Add(CreateStatCard("CPU Usage", "45%", Color.FromRgb(0, 217, 255)), 0, 0);
            Grid.SetColumn(grid.Children[^1], 0);

            grid.Children.Add(CreateStatCard("GPU Usage", "32%", Color.FromRgb(0, 255, 65)), 0, 0);
            Grid.SetColumn(grid.Children[^1], 1);

            grid.Children.Add(CreateStatCard("Memory", "4.2 GB / 16 GB", Color.FromRgb(255, 215, 0)), 0, 0);
            Grid.SetColumn(grid.Children[^1], 2);

            return grid;
        }

        private Border CreateStatCard(string label, string value, Color color)
        {
            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(26, 40, 56)),
                BorderBrush = new SolidColorBrush(color),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16),
                Margin = new Thickness(8)
            };

            var content = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 8
            };

            var labelText = new TextBlock
            {
                Text = label,
                Foreground = new SolidColorBrush(color),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold
            };

            var valueText = new TextBlock
            {
                Text = value,
                Foreground = new SolidColorBrush(Color.FromRgb(232, 232, 232)),
                FontSize = 20,
                FontWeight = FontWeights.Bold
            };

            content.Children.Add(labelText);
            content.Children.Add(valueText);
            card.Child = content;

            return card;
        }

        private void OpenMonitor()
        {
            _soundManager.PlayBladeSound("hover");
            var monitor = new TextBlock
            {
                Text = "System Monitor Coming Soon",
                Foreground = new SolidColorBrush(Color.FromRgb(168, 176, 184)),
                FontSize = 16
            };
            _mainContent.Content = monitor;
        }

        private void OpenThemes()
        {
            _soundManager.PlayBladeSound("hover");
            var themes = new TextBlock
            {
                Text = "Theme Customization Coming Soon",
                Foreground = new SolidColorBrush(Color.FromRgb(168, 176, 184)),
                FontSize = 16
            };
            _mainContent.Content = themes;
        }

        private void OpenAudio()
        {
            _soundManager.PlayBladeSound("hover");

            var audioPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 16,
                Padding = new Thickness(20)
            };

            var title = new TextBlock
            {
                Text = "AUDIO SETTINGS",
                Foreground = new SolidColorBrush(Color.FromRgb(0, 217, 255)),
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20)
            };

            audioPanel.Children.Add(title);

            // Volume control
            var volumePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 12,
                Margin = new Thickness(0, 0, 0, 12)
            };

            var volumeLabel = new TextBlock
            {
                Text = "Master Volume:",
                Foreground = new SolidColorBrush(Color.FromRgb(232, 232, 232)),
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center,
                Width = 120
            };

            var volumeSlider = new Slider
            {
                Width = 300,
                Height = 24,
                Minimum = 0,
                Maximum = 100,
                Value = _soundManager.MasterVolume * 100,
                VerticalAlignment = VerticalAlignment.Center
            };

            volumeSlider.ValueChanged += (s, e) =>
            {
                _soundManager.MasterVolume = (float)(volumeSlider.Value / 100.0);
            };

            volumePanel.Children.Add(volumeLabel);
            volumePanel.Children.Add(volumeSlider);

            audioPanel.Children.Add(volumePanel);

            // Sound toggle
            var soundToggle = new CheckBox
            {
                Content = "Enable Sound Effects",
                Foreground = new SolidColorBrush(Color.FromRgb(232, 232, 232)),
                FontSize = 14,
                IsChecked = _soundManager.SoundEnabled
            };

            soundToggle.Click += (s, e) =>
            {
                _soundManager.SoundEnabled = soundToggle.IsChecked ?? false;
            };

            audioPanel.Children.Add(soundToggle);

            _mainContent.Content = audioPanel;
        }

        private void OpenSettings()
        {
            _soundManager.PlayBladeSound("hover");

            if (_isSimpleMode)
            {
                var simpleSettings = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 16,
                    Padding = new Thickness(20)
                };

                var title = new TextBlock
                {
                    Text = "QUICK SETTINGS",
                    Foreground = new SolidColorBrush(Color.FromRgb(0, 217, 255)),
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 20)
                };

                simpleSettings.Children.Add(title);
                simpleSettings.Children.Add(CreateSimpleSettingItem("🌙 Dark Mode", "Enabled"));
                simpleSettings.Children.Add(CreateSimpleSettingItem("🎵 Sound", "Enabled"));
                simpleSettings.Children.Add(CreateSimpleSettingItem("✨ Effects", "High Quality"));
                simpleSettings.Children.Add(CreateSimpleSettingItem("🔄 Auto-Update", "On"));

                _mainContent.Content = simpleSettings;
            }
            else
            {
                var advSettings = new TextBlock
                {
                    Text = "Advanced Settings Coming Soon",
                    Foreground = new SolidColorBrush(Color.FromRgb(168, 176, 184)),
                    FontSize = 16
                };
                _mainContent.Content = advSettings;
            }
        }

        private Border CreateSimpleSettingItem(string label, string value)
        {
            var item = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(26, 40, 56)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(58, 90, 120)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(12, 10),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 12
            };

            var labelText = new TextBlock
            {
                Text = label,
                Foreground = new SolidColorBrush(Color.FromRgb(232, 232, 232)),
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center
            };

            var valueText = new TextBlock
            {
                Text = value,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 217, 255)),
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 0, 0)
            };

            panel.Children.Add(labelText);
            panel.Children.Add(valueText);
            item.Child = panel;

            return item;
        }

        private void OpenHelp()
        {
            _soundManager.PlayBladeSound("hover");
            var help = new TextBlock
            {
                Text = "Help & Tutorials Coming Soon",
                Foreground = new SolidColorBrush(Color.FromRgb(168, 176, 184)),
                FontSize = 16
            };
            _mainContent.Content = help;
        }

        private void OpenProfile()
        {
            _soundManager.PlayBladeSound("hover");
            var profile = new TextBlock
            {
                Text = "User Profile Coming Soon",
                Foreground = new SolidColorBrush(Color.FromRgb(168, 176, 184)),
                FontSize = 16
            };
            _mainContent.Content = profile;
        }

        private void RefreshCurrentView()
        {
            // Refresh based on current mode
            OpenDashboard();
        }

        private void ApplyTheme()
        {
            // Theme already applied via colors
        }
    }
}
