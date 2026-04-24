using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HELIOS.Platform.Phase10.Drivers
{
    /// <summary>
    /// Manages driver updates and version management.
    /// </summary>
    public class DriverUpdater
    {
        private readonly DriverRepository _repository;
        private readonly DriverDownloader _downloader;
        private readonly DriverInstaller _installer;
        private readonly SemaphoreSlim _semaphore;
        private Timer _updateCheckTimer;
        private bool _isScheduled;

        public DriverUpdater(DriverRepository repository, DriverDownloader downloader, DriverInstaller installer)
        {
            _repository = repository;
            _downloader = downloader;
            _installer = installer;
            _semaphore = new SemaphoreSlim(1);
            _isScheduled = false;
        }

        /// <summary>
        /// Check for available driver updates.
        /// </summary>
        public async Task<List<DriverInfo>> CheckForUpdatesAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                var availableUpdates = new List<DriverInfo>();
                var currentDrivers = await _repository.GetAllDriversAsync();

                foreach (var driver in currentDrivers)
                {
                    var hasUpdate = await CheckDriverUpdateAsync(driver);
                    if (hasUpdate)
                    {
                        availableUpdates.Add(driver);
                    }
                }

                return availableUpdates;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Check if specific driver has update.
        /// </summary>
        private async Task<bool> CheckDriverUpdateAsync(DriverInfo driver)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var latestVersion = GetLatestDriverVersion(driver.Manufacturer, driver.DriverType);
                    return Version.TryParse(driver.Version, out var currentVersion) &&
                           Version.TryParse(latestVersion, out var newVersion) &&
                           newVersion > currentVersion;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Update check failed: {ex.Message}");
                    return false;
                }
            });
        }

        /// <summary>
        /// Get latest driver version (mock implementation).
        /// </summary>
        private string GetLatestDriverVersion(string manufacturer, string driverType)
        {
            var baseVersion = new Version(1, 0, 0);
            var increment = DateTime.UtcNow.Day;
            return $"{baseVersion.Major}.{baseVersion.Minor}.{baseVersion.Build + increment}";
        }

        /// <summary>
        /// Auto-update all critical drivers.
        /// </summary>
        public async Task<List<InstallationResult>> AutoUpdateAsync()
        {
            var results = new List<InstallationResult>();
            var criticalDriverTypes = new[] { "Chipset", "GPU", "Network", "Audio" };

            try
            {
                var drivers = await _repository.GetAllDriversAsync();
                var criticalDrivers = drivers.Where(d => criticalDriverTypes.Contains(d.DriverType)).ToList();

                foreach (var driver in criticalDrivers)
                {
                    var hasUpdate = await CheckDriverUpdateAsync(driver);
                    if (hasUpdate)
                    {
                        var result = await _installer.InstallDriverAsync(driver.DriverId);
                        results.Add(result);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Auto-update failed: {ex.Message}");
            }

            return results;
        }

        /// <summary>
        /// Schedule periodic update checks.
        /// </summary>
        public async Task ScheduleUpdateCheckAsync(TimeSpan interval)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (_isScheduled)
                {
                    _updateCheckTimer?.Dispose();
                }

                _updateCheckTimer = new Timer(
                    async state => await PerformScheduledCheckAsync(),
                    null,
                    interval,
                    interval
                );

                _isScheduled = true;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Perform scheduled update check.
        /// </summary>
        private async Task PerformScheduledCheckAsync()
        {
            try
            {
                await CheckForUpdatesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Scheduled check failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Update specific driver.
        /// </summary>
        public async Task<InstallationResult> UpdateDriverAsync(string driverId)
        {
            await _semaphore.WaitAsync();
            try
            {
                var driver = await _repository.GetDriverAsync(driverId);
                if (driver == null)
                {
                    return new InstallationResult
                    {
                        Success = false,
                        DriverName = driverId,
                        Message = "Driver not found"
                    };
                }

                var hasUpdate = await CheckDriverUpdateAsync(driver);
                if (!hasUpdate)
                {
                    return new InstallationResult
                    {
                        Success = true,
                        DriverName = driver.Name,
                        Message = "Driver is already up to date"
                    };
                }

                return await _installer.InstallDriverAsync(driverId);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Get available updates.
        /// </summary>
        public async Task<List<DriverInfo>> GetAvailableUpdatesAsync()
        {
            return await CheckForUpdatesAsync();
        }

        /// <summary>
        /// Unschedule update checks.
        /// </summary>
        public async Task UnscheduleUpdateCheckAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                if (_updateCheckTimer != null)
                {
                    _updateCheckTimer.Dispose();
                    _updateCheckTimer = null;
                    _isScheduled = false;
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Check if update scheduling is active.
        /// </summary>
        public bool IsScheduled => _isScheduled;

        /// <summary>
        /// Get update status for driver.
        /// </summary>
        public async Task<(bool HasUpdate, string CurrentVersion, string LatestVersion)> GetUpdateStatusAsync(string driverId)
        {
            var driver = await _repository.GetDriverAsync(driverId);
            if (driver == null)
                return (false, null, null);

            var hasUpdate = await CheckDriverUpdateAsync(driver);
            var latestVersion = GetLatestDriverVersion(driver.Manufacturer, driver.DriverType);

            return (hasUpdate, driver.Version, latestVersion);
        }

        /// <summary>
        /// Get drivers pending update.
        /// </summary>
        public async Task<List<DriverInfo>> GetPendingUpdatesAsync()
        {
            var drivers = await _repository.GetAllDriversAsync();
            var updates = new List<DriverInfo>();

            foreach (var driver in drivers)
            {
                var hasUpdate = await CheckDriverUpdateAsync(driver);
                if (hasUpdate)
                {
                    updates.Add(driver);
                }
            }

            return updates;
        }

        /// <summary>
        /// Rollback to previous version.
        /// </summary>
        public async Task<bool> RollbackDriverAsync(string driverId)
        {
            await _semaphore.WaitAsync();
            try
            {
                var driver = await _repository.GetDriverAsync(driverId);
                if (driver == null)
                    return false;

                var previousVersion = GetPreviousVersion(driver.Version);
                driver.Version = previousVersion;
                driver.LastUpdated = DateTime.UtcNow;

                await _repository.UpdateDriverInfoAsync(driverId, driver);
                return true;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Get previous version.
        /// </summary>
        private string GetPreviousVersion(string currentVersion)
        {
            if (Version.TryParse(currentVersion, out var version))
            {
                var previous = new Version(
                    version.Major,
                    version.Minor,
                    Math.Max(0, version.Build - 1)
                );
                return previous.ToString();
            }

            return "1.0.0";
        }

        /// <summary>
        /// Get update history.
        /// </summary>
        public async Task<List<(string DriverId, DateTime UpdateDate, string Version)>> GetUpdateHistoryAsync()
        {
            var drivers = await _repository.GetAllDriversAsync();
            return drivers
                .OrderByDescending(d => d.LastUpdated)
                .Take(100)
                .Select(d => (d.DriverId, d.LastUpdated, d.Version))
                .ToList();
        }

        public void Dispose()
        {
            _updateCheckTimer?.Dispose();
            _semaphore?.Dispose();
        }
    }
}
