using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace HELIOS.Platform.Phase10.BuilderUI
{
    /// <summary>
    /// Real-time progress monitoring and display.
    /// Shows overall progress, subtask progress, time estimates, and error alerts.
    /// </summary>
    public class ProgressMonitor : UserControl
    {
        private IBuilderUIService _service;
        private ProgressBar _overallProgressBar;
        private ProgressBar _subtaskProgressBar;
        private TextBlock _currentOperationText;
        private TextBlock _timeRemainingText;
        private TextBlock _percentageText;
        private Button _pauseResumeButton;
        private Button _cancelButton;
        private TextBlock _errorAlertText;
        private ScrollViewer _logViewer;
        private bool _isPaused;

        public ProgressMonitor()
        {
            InitializeUI();
        }

        /// <summary>
        /// Initialize the progress monitor UI.
        /// </summary>
        private void InitializeUI()
        {
            var panel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(10), Spacing = 10 };

            // Overall progress section
            panel.Children.Add(new TextBlock { Text = "Overall Progress", FontWeight = System.Windows.FontWeights.Bold, FontSize = 14 });
            
            _percentageText = new TextBlock { Text = "0%", HorizontalAlignment = HorizontalAlignment.Right, Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LimeGreen) };
            panel.Children.Add(_percentageText);

            _overallProgressBar = new ProgressBar { Height = 30, Maximum = 100, Minimum = 0, Value = 0 };
            panel.Children.Add(_overallProgressBar);

            // Subtask progress section
            panel.Children.Add(new TextBlock { Text = "Current Task", FontWeight = System.Windows.FontWeights.Bold, FontSize = 12, Margin = new Thickness(0, 10, 0, 0) });
            
            _subtaskProgressBar = new ProgressBar { Height = 20, Maximum = 100, Minimum = 0, Value = 0 };
            panel.Children.Add(_subtaskProgressBar);

            // Current operation text
            _currentOperationText = new TextBlock
            {
                Text = "Waiting to start...",
                Margin = new Thickness(0, 10, 0, 0),
                TextWrapping = System.Windows.TextWrapping.Wrap,
                FontSize = 12
            };
            panel.Children.Add(_currentOperationText);

            // Time remaining
            _timeRemainingText = new TextBlock
            {
                Text = "Time Remaining: N/A",
                Margin = new Thickness(0, 5, 0, 0),
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.CornflowerBlue)
            };
            panel.Children.Add(_timeRemainingText);

            // Control buttons
            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Margin = new Thickness(0, 10, 0, 0) };
            
            _pauseResumeButton = new Button { Content = "Pause", Width = 100, Padding = new Thickness(10, 5, 10, 5) };
            _pauseResumeButton.Click += PauseResumeButton_Click;
            buttonPanel.Children.Add(_pauseResumeButton);

            _cancelButton = new Button { Content = "Cancel", Width = 100, Padding = new Thickness(10, 5, 10, 5) };
            _cancelButton.Click += CancelButton_Click;
            buttonPanel.Children.Add(_cancelButton);

            panel.Children.Add(buttonPanel);

            // Error alert
            _errorAlertText = new TextBlock
            {
                Text = "",
                Margin = new Thickness(0, 10, 0, 0),
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red),
                TextWrapping = System.Windows.TextWrapping.Wrap,
                FontSize = 11,
                FontWeight = System.Windows.FontWeights.Bold
            };
            panel.Children.Add(_errorAlertText);

            // Log viewer
            panel.Children.Add(new TextBlock { Text = "Activity Log", FontWeight = System.Windows.FontWeights.Bold, FontSize = 12, Margin = new Thickness(0, 10, 0, 0) });
            
            _logViewer = new ScrollViewer { Height = 150, Margin = new Thickness(0, 5, 0, 0) };
            var logPanel = new StackPanel { Orientation = Orientation.Vertical };
            _logViewer.Content = logPanel;
            panel.Children.Add(_logViewer);

            this.Content = panel;
        }

        /// <summary>
        /// Initialize progress monitor with builder service.
        /// </summary>
        public async Task InitializeAsync(IBuilderUIService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));

            // Subscribe to progress updates
            _service.OnProgressUpdated += async (s, progress) => await UpdateProgressAsync(progress);
            _service.OnError += (s, error) => ShowError(error);

            await Task.CompletedTask;
        }

        /// <summary>
        /// Update progress display.
        /// </summary>
        private async Task UpdateProgressAsync(BuilderProgressUpdate progress)
        {
            _overallProgressBar.Value = progress.OverallPercentage;
            _subtaskProgressBar.Value = progress.SubtaskPercentage;
            _percentageText.Text = $"{progress.OverallPercentage}%";
            _currentOperationText.Text = progress.CurrentOperation;
            _timeRemainingText.Text = $"Time Remaining: {progress.TimeRemaining:hh\\:mm\\:ss}";

            if (!string.IsNullOrEmpty(progress.ErrorMessage))
            {
                ShowError(progress.ErrorMessage);
            }

            AddLogEntry($"[{progress.Timestamp:HH:mm:ss}] {progress.CurrentOperation} - {progress.OverallPercentage}%");

            await Task.CompletedTask;
        }

        /// <summary>
        /// Show error alert.
        /// </summary>
        private void ShowError(string error)
        {
            _errorAlertText.Text = $"Error: {error}";
            AddLogEntry($"[ERROR] {error}");
        }

        /// <summary>
        /// Add entry to activity log.
        /// </summary>
        private void AddLogEntry(string entry)
        {
            var logPanel = _logViewer.Content as StackPanel;
            var textBlock = new TextBlock
            {
                Text = entry,
                Margin = new Thickness(5),
                FontSize = 11,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGray)
            };
            logPanel?.Children.Add(textBlock);

            // Keep only last 100 entries
            if (logPanel?.Children.Count > 100)
            {
                logPanel.Children.RemoveAt(0);
            }

            // Auto-scroll to bottom
            _logViewer.ScrollToEnd();
        }

        /// <summary>
        /// Handle pause/resume button click.
        /// </summary>
        private async void PauseResumeButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isPaused)
            {
                await _service.ResumeDeploymentAsync();
                _pauseResumeButton.Content = "Pause";
                _isPaused = false;
            }
            else
            {
                await _service.PauseDeploymentAsync();
                _pauseResumeButton.Content = "Resume";
                _isPaused = true;
            }
        }

        /// <summary>
        /// Handle cancel button click.
        /// </summary>
        private async void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to cancel deployment?", "Confirm Cancel", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                await _service.CancelDeploymentAsync();
            }
        }

        /// <summary>
        /// Get current progress.
        /// </summary>
        public async Task<BuilderProgressUpdate> GetCurrentProgressAsync()
        {
            return await _service.GetProgressAsync();
        }

        /// <summary>
        /// Reset progress monitor.
        /// </summary>
        public void Reset()
        {
            _overallProgressBar.Value = 0;
            _subtaskProgressBar.Value = 0;
            _percentageText.Text = "0%";
            _currentOperationText.Text = "Waiting to start...";
            _timeRemainingText.Text = "Time Remaining: N/A";
            _errorAlertText.Text = "";
            _pauseResumeButton.Content = "Pause";
            _isPaused = false;

            var logPanel = _logViewer.Content as StackPanel;
            logPanel?.Children.Clear();
        }
    }
}
