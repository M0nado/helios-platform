using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace HELIOS.Platform.Phase10.Drivers
{
    /// <summary>
    /// Handles driver installation and setup.
    /// </summary>
    public class DriverInstaller
    {
        private readonly DriverRepository _repository;
        private readonly SemaphoreSlim _semaphore;
        private readonly string _installLogDirectory;
        private readonly List<InstallationResult> _installationHistory;

        public DriverInstaller(DriverRepository repository)
        {
            _repository = repository;
            _semaphore = new SemaphoreSlim(1);
            _installLogDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "HELIOS", "DriverLogs");
            Directory.CreateDirectory(_installLogDirectory);
            _installationHistory = new List<InstallationResult>();
        }

        /// <summary>
        /// Install driver from repository.
        /// </summary>
        public async Task<InstallationResult> InstallDriverAsync(string driverId)
        {
            await _semaphore.WaitAsync();
            try
            {
                var driverInfo = await _repository.GetDriverAsync(driverId);
                if (driverInfo == null)
                {
                    return new InstallationResult
                    {
                        Success = false,
                        DriverName = driverId,
                        Message = "Driver not found in repository",
                        Error = new FileNotFoundException("Driver not found")
                    };
                }

                var driverFilePath = await _repository.GetDriverFilePathAsync(driverId);
                if (!File.Exists(driverFilePath))
                {
                    return new InstallationResult
                    {
                        Success = false,
                        DriverName = driverInfo.Name,
                        Message = "Driver file not found",
                        Error = new FileNotFoundException(driverFilePath)
                    };
                }

                var logPath = Path.Combine(_installLogDirectory, $"{driverId}_{DateTime.UtcNow.Ticks}.log");
                var result = new InstallationResult
                {
                    DriverName = driverInfo.Name,
                    Version = driverInfo.Version,
                    InstallDate = DateTime.UtcNow,
                    LogPath = logPath
                };

                try
                {
                    if (driverFilePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        result.Success = await InstallExeDriverAsync(driverFilePath, logPath);
                    }
                    else if (driverFilePath.EndsWith(".inf", StringComparison.OrdinalIgnoreCase))
                    {
                        result.Success = await InstallInfDriverAsync(driverFilePath, logPath);
                    }
                    else if (driverFilePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        result.Success = await InstallZipDriverAsync(driverFilePath, logPath);
                    }
                    else
                    {
                        result.Success = false;
                        result.Message = "Unsupported driver file format";
                    }

                    if (result.Success)
                    {
                        result.Message = "Driver installed successfully";
                        driverInfo.InstalledDate = DateTime.UtcNow;
                        await _repository.UpdateDriverInfoAsync(driverId, driverInfo);
                    }
                    else
                    {
                        result.Message = "Driver installation failed";
                    }
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.Message = $"Installation error: {ex.Message}";
                    result.Error = ex;
                    await LogErrorAsync(logPath, ex);
                }

                _installationHistory.Add(result);
                return result;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Install .exe driver silently.
        /// </summary>
        private async Task<bool> InstallExeDriverAsync(string exePath, string logPath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var processInfo = new ProcessStartInfo
                    {
                        FileName = exePath,
                        Arguments = "/s /d",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    using (var process = Process.Start(processInfo))
                    {
                        var output = process.StandardOutput.ReadToEnd();
                        var error = process.StandardError.ReadToEnd();
                        process.WaitForExit(300000);

                        File.WriteAllText(logPath, $"Output:\n{output}\n\nErrors:\n{error}");
                        return process.ExitCode == 0;
                    }
                }
                catch (Exception ex)
                {
                    File.WriteAllText(logPath, $"Installation failed: {ex.Message}");
                    return false;
                }
            });
        }

        /// <summary>
        /// Install .inf driver using pnputil.
        /// </summary>
        private async Task<bool> InstallInfDriverAsync(string infPath, string logPath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var processInfo = new ProcessStartInfo
                    {
                        FileName = "pnputil.exe",
                        Arguments = $"/add-driver \"{infPath}\" /install",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    using (var process = Process.Start(processInfo))
                    {
                        var output = process.StandardOutput.ReadToEnd();
                        var error = process.StandardError.ReadToEnd();
                        process.WaitForExit(300000);

                        File.WriteAllText(logPath, $"Output:\n{output}\n\nErrors:\n{error}");
                        return process.ExitCode == 0;
                    }
                }
                catch (Exception ex)
                {
                    File.WriteAllText(logPath, $"Installation failed: {ex.Message}");
                    return false;
                }
            });
        }

        /// <summary>
        /// Extract and install .zip driver.
        /// </summary>
        private async Task<bool> InstallZipDriverAsync(string zipPath, string logPath)
        {
            return await Task.Run(() =>
            {
                var extractPath = Path.Combine(Path.GetDirectoryName(zipPath), Path.GetFileNameWithoutExtension(zipPath));

                try
                {
                    System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractPath, overwriteFiles: true);

                    var infFiles = Directory.GetFiles(extractPath, "*.inf", SearchOption.AllDirectories);
                    if (infFiles.Length == 0)
                    {
                        File.WriteAllText(logPath, "No INF file found in ZIP");
                        return false;
                    }

                    foreach (var infFile in infFiles)
                    {
                        var processInfo = new ProcessStartInfo
                        {
                            FileName = "pnputil.exe",
                            Arguments = $"/add-driver \"{infFile}\" /install",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        };

                        using (var process = Process.Start(processInfo))
                        {
                            process.WaitForExit(300000);
                            if (process.ExitCode != 0)
                            {
                                return false;
                            }
                        }
                    }

                    Directory.Delete(extractPath, recursive: true);
                    File.WriteAllText(logPath, "Driver installed successfully from ZIP");
                    return true;
                }
                catch (Exception ex)
                {
                    File.WriteAllText(logPath, $"ZIP installation failed: {ex.Message}");
                    try { Directory.Delete(extractPath, recursive: true); } catch { }
                    return false;
                }
            });
        }

        /// <summary>
        /// Create Windows restore point.
        /// </summary>
        public async Task<bool> CreateRestorePointAsync(string description)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var script = $@"
