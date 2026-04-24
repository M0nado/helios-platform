using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using HELIOS.Platform.Presentation.Studio.Models;

namespace HELIOS.Platform.Presentation.Studio.Services
{
    /// <summary>
    /// Core dashboard service for real-time system monitoring and management
    /// Provides metrics collection, alert management, and user administration
    /// </summary>
    public class StudioDashboardService
    {
        private readonly PerformanceCounter _cpuCounter;
        private readonly PerformanceCounter _memoryCounter;
        private readonly PerformanceCounter _networkCounter;
        private SystemMetrics _lastMetrics;
        private readonly List<DashboardAlert> _alerts;
        private readonly List<DashboardUser> _users;
        private readonly DashboardSettings _settings;
        private CancellationTokenSource _refreshTokenSource;
        private Task _refreshTask;

        public event EventHandler<SystemMetrics> MetricsUpdated;
        public event EventHandler<DashboardAlert> AlertRaised;
        public event EventHandler<DashboardStatus> StatusChanged;

        public StudioDashboardService()
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
            _memoryCounter = new PerformanceCounter("Memory", "% Committed Bytes In Use", "", true);
            _networkCounter = new PerformanceCounter("Network Interface", "Bytes Total/sec", "", true);
            _lastMetrics = new SystemMetrics { Timestamp = DateTime.UtcNow };
            _alerts = new List<DashboardAlert>();
            _users = new List<DashboardUser>();
            _settings = new DashboardSettings();
        }

        /// <summary>
        /// Initialize the dashboard service with auto-refresh
        /// </summary>
        public async Task InitializeAsync()
        {
            _refreshTokenSource = new CancellationTokenSource();
            _refreshTask = StartAutoRefresh(_refreshTokenSource.Token);
            await Task.CompletedTask;
        }

        /// <summary>
        /// Collect real-time system metrics
        /// Performance target: <500ms update time
        /// </summary>
        public SystemMetrics CollectMetrics()
        {
            var metrics = new SystemMetrics
            {
                Timestamp = DateTime.UtcNow,
                CpuUsagePercent = Math.Round(_cpuCounter.NextValue(), 2),
                MemoryUsagePercent = Math.Round(_memoryCounter.NextValue(), 2),
                ProcessCount = Process.GetProcessCount(),
                ThreadCount = Process.GetCurrentProcess().Threads.Count
            };

            // Collect memory info
            var memInfo = GC.GetTotalMemory(false);
            metrics.MemoryUsedMB = memInfo / (1024 * 1024);

            // Collect disk info
            var diskInfo = GetDiskMetrics();
            metrics.DiskUsagePercent = diskInfo.usagePercent;
            metrics.DiskUsedGB = diskInfo.usedGB;
            metrics.DiskTotalGB = diskInfo.totalGB;

            // Collect network info
            metrics.NetworkBytesPerSecond = _networkCounter.NextValue();

            // Try to collect GPU metrics if available
            metrics.GpuUsagePercent = TryCollectGpuMetrics();

            _lastMetrics = metrics;
            MetricsUpdated?.Invoke(this, metrics);

            return metrics;
        }

        /// <summary>
        /// Get current dashboard status
        /// </summary>
        public DashboardStatus GetStatus()
        {
            CollectMetrics();

            var status = new DashboardStatus
            {
                LastUpdatedAt = DateTime.UtcNow,
                ActiveAlerts = _alerts.Count(a => !a.IsResolved),
                WarningCount = _alerts.Count(a => a.Severity == AlertSeverity.Warning && !a.IsResolved),
                CriticalCount = _alerts.Count(a => a.Severity == AlertSeverity.Critical && !a.IsResolved),
                ActiveUsers = _users.Count(u => u.IsActive)
            };

            // Determine overall health based on metrics and alerts
            if (status.CriticalCount > 0)
                status.OverallHealth = SystemHealth.Critical;
            else if (status.WarningCount > 0)
                status.OverallHealth = SystemHealth.Warning;
            else if (_lastMetrics.CpuUsagePercent > 90 || _lastMetrics.MemoryUsagePercent > 90)
                status.OverallHealth = SystemHealth.Warning;
            else
                status.OverallHealth = SystemHealth.Healthy;

            StatusChanged?.Invoke(this, status);
            return status;
        }

        /// <summary>
        /// Create a new alert
        /// </summary>
        public DashboardAlert CreateAlert(AlertSeverity severity, string title, string message)
        {
            var alert = new DashboardAlert
            {
                Id = Guid.NewGuid().ToString("N"),
                Severity = severity,
                Title = title,
                Message = message,
                CreatedAt = DateTime.UtcNow
            };

            _alerts.Add(alert);
            AlertRaised?.Invoke(this, alert);

            return alert;
        }

