using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;

namespace MonadoBlade.GUI.ViewModels
{
    public class DeveloperToolsViewModel : ViewModelBase
    {
        public class ApiRequest { public string Id { get; set; } public string Method { get; set; } public string Endpoint { get; set; } public string Payload { get; set; } public string Response { get; set; } public int StatusCode { get; set; } public double Duration { get; set; } public DateTime Timestamp { get; set; } }
        public class ThemeColor { public string Name { get; set; } public Color Value { get; set; } public string HexValue { get; set; } }
        public class PluginTemplate { public string Id { get; set; } public string Name { get; set; } public string Description { get; set; } public string TargetFramework { get; set; } }
        public class PerformanceProfile { public string FunctionName { get; set; } public long TimeMs { get; set; } public int CallCount { get; set; } public double AverageMs { get; set; } public Color TrendColor { get; set; } }

        private ObservableCollection<ApiRequest> _apiRequests;
        private ObservableCollection<ThemeColor> _themeColors;
        private ObservableCollection<PluginTemplate> _pluginTemplates;
        private ObservableCollection<PerformanceProfile> _performanceProfiles;
        private RelayCommand<ApiRequest> _executeApiCommand;
        private RelayCommand _clearApiCommand;
        private RelayCommand _generatePluginCommand;
        private RelayCommand _exportThemeCommand;
        private RelayCommand _startProfilingCommand;
        private RelayCommand _stopProfilingCommand;
        private string _apiMethod = "GET";
        private string _apiEndpoint = "https://api.example.com/";
        private string _apiPayload;
        private string _selectedTemplateId;
        private string _pluginName;
        private bool _isProfiling;
        private string _statusMessage;
        private Color _primaryColor;
        private Color _secondaryColor;
        private Color _accentColor;

        public DeveloperToolsViewModel()
        {
            _apiRequests = new ObservableCollection<ApiRequest>();
            _themeColors = new ObservableCollection<ThemeColor>();
            _pluginTemplates = new ObservableCollection<PluginTemplate>();
            _performanceProfiles = new ObservableCollection<PerformanceProfile>();
            InitializeDefaults();
        }

        private void InitializeDefaults()
        {
            _themeColors.Add(new ThemeColor { Name = "Primary", Value = Color.FromRgb(0, 217, 255), HexValue = "#00D9FF" });
            _themeColors.Add(new ThemeColor { Name = "Secondary", Value = Color.FromRgb(102, 51, 153), HexValue = "#663399" });
            _themeColors.Add(new ThemeColor { Name = "Accent", Value = Color.FromRgb(255, 107, 107), HexValue = "#FF6B6B" });
            _pluginTemplates.Add(new PluginTemplate { Id = "basic", Name = "Basic Plugin", Description = "Simple plugin with lifecycle hooks", TargetFramework = ".NET 6.0+" });
            _pluginTemplates.Add(new PluginTemplate { Id = "ui-widget", Name = "UI Widget", Description = "Plugin with custom UI components", TargetFramework = ".NET 6.0+" });
            _pluginTemplates.Add(new PluginTemplate { Id = "service", Name = "Background Service", Description = "Long-running background service plugin", TargetFramework = ".NET 6.0+" });
            _primaryColor = Colors.Cyan;
            _secondaryColor = Color.FromRgb(102, 51, 153);
            _accentColor = Colors.Red;
        }

