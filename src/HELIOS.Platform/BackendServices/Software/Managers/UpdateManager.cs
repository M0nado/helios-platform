using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using HELIOS.Platform.BackendServices.Software.Models;
using HELIOS.Platform.BackendServices.Software.Installers;

namespace HELIOS.Platform.BackendServices.Software.Managers
{
    /// <summary>
    /// Manages software updates and update schedules
    /// </summary>
    public class UpdateManager : IUpdateManager
    {
        private readonly ISoftwareInstallerService _installerService;
        private readonly ISoftwareDiscoveryService _discoveryService;
        private UpdateSchedule _schedule;
        private System.Timers.Timer _updateTimer;

        public UpdateManager(
            ISoftwareInstallerService installerService,
            ISoftwareDiscoveryService discoveryService)
        {
            _installerService = installerService;
            _discoveryService = discoveryService;
            _schedule = new UpdateSchedule();
        }

        /// <summary>
        /// Check for updates for a list of packages
        /// </summary>
        public async Task<List<SoftwarePackage>> CheckForUpdatesAsync(List<SoftwarePackage> packages)
        {
            var updatable = new List<SoftwarePackage>();

            foreach (var package in packages)
            {
                if (!package.IsInstalled || !package.AutoUpdate)
                    continue;

                var latestVersion = await GetLatestVersionAsync(package);

                if (!string.IsNullOrEmpty(latestVersion) && latestVersion != package.CurrentVersion)
                {
                    package.LatestVersion = latestVersion;
                    updatable.Add(package);
                }

                package.LastCheckedUpdate = DateTime.Now;
            }

            return updatable;
        }

        /// <summary>
        /// Update all packages that have available updates
        /// </summary>
        public async Task<bool> UpdateAllAsync(List<SoftwarePackage> packages)
        {
            var updatable = await CheckForUpdatesAsync(packages);

            if (updatable.Count == 0)
                return true;

            var allSuccess = true;

            foreach (var package in updatable)
            {
                try
                {
                    var success = await _installerService.UpdatePackageAsync(package);
                    if (!success)
                        allSuccess = false;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Update failed for {package.Name}: {ex.Message}");
                    allSuccess = false;
                }
            }

            return allSuccess;
        }

        /// <summary>
        /// Schedule updates for off-peak hours
        /// </summary>
        public async Task<bool> ScheduleUpdatesAsync(
            List<SoftwarePackage> packages,
            TimeSpan offPeakStart,
            TimeSpan offPeakEnd)
        {
            _schedule.OffPeakStart = offPeakStart;
            _schedule.OffPeakEnd = offPeakEnd;
            _schedule.AutoUpdateEnabled = true;

            // Configure packages for auto-update
            foreach (var package in packages)
            {
                package.AutoUpdate = true;
            }

            // Start scheduled update timer
            return await StartScheduledUpdatesAsync();
        }

        /// <summary>
        /// Get the current update schedule
        /// </summary>
        public async Task<UpdateSchedule> GetUpdateScheduleAsync()
        {
            await Task.Delay(0); // Make it async
            return _schedule;
        }

        /// <summary>
        /// Get the latest version of a package from various sources
        /// </summary>
        private async Task<string> GetLatestVersionAsync(SoftwarePackage package)
        {
            try
            {
                // Try different methods to get the latest version
                var version = await GetVersionFromWingetAsync(package);
                if (!string.IsNullOrEmpty(version))
                    return version;

                version = await GetVersionFromChocolateyAsync(package);
                if (!string.IsNullOrEmpty(version))
                    return version;

                version = await GetVersionFromOfficialAsync(package);
                if (!string.IsNullOrEmpty(version))
                    return version;

                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting latest version for {package.Name}: {ex.Message}");
                return null;
            }
        }

        private async Task<string> GetVersionFromWingetAsync(SoftwarePackage package)
        {
            try
            {
                var result = await ExecuteCommandAsync($"winget show -e --id {package.Id} --accept-source-agreements");
                
                if (result.ExitCode == 0)
                {
                    var lines = result.Output.Split('\n');
                    foreach (var line in lines)
                    {
                        if (line.Contains("Version"))
                        {
                            var versionPart = line.Split(':');
                            if (versionPart.Length > 1)
                            {
                                return versionPart[1].Trim();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Winget version check failed: {ex.Message}");
            }

            return null;
        }

        private async Task<string> GetVersionFromChocolateyAsync(SoftwarePackage package)
        {
            try
            {
                var result = await ExecuteCommandAsync($"choco info {package.Id}");

                if (result.ExitCode == 0)
                {
                    var lines = result.Output.Split('\n');
                    foreach (var line in lines)
                    {
                        if (line.Contains("Latest Version"))
                        {
                            var versionPart = line.Split(':');
                            if (versionPart.Length > 1)
                            {
                                return versionPart[1].Trim();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Chocolatey version check failed: {ex.Message}");
            }

            return null;
        }

        private async Task<string> GetVersionFromOfficialAsync(SoftwarePackage package)
        {
            // This would be specific to each package's website
            // For now, return null as it requires custom logic per package
            await Task.Delay(0);
            return null;
        }

        /// <summary>
        /// Start the scheduled update timer
        /// </summary>
        private async Task<bool> StartScheduledUpdatesAsync()
        {
            try
            {
                if (_updateTimer != null)
                {
                    _updateTimer.Stop();
                    _updateTimer.Dispose();
                }

                _updateTimer = new System.Timers.Timer();
                
                // Check every hour if it's update time
                _updateTimer.Interval = 3600000; // 1 hour

                _updateTimer.Elapsed += async (s, e) =>
                {
                    if (IsUpdateTime())
                    {
                        // Trigger update check
                        Debug.WriteLine("Update check triggered by scheduler");
                    }
                };

                _updateTimer.AutoReset = true;
                _updateTimer.Start();

                await Task.Delay(0);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to start scheduled updates: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Check if it's within the scheduled update window
        /// </summary>
        private bool IsUpdateTime()
        {
            var now = DateTime.Now;

            // Check if today is update day
            if (now.DayOfWeek != _schedule.UpdateDay)
                return false;

            // Check if within update time window
            var currentTime = now.TimeOfDay;

            if (_schedule.OnlyOffPeakHours)
            {
                return currentTime >= _schedule.OffPeakStart || currentTime <= _schedule.OffPeakEnd;
            }

            return currentTime >= _schedule.UpdateTime &&
                   currentTime < _schedule.UpdateTime.Add(TimeSpan.FromHours(1));
        }

        /// <summary>
        /// Execute a command and get result
        /// </summary>
        private async Task<CommandResult> ExecuteCommandAsync(string command)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {command}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();

                    await Task.Run(() => process.WaitForExit(30000));

                    return new CommandResult
                    {
                        Success = process.ExitCode == 0,
                        Output = output,
                        Error = error,
                        ExitCode = process.ExitCode
                    };
                }
            }
            catch (Exception ex)
            {
                return new CommandResult
                {
                    Success = false,
                    Error = ex.Message,
                    ExitCode = -1
                };
            }
        }

        /// <summary>
        /// Command execution result
        /// </summary>
        private class CommandResult
        {
            public bool Success { get; set; }
            public string Output { get; set; }
            public string Error { get; set; }
            public int ExitCode { get; set; }
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            _updateTimer?.Dispose();
        }
    }
}
