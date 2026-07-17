using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace HELIOS.Platform.Phase10.BuilderUI
{
    /// <summary>
    /// Final review and confirmation component for step 7 of the wizard.
    /// Displays all selections, summary information, and handles deployment.
    /// </summary>
    public class SummaryReview : UserControl
    {
        private IBuilderUIService _service;
        private TextBlock _summaryText;
        private CheckBox _termsCheckbox;
        private Button _deployButton;
        private TextBlock _logLinkText;

        public event EventHandler DeploymentStarted;

        public SummaryReview()
        {
            InitializeUI();
        }

        /// <summary>
        /// Initialize the summary review UI.
        /// </summary>
        private void InitializeUI()
        {
            var panel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(10) };

            // Title
            panel.Children.Add(new TextBlock { Text = "Review & Create", FontWeight = System.Windows.FontWeights.Bold, FontSize = 14 });

            // Summary section
            panel.Children.Add(new TextBlock { Text = "Deployment Summary", FontWeight = System.Windows.FontWeights.Bold, FontSize = 12, Margin = new Thickness(0, 10, 0, 5) });

            var scrollViewer = new ScrollViewer { Height = 250, Margin = new Thickness(0, 5, 0, 10) };
            _summaryText = new TextBlock
            {
                Text = "Loading summary...",
                TextWrapping = System.Windows.TextWrapping.Wrap,
                Margin = new Thickness(10),
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.CornflowerBlue)
            };
            scrollViewer.Content = _summaryText;
            panel.Children.Add(scrollViewer);

            // Terms checkbox
            _termsCheckbox = new CheckBox
            {
                Content = new TextBlock
                {
                    Text = "I accept the HELIOS Terms and Conditions and acknowledge that all data on the selected drive will be erased.",
                    TextWrapping = System.Windows.TextWrapping.Wrap
                },
                Margin = new Thickness(10),
                IsChecked = false
            };
            panel.Children.Add(_termsCheckbox);

            // Deploy button
            _deployButton = new Button
            {
                Content = "🚀 Deploy HELIOS",
                Padding = new Thickness(15, 10, 15, 10),
                Margin = new Thickness(10),
                FontSize = 14,
                FontWeight = System.Windows.FontWeights.Bold,
                IsEnabled = false,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.DarkGreen),
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White)
            };
            _deployButton.Click += DeployButton_Click;
            panel.Children.Add(_deployButton);

            // Terms checkbox event handler
            _termsCheckbox.Checked += (s, e) => _deployButton.IsEnabled = _termsCheckbox.IsChecked ?? false;
            _termsCheckbox.Unchecked += (s, e) => _deployButton.IsEnabled = false;

            // Log link
            _logLinkText = new TextBlock
            {
                Text = "After deployment completes, detailed logs will be available.",
                Margin = new Thickness(10),
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray),
                FontSize = 10
            };
            panel.Children.Add(_logLinkText);

            this.Content = panel;
        }

        /// <summary>
        /// Initialize summary review with builder service.
        /// </summary>
        public async Task InitializeAsync(IBuilderUIService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            await LoadSummaryAsync();
        }

        /// <summary>
        /// Load and display deployment summary.
        /// </summary>
        public async Task LoadSummaryAsync()
        {
            try
            {
                var summary = await _service.GetDeploymentSummaryAsync();

                var summaryLines = new List<string>
                {
                    "Target Drive:",
                    $"  • {summary.TargetDrive}",
                    "",
                    "Windows Version:",
                    $"  • {summary.WindowsVersion}",
                    "",
                    "Optimization Profile:",
                    $"  • {summary.SelectedProfile}",
                    "",
                    "Selected Packages:",
                };

                foreach (var pkg in summary.SelectedPackages)
                {
                    summaryLines.Add($"  • {pkg.Name} ({FormatBytes(pkg.Size)})");
                }

                summaryLines.AddRange(new[]
                {
                    "",
                    "Summary:",
                    $"  • Total Size: {FormatBytes(summary.TotalSize)}",
                    $"  • Estimated Time: {summary.EstimatedMinutes} minutes",
                    $"  • Created At: {summary.CreatedAt:yyyy-MM-dd HH:mm:ss}"
                });

                _summaryText.Text = string.Join("\n", summaryLines);
            }
            catch (Exception ex)
            {
                _summaryText.Text = $"Error loading summary: {ex.Message}";
                _summaryText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
            }
        }

        /// <summary>
        /// Handle deploy button click.
        /// </summary>
        private async void DeployButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!(_termsCheckbox.IsChecked ?? false))
                {
                    MessageBox.Show("Please accept the terms and conditions to proceed.", "Confirmation Required");
                    return;
                }

                // Accept terms
                await _service.AcceptTermsAsync();

                // Show final confirmation
                var result = MessageBox.Show(
                    "This will erase all data on the selected drive and create a bootable HELIOS USB.\n\nDo you want to proceed?",
                    "Final Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

                if (result == MessageBoxResult.Yes)
                {
                    _deployButton.IsEnabled = false;
                    _deployButton.Content = "Deploying...";

                    bool success = await _service.StartDeploymentAsync();

                    if (success)
                    {
                        MessageBox.Show("Deployment started successfully! Do not disconnect the drive.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        DeploymentStarted?.Invoke(this, EventArgs.Empty);
                    }
                    else
                    {
                        MessageBox.Show("Failed to start deployment.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        _deployButton.Content = "🚀 Deploy HELIOS";
                        _deployButton.IsEnabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during deployment: {ex.Message}", "Error");
                _deployButton.Content = "🚀 Deploy HELIOS";
                _deployButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// Get whether terms are accepted.
        /// </summary>
        public bool AreTermsAccepted()
        {
            return _termsCheckbox.IsChecked ?? false;
        }

        /// <summary>
        /// Set log link information.
        /// </summary>
        public void SetLogLink(string logPath)
        {
            _logLinkText.Text = $"Logs available at: {logPath}";
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