$RestorePoint = New-ItemProperty -Path 'HKLM:\Software\Microsoft\Windows NT\CurrentVersion' -Name RestorePoint -Value 1 -Force
Write-Host 'Restore point created: {description}'
";

                    using (var process = new Process())
                    {
                        process.StartInfo = new ProcessStartInfo
                        {
                            FileName = "powershell.exe",
                            Arguments = $"-Command \"{script}\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        };

                        process.Start();
                        process.WaitForExit();
                        return process.ExitCode == 0;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Restore point creation failed: {ex.Message}");
                    return false;
                }
            });
        }

        /// <summary>
        /// Verify driver installation.
        /// </summary>
        public async Task<bool> VerifyInstallationAsync(string driverId)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var logPath = Path.Combine(_installLogDirectory, $"{driverId}_*.log");
                    var logs = Directory.GetFiles(_installLogDirectory, $"{driverId}_*.log");
                    if (logs.Length == 0)
                        return false;

                    var latestLog = logs.OrderByDescending(f => f).First();
                    var content = File.ReadAllText(latestLog);
                    return !content.Contains("failed", StringComparison.OrdinalIgnoreCase);
                }
                catch
                {
                    return false;
                }
            });
        }

        /// <summary>
        /// Get installation history.
        /// </summary>
        public async Task<List<InstallationResult>> GetInstallationHistoryAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                return _installationHistory.ToList();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Get installation history from disk.
        /// </summary>
        public async Task<List<InstallationResult>> GetHistoryFromDiskAsync()
        {
            return await Task.Run(() =>
            {
                var history = new List<InstallationResult>();

                try
                {
                    var logs = Directory.GetFiles(_installLogDirectory, "*.log");
                    foreach (var logFile in logs.Take(100))
                    {
                        try
                        {
                            var content = File.ReadAllText(logFile);
                            history.Add(new InstallationResult
                            {
                                LogPath = logFile,
                                InstallDate = File.GetCreationTime(logFile),
                                Success = !content.Contains("failed", StringComparison.OrdinalIgnoreCase),
                                Message = content.Substring(0, Math.Min(content.Length, 200))
                            });
                        }
                        catch { }
                    }
                }
                catch { }

                return history;
            });
        }

        /// <summary>
        /// Clear installation history.
        /// </summary>
        public async Task ClearHistoryAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                _installationHistory.Clear();

                try
                {
                    var logs = Directory.GetFiles(_installLogDirectory, "*.log");
                    foreach (var log in logs)
                    {
                        File.Delete(log);
                    }
                }
                catch { }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Log installation error.
        /// </summary>
        private async Task LogErrorAsync(string logPath, Exception ex)
        {
            await Task.Run(() =>
            {
                try
                {
                    var errorLog = $"Error: {ex.GetType().Name}\nMessage: {ex.Message}\nStackTrace: {ex.StackTrace}";
                    File.AppendAllText(logPath, $"\n\n{errorLog}");
                }
                catch { }
            });
        }

        /// <summary>
        /// Install all drivers in repository.
        /// </summary>
        public async Task<List<InstallationResult>> InstallAllAsync()
        {
            var drivers = await _repository.GetAllDriversAsync();
            var results = new List<InstallationResult>();

            foreach (var driver in drivers)
            {
                var result = await InstallDriverAsync(driver.DriverId);
                results.Add(result);
            }

            return results;
        }
    }
}
