using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace HELIOS.Platform.Phase10.BuilderUI
{
    /// <summary>
    /// Drive selection component for step 2 of the wizard.
    /// Lists USB drives and local disks with capacity information.
    /// </summary>
    public class DriveSelector : UserControl
    {
        private IBuilderUIService _service;
        private ListBox _driveListBox;
        private TextBlock _selectedDriveInfo;
        private TextBlock _warningText;
        private string _selectedDriveId;

        public event EventHandler<string> DriveSelected;

        public DriveSelector()
        {
            InitializeUI();
        }

        /// <summary>
        /// Initialize the drive selector UI.
        /// </summary>
        private void InitializeUI()
        {
            var panel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(10) };

            // Title
            panel.Children.Add(new TextBlock { Text = "Available Drives", FontWeight = System.Windows.FontWeights.Bold, FontSize = 14 });

            // Drive list
            _driveListBox = new ListBox { Height = 200, Margin = new Thickness(0, 10, 0, 10) };
            _driveListBox.SelectionChanged += DriveListBox_SelectionChanged;
            panel.Children.Add(_driveListBox);

            // Selected drive info
            _selectedDriveInfo = new TextBlock
            {
                Text = "No drive selected",
                Margin = new Thickness(10),
                TextWrapping = System.Windows.TextWrapping.Wrap,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.CornflowerBlue)
            };
            panel.Children.Add(_selectedDriveInfo);

            // Health check button
            var healthButton = new Button
            {
                Content = "Check Drive Health",
                Width = 150,
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(10)
            };
            healthButton.Click += async (s, e) => await CheckDriveHealthAsync();
            panel.Children.Add(healthButton);

            // Warning text
            _warningText = new TextBlock
            {
                Text = "",
                Margin = new Thickness(10),
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Orange),
                TextWrapping = System.Windows.TextWrapping.Wrap,
                FontSize = 11
            };
            panel.Children.Add(_warningText);

            this.Content = panel;
        }

        /// <summary>
        /// Initialize drive selector with builder service.
        /// </summary>
        public async Task InitializeAsync(IBuilderUIService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            await LoadDrivesAsync();
        }

        /// <summary>
        /// Load available drives.
        /// </summary>
        public async Task LoadDrivesAsync()
        {
            try
            {
                _driveListBox.Items.Clear();
                var drives = await _service.GetAvailableDrivesAsync();
                var recommendedDrive = await _service.GetRecommendedDriveAsync();

                if (!drives.Any())
                {
                    _driveListBox.Items.Add(new TextBlock { Text = "No drives available" });
                    return;
                }

                foreach (var drive in drives)
                {
                    var displayText = $"{drive.DriveName} ({FormatBytes(drive.TotalCapacity)}) - " +
                                    $"Free: {FormatBytes(drive.FreeSpace)} - {drive.DriveType}";

                    if (drive.IsRecommended)
                    {
                        displayText += " [RECOMMENDED]";
                    }

                    var item = new ListBoxItem
                    {
                        Content = displayText,
                        Tag = drive.DriveId,
                        Padding = new Thickness(10, 5, 10, 5),
                        Margin = new Thickness(5)
                    };

                    // Highlight recommended drive
                    if (drive.IsRecommended)
                    {
                        item.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 50, 205, 50));
                    }

                    // Mark unhealthy drives
                    if (!drive.IsHealthy)
                    {
                        item.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
                    }

                    _driveListBox.Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading drives: {ex.Message}", "Error");
            }
        }

        /// <summary>
        /// Handle drive list selection changed.
        /// </summary>
        private async void DriveListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_driveListBox.SelectedItem is ListBoxItem selectedItem)
            {
                _selectedDriveId = (string)selectedItem.Tag;
                
                // Select drive in service
                await _service.SelectDriveAsync(_selectedDriveId);

                // Update info
                var drives = await _service.GetAvailableDrivesAsync();
                var selectedDrive = drives.FirstOrDefault(d => d.DriveId == _selectedDriveId);

                if (selectedDrive != null)
                {
                    _selectedDriveInfo.Text = $"Selected: {selectedDrive.DriveName}\n" +
                                            $"Capacity: {FormatBytes(selectedDrive.TotalCapacity)}\n" +
                                            $"Free Space: {FormatBytes(selectedDrive.FreeSpace)}\n" +
                                            $"Type: {selectedDrive.DriveType}\n" +
                                            $"Health: {(selectedDrive.IsHealthy ? "Good" : "Poor")}";

                    if (!selectedDrive.IsHealthy && selectedDrive.HealthWarnings.Any())
                    {
                        _warningText.Text = "⚠ Drive Health Issues:\n" + string.Join("\n", selectedDrive.HealthWarnings);
                    }
                    else
                    {
                        _warningText.Text = "";
                    }

                    DriveSelected?.Invoke(this, _selectedDriveId);
                }
            }
        }

        /// <summary>
        /// Check health of selected drive.
        /// </summary>
        private async Task CheckDriveHealthAsync()
        {
            if (string.IsNullOrEmpty(_selectedDriveId))
            {
                MessageBox.Show("Please select a drive first.", "Information");
                return;
            }

            try
            {
                bool isHealthy = await _service.CheckDriveHealthAsync(_selectedDriveId);
                if (isHealthy)
                {
                    MessageBox.Show("Drive health check passed.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Drive health check failed. Consider using a different drive.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                // Reload drives to update health status
                await LoadDrivesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error checking drive health: {ex.Message}", "Error");
            }
        }

        /// <summary>
        /// Get selected drive ID.
        /// </summary>
        public string GetSelectedDriveId()
        {
            return _selectedDriveId;
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