        /// <summary>
        /// Resolve an alert
        /// </summary>
        public void ResolveAlert(string alertId)
        {
            var alert = _alerts.FirstOrDefault(a => a.Id == alertId && !a.IsResolved);
            if (alert != null)
            {
                alert.ResolvedAt = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Get all active and historical alerts
        /// </summary>
        public IEnumerable<DashboardAlert> GetAlerts(bool activeOnly = false)
        {
            if (activeOnly)
                return _alerts.Where(a => !a.IsResolved);
            return _alerts;
        }

        /// <summary>
        /// Clear resolved alerts older than specified days
        /// </summary>
        public void ClearOldAlerts(int daysToKeep = 30)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
            _alerts.RemoveAll(a => a.IsResolved && a.ResolvedAt < cutoffDate);
        }

        /// <summary>
        /// Create or update a user
        /// </summary>
        public DashboardUser CreateOrUpdateUser(string username, string displayName, string email, string[] roles = null)
        {
            var existingUser = _users.FirstOrDefault(u => u.Username == username);
            if (existingUser != null)
            {
                existingUser.DisplayName = displayName;
                existingUser.Email = email;
                if (roles != null)
                    existingUser.Roles = roles;
                return existingUser;
            }

            var user = new DashboardUser
            {
                Id = Guid.NewGuid().ToString("N"),
                Username = username,
                DisplayName = displayName,
                Email = email,
                Roles = roles ?? new[] { "User" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _users.Add(user);
            return user;
        }

        /// <summary>
        /// Delete a user
        /// </summary>
        public bool DeleteUser(string username)
        {
            return _users.RemoveAll(u => u.Username == username) > 0;
        }

        /// <summary>
        /// Get all users
        /// </summary>
        public IEnumerable<DashboardUser> GetUsers()
        {
            return _users.AsReadOnly();
        }

        /// <summary>
        /// Update user last login time
        /// </summary>
        public void UpdateUserLogin(string username)
        {
            var user = _users.FirstOrDefault(u => u.Username == username);
            if (user != null)
            {
                user.LastLoginAt = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Update dashboard settings
        /// </summary>
        public void UpdateSettings(DashboardSettings settings)
        {
            if (settings == null)
                return;

            _settings.RefreshIntervalSeconds = settings.RefreshIntervalSeconds;
            _settings.EnableNotifications = settings.EnableNotifications;
            _settings.EnableDarkMode = settings.EnableDarkMode;
            _settings.CpuAlertThreshold = settings.CpuAlertThreshold;
            _settings.MemoryAlertThreshold = settings.MemoryAlertThreshold;
            _settings.DiskAlertThreshold = settings.DiskAlertThreshold;
            _settings.EnableAutoRefresh = settings.EnableAutoRefresh;
            _settings.MaxHistoryDays = settings.MaxHistoryDays;
            _settings.Theme = settings.Theme;
        }

        /// <summary>
        /// Get current settings
        /// </summary>
        public DashboardSettings GetSettings()
        {
            return _settings;
        }

        /// <summary>
        /// Start auto-refresh of metrics
        /// </summary>
        private async Task StartAutoRefresh(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_settings.RefreshIntervalSeconds * 1000, cancellationToken);

                    if (_settings.EnableAutoRefresh && !cancellationToken.IsCancellationRequested)
                    {
                        CollectMetrics();
                        ValidateThresholds();
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Validate metrics against thresholds and raise alerts
        /// </summary>
        private void ValidateThresholds()
        {
            if (_lastMetrics.CpuUsagePercent > _settings.CpuAlertThreshold)
            {
                var existingAlert = _alerts.FirstOrDefault(
                    a => a.Title == "High CPU Usage" && !a.IsResolved);
                
                if (existingAlert == null)
                {
                    CreateAlert(AlertSeverity.Warning, "High CPU Usage",
                        $"CPU usage is at {_lastMetrics.CpuUsagePercent}%");
                }
            }

            if (_lastMetrics.MemoryUsagePercent > _settings.MemoryAlertThreshold)
            {
                var existingAlert = _alerts.FirstOrDefault(
                    a => a.Title == "High Memory Usage" && !a.IsResolved);
                
                if (existingAlert == null)
                {
                    CreateAlert(AlertSeverity.Warning, "High Memory Usage",
                        $"Memory usage is at {_lastMetrics.MemoryUsagePercent}%");
                }
            }

            if (_lastMetrics.DiskUsagePercent > _settings.DiskAlertThreshold)
            {
                var existingAlert = _alerts.FirstOrDefault(
                    a => a.Title == "High Disk Usage" && !a.IsResolved);
                
                if (existingAlert == null)
                {
                    CreateAlert(AlertSeverity.Warning, "High Disk Usage",
                        $"Disk usage is at {_lastMetrics.DiskUsagePercent}%");
                }
            }
        }

        /// <summary>
        /// Get disk metrics for C: drive
        /// </summary>
        private (double usagePercent, long usedGB, long totalGB) GetDiskMetrics()
        {
            try
            {
                var driveInfo = System.IO.DriveInfo.GetDrives()
                    .FirstOrDefault(d => d.Name.StartsWith("C:"));

                if (driveInfo != null)
                {
                    var totalBytes = driveInfo.TotalSize;
                    var freeBytes = driveInfo.AvailableFreeSpace;
                    var usedBytes = totalBytes - freeBytes;

                    var usagePercent = (double)usedBytes / totalBytes * 100;
                    var usedGB = usedBytes / (1024 * 1024 * 1024);
                    var totalGB = totalBytes / (1024 * 1024 * 1024);

                    return (usagePercent, usedGB, totalGB);
                }
            }
            catch { }

            return (0, 0, 0);
        }

        /// <summary>
        /// Try to collect GPU metrics (optional, may not be available)
        /// </summary>
        private double TryCollectGpuMetrics()
        {
            try
            {
                var scope = new ManagementScope(@"\\.\root\cimv2");
                scope.Connect();

                var query = new ObjectQuery("SELECT * FROM Win32_VideoController");
                var searcher = new ManagementObjectSearcher(scope, query);

                foreach (ManagementObject item in searcher.Get())
                {
                    var videoProcessor = item["VideoProcessor"];
                    if (videoProcessor != null)
                        return 0.0; // Placeholder, actual implementation would query real GPU stats
                }
            }
            catch { }

            return 0.0;
        }

        /// <summary>
        /// Stop the dashboard service and cleanup resources
        /// </summary>
        public void Shutdown()
        {
            _refreshTokenSource?.Cancel();
            _refreshTask?.Wait(TimeSpan.FromSeconds(10));
            _refreshTokenSource?.Dispose();
            _cpuCounter?.Dispose();
            _memoryCounter?.Dispose();
            _networkCounter?.Dispose();
        }
    }
}
