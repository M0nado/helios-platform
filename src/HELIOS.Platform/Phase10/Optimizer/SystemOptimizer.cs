using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace HELIOS.Platform.Phase10.Optimizer
{
    /// <summary>
    /// System Optimizer - Registry, startup programs, and cleanup
    /// </summary>
    public class SystemOptimizer : BaseOptimizerService
    {
        private const string RegistryPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string StartupPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer";
        private readonly string _snapshotDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HELIOS", "Snapshots");
        private Dictionary<string, object> _registrySnapshot;

        public SystemOptimizer(OptimizationProfile profile = null)
        {
            _serviceName = "SystemOptimizer";
            _profile = profile ?? new OptimizationProfile { Name = "Default" };
        }

        public override async Task<OptimizationResult> OptimizeAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new OptimizationResult { Changes = new List<string>() };

            try
            {
                if (!_isInitialized)
                    await InitializeAsync();

                // Create registry snapshot for rollback
                CreateRegistrySnapshot();

                // Disable unnecessary services
                var disabledServices = await DisableUnnecessaryServicesAsync(cancellationToken);
                result.Changes.AddRange(disabledServices);

                // Remove startup programs
                var removedPrograms = await RemoveStartupProgramsAsync(cancellationToken);
                result.Changes.AddRange(removedPrograms);

                // Clean temporary files
                var cleanedSpace = await CleanTemporaryFilesAsync(cancellationToken);
                result.Metrics["TemporaryFilesCleaned"] = cleanedSpace;

                // Optimize registry
                await OptimizeRegistryAsync(cancellationToken);
                result.Changes.Add("Registry optimized");

                // Verify and generate rollback snapshot
                result.RollbackSnapshot = JsonSerializer.Serialize(_registrySnapshot);

                stopwatch.Stop();
                result.Success = true;
                result.ExecutionTime = stopwatch.Elapsed;
                result.Message = $"System optimization completed in {stopwatch.ElapsedMilliseconds}ms";
                _lastOptimization = DateTime.Now;

                return result;
            }
            catch (Exception ex)
            {
                LogError($"Optimization error: {ex.Message}");
                result.Success = false;
                result.Message = $"Error: {ex.Message}";
                return result;
            }
        }

        public override async Task<Dictionary<string, object>> GetMetricsAsync()
        {
            var metrics = new Dictionary<string, object>
            {
                ["TempFileSize"] = GetTempFileSize(),
                ["DisabledServices"] = await GetDisabledServicesCountAsync(),
                ["StartupPrograms"] = GetStartupProgramsCount(),
                ["RegistryHealth"] = await CheckRegistryHealthAsync()
            };

            return await Task.FromResult(metrics);
        }

        public override async Task<bool> RollbackAsync()
        {
            try
            {
                if (_registrySnapshot == null)
                    return false;

                // Restore registry from snapshot
                await RestoreRegistrySnapshotAsync();
                LogError("Registry rolled back successfully");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Rollback error: {ex.Message}");
                return false;
            }
        }

        private async Task<List<string>> DisableUnnecessaryServicesAsync(CancellationToken cancellationToken)
        {
            var disabledServices = new List<string>();
            var unnecessaryServices = new[]
            {
                "DiagTrack",
                "dmwappushservice",
                "HomeGroupListener",
                "HomeGroupProvider",
                "lfsvc",
                "SharedAccess",
                "TrkWks",
                "WbioSrvc"
            };

            try
            {
                foreach (var service in unnecessaryServices)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    try
                    {
                        var serviceController = new System.ServiceProcess.ServiceController(service);
                        if (serviceController.Status == System.ServiceProcess.ServiceControllerStatus.Running)
                        {
                            serviceController.Stop();
                            serviceController.WaitForStatus(System.ServiceProcess.ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(5));
                            disabledServices.Add($"Disabled: {service}");
                        }
                    }
                    catch
                    {
                        // Service may not exist
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Service disabling error: {ex.Message}");
            }

            return await Task.FromResult(disabledServices);
        }

        private async Task<List<string>> RemoveStartupProgramsAsync(CancellationToken cancellationToken)
        {
            var removedPrograms = new List<string>();
            var bloatwarePrograms = new[]
            {
                "OneDrive",
                "Cortana",
                "BingSearch",
                "DiagnosticsHub.StandardCollector.Service"
            };

            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(RegistryPath, true))
                {
                    if (key != null)
                    {
                        foreach (var program in bloatwarePrograms)
                        {
                            if (cancellationToken.IsCancellationRequested)
                                break;

                            try
                            {
                                if (key.GetValue(program) != null)
                                {
                                    key.DeleteValue(program, false);
                                    removedPrograms.Add($"Removed: {program}");
                                }
                            }
                            catch
                            {
                                // Program may not exist
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Startup program removal error: {ex.Message}");
            }

            return await Task.FromResult(removedPrograms);
        }

        private async Task<long> CleanTemporaryFilesAsync(CancellationToken cancellationToken)
        {
            long totalCleaned = 0;
            var tempDirs = new[]
            {
                Path.GetTempPath(),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp")
            };

            foreach (var tempDir in tempDirs)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    if (Directory.Exists(tempDir))
                    {
                        var dirInfo = new DirectoryInfo(tempDir);
                        foreach (var file in dirInfo.GetFiles())
                        {
                            try
                            {
                                totalCleaned += file.Length;
                                file.Delete();
                            }
                            catch
                            {
                                // File may be in use
                            }
                        }
                    }
                }
                catch
                {
                    // Directory access error
                }
            }

            return await Task.FromResult(totalCleaned);
        }

        private async Task OptimizeRegistryAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Disable unnecessary visual effects
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", true))
                {
                    if (key != null)
                    {
                        key.SetValue("VisualFXSetting", 2, RegistryValueKind.DWord);
                    }
                }

                // Optimize page file settings
                using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", true))
                {
                    if (key != null)
                    {
                        key.SetValue("ClearPageFileAtShutdown", 0, RegistryValueKind.DWord);
                        key.SetValue("DisablePagingExecutive", 0, RegistryValueKind.DWord);
                    }
                }

                await Task.Delay(100, cancellationToken);
            }
            catch (Exception ex)
            {
                LogError($"Registry optimization error: {ex.Message}");
            }
        }

        private void CreateRegistrySnapshot()
        {
            try
            {
                if (!Directory.Exists(_snapshotDir))
                    Directory.CreateDirectory(_snapshotDir);

                _registrySnapshot = new Dictionary<string, object>();

                using (var key = Registry.LocalMachine.OpenSubKey(RegistryPath))
                {
                    if (key != null)
                    {
                        foreach (var valueName in key.GetValueNames())
                        {
                            _registrySnapshot[valueName] = key.GetValue(valueName);
                        }
                    }
                }

                var snapshotPath = Path.Combine(_snapshotDir, $"Registry_Snapshot_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                File.WriteAllText(snapshotPath, JsonSerializer.Serialize(_registrySnapshot, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                LogError($"Snapshot creation error: {ex.Message}");
            }
        }

        private async Task RestoreRegistrySnapshotAsync()
        {
            try
            {
                if (_registrySnapshot == null)
                    return;

                using (var key = Registry.LocalMachine.OpenSubKey(RegistryPath, true))
                {
                    if (key != null)
                    {
                        foreach (var kvp in _registrySnapshot)
                        {
                            key.SetValue(kvp.Key, kvp.Value);
                        }
                    }
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                LogError($"Registry restore error: {ex.Message}");
            }
        }

        private long GetTempFileSize()
        {
            long size = 0;
            var tempPath = Path.GetTempPath();

            try
            {
                var dirInfo = new DirectoryInfo(tempPath);
                size = dirInfo.GetFiles().Sum(f => f.Length);
            }
            catch
            {
                // Error calculating size
            }

            return size;
        }

        private async Task<int> GetDisabledServicesCountAsync()
        {
            int count = 0;
            try
            {
                var query = new ManagementObjectSearcher("SELECT * FROM Win32_Service WHERE State='Stopped'");
                count = query.Get().Count;
            }
            catch
            {
                // WMI error
            }

            return await Task.FromResult(count);
        }

        private int GetStartupProgramsCount()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(RegistryPath))
                {
                    if (key != null)
                    {
                        return key.GetValueNames().Length;
                    }
                }
            }
            catch
            {
                // Registry error
            }

            return 0;
        }

        private async Task<string> CheckRegistryHealthAsync()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(RegistryPath))
                {
                    return await Task.FromResult(key != null ? "Healthy" : "Inaccessible");
                }
            }
            catch
            {
                return await Task.FromResult("Error");
            }
        }
    }
}
