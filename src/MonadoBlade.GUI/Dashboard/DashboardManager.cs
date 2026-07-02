// MONADO BLADE v3.4.0 - DASHBOARD SYSTEM
// File: src/MonadoBlade.GUI/Dashboard/DashboardManager.cs

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MonadoBlade.Core.SystemIntegration;

namespace MonadoBlade.GUI.Dashboard
{
    /// <summary>
    /// Dashboard management system with real-time metrics and customizable widgets
    /// </summary>
    public class DashboardManager
    {
        public class DashboardWidget
        {
            public string Id { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public WidgetType Type { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public int Row { get; set; }
            public int Column { get; set; }
            public bool IsVisible { get; set; }
            public DateTime LastUpdated { get; set; }
        }

        public enum WidgetType
        {
            SystemMetrics,
            ProcessMonitor,
            PerformanceChart,
            NetworkMonitor,
            MemoryAnalysis,
            CPUMonitor,
            GPUMonitor,
            DiskMonitor,
            AlertsList,
            ActivityLog
        }

        public class DashboardLayout
        {
            public string Name { get; set; }
            public ObservableCollection<DashboardWidget> Widgets { get; set; }
            public int Columns { get; set; }
            public int Rows { get; set; }
            public bool IsCustom { get; set; }

            public DashboardLayout()
            {
                Widgets = new ObservableCollection<DashboardWidget>();
                Columns = 4;
                Rows = 3;
            }
        }

        public class MetricsSnapshot
        {
            public WindowsSystemBridge.PerformanceMonitor.SystemMetrics SystemMetrics { get; set; }
            public List<WindowsSystemBridge.TaskManager.ProcessInfo> TopProcesses { get; set; }
            public DateTime Timestamp { get; set; }
            public TimeSpan UpdateDuration { get; set; }
        }

        private DashboardLayout _currentLayout;
        private ObservableCollection<MetricsSnapshot> _metricsHistory;

        public DashboardManager()
        {
            _currentLayout = CreateDefaultLayout();
            _metricsHistory = new ObservableCollection<MetricsSnapshot>();
        }

        /// <summary>
        /// Create default dashboard layout
        /// </summary>
        private DashboardLayout CreateDefaultLayout()
        {
            var layout = new DashboardLayout { Name = "Default Layout" };

            layout.Widgets.Add(new DashboardWidget
            {
                Id = "cpu-monitor",
                Title = "CPU Usage",
                Description = "Real-time CPU utilization",
                Type = WidgetType.CPUMonitor,
                Width = 2,
                Height = 2,
                Row = 0,
                Column = 0,
                IsVisible = true
            });

            layout.Widgets.Add(new DashboardWidget
            {
                Id = "memory-monitor",
                Title = "Memory Usage",
                Description = "RAM utilization",
                Type = WidgetType.MemoryAnalysis,
                Width = 2,
                Height = 2,
                Row = 0,
                Column = 2,
                IsVisible = true
            });

            layout.Widgets.Add(new DashboardWidget
            {
                Id = "process-monitor",
                Title = "Top Processes",
                Description = "Running processes by resource usage",
                Type = WidgetType.ProcessMonitor,
                Width = 4,
                Height = 2,
                Row = 2,
                Column = 0,
                IsVisible = true
            });

            layout.Widgets.Add(new DashboardWidget
            {
                Id = "network-monitor",
                Title = "Network Activity",
                Description = "Network I/O monitoring",
                Type = WidgetType.NetworkMonitor,
                Width = 2,
                Height = 2,
                Row = 4,
                Column = 0,
                IsVisible = true
            });

            layout.Widgets.Add(new DashboardWidget
            {
                Id = "disk-monitor",
                Title = "Disk Usage",
                Description = "Storage utilization",
                Type = WidgetType.DiskMonitor,
                Width = 2,
                Height = 2,
                Row = 4,
                Column = 2,
                IsVisible = true
            });

            return layout;
        }

        /// <summary>
        /// Get current dashboard layout
        /// </summary>
        public DashboardLayout GetCurrentLayout() => _currentLayout;

        /// <summary>
        /// Update metrics snapshot
        /// </summary>
        public MetricsSnapshot UpdateMetrics()
        {
            var startTime = DateTime.Now;
            var snapshot = new MetricsSnapshot
            {
                SystemMetrics = WindowsSystemBridge.PerformanceMonitor.GetSystemMetrics(),
                TopProcesses = WindowsSystemBridge.TaskManager.GetRunningProcesses(),
                Timestamp = DateTime.Now
            };
            snapshot.UpdateDuration = DateTime.Now - startTime;
            _metricsHistory.Add(snapshot);

            if (_metricsHistory.Count > 100)
            {
                _metricsHistory.RemoveAt(0);
            }

            return snapshot;
        }

        /// <summary>
        /// Get metrics history
        /// </summary>
        public ObservableCollection<MetricsSnapshot> GetMetricsHistory() => _metricsHistory;

        /// <summary>
        /// Add custom widget
        /// </summary>
        public void AddWidget(DashboardWidget widget)
        {
            _currentLayout.Widgets.Add(widget);
        }

        /// <summary>
        /// Remove widget
        /// </summary>
        public void RemoveWidget(string widgetId)
        {
            var widget = _currentLayout.Widgets.FirstOrDefault(w => w.Id == widgetId);
            if (widget != null)
            {
                _currentLayout.Widgets.Remove(widget);
            }
        }

        /// <summary>
        /// Save layout
        /// </summary>
        public void SaveLayout(string layoutName)
        {
            _currentLayout.Name = layoutName;
            _currentLayout.IsCustom = true;
        }

        /// <summary>
        /// Reset to default layout
        /// </summary>
        public void ResetToDefault()
        {
            _currentLayout = CreateDefaultLayout();
        }
    }
}
