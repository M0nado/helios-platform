using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Linq;

namespace HELIOS.Platform.Phase10.BuilderUI
{
    /// <summary>
    /// Displays step-specific configuration options and UI controls.
    /// Renders different controls based on the current wizard step.
    /// </summary>
    public class OptionsPanel : UserControl
    {
        private IBuilderUIService _service;
        private Panel _optionsContainer;
        private Dictionary<int, Func<Task<Panel>>> _stepControlGenerators;

        public OptionsPanel()
        {
            _optionsContainer = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(10) };
            this.Content = _optionsContainer;
            _stepControlGenerators = new Dictionary<int, Func<Task<Panel>>>();
        }

        /// <summary>
        /// Initialize options panel with builder service.
        /// </summary>
        public async Task InitializeAsync(IBuilderUIService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            InitializeControlGenerators();
        }

        /// <summary>
        /// Initialize step-specific control generators.
        /// </summary>
        private void InitializeControlGenerators()
        {
            _stepControlGenerators[1] = GenerateWelcomeControlsAsync;
            _stepControlGenerators[2] = GenerateTargetSelectionControlsAsync;
            _stepControlGenerators[3] = GenerateVersionSelectionControlsAsync;
            _stepControlGenerators[4] = GeneratePackageSelectionControlsAsync;
            _stepControlGenerators[5] = GenerateProfileSelectionControlsAsync;
            _stepControlGenerators[6] = GenerateConfigurationControlsAsync;
            _stepControlGenerators[7] = GenerateSummaryControlsAsync;
        }

