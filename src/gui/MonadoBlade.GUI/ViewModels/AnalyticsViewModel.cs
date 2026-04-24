using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;

namespace MonadoBlade.GUI.ViewModels
{
    public class AnalyticsViewModel : ViewModelBase
    {
        public class MetricDataPoint
        {
            public DateTime Timestamp { get; set; }
            public double CpuValue { get; set; }
            public double MemoryValue { get; set; }
            public double DiskValue { get; set; }
        }

        public class ProcessInfo
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public double MemoryMB { get; set; }
            public double CpuPercent { get; set; }
            public int ThreadCount { get; set; }
            public string Status { get; set; }
            public Color StatusColor { get; set; }
        }

        public class SystemHealthStatus
        {
            public string Category { get; set; }
            public string Status { get; set; }
            public Color StatusColor { get; set; }
            public string Details { get; set; }
            public double HealthScore { get; set; }
        }

        public class LogEntry
        {
            public DateTime Timestamp { get; set; }
            public string Level { get; set; }
            public string Source { get; set; }
            public string Message { get; set; }
            public Color LevelColor { get; set; }
        }

        private ObservableCollection<MetricDataPoint> _metricsHistory;
        private ObservableCollection<ProcessInfo> _processList;
        private ObservableCollection<SystemHealthStatus> _healthStatuses;
        private ObservableCollection<LogEntry> _logEntries;
        private RelayCommand _refreshCommand;
        private RelayCommand<ProcessInfo> _terminateProcessCommand;
        private RelayCommand _clearLogsCommand;
        private RelayCommand<string> _filterLogsCommand;
        private bool _isRefreshing;
        private string _selectedLogLevel;
        private double _averageCpu;
        private double _averageMemory;
        private double _peakCpu;
        private double _peakMemory;

        public AnalyticsViewModel()
        {
            _metricsHistory = new ObservableCollection<MetricDataPoint>();
            _processList = new ObservableCollection<ProcessInfo>();
            _healthStatuses = new ObservableCollection<SystemHealthStatus>();
            _logEntries = new ObservableCollection<LogEntry>();
            _selectedLogLevel = "All";

            InitializeData();
            LoadProcesses();
            UpdateHealthStatus();
        }

        private void InitializeData()
        {
            for (int i = 0; i < 60; i++)
            {
                _metricsHistory.Add(new MetricDataPoint
                {
                    Timestamp = DateTime.Now.AddMinutes(-i),
                    CpuValue = Math.Sin(i * 0.1) * 30 + 40,
                    MemoryValue = Math.Cos(i * 0.08) * 20 + 50,
                    DiskValue = 65
                });
            }
        }

        public ObservableCollection<MetricDataPoint> MetricsHistory { get => _metricsHistory; set => SetProperty(ref _metricsHistory, value); }
        public ObservableCollection<ProcessInfo> ProcessList { get => _processList; set => SetProperty(ref _processList, value); }
        public ObservableCollection<SystemHealthStatus> HealthStatuses { get => _healthStatuses; set => SetProperty(ref _healthStatuses, value); }
        public ObservableCollection<LogEntry> LogEntries { get => _logEntries; set => SetProperty(ref _logEntries, value); }
        public bool IsRefreshing { get => _isRefreshing; set => SetProperty(ref _isRefreshing, value); }
        public string SelectedLogLevel { get => _selectedLogLevel; set => SetProperty(ref _selectedLogLevel, value); }
        public double AverageCpu { get => _averageCpu; set => SetProperty(ref _averageCpu, value); }
        public double AverageMemory { get => _averageMemory; set => SetProperty(ref _averageMemory, value); }
        public double PeakCpu { get => _peakCpu; set => SetProperty(ref _peakCpu, value); }
        public double PeakMemory { get => _peakMemory; set => SetProperty(ref _peakMemory, value); }

