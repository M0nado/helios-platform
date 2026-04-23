using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace HELIOS.Platform.Phase10.BuilderUI
{
    /// <summary>
    /// Profile selector component for step 5 of the wizard.
    /// Allows selection of optimization profiles with preview capabilities.
    /// </summary>
    public class ProfileSelector : UserControl
    {
        private IBuilderUIService _service;
        private StackPanel _profilesPanel;
        private TextBlock _previewText;
        private string _selectedProfileId;

        public event EventHandler<string> ProfileSelected;

        public ProfileSelector()
        {
            InitializeUI();
        }

        /// <summary>
        /// Initialize the profile selector UI.
        /// </summary>
        private void InitializeUI()
        {
            var panel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(10), Spacing = 10 };

            // Title
            panel.Children.Add(new TextBlock { Text = "Choose Optimization Profile", FontWeight = System.Windows.FontWeights.Bold, FontSize = 14 });

            // Profiles container
            _profilesPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 10, Margin = new Thickness(0, 10, 0, 10) };
            panel.Children.Add(_profilesPanel);

            // Preview section
            panel.Children.Add(new Separator());
            panel.Children.Add(new TextBlock { Text = "Profile Preview", FontWeight = System.Windows.FontWeights.Bold, FontSize = 12 });

            _previewText = new TextBlock
            {
                Text = "Select a profile to see preview",
                TextWrapping = System.Windows.TextWrapping.Wrap,
                Margin = new Thickness(10),
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.CornflowerBlue),
                MinHeight = 100
            };
            panel.Children.Add(_previewText);

            // Custom profile button
            var customButton = new Button
            {
                Content = "Create Custom Profile",
                Width = 150,
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(10)
            };
            customButton.Click += CreateCustomProfile_Click;
            panel.Children.Add(customButton);

            this.Content = panel;
        }

        /// <summary>
        /// Initialize profile selector with builder service.
        /// </summary>
        public async Task InitializeAsync(IBuilderUIService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            await LoadProfilesAsync();
        }

        /// <summary>
        /// Load all available profiles.
        /// </summary>
        public async Task LoadProfilesAsync()
        {
            try
            {
                _profilesPanel.Children.Clear();
                var profiles = await _service.GetAvailableProfilesAsync();
                var recommendedProfile = await _service.GetRecommendedProfileAsync();

                foreach (var profile in profiles)
                {
                    var profileGroup = new GroupBox
                    {
                        Header = profile.Name + (profile.IsRecommended ? " (Recommended)" : ""),
                        Margin = new Thickness(5),
                        Padding = new Thickness(10),
                        BorderBrush = new System.Windows.Media.SolidColorBrush(
                            profile.IsRecommended ? System.Windows.Media.Colors.LimeGreen : System.Windows.Media.Colors.Gray)
                    };

                    var profileContent = new StackPanel { Orientation = Orientation.Vertical, Spacing = 5 };

                    var radio = new RadioButton
                    {
                        Content = profile.Name,
                        Tag = profile.ProfileId,
                        IsChecked = profile.IsRecommended,
                        Margin = new Thickness(5),
                        FontWeight = profile.IsRecommended ? System.Windows.FontWeights.Bold : System.Windows.FontWeights.Normal
                    };

                    radio.Checked += async (s, e) => await SelectProfileAsync(profile.ProfileId);
                    profileContent.Children.Add(radio);

                    var descBlock = new TextBlock
                    {
                        Text = profile.Description,
                        TextWrapping = System.Windows.TextWrapping.Wrap,
                        Margin = new Thickness(25, 5, 5, 5),
                        FontSize = 11,
                        Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray)
                    };
                    profileContent.Children.Add(descBlock);

                    var optimizationsBlock = new TextBlock
                    {
                        Text = "Optimizations: " + profile.Optimizations,
                        TextWrapping = System.Windows.TextWrapping.Wrap,
                        Margin = new Thickness(25, 5, 5, 10),
                        FontSize = 10,
                        Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.CornflowerBlue)
                    };
                    profileContent.Children.Add(optimizationsBlock);

                    profileGroup.Content = profileContent;
                    _profilesPanel.Children.Add(profileGroup);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading profiles: {ex.Message}", "Error");
            }
        }

        /// <summary>
        /// Select a profile and show preview.
        /// </summary>
        private async Task SelectProfileAsync(string profileId)
        {
            try
            {
                _selectedProfileId = profileId;
                await _service.SelectProfileAsync(profileId);

                // Show preview
                var preview = await _service.GetProfilePreviewAsync(profileId);
                var previewText = "Included Packages:\n";
                previewText += string.Join("\n", preview.Select(p => $"• {p.Name} ({FormatBytes(p.Size)})"));

                _previewText.Text = previewText;
                _previewText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LimeGreen);

                ProfileSelected?.Invoke(this, profileId);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error selecting profile: {ex.Message}", "Error");
            }
        }

        /// <summary>
        /// Create a custom profile.
        /// </summary>
        private async void CreateCustomProfile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var profile = new OptimizationProfile
                {
                    ProfileId = Guid.NewGuid().ToString(),
                    Name = "Custom Profile",
                    Description = "Custom user-defined profile",
                    Optimizations = "Custom optimizations",
                    IsCustom = true
                };

                bool created = await _service.CreateCustomProfileAsync(profile);
                if (created)
                {
                    MessageBox.Show("Custom profile created successfully.", "Success");
                    await LoadProfilesAsync();
                }
                else
                {
                    MessageBox.Show("Failed to create custom profile.", "Error");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating custom profile: {ex.Message}", "Error");
            }
        }

        /// <summary>
        /// Get selected profile ID.
        /// </summary>
        public string GetSelectedProfileId()
        {
            return _selectedProfileId;
        }

        /// <summary>
        /// Format bytes to human-readable size.
        /// </summary>
        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }
    }
}
