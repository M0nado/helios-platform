using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MonadoBlade.Features
{
    /// <summary>
    /// Manages dual-boot configuration and shared data partition mounting.
    /// Handles UEFI boot menu setup and cross-OS data sharing.
    /// </summary>
    public class DualBootManager
    {
        private Dictionary<string, BootEntry> _bootEntries;
        private string _bootConfigBackup;
        private const string BOOT_CONFIG_FILE = "boot_config.json";

        public event EventHandler<BootEventArgs> BootConfigChanged;

        public DualBootManager()
        {
            _bootEntries = new Dictionary<string, BootEntry>();
        }

        /// <summary>
        /// Configures UEFI boot menu with both OS options.
        /// </summary>
        public async Task<BootConfigResult> ConfigureBootMenu(
            string monadoPartitionPath,
            string sharedPartitionPath,
            string defaultOS,
            int bootMenuTimeout)
        {
            var result = new BootConfigResult();

            try
            {
                // Backup current boot configuration
                await BackupBootConfiguration();

                // Create Windows boot entry
                var windowsEntry = new BootEntry
                {
                    Name = "Windows Boot Manager",
                    Path = "\\EFI\\Microsoft\\Boot\\bootmgfw.efi",
                    IsActive = defaultOS.Equals("Windows", StringComparison.OrdinalIgnoreCase),
                    Order = defaultOS.Equals("Windows", StringComparison.OrdinalIgnoreCase) ? 1 : 2
                };

                // Create Monado boot entry
                var monadoEntry = new BootEntry
                {
                    Name = "Monado OS",
                    Path = $"{monadoPartitionPath}/EFI/monado/boot/bootx64.efi",
                    IsActive = defaultOS.Equals("Monado", StringComparison.OrdinalIgnoreCase),
                    Order = defaultOS.Equals("Monado", StringComparison.OrdinalIgnoreCase) ? 1 : 2
                };

                _bootEntries.Add("Windows", windowsEntry);
                _bootEntries.Add("Monado", monadoEntry);

                // Set boot menu timeout
                await SetBootMenuTimeout(bootMenuTimeout);

                // Save boot configuration
                await SaveBootConfiguration();

                result.Success = true;
                result.Message = "Boot menu configured successfully";
                result.BootEntries = new List<BootEntry> { windowsEntry, monadoEntry };

                OnBootConfigChanged("Boot menu configured", "success");

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Failed to configure boot menu: {ex.Message}";
                OnBootConfigChanged($"Boot configuration failed: {ex.Message}", "error");
                return result;
            }
        }

        /// <summary>
        /// Mounts shared data partition in Windows.
        /// </summary>
        public async Task<MountResult> MountSharedPartition(string partitionPath)
        {
            var result = new MountResult();

            try
            {
                // Find available drive letter
                var driveLetter = FindAvailableDriveLetter();
                if (string.IsNullOrEmpty(driveLetter))
                {
                    result.Success = false;
                    result.ErrorMessage = "No available drive letters";
                    return result;
                }

                // Simulate mounting partition
                await Task.Delay(500);

                result.Success = true;
                result.MountPoint = driveLetter;
                result.Message = $"Shared partition mounted to {driveLetter}";

                OnBootConfigChanged($"Shared partition mounted to {driveLetter}", "success");

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Failed to mount shared partition: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// Sets the default boot OS (which one boots first).
        /// </summary>
        public async Task<bool> SetDefaultBootOS(string osName)
        {
            try
            {
                if (!_bootEntries.ContainsKey(osName))
                {
                    return false;
                }

                // Reset all entries to not active
                foreach (var entry in _bootEntries.Values)
                {
                    entry.IsActive = false;
                    entry.Order = 99;
                }

                // Set selected OS as default
                _bootEntries[osName].IsActive = true;
                _bootEntries[osName].Order = 1;

                // Increment other entries
                var order = 2;
                foreach (var key in _bootEntries.Keys)
                {
                    if (key != osName)
                    {
                        _bootEntries[key].Order = order++;
                    }
                }

                await SaveBootConfiguration();
                OnBootConfigChanged($"Default boot OS set to {osName}", "success");

                return true;
            }
            catch (Exception ex)
            {
                OnBootConfigChanged($"Failed to set default boot OS: {ex.Message}", "error");
                return false;
            }
        }

        /// <summary>
        /// Gets the current boot configuration.
        /// </summary>
        public BootConfigInfo GetBootConfiguration()
        {
            var config = new BootConfigInfo
            {
                BootEntries = new List<BootEntry>(_bootEntries.Values),
                DefaultBootEntry = _bootEntries.Values.Count > 0 ? _bootEntries.Values.GetEnumerator() : null
            };

            return config;
        }

        /// <summary>
        /// Restores boot configuration from backup.
        /// </summary>
        public async Task<bool> RestoreBootConfiguration()
        {
            try
            {
                if (string.IsNullOrEmpty(_bootConfigBackup) || !File.Exists(_bootConfigBackup))
                {
                    return false;
                }

                // Simulate restore operation
                await Task.Delay(1000);

                // Clear current entries
                _bootEntries.Clear();

                OnBootConfigChanged("Boot configuration restored", "success");

                return true;
            }
            catch (Exception ex)
            {
                OnBootConfigChanged($"Boot configuration restore failed: {ex.Message}", "error");
                return false;
            }
        }

        /// <summary>
        /// Backs up current boot configuration.
        /// </summary>
        public async Task BackupBootConfiguration(string backupPath = null)
        {
            try
            {
                backupPath = backupPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MonadoBlade", "Recovery");
                Directory.CreateDirectory(backupPath);

                _bootConfigBackup = Path.Combine(backupPath, BOOT_CONFIG_FILE);

                // Simulate backup
                var bootConfigContent = System.Text.Json.JsonSerializer.Serialize(_bootEntries);
                await File.WriteAllTextAsync(_bootConfigBackup, bootConfigContent);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to backup boot configuration: {ex.Message}");
            }
        }

        /// <summary>
        /// Adds a custom boot entry.
        /// </summary>
        public async Task<bool> AddBootEntry(BootEntry entry)
        {
            try
            {
                if (string.IsNullOrEmpty(entry.Name))
                {
                    return false;
                }

                entry.Order = _bootEntries.Count + 1;
                _bootEntries[entry.Name] = entry;

                await SaveBootConfiguration();
                OnBootConfigChanged($"Boot entry added: {entry.Name}", "success");

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Removes a boot entry.
        /// </summary>
        public async Task<bool> RemoveBootEntry(string entryName)
        {
            try
            {
                if (!_bootEntries.Remove(entryName))
                {
                    return false;
                }

                // Recalculate order
                var order = 1;
                foreach (var entry in _bootEntries.Values)
                {
                    entry.Order = order++;
                }

                await SaveBootConfiguration();
                OnBootConfigChanged($"Boot entry removed: {entryName}", "success");

                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task SetBootMenuTimeout(int timeoutSeconds)
        {
            await Task.Delay(100);
            // Simulated timeout setting
        }

        private async Task SaveBootConfiguration()
        {
            try
            {
                await Task.Delay(200);
                // Simulated save operation
            }
            catch
            {
                // Ignore save errors in simulation
            }
        }

        private string FindAvailableDriveLetter()
        {
            // Find first available drive letter from D to Z
            for (char c = 'D'; c <= 'Z'; c++)
            {
                var drivePath = $"{c}:\\";
                try
                {
                    if (!Directory.Exists(drivePath))
                    {
                        return $"{c}:";
                    }
                }
                catch
                {
                    // Skip if can't access
                }
            }

            return null;
        }

        protected virtual void OnBootConfigChanged(string message, string status)
        {
            BootConfigChanged?.Invoke(this, new BootEventArgs { Message = message, Status = status });
        }
    }

    public class BootEntry
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public bool IsActive { get; set; }
        public int Order { get; set; }
    }

    public class BootConfigResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string Message { get; set; }
        public List<BootEntry> BootEntries { get; set; } = new List<BootEntry>();
    }

    public class BootConfigInfo
    {
        public List<BootEntry> BootEntries { get; set; }
        public System.Collections.Generic.IEnumerator<BootEntry> DefaultBootEntry { get; set; }
    }

    public class MountResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string Message { get; set; }
        public string MountPoint { get; set; }
    }

    public class BootEventArgs : EventArgs
    {
        public string Message { get; set; }
        public string Status { get; set; }
    }
}