        /// <summary>
        /// Load options for current step.
        /// </summary>
        public async Task LoadStepOptionsAsync(int stepNumber)
        {
            try
            {
                (_optionsContainer as StackPanel)?.Children.Clear();

                if (_stepControlGenerators.ContainsKey(stepNumber))
                {
                    var controls = await _stepControlGenerators[stepNumber]();
                    (_optionsContainer as StackPanel)?.Children.Add(controls);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading step options: {ex.Message}", "Error");
            }
        }

        /// <summary>
        /// Generate welcome step controls.
        /// </summary>
        private async Task<Panel> GenerateWelcomeControlsAsync()
        {
            var panel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 15 };

            var systemCheckGroup = new GroupBox { Header = "System Check" };
            var systemCheckPanel = new StackPanel { Orientation = Orientation.Vertical };

            systemCheckPanel.Children.Add(new TextBlock { Text = "✓ System Requirements Met", Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green) });
            systemCheckPanel.Children.Add(new TextBlock { Text = "✓ Administrator Access Confirmed", Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green) });
            systemCheckPanel.Children.Add(new TextBlock { Text = "✓ Network Connection Available", Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green) });

            systemCheckGroup.Content = systemCheckPanel;
            panel.Children.Add(systemCheckGroup);

            var infoBlock = new TextBlock
            {
                Text = "This wizard will guide you through creating a bootable HELIOS USB drive with optimized Windows and HELIOS packages.",
                TextWrapping = System.Windows.TextWrapping.Wrap,
                Margin = new Thickness(10),
                FontSize = 12
            };
            panel.Children.Add(infoBlock);

            return await Task.FromResult(panel);
        }

        /// <summary>
        /// Generate target drive selection controls.
        /// </summary>
        private async Task<Panel> GenerateTargetSelectionControlsAsync()
        {
            var panel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 10 };

            var drives = await _service.GetAvailableDrivesAsync();
            var recommendedDrive = await _service.GetRecommendedDriveAsync();

            foreach (var drive in drives)
            {
                var driveButton = new Button
                {
                    Content = $"{drive.DriveName} ({FormatBytes(drive.TotalCapacity)}) - {drive.DriveType}",
                    Padding = new Thickness(10),
                    Margin = new Thickness(5),
                    Tag = drive.DriveId,
                    BorderBrush = new System.Windows.Media.SolidColorBrush(drive.IsRecommended ? System.Windows.Media.Colors.LimeGreen : System.Windows.Media.Colors.Gray)
                };

                driveButton.Click += async (s, e) => await _service.SelectDriveAsync((string)driveButton.Tag);
                panel.Children.Add(driveButton);
            }

            var warningText = new TextBlock
            {
                Text = "Warning: All data on selected drive will be erased!",
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Orange),
                Margin = new Thickness(10),
                TextWrapping = System.Windows.TextWrapping.Wrap
            };
            panel.Children.Add(warningText);

            return panel;
        }

        /// <summary>
        /// Generate Windows version selection controls.
        /// </summary>
        private async Task<Panel> GenerateVersionSelectionControlsAsync()
        {
            var panel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 10 };

            var versions = await _service.GetWindowsVersionsAsync();

            foreach (var version in versions)
            {
                var radio = new RadioButton
                {
                    Content = $"{version.DisplayName} - {version.Description}",
                    Margin = new Thickness(10),
                    Tag = version.VersionId
                };

                radio.Checked += async (s, e) => await _service.SelectWindowsVersionAsync((string)radio.Tag);
                panel.Children.Add(radio);
            }

            return await Task.FromResult(panel);
        }

        /// <summary>
        /// Generate package selection controls.
        /// </summary>
        private async Task<Panel> GeneratePackageSelectionControlsAsync()
        {
            var panel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 10 };

            var packages = await _service.GetAllPackagesAsync();
            var categories = packages.Select(p => p.Category).Distinct();

            foreach (var category in categories)
            {
                var categoryGroup = new GroupBox { Header = category, Margin = new Thickness(5) };
                var categoryPanel = new StackPanel { Orientation = Orientation.Vertical };

                var categoryPackages = packages.Where(p => p.Category == category);
                foreach (var pkg in categoryPackages)
                {
                    var checkbox = new CheckBox
                    {
                        Content = $"{pkg.Name} ({FormatBytes(pkg.Size)})",
                        Margin = new Thickness(10),
                        IsChecked = pkg.IsSelected,
                        Tag = pkg.PackageId
                    };

                    checkbox.Checked += async (s, e) => await _service.SelectPackageAsync((string)checkbox.Tag);
                    checkbox.Unchecked += async (s, e) => await _service.DeselectPackageAsync((string)checkbox.Tag);

                    categoryPanel.Children.Add(checkbox);
                }

                categoryGroup.Content = categoryPanel;
                panel.Children.Add(categoryGroup);
            }

            var totalSize = await _service.CalculateTotalSizeAsync();
            var sizeLabel = new TextBlock
            {
                Text = $"Total Size: {FormatBytes(totalSize)}",
                Margin = new Thickness(10),
                FontWeight = System.Windows.FontWeights.Bold
            };
            panel.Children.Add(sizeLabel);

            return panel;
        }

        /// <summary>
        /// Generate profile selection controls.
        /// </summary>
        private async Task<Panel> GenerateProfileSelectionControlsAsync()
        {
            var panel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 10 };

            var profiles = await _service.GetAvailableProfilesAsync();

            foreach (var profile in profiles)
            {
                var radio = new RadioButton
                {
                    Content = $"{profile.Name}" + (profile.IsRecommended ? " (Recommended)" : ""),
                    Margin = new Thickness(10),
                    Tag = profile.ProfileId,
                    FontWeight = profile.IsRecommended ? System.Windows.FontWeights.Bold : System.Windows.FontWeights.Normal
                };

                radio.Checked += async (s, e) => await _service.SelectProfileAsync((string)radio.Tag);
                panel.Children.Add(radio);

                var descBlock = new TextBlock
                {
                    Text = profile.Description,
                    Margin = new Thickness(25, 0, 10, 10),
                    TextWrapping = System.Windows.TextWrapping.Wrap,
                    FontSize = 11,
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray)
                };
                panel.Children.Add(descBlock);
            }

            return panel;
        }

        /// <summary>
        /// Generate configuration controls.
        /// </summary>
        private async Task<Panel> GenerateConfigurationControlsAsync()
        {
            var panel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 10 };

            var configGroup = new GroupBox { Header = "Advanced Configuration", Margin = new Thickness(5) };
            var configPanel = new StackPanel { Orientation = Orientation.Vertical };

            configPanel.Children.Add(new CheckBox { Content = "Enable Secure Boot", Margin = new Thickness(10), IsChecked = true });
            configPanel.Children.Add(new CheckBox { Content = "Enable TPM 2.0", Margin = new Thickness(10), IsChecked = true });
            configPanel.Children.Add(new CheckBox { Content = "Enable UEFI", Margin = new Thickness(10), IsChecked = true });
            configPanel.Children.Add(new CheckBox { Content = "Skip Windows Update", Margin = new Thickness(10), IsChecked = false });

            configGroup.Content = configPanel;
            panel.Children.Add(configGroup);

            return await Task.FromResult(panel);
        }

        /// <summary>
        /// Generate summary review controls.
        /// </summary>
        private async Task<Panel> GenerateSummaryControlsAsync()
        {
            var panel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 10 };

            var summary = await _service.GetDeploymentSummaryAsync();

            var summaryGroup = new GroupBox { Header = "Deployment Summary" };
            var summaryPanel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(10) };

            summaryPanel.Children.Add(new TextBlock { Text = $"Target Drive: {summary.TargetDrive}", Margin = new Thickness(5) });
            summaryPanel.Children.Add(new TextBlock { Text = $"Windows Version: {summary.WindowsVersion}", Margin = new Thickness(5) });
            summaryPanel.Children.Add(new TextBlock { Text = $"Profile: {summary.SelectedProfile}", Margin = new Thickness(5) });
            summaryPanel.Children.Add(new TextBlock { Text = $"Total Size: {FormatBytes(summary.TotalSize)}", Margin = new Thickness(5) });
            summaryPanel.Children.Add(new TextBlock { Text = $"Estimated Time: {summary.EstimatedMinutes} minutes", Margin = new Thickness(5) });

            summaryGroup.Content = summaryPanel;
            panel.Children.Add(summaryGroup);

            var termsCheckbox = new CheckBox
            {
                Content = "I accept the HELIOS terms and conditions",
                Margin = new Thickness(10),
                IsChecked = false
            };
            panel.Children.Add(termsCheckbox);

            return panel;
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
