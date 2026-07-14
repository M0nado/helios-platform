using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HELIOS.Platform.Core.Logging;

namespace HELIOS.Platform.Phase10.BootEnvironment
{
    /// <summary>
    /// USB device health monitoring including failure detection, hot-plug support,
    /// safe ejection, and recovery on disconnect.
    /// </summary>
    public class USBHealthMonitor
    {
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _semaphore;
        private Dictionary<string, USBDeviceInfo> _monitoredDevices;
        private Dictionary<string, CancellationTokenSource> _monitoringTasks;
        private const int ERROR_THRESHOLD = 5;

        public USBHealthMonitor(ILogger logger = null)
        {
            _logger = logger ?? new ConsoleLogger();
            _semaphore = new SemaphoreSlim(1, 1);
            _monitoredDevices = new Dictionary<string, USBDeviceInfo>();
            _monitoringTasks = new Dictionary<string, CancellationTokenSource>();
        }

        /// <summary>
        /// Gets health information for USB device.
        /// </summary>
        public async Task<USBDeviceInfo> GetUSBHealthAsync(string deviceId)
        {
            await _semaphore.WaitAsync();
            try
            {
                _logger.Info($"Getting USB health for: {deviceId}");

                if (string.IsNullOrWhiteSpace(deviceId))
                {
                    _logger.Error("Device ID cannot be null or empty");
                    return null;
                }

                var drive = FindRemovableDrive(deviceId);
                if (drive == null)
                {
                    _logger.Warning($"USB device was not found or is not removable: {deviceId}");
                    return new USBDeviceInfo
                    {
                        DeviceId = deviceId,
                        FriendlyName = "Unverified USB device",
                        FileSystem = "Unknown",
                        IsHealthy = false,
                        ErrorCount = 1,
                        LastHealthCheck = DateTime.UtcNow,
                        HealthPercentage = 0
                    };
                }

                return CreateDeviceInfo(drive);
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to get USB health", ex);
                return null;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Monitors USB device health at specified intervals.
        /// </summary>
        public async Task<bool> MonitorUSBHealthAsync(string deviceId, TimeSpan checkInterval)
        {
            await _semaphore.WaitAsync();
            try
            {
                _logger.Info($"Starting USB health monitoring for {deviceId} (interval: {checkInterval.TotalSeconds}s)");

                if (string.IsNullOrWhiteSpace(deviceId))
                {
                    _logger.Error("Device ID cannot be null or empty");
                    return false;
                }

                if (checkInterval.TotalSeconds < 1)
                {
                    _logger.Error("Check interval must be at least 1 second");
                    return false;
                }

                // Check if already monitoring
                if (_monitoringTasks.ContainsKey(deviceId))
                {
                    _logger.Warning($"Already monitoring device: {deviceId}");
                    return true;
                }

                var cts = new CancellationTokenSource();
                _monitoringTasks[deviceId] = cts;

                // Start background monitoring task
                _ = MonitorDeviceBackgroundAsync(deviceId, checkInterval, cts.Token);

                _logger.Info($"USB health monitoring started for {deviceId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to start USB health monitoring", ex);
                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Stops monitoring USB device health.
        /// </summary>
        public async Task<bool> StopUSBMonitorAsync(string deviceId)
        {
            await _semaphore.WaitAsync();
            try
            {
                _logger.Info($"Stopping USB health monitoring for {deviceId}");

                if (string.IsNullOrWhiteSpace(deviceId))
                {
                    _logger.Error("Device ID cannot be null or empty");
                    return false;
                }

                if (!_monitoringTasks.ContainsKey(deviceId))
                {
                    _logger.Warning($"Not monitoring device: {deviceId}");
                    return true;
                }

                var cts = _monitoringTasks[deviceId];
                cts.Cancel();
                cts.Dispose();

                _monitoringTasks.Remove(deviceId);
                _monitoredDevices.Remove(deviceId);

                _logger.Info($"USB health monitoring stopped for {deviceId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to stop USB health monitoring", ex);
                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Gets all connected USB devices.
        /// </summary>
        public async Task<List<USBDeviceInfo>> GetAllUSBDevicesAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                _logger.Debug("Enumerating all USB devices");

                var devices = DriveInfo.GetDrives()
                    .Where(drive => drive.DriveType == DriveType.Removable)
                    .Select(CreateDeviceInfo)
                    .ToList();

                _logger.Debug($"Found {devices.Count} USB device(s)");
                return devices;
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to enumerate USB devices", ex);
                return new List<USBDeviceInfo>();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Detects failed USB devices.
        /// </summary>
        public async Task<List<string>> DetectFailedDevicesAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                _logger.Info("Detecting failed USB devices");

                var failedDevices = new List<string>();
                foreach (var kvp in _monitoredDevices)
                {
                    if (kvp.Value.ErrorCount >= ERROR_THRESHOLD)
                    {
                        failedDevices.Add(kvp.Key);
                        _logger.Warning($"Failed device detected: {kvp.Key}");
                    }
                }

                return failedDevices;
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to detect failed devices", ex);
                return new List<string>();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Safely ejects USB device.
        /// </summary>
        public async Task<bool> SafeEjectAsync(string deviceId)
        {
            await _semaphore.WaitAsync();
            try
            {
                _logger.Info($"Safely ejecting USB device: {deviceId}");

                if (string.IsNullOrWhiteSpace(deviceId))
                {
                    _logger.Error("Device ID cannot be null or empty");
                    return false;
                }

                if (_monitoringTasks.Remove(deviceId, out var monitoringTask))
                {
                    monitoringTask.Cancel();
                    monitoringTask.Dispose();
                    _monitoredDevices.Remove(deviceId);
                }

                // No platform ejection API is wired here. Returning success would let an
                // erase/deployment wizard proceed on a device that is still mounted.
                _logger.Warning(
                    $"USB ejection is not available for {deviceId}; monitoring stopped, device remains mounted");
                return false;
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to eject USB device", ex);
                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Gets health status of all monitored devices.
        /// </summary>
        public async Task<Dictionary<string, float>> GetAllDeviceHealthAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                var healthStatus = new Dictionary<string, float>();

                foreach (var kvp in _monitoredDevices)
                {
                    healthStatus[kvp.Key] = kvp.Value.HealthPercentage;
                }

                return healthStatus;
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to get all device health", ex);
                return new Dictionary<string, float>();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        // Private helper methods

        private static DriveInfo FindRemovableDrive(string deviceId)
        {
            return DriveInfo.GetDrives().FirstOrDefault(drive =>
                drive.DriveType == DriveType.Removable &&
                (string.Equals(drive.Name, deviceId, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(drive.RootDirectory.FullName, deviceId, StringComparison.OrdinalIgnoreCase)));
        }

        private static USBDeviceInfo CreateDeviceInfo(DriveInfo drive)
        {
            var isReady = drive.IsReady;
            var capacity = isReady ? drive.TotalSize : 0;
            var freeSpace = isReady ? drive.AvailableFreeSpace : 0;

            return new USBDeviceInfo
            {
                DeviceId = drive.Name,
                FriendlyName = isReady && !string.IsNullOrWhiteSpace(drive.VolumeLabel)
                    ? drive.VolumeLabel
                    : drive.Name,
                CapacityBytes = capacity,
                UsedBytes = Math.Max(0, capacity - freeSpace),
                FileSystem = isReady ? drive.DriveFormat : "Unknown",
                IsHealthy = isReady,
                ErrorCount = isReady ? 0 : 1,
                LastHealthCheck = DateTime.UtcNow,
                HealthPercentage = isReady ? 100 : 0
            };
        }

        private async Task MonitorDeviceBackgroundAsync(string deviceId, TimeSpan checkInterval, CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        var health = await GetUSBHealthAsync(deviceId);
                        if (health != null)
                        {
                            _monitoredDevices[deviceId] = health;

                            var statusText = health.IsHealthy 
                                ? $"HEALTHY ({health.HealthPercentage}%)" 
                                : $"UNHEALTHY ({health.ErrorCount} errors)";
                            
                            _logger.Debug($"Device {deviceId} status: {statusText}");

                            // Check if health degraded
                            if (health.ErrorCount >= ERROR_THRESHOLD)
                            {
                                _logger.Warning($"Device {deviceId} has exceeded error threshold");
                            }
                        }

                        await Task.Delay((int)checkInterval.TotalMilliseconds, ct);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Error monitoring device {deviceId}", ex);
                    }
                }
            }
            finally
            {
                _logger.Debug($"Monitoring stopped for device {deviceId}");
            }
        }
    }
}
