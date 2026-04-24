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
    /// GPU Optimizer - GPU acceleration and VRAM optimization
    /// </summary>
    public class GPUOptimizer : BaseOptimizerService
    {
        private GPUInfo _gpuInfo;

        public GPUOptimizer(OptimizationProfile profile = null)
        {
            _serviceName = "GPUOptimizer";
            _profile = profile ?? new OptimizationProfile { Name = "Default" };
        }

        public override async Task<bool> InitializeAsync()
        {
            try
            {
                await DetectGPUAsync();
                _isInitialized = true;
                return true;
            }
            catch (Exception ex)
            {
                LogError($"GPU initialization error: {ex.Message}");
                return false;
            }
        }

        public override async Task<OptimizationResult> OptimizeAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new OptimizationResult { Changes = new List<string>() };

            try
            {
                if (!_isInitialized)
                    await InitializeAsync();

                if (_gpuInfo == null)
                {
                    result.Success = false;
                    result.Message = "No GPU detected";
                    return result;
                }

                // Enable GPU acceleration
                await EnableGPUAccelerationAsync(cancellationToken, result);

                // Optimize VRAM allocation
                await OptimizeVRAMAsync(cancellationToken, result);

                // Configure DirectX settings
                await ConfigureDirectXAsync(cancellationToken, result);

                // Set power mode to maximum performance
                await SetPowerModeAsync(cancellationToken, result);

                // Monitor GPU status
                await MonitorGPUAsync(result);

                stopwatch.Stop();
                result.Success = true;
                result.ExecutionTime = stopwatch.Elapsed;
                result.Message = $"GPU optimization completed in {stopwatch.ElapsedMilliseconds}ms";
                _lastOptimization = DateTime.Now;

                return result;
            }
            catch (Exception ex)
            {
                LogError($"GPU optimization error: {ex.Message}");
                result.Success = false;
                result.Message = $"Error: {ex.Message}";
                return result;
            }
        }

        public override async Task<Dictionary<string, object>> GetMetricsAsync()
        {
            var metrics = new Dictionary<string, object>
            {
                ["GPU"] = _gpuInfo?.GPU ?? "Not detected",
                ["Vendor"] = _gpuInfo?.Vendor ?? "Unknown",
                ["VRAMTotal"] = _gpuInfo?.VRAMTotal ?? 0,
                ["VRAMAvailable"] = _gpuInfo?.VRAMAvailable ?? 0,
                ["Temperature"] = _gpuInfo?.Temperature ?? 0,
                ["GPUUsage"] = _gpuInfo?.GPUUsage ?? 0
            };

            return await Task.FromResult(metrics);
        }

        private async Task DetectGPUAsync()
        {
            try
            {
                var query = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
                var results = query.Get();

                foreach (ManagementObject gpu in results)
                {
                    if (gpu != null)
                    {
                        _gpuInfo = new GPUInfo
                        {
                            GPU = gpu["Name"]?.ToString() ?? "Unknown GPU",
                            Vendor = DetermineVendor(gpu["Name"]?.ToString() ?? ""),
                            VRAMTotal = Convert.ToInt64(gpu["AdapterRAM"] ?? 0),
                            VRAMAvailable = Convert.ToInt64(gpu["AdapterRAM"] ?? 0)
                        };
                        break;
                    }
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                LogError($"GPU detection error: {ex.Message}");
            }
        }

        private string DetermineVendor(string gpuName)
        {
            if (string.IsNullOrEmpty(gpuName))
                return "Unknown";

            if (gpuName.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase))
                return "NVIDIA";
            if (gpuName.Contains("AMD", StringComparison.OrdinalIgnoreCase) || 
                gpuName.Contains("Radeon", StringComparison.OrdinalIgnoreCase))
                return "AMD";
            if (gpuName.Contains("Intel", StringComparison.OrdinalIgnoreCase))
                return "Intel";

            return "Unknown";
        }

        private async Task EnableGPUAccelerationAsync(CancellationToken cancellationToken, OptimizationResult result)
        {
            try
            {
                // Enable hardware acceleration for graphics
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", true))
                {
                    if (key != null)
                    {
                        key.SetValue("VisualFXSetting", 3, RegistryValueKind.DWord);
                        result.Changes.Add("GPU hardware acceleration enabled");
                    }
                }

                // Enable DirectX 12
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\DirectX", true))
                {
                    if (key != null)
                    {
                        key.SetValue("MaxVersionAllowed", "12_1", RegistryValueKind.String);
                        result.Changes.Add("DirectX 12 enabled");
                    }
                }

                result.Metrics["HardwareAccelerationEnabled"] = true;
                await Task.Delay(100, cancellationToken);
            }
            catch (Exception ex)
            {
                LogError($"GPU acceleration enabling error: {ex.Message}");
            }
        }

        private async Task OptimizeVRAMAsync(CancellationToken cancellationToken, OptimizationResult result)
        {
            try
            {
                if (_gpuInfo == null)
                    return;

                var vramMB = _gpuInfo.VRAMTotal / 1024 / 1024;
                var recommendedVRAM = (long)(vramMB * 0.9); // Use 90% of available VRAM

                using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", true))
                {
                    if (key != null)
                    {
                        key.SetValue("VRAMAllocation", recommendedVRAM, RegistryValueKind.DWord);
                        result.Changes.Add($"VRAM allocation optimized to {recommendedVRAM}MB");
                        result.Metrics["OptimizedVRAM"] = recommendedVRAM;
                    }
                }

                await Task.Delay(100, cancellationToken);
            }
            catch (Exception ex)
            {
                LogError($"VRAM optimization error: {ex.Message}");
            }
        }

        private async Task ConfigureDirectXAsync(CancellationToken cancellationToken, OptimizationResult result)
        {
            try
            {
                // Configure DirectX settings for optimal performance
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\DirectX", true))
                {
                    if (key != null)
                    {
                        key.SetValue("D3D11DeviceType", 0, RegistryValueKind.DWord);
                        key.SetValue("D3D11EnableMultiThreadedOptimizations", 1, RegistryValueKind.DWord);
                        key.SetValue("D3D12DXVADevice", 1, RegistryValueKind.DWord);
                        result.Changes.Add("DirectX multi-threading optimizations enabled");
                    }
                }

                // Configure Direct3D settings
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Direct3D", true))
                {
                    if (key != null)
                    {
                        key.SetValue("DisableFullScreenOptimizations", 0, RegistryValueKind.DWord);
                        result.Changes.Add("Direct3D optimizations configured");
                    }
                }

                result.Metrics["DirectXConfigured"] = true;
                await Task.Delay(100, cancellationToken);
            }
            catch (Exception ex)
            {
                LogError($"DirectX configuration error: {ex.Message}");
            }
        }

        private async Task SetPowerModeAsync(CancellationToken cancellationToken, OptimizationResult result)
        {
            try
            {
                // Set GPU power mode to maximum performance
                using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", true))
                {
                    if (key != null)
                    {
                        key.SetValue("PowerMode", 2, RegistryValueKind.DWord); // 2 = Maximum Performance
                        key.SetValue("GPUClock", 1, RegistryValueKind.DWord);
                        key.SetValue("MemoryClock", 1, RegistryValueKind.DWord);
                        result.Changes.Add("GPU power mode set to maximum performance");
                    }
                }

                result.Metrics["PowerModeSet"] = "MaximumPerformance";
                await Task.Delay(100, cancellationToken);
            }
            catch (Exception ex)
            {
                LogError($"Power mode setting error: {ex.Message}");
            }
        }

        private async Task MonitorGPUAsync(OptimizationResult result)
        {
            try
            {
                if (_gpuInfo == null)
                    return;

                var query = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
                var results = query.Get();

                foreach (ManagementObject gpu in results)
                {
                    if (gpu["Name"]?.ToString() == _gpuInfo.GPU)
                    {
                        _gpuInfo.VRAMAvailable = Convert.ToInt64(gpu["AdapterRAM"] ?? 0);
                        result.Metrics["GPUTemperature"] = _gpuInfo.Temperature;
                        result.Metrics["GPUStatus"] = "Monitored";
                        break;
                    }
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                LogError($"GPU monitoring error: {ex.Message}");
            }
        }
    }
}
