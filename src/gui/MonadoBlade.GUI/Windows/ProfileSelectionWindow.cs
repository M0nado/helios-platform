using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using MonadoBlade.GUI.Components;

namespace MonadoBlade.GUI.Windows
{
    /// <summary>
    /// Profile/User Selection Window - Login screen with Monado wheels for each profile.
    /// Features: Profile carousel, animated wheel selection, password entry, professional UI.
    /// </summary>
    public partial class ProfileSelectionWindow : Window
    {
        private ObservableCollection<UserProfile> _profiles = new ObservableCollection<UserProfile>();
        private int _currentProfileIndex = 0;
        private UserProfile _selectedProfile;
        private bool _isAuthenticating = false;

        public string SelectedUsername { get; private set; } = "";
        public bool AuthenticationSuccess { get; private set; } = false;

        public ProfileSelectionWindow()
        {
            InitializeComponent();
            SetupWindow();
            LoadProfiles();
        }

        private void SetupWindow()
        {
            Width = 1000;
            Height = 700;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = new SolidColorBrush(Color.FromRgb(10, 20, 40));
            AllowsTransparency = false;

            // Main grid
            var mainGrid = new Grid
            {
                Background = new SolidColorBrush(Color.FromRgb(10, 20, 40))
            };

            // Background kanji effect (subtle)
            var backgroundCanvas = new Canvas
            {
                Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromRgb(10, 20, 40), 0),
                        new GradientStop(Color.FromRgb(20, 35, 60), 1)
                    }
                },
                Opacity = 0.95
            };

            // Content panel
            var contentPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Title
            var titleBlock = new TextBlock
            {
                Text = "MONADO BLADE - USER SELECTION",
                Foreground = new SolidColorBrush(Color.FromRgb(0, 217, 255)),
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 40)
            };

            contentPanel.Children.Add(titleBlock);

            // Profile carousel
            var carouselPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 40)
            };

            // Previous button
            var prevButton = CreateNavigationButton("◀", -1);
            carouselPanel.Children.Add(prevButton);

            // Profile cards container
            var profilesContainer = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(20, 0)
            };

            // Add profile cards with Monado wheels
            for (int i = 0; i < 3; i++) // Show 3 profiles
            {
                var profileCard = CreateProfileCard(i);
                profilesContainer.Children.Add(profileCard);
            }

            carouselPanel.Children.Add(profilesContainer);

            // Next button
            var nextButton = CreateNavigationButton("▶", 1);
            carouselPanel.Children.Add(nextButton);

            contentPanel.Children.Add(carouselPanel);

            // Selection info
            var infoPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 30)
            };

            var selectedLabel = new TextBlock
            {
                Text = "Selected: Administrator",
                Foreground = new SolidColorBrush(Color.FromRgb(0, 217, 255)),
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 12),
                Name = "SelectedProfileLabel"
            };

            infoPanel.Children.Add(selectedLabel);

            // Password entry (shown for admin/user)
            var passwordPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(0, 0, 0, 20)
            };

            var passwordLabel = new TextBlock
            {
                Text = "Password:",
                Foreground = new SolidColorBrush(Color.FromRgb(168, 176, 184)),
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var passwordBox = new PasswordBox
            {
                Width = 250,
                Height = 36,
                Foreground = new SolidColorBrush(Color.FromRgb(232, 232, 232)),
                Background = new SolidColorBrush(Color.FromRgb(26, 40, 56)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(58, 90, 120)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12, 8),
                FontSize = 14,
                Name = "PasswordBox"
            };

            passwordPanel.Children.Add(passwordLabel);
            passwordPanel.Children.Add(passwordBox);

            infoPanel.Children.Add(passwordPanel);

            contentPanel.Children.Add(infoPanel);

            // Action buttons
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Spacing = 12
            };

            var signInButton = new Button
            {
                Content = "SIGN IN",
                Width = 120,
                Height = 40,
                Foreground = new SolidColorBrush(Color.FromRgb(232, 232, 232)),
                Background = new SolidColorBrush(Color.FromRgb(0, 217, 255)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0, 217, 255)),
                BorderThickness = new Thickness(1),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold
            };

            signInButton.Click += (s, e) => HandleSignIn();

            var guestButton = new Button
            {
                Content = "GUEST",
                Width = 120,
                Height = 40,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 217, 255)),
                Background = new SolidColorBrush(Color.FromRgb(26, 40, 56)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0, 217, 255)),
                BorderThickness = new Thickness(1.5),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold
            };

            guestButton.Click += (s, e) => HandleGuest();

            buttonPanel.Children.Add(signInButton);
            buttonPanel.Children.Add(guestButton);

            contentPanel.Children.Add(buttonPanel);

            mainGrid.Children.Add(backgroundCanvas);
            mainGrid.Children.Add(contentPanel);

            Content = mainGrid;
        }

        private Button CreateNavigationButton(string text, int direction)
        {
            var button = new Button
            {
                Content = text,
                Width = 40,
                Height = 40,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 217, 255)),
                Background = new SolidColorBrush(Color.FromRgb(26, 40, 56)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(58, 90, 120)),
                BorderThickness = new Thickness(1),
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0)
            };

            button.Click += (s, e) => NavigateProfiles(direction);

            return button;
        }

        private Border CreateProfileCard(int index)
        {
            var card = new Border
            {
                Width = 200,
                Height = 280,
                Background = new SolidColorBrush(Color.FromRgb(26, 40, 56)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(58, 90, 120)),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(12),
                Cursor = System.Windows.Input.Cursors.Hand,
                Padding = new Thickness(16)
            };

            var content = new StackPanel
            {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Monado wheel (small version for profile)
            var wheelCanvas = new Canvas
            {
                Width = 120,
                Height = 120,
                Margin = new Thickness(0, 0, 0, 12)
            };

            // Draw simplified wheel
            var wheelCircle = new Ellipse
            {
                Width = 120,
                Height = 120,
                Fill = new SolidColorBrush(Color.FromRgb(0, 217, 255)),
                Opacity = 0.1
            };

            var wheelBorder = new Ellipse
            {
                Width = 120,
                Height = 120,
                Stroke = new SolidColorBrush(Color.FromRgb(0, 217, 255)),
                StrokeThickness = 2
            };

            var wheelCenter = new Ellipse
            {
                Width = 20,
                Height = 20,
                Fill = new SolidColorBrush(Color.FromRgb(0, 217, 255)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            wheelCanvas.Children.Add(wheelCircle);
            wheelCanvas.Children.Add(wheelBorder);
            wheelCanvas.Children.Add(wheelCenter);

            content.Children.Add(wheelCanvas);

            // Profile name
            var profileName = new TextBlock
            {
                Text = "User Profile",
                Foreground = new SolidColorBrush(Color.FromRgb(232, 232, 232)),
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            };

            content.Children.Add(profileName);

            // Status indicator
            var statusIndicator = new TextBlock
            {
                Text = "● Online",
                Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 65)),
                FontSize = 12,
                TextAlignment = TextAlignment.Center
            };

            content.Children.Add(statusIndicator);

            card.Child = content;

            // Hover effect
            card.MouseEnter += (s, e) =>
            {
                card.Background = new SolidColorBrush(Color.FromRgb(37, 53, 72));
                card.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 217, 255));
            };

            card.MouseLeave += (s, e) =>
            {
                card.Background = new SolidColorBrush(Color.FromRgb(26, 40, 56));
                card.BorderBrush = new SolidColorBrush(Color.FromRgb(58, 90, 120));
            };

            card.MouseDown += (s, e) => SelectProfile(index);

            return card;
        }

        private void LoadProfiles()
        {
            _profiles.Add(new UserProfile { Name = "Administrator", Icon = "👤", IsAdmin = true });
            _profiles.Add(new UserProfile { Name = "User", Icon = "👤", IsAdmin = false });
            _profiles.Add(new UserProfile { Name = "Developer", Icon = "💻", IsAdmin = false });
            _profiles.Add(new UserProfile { Name = "Guest", Icon = "👥", IsAdmin = false });

            _selectedProfile = _profiles[0];
        }

        private void NavigateProfiles(int direction)
        {
            _currentProfileIndex = (_currentProfileIndex + direction) % _profiles.Count;
            if (_currentProfileIndex < 0) _currentProfileIndex = _profiles.Count - 1;

            _selectedProfile = _profiles[_currentProfileIndex];
            UpdateProfileDisplay();
        }

        private void SelectProfile(int index)
        {
            _currentProfileIndex = index;
            _selectedProfile = _profiles[_currentProfileIndex];
            UpdateProfileDisplay();
        }

        private void UpdateProfileDisplay()
        {
            // Update the selected profile label
            if (FindName("SelectedProfileLabel") is TextBlock label)
            {
                label.Text = $"Selected: {_selectedProfile.Name}";
            }
        }

        private void HandleSignIn()
        {
            if (_isAuthenticating) return;

            if (string.IsNullOrEmpty(_selectedProfile.Name))
            {
                ShowError("Please select a profile");
                return;
            }

            // For guest, no password needed
            if (_selectedProfile.Name == "Guest")
            {
                SelectedUsername = "Guest";
                AuthenticationSuccess = true;
                DialogResult = true;
                Close();
                return;
            }

            // For admin/user, require password
            // In real implementation, this would validate credentials
            _isAuthenticating = true;
            SelectedUsername = _selectedProfile.Name;
            AuthenticationSuccess = true;
            DialogResult = true;
            Close();
        }

        private void HandleGuest()
        {
            SelectedUsername = "Guest";
            AuthenticationSuccess = true;
            DialogResult = true;
            Close();
        }

        private void ShowError(string message)
        {
            MessageBox.Show(message, "Authentication", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
