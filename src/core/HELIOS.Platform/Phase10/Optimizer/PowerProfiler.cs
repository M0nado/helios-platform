using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace HELIOS.Platform.Phase10.Optimizer
{
    /// <summary>
    /// Power Profiler - Power mode optimization and thermal management
    /// </summary>
    public class PowerProfiler : BaseOptimizerService
    {
        private PowerProfileType _currentProfile = PowerProfileType.BalancedPower;
        private Dictionary<PowerProfileType, string> _profileGuids = new();

        public PowerProfiler(OptimizationProfile profile = null)
        {
            _serviceName = "PowerProfiler";
            _profile = profile ?? new OptimizationProfile { Name = "Default" };
            InitializePowerProfiles();
        }

        private void InitializePowerProfiles()
        {
            // Power plan GUIDs
            _profileGuids[PowerProfileType.BalancedPower] = "381b4222-f694-41f0-9685-ff5bb260df2e";
            _profileGuids[PowerProfileType.PowerSaver] = "a1841308-3541-4fab-bc81-f71556f20b4a";
            _profileGuids[PowerProfileType.Gaming] = "8c5e7fda-e8bf-45a9-a30a-202b65125c5c";
        }

        public override async Task<OptimizationResult> OptimizeAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new OptimizationResult { Changes = new List<string>() };

            try
            {
                if (!_isInitialized)
                    await InitializeAsync();

                // Detect system profile
                var detectedProfile = await DetectSystemProfileAsync();
                result.Changes.Add($"Profile detected: {detectedProfile}");

                // Apply power plan
                await ApplyPowerPlanAsync(detectedProfile, cancellationToken, result);

                // Set CPU frequency
                await SetCPUFrequencyAsync(cancellationToken, result);

                // Configure power buttons
                await ConfigurePowerButtonsAsync(cancellationToken, result);

                // Monitor thermals
                var thermals = await MonitorThermalsAsync();
                result.Metrics["CPUTemperature"] = thermals.Item1;
                result.Metrics["GPUTemperature"] = thermals.Item2;

                stopwatch.Stop();
                result.Success = true;
                result.ExecutionTime = stopwatch.Elapsed;
                result.Message = $"Power profiling completed in {stopwatch.ElapsedMilliseconds}ms";
                _lastOptimization = DateTime.Now;

                return result;
            }
            catch (Exception ex)
            {
                LogError($"Power profiling error: {ex.Message}");
                result.Success = false;
                result.Message = $"Error: {ex.Message}";
                return result;
            }
        }

        public override async Task<Dictionary<string, object>> GetMetricsAsync()
        {
            var metrics = new Dictionary<string, object>
            {
                ["CurrentProfile"] = _currentProfile.ToString(),
                ["PowerMode"] = GetPowerMode(),
                ["BatteryStatus"] = GetBatteryStatus(),
                ["CPUFrequency"] = GetCPUFrequency(),
                ["ThermalStatus"] = await GetThermalStatusAsync()
            };

            return await Task.FromResult(metrics);
        }

        private async Task<string> DetectSystemProfileAsync()
        {
            try
            {
                var query = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
                var results = query.Get();

                if (results.Count > 0)
                {
                    // Detect if it's a laptop
                    var batteryQuery = new ManagementObjectSearcher("SELECT * FROM Win32_Battery");
                    var batteryResults = batteryQuery.Get();

                    if (batteryResults.Count > 0)
                    {
                        _currentProfile = PowerProfileType.BalancedPower;
                        return "Laptop - Balanced Power";
                    }
                }

                // Default to gaming profile for high-performance systems
                var processorCount = Environment.ProcessorCount;
                if (processorCount >= 8)
                {
                    _currentProfile = PowerProfileType.Gaming;
                    return "Gaming";
                }

                _currentProfile = PowerProfileType.Work;
                return "Work";
            }
            catch
            {
                _currentProfile = PowerProfileType.BalancedPower;
                return "BalancedPower";
            }
        }

        private async Task ApplyPowerPlanAsync(string profile, CancellationToken cancellationToken, OptimizationResult result)
        {
            try
            {
                PowerProfileType profileType = profile.Contains("Gaming", StringComparison.OrdinalIgnoreCase) 
                    ? PowerProfileType.Gaming 
                    : PowerProfileType.BalancedPower;

                _currentProfile = profileType;

                // Apply power plan via registry
                using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Power", true))
                {
                    if (key != null)
                    {
                        switch (profileType)
                        {
                            case PowerProfileType.Gaming:
                                key.SetValue("LastID", 1, RegistryValueKind.DWord);
                                result.Changes.Add("Gaming power plan applied");
                                break;
                            case PowerProfileType.Work:
                                key.SetValue("LastID", 0, RegistryValueKind.DWord);
                                result.Changes.Add("Work power plan applied");
                                break;
                            default:
                                key.SetValue("LastID", 0, RegistryValueKind.DWord);
                                result.Changes.Add("Balanced power plan applied");
                                break;
                        }
                    }
                }

                result.Metrics["PowerPlanApplied"] = profileType.ToString();
                await Task.Delay(100, cancellationToken);
            }
            catch (Exception ex)
            {
                LogError($"Power plan application error: {ex.Message}");
            }
        }

        private async Task SetCPUFrequencyAsync(CancellationToken cancellationToken, OptimizationResult result)
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\75b0ae3b-bcc7-4a72-8c11-0996352aea5d", true))
                {
                    if (key != null)
                    {
                        switch (_currentProfile)
                        {
                            case PowerProfileType.Gaming:
                                key.SetValue("ACSettingIndex", 100, RegistryValueKind.DWord);
                                result.Changes.Add("CPU maximum frequency set to 100%");
                                break;
                            case PowerProfileType.BalancedPower:
                                key.SetValue("ACSettingIndex", 80, RegistryValueKind.DWord);
                                result.Changes.Add("CPU frequency set to 80%");
                                break;
                            default:
                                key.SetValue("ACSettingIndex", 100, RegistryValueKind.DWord);
                                break;
                        }
                    }
                }

                result.Metrics["CPUFrequencyConfigured"] = true;
                await Task.Delay(100, cancellationToken);
            }
            catch (Exception ex)
            {
                LogError($"CPU frequency setting error: {ex.Message}");
            }
        }

        private async Task ConfigurePowerButtonsAsync(CancellationToken cancellationToken, OptimizationResult result)
        {
            try
            {
                // Configure power button behavior
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\PowerMenuObject", true))
                {
                    if (key != null)
                    {
                        key.SetValue("StartupType", 2, RegistryValueKind.DWord);
                        result.Changes.Add("Power button configured");
                    }
                }

                // Configure sleep button
                using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Power", true))
                {
                    if (key != null)
                    {
                        key.SetValue("SleepButtonAction", 0, RegistryValueKind.DWord);
                        result.Changes.Add("Sleep button configured");
                    }
                }

                result.Metrics["PowerButtonsConfigured"] = true;
                await Task.Delay(100, cancellationToken);
            }
            catch (Exception ex)
            {
                LogError($"Power button configuration error: {ex.Message}");
            }
        }

        private async Task<(float, float)> MonitorThermalsAsync()
        {
            float cpuTemp = 0;
            float gpuTemp = 0;

            try
            {
                // CPU Temperature (via WMI)
                var cpuQuery = new ManagementObjectSearcher("SELECT * FROM Win32_TemperatureProbe");
                var cpuResults = cpuQuery.Get();

                foreach (ManagementObject probe in cpuResults)
                {
                    try
                    {
                        var temp = Convert.ToInt32(probe["CurrentReading"]) / 10f;
                        if (temp > cpuTemp)
                            cpuTemp = temp;
                    }
                    catch
                    {
                        // Temperature reading error
                    }
                }

                // GPU Temperature (estimated)
                var gpuQuery = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
                gpuResults = gpuQuery.Get();

                if (gpuResults.Count > 0)
                {
                    gpuTemp = 45; // Default estimate
                }
            }
            catch
            {
                // WMI error
            }

            return await Task.FromResult((cpuTemp, gpuTemp));
        }

        private string GetPowerMode()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Power"))
                {
                    if (key != null)
                    {
                        var mode = key.GetValue("LastID");
                        return mode?.ToString() ?? "Unknown";
                    }
                }
            }
            catch
            {
                // Registry error
            }

            return "Unknown";
        }

        private string GetBatteryStatus()
        {
            try
            {
                var query = new ManagementObjectSearcher("SELECT * FROM Win32_Battery");
                var results = query.Get();

                if (results.Count > 0)
                {
                    foreach (ManagementObject battery in results)
                    {
                        var status = Convert.ToInt32(battery["Status"]);
                        return status == 2 ? "Discharging" : "Charging";
                    }
                }
            }
            catch
            {
                // Battery query error
            }

            return "Not available";
        }

        private string GetCPUFrequency()
        {
            try
            {
                var query = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
                var results = query.Get();

                if (results.Count > 0)
                {
                    var processor = results[0];
                    var maxSpeed = Convert.ToInt32(processor["MaxClockSpeed"]);
                    return $"{maxSpeed} MHz";
                }
            }
            catch
            {
                // Processor query error
            }

            return "Unknown";
        }

        private async Task<string> GetThermalStatusAsync()
        {
            var (cpuTemp, gpuTemp) = await MonitorThermalsAsync();

            if (cpuTemp > 80)
                return "Hot - Consider cooling";
            if (cpuTemp > 60)
                return "Warm - Normal operation";
            if (cpuTemp > 40)
                return "Cool - Optimal";

            return "Unknown";
        }
    }
}