        public ObservableCollection<ApiRequest> ApiRequests { get => _apiRequests; set => SetProperty(ref _apiRequests, value); }
        public ObservableCollection<ThemeColor> ThemeColors { get => _themeColors; set => SetProperty(ref _themeColors, value); }
        public ObservableCollection<PluginTemplate> PluginTemplates { get => _pluginTemplates; set => SetProperty(ref _pluginTemplates, value); }
        public ObservableCollection<PerformanceProfile> PerformanceProfiles { get => _performanceProfiles; set => SetProperty(ref _performanceProfiles, value); }
        public string ApiMethod { get => _apiMethod; set => SetProperty(ref _apiMethod, value); }
        public string ApiEndpoint { get => _apiEndpoint; set => SetProperty(ref _apiEndpoint, value); }
        public string ApiPayload { get => _apiPayload; set => SetProperty(ref _apiPayload, value); }
        public string SelectedTemplateId { get => _selectedTemplateId; set => SetProperty(ref _selectedTemplateId, value); }
        public string PluginName { get => _pluginName; set => SetProperty(ref _pluginName, value); }
        public bool IsProfiling { get => _isProfiling; set => SetProperty(ref _isProfiling, value); }
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
        public Color PrimaryColor { get => _primaryColor; set => SetProperty(ref _primaryColor, value); }
        public Color SecondaryColor { get => _secondaryColor; set => SetProperty(ref _secondaryColor, value); }
        public Color AccentColor { get => _accentColor; set => SetProperty(ref _accentColor, value); }

        public ICommand ExecuteApiCommand => _executeApiCommand ?? (_executeApiCommand = new RelayCommand<ApiRequest>(ExecuteApi, CanExecuteApi));
        public ICommand ClearApiCommand => _clearApiCommand ?? (_clearApiCommand = new RelayCommand(ClearApi, CanClearApi));
        public ICommand GeneratePluginCommand => _generatePluginCommand ?? (_generatePluginCommand = new RelayCommand(GeneratePlugin, CanGeneratePlugin));
        public ICommand ExportThemeCommand => _exportThemeCommand ?? (_exportThemeCommand = new RelayCommand(ExportTheme));
        public ICommand StartProfilingCommand => _startProfilingCommand ?? (_startProfilingCommand = new RelayCommand(StartProfiling, CanStartProfiling));
        public ICommand StopProfilingCommand => _stopProfilingCommand ?? (_stopProfilingCommand = new RelayCommand(StopProfiling, CanStopProfiling));

        private bool CanExecuteApi(ApiRequest req) => !string.IsNullOrEmpty(_apiEndpoint);
        private bool CanClearApi() => _apiRequests.Count > 0;
        private bool CanGeneratePlugin() => !string.IsNullOrEmpty(_pluginName) && !string.IsNullOrEmpty(_selectedTemplateId);
        private bool CanStartProfiling() => !_isProfiling;
        private bool CanStopProfiling() => _isProfiling;

        private void ExecuteApi(ApiRequest req) { var request = new ApiRequest { Id = Guid.NewGuid().ToString(), Method = ApiMethod, Endpoint = ApiEndpoint, Payload = ApiPayload, Timestamp = DateTime.Now, Duration = new Random().Next(10, 500), StatusCode = 200, Response = $\"{{ \\\"success\\\": true, \\\"timestamp\\\": \\\"{DateTime.Now:O}\\\" }}\" }; ApiRequests.Insert(0, request); StatusMessage = $\"API call completed in {request.Duration}ms\"; }
        private void ClearApi() { ApiRequests.Clear(); StatusMessage = \"API request history cleared\"; }
        private void GeneratePlugin() { var template = PluginTemplates.FirstOrDefault(t => t.Id == SelectedTemplateId); if (template != null) StatusMessage = $\"Plugin '{PluginName}' generated successfully\"; }
        private void ExportTheme() { StatusMessage = \"Theme exported to clipboard\"; }
        private void StartProfiling() { IsProfiling = true; PerformanceProfiles.Clear(); PerformanceProfiles.Add(new PerformanceProfile { FunctionName = \"OnRender\", TimeMs = 5, CallCount = 1200, AverageMs = 4.2, TrendColor = Colors.Green }); StatusMessage = \"Performance profiling started\"; }
        private void StopProfiling() { IsProfiling = false; StatusMessage = \"Performance profiling stopped\"; }
    }
}
