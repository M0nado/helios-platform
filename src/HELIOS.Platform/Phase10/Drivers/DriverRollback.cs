using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HELIOS.Platform.Phase10.Drivers
{
    /// <summary>
    /// Manages driver rollback and recovery operations.
    /// </summary>
    public class DriverRollback
    {
        private readonly DriverRepository _repository;
        private readonly SemaphoreSlim _semaphore;
        private readonly string _backupDirectory;
        private readonly DriverHealthMonitor _healthMonitor;

        public DriverRollback(DriverRepository repository, DriverHealthMonitor healthMonitor = null)
        {
            _repository = repository;
            _healthMonitor = healthMonitor;
            _semaphore = new SemaphoreSlim(1);
            _backupDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramData), "HELIOS", "DriverBackups");
            Directory.CreateDirectory(_backupDirectory);
        }

        /// <summary>
        /// Backup driver before update.
        /// </summary>
        public async Task<bool> BackupDriverAsync(string driverId)
        {
            await _semaphore.WaitAsync();
            try
            {
                var driverInfo = await _repository.GetDriverAsync(driverId);
                if (driverInfo == null)
                    return false;

                var sourceFile = await _repository.GetDriverFilePathAsync(driverId);
                if (!File.Exists(sourceFile))
                    return false;

                var backupDir = Path.Combine(_backupDirectory, driverId);
                Directory.CreateDirectory(backupDir);

                var backupFile = Path.Combine(backupDir, $"{driverId}_v{driverInfo.Version}_{DateTime.UtcNow.Ticks}.bak");
                File.Copy(sourceFile, backupFile, overwrite: true);

                return File.Exists(backupFile);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Get available backups for driver.
        /// </summary>
        public async Task<List<DriverInfo>> GetAvailableBackupsAsync(string driverId)
        {
            return await Task.Run(() =>
            {
                var backups = new List<DriverInfo>();
                var backupDir = Path.Combine(_backupDirectory, driverId);

                if (!Directory.Exists(backupDir))
                    return backups;

                try
                {
                    var backupFiles = Directory.GetFiles(backupDir, "*.bak");
                    foreach (var file in backupFiles.OrderByDescending(f => f))
                    {
                        try
                        {
                            var fileInfo = new FileInfo(file);
                            backups.Add(new DriverInfo
                            {
                                DriverId = driverId,
                                Name = Path.GetFileNameWithoutExtension(file),
                                LastUpdated = fileInfo.CreationTime,
                                Version = ExtractVersionFromFileName(file)
                            });
                        }
                        catch { }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to enumerate backups: {ex.Message}");
                }

                return backups;
            });
        }

        /// <summary>
        /// Rollback driver to previous version.
        /// </summary>
        public async Task<bool> RollbackDriverAsync(string driverId)
        {
            await _semaphore.WaitAsync();
            try
            {
                var backups = await GetAvailableBackupsAsync(driverId);
                if (backups.Count == 0)
                    return false;

                var latestBackup = backups.FirstOrDefault();
                if (latestBackup == null)
                    return false;

                var backupDir = Path.Combine(_backupDirectory, driverId);
                var backupFiles = Directory.GetFiles(backupDir, "*.bak").OrderByDescending(f => f);
                var latestBackupFile = backupFiles.FirstOrDefault();

                if (latestBackupFile == null || !File.Exists(latestBackupFile))
                    return false;

                var driverInfo = await _repository.GetDriverAsync(driverId);
                if (driverInfo == null)
                    return false;

                try
                {
                    var targetPath = await _repository.GetDriverFilePathAsync(driverId);
                    if (targetPath != null)
                    {
                        File.Copy(latestBackupFile, targetPath, overwrite: true);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Rollback failed: {ex.Message}");
                    return false;
                }

                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Detect problematic drivers.
        /// </summary>
        public async Task<List<string>> DetectProblematicDriversAsync()
        {
            var problematicDrivers = new List<string>();

            try
            {
                if (_healthMonitor != null)
                {
                    var healthStatuses = await _healthMonitor.GetAllHealthStatusesAsync();
                    foreach (var status in healthStatuses)
                    {
                        if (!status.IsHealthy && status.CrashCount > 0)
                        {
                            problematicDrivers.Add(status.DriverId);
                        }
                    }
                }
                else
                {
                    var drivers = await _repository.GetAllDriversAsync();
                    problematicDrivers = drivers
                        .Where(d => !d.IsStable)
                        .Select(d => d.DriverId)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Problem detection failed: {ex.Message}");
            }

            return problematicDrivers;
        }

        /// <summary>
        /// Automatic rollback if driver is problematic.
        /// </summary>
        public async Task<bool> AutoRollbackAsync(string driverId)
        {
            await _semaphore.WaitAsync();
            try
            {
                var problematicDrivers = await DetectProblematicDriversAsync();
                if (!problematicDrivers.Contains(driverId))
                    return false;

                var backups = await GetAvailableBackupsAsync(driverId);
                if (backups.Count == 0)
                    return false;

                return await RollbackDriverAsync(driverId);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Restore from Windows restore point.
        /// </summary>
        public async Task<bool> RestoreFromRestorePointAsync(string restorePointId)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var script = $@"
rstrui.exe
";

                    using (var process = new Process())
                    {
                        process.StartInfo = new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = $"/c {script}",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };

                        process.Start();
                        process.WaitForExit(60000);
                        return process.ExitCode == 0;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Restore point restore failed: {ex.Message}");
                    return false;
                }
            });
        }

        /// <summary>
        /// Clear old backups (older than specified days).
        /// </summary>
        public async Task ClearOldBackupsAsync(int daysOld)
        {
            await Task.Run(() =>
            {
                try
                {
                    var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);

                    var driverDirs = Directory.GetDirectories(_backupDirectory);
                    foreach (var driverDir in driverDirs)
                    {
                        var backupFiles = Directory.GetFiles(driverDir, "*.bak");
                        foreach (var file in backupFiles)
                        {
                            var fileInfo = new FileInfo(file);
                            if (fileInfo.CreationTime < cutoffDate)
                            {
                                File.Delete(file);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Backup cleanup failed: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Get backup directory size.
        /// </summary>
        public async Task<long> GetBackupSizeAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!Directory.Exists(_backupDirectory))
                        return 0;

                    var dirInfo = new DirectoryInfo(_backupDirectory);
                    return dirInfo.EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);
                }
                catch
                {
                    return 0;
                }
            });
        }

        /// <summary>
        /// Extract version from backup filename.
        /// </summary>
        private string ExtractVersionFromFileName(string fileName)
        {
            try
            {
                var parts = Path.GetFileNameWithoutExtension(fileName).Split('_');
                if (parts.Length >= 2)
                {
                    var versionPart = parts[1];
                    if (versionPart.StartsWith("v"))
                    {
                        return versionPart.Substring(1);
                    }
                }
            }
            catch { }

            return "Unknown";
        }

        /// <summary>
        /// Get all backups for all drivers.
        /// </summary>
        public async Task<Dictionary<string, List<DriverInfo>>> GetAllBackupsAsync()
        {
            return await Task.Run(() =>
            {
                var allBackups = new Dictionary<string, List<DriverInfo>>();

                try
                {
                    var driverDirs = Directory.GetDirectories(_backupDirectory);
                    foreach (var driverDir in driverDirs)
                    {
                        var driverId = Path.GetFileName(driverDir);
                        var backups = new List<DriverInfo>();

                        var backupFiles = Directory.GetFiles(driverDir, "*.bak")
                            .OrderByDescending(f => f);

                        foreach (var file in backupFiles)
                        {
                            var fileInfo = new FileInfo(file);
                            backups.Add(new DriverInfo
                            {
                                DriverId = driverId,
                                Name = Path.GetFileNameWithoutExtension(file),
                                LastUpdated = fileInfo.CreationTime,
                                Version = ExtractVersionFromFileName(file)
                            });
                        }

                        if (backups.Count > 0)
                        {
                            allBackups[driverId] = backups;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to enumerate all backups: {ex.Message}");
                }

                return allBackups;
            });
        }

        /// <summary>
        /// Delete specific backup.
        /// </summary>
        public async Task<bool> DeleteBackupAsync(string driverId, string backupFileName)
        {
            await _semaphore.WaitAsync();
            try
            {
                var backupPath = Path.Combine(_backupDirectory, driverId, backupFileName);
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                    return true;
                }

                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Restore specific backup.
        /// </summary>
        public async Task<bool> RestoreBackupAsync(string driverId, string backupFileName)
        {
            await _semaphore.WaitAsync();
            try
            {
                var backupPath = Path.Combine(_backupDirectory, driverId, backupFileName);
                if (!File.Exists(backupPath))
                    return false;

                var targetPath = await _repository.GetDriverFilePathAsync(driverId);
                if (targetPath == null)
                    return false;

                File.Copy(backupPath, targetPath, overwrite: true);
                return true;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
