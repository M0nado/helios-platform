using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace MonadoBlade.GUI.ViewModels
{
    /// <summary>
    /// Dashboard ViewModel - Provides data binding for dashboard UI
    /// Implements INotifyPropertyChanged for WPF binding support
    /// </summary>
    public class DashboardViewModel : INotifyPropertyChanged
    {
        private DispatcherTimer _updateTimer;
        
        // Observable collections for binding
        public ObservableCollection<MetricSnapshot> SystemMetrics { get; private set; }
        public ObservableCollection<AlertItem> ActiveAlerts { get; private set; }
        public ObservableCollection<ProcessInfo> RunningProcesses { get; private set; }

        // Bindable properties
        private double _cpuPercentage;
        public double CpuPercentage
        {
            get => _cpuPercentage;
            set
            {
                if (_cpuPercentage != value)
                {
                    _cpuPercentage = value;
                    OnPropertyChanged();
                }
            }
        }

        private double _memoryPercentage;
        public double MemoryPercentage
        {
            get => _memoryPercentage;
            set
            {
                if (_memoryPercentage != value)
                {
                    _memoryPercentage = value;
                    OnPropertyChanged();
                }
            }
        }

        private double _diskPercentage;
        public double DiskPercentage
        {
            get => _diskPercentage;
            set
            {
                if (_diskPercentage != value)
                {
                    _diskPercentage = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _systemStatus;
        public string SystemStatus
        {
            get => _systemStatus;
            set
            {
                if (_systemStatus != value)
                {
                    _systemStatus = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public DashboardViewModel()
        {
            // Initialize collections
            SystemMetrics = new ObservableCollection<MetricSnapshot>();
            ActiveAlerts = new ObservableCollection<AlertItem>();
            RunningProcesses = new ObservableCollection<ProcessInfo>();

            // Initialize default values
            SystemStatus = "Initializing...";
            IsLoading = false;

            // Setup update timer (1 second refresh)
            _updateTimer = new DispatcherTimer();
            _updateTimer.Interval = TimeSpan.FromSeconds(1);
            _updateTimer.Tick += (s, e) => UpdateMetrics();
        }

        /// <summary>
        /// Start monitoring system metrics
        /// </summary>
        public void StartMonitoring()
        {
            IsLoading = true;
            _updateTimer.Start();
            UpdateMetrics();
        }

        /// <summary>
        /// Stop monitoring system metrics
        /// </summary>
        public void StopMonitoring()
        {
            _updateTimer.Stop();
            IsLoading = false;
        }

        /// <summary>
        /// Update all metrics from dashboard manager
        /// </summary>
        private void UpdateMetrics()
        {
            try
            {
                // Simulate metric updates
                CpuPercentage = new Random().Next(10, 80);
                MemoryPercentage = new Random().Next(30, 75);
                DiskPercentage = new Random().Next(40, 85);

                // Update system status
                SystemStatus = CpuPercentage > 90 ? "High Load" : "System Healthy";

                // Add sample metrics
                if (SystemMetrics.Count < 5)
                {
                    SystemMetrics.Add(new MetricSnapshot
                    {
                        Name = $"Metric {SystemMetrics.Count + 1}",
                        Value = CpuPercentage,
                        Unit = "%",
                        Timestamp = DateTime.Now,
                        Status = CpuPercentage > 80 ? "Warning" : "Healthy"
                    });
                }
            }
            catch (Exception ex)
            {
                SystemStatus = $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Efficiently update observable collection
        /// Prevents unnecessary UI updates
        /// </summary>
        private void UpdateCollection<T>(ObservableCollection<T> target, System.Collections.Generic.List<T> source)
        {
            target.Clear();
            foreach (var item in source)
            {
                target.Add(item);
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void Dispose()
        {
            _updateTimer?.Stop();
            _updateTimer = null;
        }
    }

    /// <summary>
    /// Represents a single metric snapshot
    /// </summary>
    public class MetricSnapshot
    {
        public string Name { get; set; }
        public double Value { get; set; }
        public string Unit { get; set; }
        public DateTime Timestamp { get; set; }
        public string Status { get; set; } // Healthy, Warning, Critical
    }

    /// <summary>
    /// Represents a system alert
    /// </summary>
    public class AlertItem
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public string Severity { get; set; } // Info, Warning, Error, Critical
        public DateTime Timestamp { get; set; }
        public string Source { get; set; }
    }

    /// <summary>
    /// Represents a running process
    /// </summary>
    public class ProcessInfo
    {
        public string Name { get; set; }
        public int ProcessId { get; set; }
        public double CpuUsage { get; set; }
        public long MemoryUsage { get; set; }
        public string State { get; set; }
        public int ThreadCount { get; set; }
    }
}