        public ICommand RefreshCommand => _refreshCommand ?? (_refreshCommand = new RelayCommand(Refresh, CanRefresh));
        public ICommand TerminateProcessCommand => _terminateProcessCommand ?? (_terminateProcessCommand = new RelayCommand<ProcessInfo>(TerminateProcess, CanTerminate));
        public ICommand ClearLogsCommand => _clearLogsCommand ?? (_clearLogsCommand = new RelayCommand(ClearLogs, CanClearLogs));
        public ICommand FilterLogsCommand => _filterLogsCommand ?? (_filterLogsCommand = new RelayCommand<string>(FilterLogs, CanFilter));

        private bool CanRefresh() => !IsRefreshing;
        private bool CanTerminate(ProcessInfo proc) => proc != null && !IsRefreshing;
        private bool CanClearLogs() => _logEntries.Count > 0;
        private bool CanFilter(string filter) => !IsRefreshing;

        private void Refresh()
        {
            IsRefreshing = true;
            try
            {
                LoadProcesses();
                UpdateHealthStatus();
            }
            finally { IsRefreshing = false; }
        }

        private void LoadProcesses()
        {
            ProcessList.Clear();
            var processes = Process.GetProcesses().OrderByDescending(p => p.WorkingSet64).Take(20);
            foreach (var proc in processes)
            {
                try
                {
                    ProcessList.Add(new ProcessInfo
                    {
                        Id = proc.Id,
                        Name = proc.ProcessName,
                        MemoryMB = proc.WorkingSet64 / (1024.0 * 1024.0),
                        CpuPercent = GetProcessCpuUsage(proc),
                        ThreadCount = proc.Threads.Count,
                        Status = proc.Responding ? "Running" : "Not Responding",
                        StatusColor = proc.Responding ? Colors.Green : Colors.Red
                    });
                }
                catch { }
            }
            UpdateAverages();
        }

        private double GetProcessCpuUsage(Process process)
        {
            try
            {
                var cpuCounter = new PerformanceCounter("Process", "% Processor Time", process.ProcessName, true);
                cpuCounter.NextValue();
                System.Threading.Thread.Sleep(50);
                return cpuCounter.NextValue() / Environment.ProcessorCount;
            }
            catch { return 0; }
        }

        private void UpdateAverages()
        {
            if (ProcessList.Count == 0) return;
            AverageCpu = ProcessList.Average(p => p.CpuPercent);
            AverageMemory = ProcessList.Average(p => p.MemoryMB);
            PeakCpu = ProcessList.Max(p => p.CpuPercent);
            PeakMemory = ProcessList.Max(p => p.MemoryMB);
        }

        private void UpdateHealthStatus()
        {
            HealthStatuses.Clear();
            var cpuMetric = MetricsHistory.FirstOrDefault();
            var healthScore = 100 - (cpuMetric?.CpuValue ?? 50);

            HealthStatuses.Add(new SystemHealthStatus { Category = "CPU", Status = cpuMetric?.CpuValue < 70 ? "Healthy" : "Warning", StatusColor = cpuMetric?.CpuValue < 70 ? Colors.Green : Colors.Orange, Details = $"Current: {cpuMetric?.CpuValue ?? 0:F1}%", HealthScore = healthScore });
            HealthStatuses.Add(new SystemHealthStatus { Category = "Memory", Status = cpuMetric?.MemoryValue < 80 ? "Healthy" : "Warning", StatusColor = cpuMetric?.MemoryValue < 80 ? Colors.Green : Colors.Orange, Details = $"Current: {cpuMetric?.MemoryValue ?? 0:F1}%", HealthScore = healthScore * 0.95 });
            HealthStatuses.Add(new SystemHealthStatus { Category = "Disk", Status = "Healthy", StatusColor = Colors.Green, Details = "65% Used", HealthScore = healthScore * 0.98 });
            HealthStatuses.Add(new SystemHealthStatus { Category = "Network", Status = "Healthy", StatusColor = Colors.Green, Details = "Connected", HealthScore = healthScore * 0.99 });
        }

        private void TerminateProcess(ProcessInfo proc)
        {
            if (proc == null) return;
            try
            {
                Process.GetProcessById(proc.Id).Kill();
                ProcessList.Remove(proc);
            }
            catch { }
        }

        private void ClearLogs() { LogEntries.Clear(); }
        private void FilterLogs(string level) { SelectedLogLevel = level ?? "All"; }
    }
}
