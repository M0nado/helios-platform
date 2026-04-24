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
    /// Main WPF host for the builder UI.
    /// Implements the Xenblade theme and responsive window.
    /// </summary>
    public partial class BuilderUIHost : Window
    {
        private IBuilderUIService _builderService;
        private BuilderViewModel _viewModel;
        private StepWizardEngine _wizardEngine;

        public BuilderUIHost()
        {
            InitializeComponent();
            this.Width = 1280;
            this.Height = 720;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 15, 35));
            this.Title = "HELIOS USB Builder";
            
            _viewModel = new BuilderViewModel();
            this.DataContext = _viewModel;
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
                _builderService.OnStepChanged += (s, step) => _viewModel.CurrentStep = step;
                _builderService.OnProgressUpdated += (s, progress) => UpdateProgress(progress);
                _builderService.OnError += (s, error) => HandleError(error);
                _builderService.OnDeploymentCompleted += (s, success) => HandleDeploymentCompleted(success);

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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _builderService?.ShutdownAsync().ConfigureAwait(false);
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
