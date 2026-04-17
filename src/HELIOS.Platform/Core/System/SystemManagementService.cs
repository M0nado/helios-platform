using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading.Tasks;
using HELIOS.Platform.Core.Logging;

namespace HELIOS.Platform.Core.Administration
{
    /// <summary>
    /// Partition information model.
    /// </summary>
    public class PartitionInfo
    {
        public string DriveLetter { get; set; } = string.Empty;
        public string FileSystem { get; set; } = string.Empty;
        public long TotalSizeBytes { get; set; }
        public long UsedSizeBytes { get; set; }
        public long FreeSizeBytes { get; set; }
        public int UsagePercent => TotalSizeBytes > 0 ? (int)((UsedSizeBytes * 100) / TotalSizeBytes) : 0;
        public string VolumeLabel { get; set; } = string.Empty;
        public bool IsSystem { get; set; }
    }

    /// <summary>
    /// Service information model.
    /// </summary>
    public class WindowsService
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StartType { get; set; } = string.Empty;
        public bool IsRunning { get; set; }
    }

    /// <summary>
    /// Interface for partition and service management.
    /// </summary>
    public interface ISystemManagementService
    {
        Task<List<PartitionInfo>> GetPartitionsAsync();
        Task<List<WindowsService>> GetServicesAsync();
        Task<bool> StartServiceAsync(string serviceName);
        Task<bool> StopServiceAsync(string serviceName);
        Task<bool> RestartServiceAsync(string serviceName);
    }

    /// <summary>
    /// System management service for partition and service operations.
    /// </summary>
    public class SystemManagementService : ISystemManagementService
    {
        private readonly Core.Logging.ILogger _logger;

        public SystemManagementService()
        {
            _logger = new ConsoleLogger();
        }

        /// <summary>
        /// Get all partitions on the system.
        /// </summary>
        public async Task<List<PartitionInfo>> GetPartitionsAsync()
        {
            return await Task.Run(() =>
            {
                var partitions = new List<PartitionInfo>();

                try
                {
                    var drives = System.IO.DriveInfo.GetDrives();
                    foreach (var drive in drives)
                    {
                        if (drive.IsReady)
                        {
                            partitions.Add(new PartitionInfo
                            {
                                DriveLetter = drive.Name.TrimEnd('\\'),
                                FileSystem = drive.DriveFormat,
                                TotalSizeBytes = drive.TotalSize,
                                UsedSizeBytes = drive.TotalSize - drive.AvailableFreeSpace,
                                FreeSizeBytes = drive.AvailableFreeSpace,
                                VolumeLabel = drive.VolumeLabel,
                                IsSystem = drive.Name.StartsWith("C:")
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error retrieving partitions: {ex.Message}");
                }

                return partitions;
            });
        }

        /// <summary>
        /// Get list of Windows services.
        /// </summary>
        public async Task<List<WindowsService>> GetServicesAsync()
        {
            return await Task.Run(() =>
            {
                var services = new List<WindowsService>();

                try
                {
                    // Get key system services
                    var serviceNames = new[]
                    {
                        "Dnscache",      // DNS Client
                        "wuauserv",      // Windows Update
                        "BITS",          // Background Intelligent Transfer Service
                        "spooler",       // Print Spooler
                        "Winmgmt",       // Windows Management Instrumentation
                        "EventLog",      // Event Viewer
                        "WinRM",         // Windows Remote Management
                        "RpcSs"          // Remote Procedure Call
                    };

                    foreach (var serviceName in serviceNames)
                    {
                        try
                        {
                            var serviceController = new ServiceController(serviceName);
                            services.Add(new WindowsService
                            {
                                Name = serviceName,
                                DisplayName = serviceController.DisplayName,
                                Status = serviceController.Status.ToString(),
                                IsRunning = serviceController.Status == ServiceControllerStatus.Running,
                                StartType = GetServiceStartType(serviceName)
                            });
                        }
                        catch { }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error retrieving services: {ex.Message}");
                }

                return services;
            });
        }

        /// <summary>
        /// Start a Windows service.
        /// </summary>
        public async Task<bool> StartServiceAsync(string serviceName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var service = new ServiceController(serviceName);
                    if (service.Status != ServiceControllerStatus.Running)
                    {
                        service.Start();
                        service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
                        _logger.Info($"Service '{serviceName}' started successfully");
                        return true;
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error starting service '{serviceName}': {ex.Message}");
                    return false;
                }
            });
        }

        /// <summary>
        /// Stop a Windows service.
        /// </summary>
        public async Task<bool> StopServiceAsync(string serviceName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var service = new ServiceController(serviceName);
                    if (service.Status != ServiceControllerStatus.Stopped)
                    {
                        service.Stop();
                        service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                        _logger.Info($"Service '{serviceName}' stopped successfully");
                        return true;
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error stopping service '{serviceName}': {ex.Message}");
                    return false;
                }
            });
        }

        /// <summary>
        /// Restart a Windows service.
        /// </summary>
        public async Task<bool> RestartServiceAsync(string serviceName)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    await StopServiceAsync(serviceName);
                    await Task.Delay(1000);
                    return await StartServiceAsync(serviceName);
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error restarting service '{serviceName}': {ex.Message}");
                    return false;
                }
            });
        }

        /// <summary>
        /// Get service start type via WMI query.
        /// </summary>
        private static string GetServiceStartType(string serviceName)
        {
            try
            {
                var startProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-Command \"(Get-Service -Name '{serviceName}').StartType\"",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                startProcess.Start();
                var output = startProcess.StandardOutput.ReadToEnd().Trim();
                startProcess.WaitForExit();

                return output ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }
    }
}
