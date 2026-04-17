using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using HELIOS.Platform.Core;
using HELIOS.Platform.BackendServices.ServerManagement;

namespace HELIOS.Platform.Presentation.Pages
{
    public sealed partial class DashboardPage : Page
    {
        private DispatcherTimer _updateTimer;

        public DashboardPage()
        {
            this.InitializeComponent();
            SetupUpdateTimer();
            LoadSystemMetrics();
        }

        private void SetupUpdateTimer()
        {
            _updateTimer = new DispatcherTimer();
            _updateTimer.Interval = TimeSpan.FromSeconds(3);
            _updateTimer.Tick += async (s, e) => await LoadSystemMetrics();
            _updateTimer.Start();
        }

        private async Task LoadSystemMetrics()
        {
            try
            {
                var orchestrator = ServiceContainer.Instance.GetService<IServiceOrchestrator>();
                if (orchestrator == null) return;

                var resources = await orchestrator.GetSystemResourcesAsync();

                // Update UI on main thread
                DispatcherQueue.TryEnqueue(() =>
                {
                    CpuValue.Text = $"{resources.CpuUsagePercent:F1}%";
                    CpuProgress.Value = resources.CpuUsagePercent;

                    MemoryValue.Text = $"{resources.MemoryUsageMB:N0} MB";
                    MemoryProgress.Value = resources.MemoryUsageMB;

                    DiskValue.Text = $"{resources.DiskUsagePercent}%";
                    DiskProgress.Value = resources.DiskUsagePercent;

                    ServiceCount.Text = $"{resources.ActiveServices} services running";
                    UptimeValue.Text = FormatUptime(resources.SystemUptimeSeconds);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading metrics: {ex.Message}");
            }
        }

        private string FormatUptime(long seconds)
        {
            var timespan = TimeSpan.FromSeconds(seconds);
            if (timespan.Days > 0)
                return $"{timespan.Days}d {timespan.Hours}h";
            else if (timespan.Hours > 0)
                return $"{timespan.Hours}h {timespan.Minutes}m";
            else
                return $"{timespan.Minutes}m";
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadSystemMetrics();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _updateTimer?.Stop();
            _updateTimer = null;
        }
    }
}
