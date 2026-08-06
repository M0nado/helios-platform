using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Linq;

namespace HELIOS.Platform.Phase10.BuilderUI
{
    /// <summary>
    /// Main WPF host for the builder UI.
    /// Implements the Xenblade theme and responsive window.
    /// </summary>
    public class BuilderUIHost : Window
    {
        private IBuilderUIService _builderService;
        private BuilderViewModel _viewModel;
        private StepWizardEngine _wizardEngine;
        private bool _shutdownInProgress;
        private bool _shutdownCompleted;

        public BuilderUIHost()
        {
            this.Width = 1280;
            this.Height = 720;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 15, 35));
            this.Title = "HELIOS USB Builder";
            
            _viewModel = new BuilderViewModel();
            this.DataContext = _viewModel;
            InitializeLayout();
            Closing += Window_Closing;
        }

        private void InitializeLayout()
        {
            var root = new Grid { Margin = new Thickness(32) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var title = new TextBlock
            {
                Text = "HELIOS USB Builder",
                FontSize = 30,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 20)
            };
            root.Children.Add(title);

            var status = new StackPanel { Margin = new Thickness(0, 0, 0, 16) };
            Grid.SetRow(status, 1);

            var step = new TextBlock { FontSize = 18, Foreground = Brushes.LightBlue };
            step.SetBinding(TextBlock.TextProperty, new Binding(nameof(BuilderViewModel.CurrentStep))
            {
                StringFormat = "Wizard step {0}"
            });
            status.Children.Add(step);

            var operation = new TextBlock { Foreground = Brushes.White, Margin = new Thickness(0, 6, 0, 0) };
            operation.SetBinding(TextBlock.TextProperty, new Binding(nameof(BuilderViewModel.CurrentOperation))
            {
                TargetNullValue = "Waiting for builder service initialization"
            });
            status.Children.Add(operation);

            var remaining = new TextBlock { Foreground = Brushes.LightGray, Margin = new Thickness(0, 4, 0, 0) };
            remaining.SetBinding(TextBlock.TextProperty, new Binding(nameof(BuilderViewModel.TimeRemaining))
            {
                StringFormat = "Estimated time remaining: {0}"
            });
            status.Children.Add(remaining);
            root.Children.Add(status);

            var progressPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 16) };
            Grid.SetRow(progressPanel, 2);

            var overallProgress = new ProgressBar { Height = 16, Minimum = 0, Maximum = 100 };
            overallProgress.SetBinding(ProgressBar.ValueProperty, nameof(BuilderViewModel.OverallProgress));
            progressPanel.Children.Add(overallProgress);

            var subtaskProgress = new ProgressBar
            {
                Height = 8,
                Minimum = 0,
                Maximum = 100,
                Margin = new Thickness(0, 8, 0, 0)
            };
            subtaskProgress.SetBinding(ProgressBar.ValueProperty, nameof(BuilderViewModel.SubtaskProgress));
            progressPanel.Children.Add(subtaskProgress);
            root.Children.Add(progressPanel);

            var logs = new ListBox
            {
                Background = new SolidColorBrush(Color.FromRgb(24, 24, 48)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(55, 80, 120))
            };
            logs.SetBinding(ItemsControl.ItemsSourceProperty, nameof(BuilderViewModel.Logs));
            Grid.SetRow(logs, 3);
            root.Children.Add(logs);

            var error = new TextBlock
            {
                Foreground = Brushes.OrangeRed,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 12, 0, 0)
            };
            error.SetBinding(TextBlock.TextProperty, nameof(BuilderViewModel.LastError));
            Grid.SetRow(error, 4);
            root.Children.Add(error);

            Content = root;
        }

        /// <summary>
        /// Initialize the builder UI with service and theme.
        /// </summary>
        public async Task InitializeAsync(IBuilderUIService service)
        {
            try
            {
                _builderService = service ?? throw new ArgumentNullException(nameof(service));

                // Initialize service
                bool initialized = await _builderService.InitializeAsync();
                if (!initialized)
                {
                    MessageBox.Show("Failed to initialize builder service", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Initialize wizard engine
                _wizardEngine = new StepWizardEngine(_builderService);
                await _wizardEngine.InitializeAsync();

                // Load Xenblade theme
                ApplyXenbladTheme();

                // Subscribe to events
                _builderService.OnStepChanged += BuilderService_OnStepChanged;
                _builderService.OnProgressUpdated += BuilderService_OnProgressUpdated;
                _builderService.OnError += BuilderService_OnError;
                _builderService.OnDeploymentCompleted += BuilderService_OnDeploymentCompleted;

                // Load initial step
                var currentStep = await _builderService.GetCurrentStepAsync();
                _viewModel.CurrentStep = currentStep?.StepNumber ?? 1;

                this.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Initialization error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Apply Xenblade theme styling.
        /// </summary>
        private void ApplyXenbladTheme()
        {
            var theme = new ResourceDictionary();
            theme.Source = new Uri("pack://application:,,,/HELIOS.Platform;component/Phase10/BuilderUI/Styles/XenbladTheme.xaml");
            this.Resources.MergedDictionaries.Add(theme);
        }

        /// <summary>
        /// Update progress display.
        /// </summary>
        private void UpdateProgress(BuilderProgressUpdate progress)
        {
            _viewModel.OverallProgress = progress.OverallPercentage;
            _viewModel.SubtaskProgress = progress.SubtaskPercentage;
            _viewModel.CurrentOperation = progress.CurrentOperation;
            _viewModel.TimeRemaining = progress.TimeRemaining.ToString(@"hh\:mm\:ss");
        }

        /// <summary>
        /// Handle errors.
        /// </summary>
        private void HandleError(string error)
        {
            _viewModel.LastError = error;
            MessageBox.Show(error, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>
        /// Handle deployment completion.
        /// </summary>
        private void HandleDeploymentCompleted(bool success)
        {
            if (success)
            {
                MessageBox.Show("Deployment completed successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Deployment failed. Check logs for details.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BuilderService_OnStepChanged(object sender, int step)
        {
            _viewModel.CurrentStep = step;
        }

        private void BuilderService_OnProgressUpdated(object sender, BuilderProgressUpdate progress)
        {
            UpdateProgress(progress);
        }

        private void BuilderService_OnError(object sender, string error)
        {
            HandleError(error);
        }

        private void BuilderService_OnDeploymentCompleted(object sender, bool success)
        {
            HandleDeploymentCompleted(success);
        }

        private async void Window_Closing(object sender, CancelEventArgs e)
        {
            if (_shutdownCompleted || _builderService == null)
            {
                UnsubscribeBuilderEvents();
                return;
            }

            e.Cancel = true;
            if (_shutdownInProgress)
            {
                return;
            }

            _shutdownInProgress = true;
            IsEnabled = false;

            try
            {
                await _builderService.ShutdownAsync();
                _shutdownCompleted = true;
                UnsubscribeBuilderEvents();
                Closing -= Window_Closing;
                Close();
            }
            catch (Exception ex)
            {
                _shutdownInProgress = false;
                IsEnabled = true;
                _viewModel.LastError = $"Shutdown failed: {ex.Message}";
                MessageBox.Show(
                    _viewModel.LastError,
                    "Unable to close HELIOS USB Builder",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void UnsubscribeBuilderEvents()
        {
            if (_builderService == null)
            {
                return;
            }

            _builderService.OnStepChanged -= BuilderService_OnStepChanged;
            _builderService.OnProgressUpdated -= BuilderService_OnProgressUpdated;
            _builderService.OnError -= BuilderService_OnError;
            _builderService.OnDeploymentCompleted -= BuilderService_OnDeploymentCompleted;
        }
    }

    /// <summary>
    /// ViewModel for MVVM binding in WPF.
    /// </summary>
    public class BuilderViewModel : INotifyPropertyChanged
    {
        private int _currentStep;
        private int _overallProgress;
        private int _subtaskProgress;
        private string _currentOperation;
        private string _timeRemaining;
        private string _lastError;
        private bool _isDeploying;
        private ObservableCollection<string> _logs;

        public event PropertyChangedEventHandler PropertyChanged;

        public BuilderViewModel()
        {
            _logs = new ObservableCollection<string>();
            _currentStep = 1;
            _overallProgress = 0;
            _subtaskProgress = 0;
            _timeRemaining = "N/A";
        }

        public int CurrentStep
        {
            get => _currentStep;
            set
            {
                if (_currentStep != value)
                {
                    _currentStep = value;
                    OnPropertyChanged(nameof(CurrentStep));
                }
            }
        }

        public int OverallProgress
        {
            get => _overallProgress;
            set
            {
                if (_overallProgress != value)
                {
                    _overallProgress = value;
                    OnPropertyChanged(nameof(OverallProgress));
                }
            }
        }

        public int SubtaskProgress
        {
            get => _subtaskProgress;
            set
            {
                if (_subtaskProgress != value)
                {
                    _subtaskProgress = value;
                    OnPropertyChanged(nameof(SubtaskProgress));
                }
            }
        }

        public string CurrentOperation
        {
            get => _currentOperation;
            set
            {
                if (_currentOperation != value)
                {
                    _currentOperation = value;
                    OnPropertyChanged(nameof(CurrentOperation));
                }
            }
        }

        public string TimeRemaining
        {
            get => _timeRemaining;
            set
            {
                if (_timeRemaining != value)
                {
                    _timeRemaining = value;
                    OnPropertyChanged(nameof(TimeRemaining));
                }
            }
        }

        public string LastError
        {
            get => _lastError;
            set
            {
                if (_lastError != value)
                {
                    _lastError = value;
                    OnPropertyChanged(nameof(LastError));
                }
            }
        }

        public bool IsDeploying
        {
            get => _isDeploying;
            set
            {
                if (_isDeploying != value)
                {
                    _isDeploying = value;
                    OnPropertyChanged(nameof(IsDeploying));
                }
            }
        }

        public ObservableCollection<string> Logs
        {
            get => _logs;
            set
            {
                if (_logs != value)
                {
                    _logs = value;
                    OnPropertyChanged(nameof(Logs));
                }
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }
}
