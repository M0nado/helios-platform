using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;

namespace HELIOS.Platform.Phase10.Drivers
{
    /// <summary>
    /// Detects hardware devices requiring drivers using WMI.
    /// </summary>
    public class DriverDetector : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;
        private bool _disposed;

        public DriverDetector()
        {
            _semaphore = new SemaphoreSlim(1);
        }

        /// <summary>
        /// Detect all hardware devices.
        /// </summary>
        public async Task<List<DetectedDevice>> DetectAllHardwareAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                var devices = new List<DetectedDevice>();

                devices.AddRange(await DetectChipsetsAsync());
                devices.AddRange(await DetectGpuAsync());
                devices.AddRange(await DetectAudioAsync());
                devices.AddRange(await DetectNetworkAsync());
                devices.AddRange(await DetectStorageAsync());
                devices.AddRange(await DetectUsbAsync());
                devices.AddRange(await DetectBiometricsAsync());
                devices.AddRange(await DetectWirelessAsync());

                return devices;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Detect chipset devices (Intel/AMD).
        /// </summary>
        public async Task<List<DetectedDevice>> DetectChipsetsAsync()
        {
            return await Task.Run(() =>
            {
                var devices = new List<DetectedDevice>();

                try
                {
                    var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard");
                    var collection = searcher.Get();

                    foreach (ManagementObject device in collection)
                    {
                        var manufacturer = device["Manufacturer"]?.ToString() ?? "";
                        bool isIntel = manufacturer.Contains("Intel", StringComparison.OrdinalIgnoreCase);
                        bool isAmd = manufacturer.Contains("AMD", StringComparison.OrdinalIgnoreCase);

                        if (isIntel || isAmd)
                        {
                            devices.Add(new DetectedDevice
                            {
                                DeviceId = Guid.NewGuid().ToString(),
                                DeviceName = device["Product"]?.ToString() ?? "Unknown Chipset",
                                DeviceType = "Chipset",
                                Status = "Active",
                                Manufacturer = manufacturer,
                                Description = $"{(isIntel ? "Intel" : "AMD")} Chipset",
                                RequiresDriver = true
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Chipset detection error: {ex.Message}");
                }

                return devices;
            });
        }

        /// <summary>
        /// Detect GPU devices (NVIDIA/AMD).
        /// </summary>
        public async Task<List<DetectedDevice>> DetectGpuAsync()
        {
            return await Task.Run(() =>
            {
                var devices = new List<DetectedDevice>();

                try
                {
                    var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
                    var collection = searcher.Get();

                    foreach (ManagementObject device in collection)
                    {
                        var name = device["Name"]?.ToString() ?? "Unknown GPU";
                        var manufacturer = device["Manufacturer"]?.ToString() ?? "";

                        if (!name.Contains("RemoteFX", StringComparison.OrdinalIgnoreCase))
                        {
                            devices.Add(new DetectedDevice
                            {
                                DeviceId = Guid.NewGuid().ToString(),
                                DeviceName = name,
                                DeviceType = "GPU",
                                Status = "Active",
                                Manufacturer = manufacturer,
                                PnpDeviceId = device["PNPDeviceID"]?.ToString() ?? "",
                                Description = $"Graphics Adapter - {name}",
                                RequiresDriver = true,
                                CurrentDriverVersion = device["DriverVersion"]?.ToString() ?? "Unknown"
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"GPU detection error: {ex.Message}");
                }

                return devices;
            });
        }

        /// <summary>
        /// Detect audio devices.
        /// </summary>
        public async Task<List<DetectedDevice>> DetectAudioAsync()
        {
            return await Task.Run(() =>
            {
                var devices = new List<DetectedDevice>();

                try
                {
                    var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_SoundDevice");
                    var collection = searcher.Get();

                    foreach (ManagementObject device in collection)
                    {
                        devices.Add(new DetectedDevice
                        {
                            DeviceId = Guid.NewGuid().ToString(),
                            DeviceName = device["Name"]?.ToString() ?? "Unknown Audio",
                            DeviceType = "Audio",
                            Status = device["Status"]?.ToString() ?? "Active",
                            Manufacturer = device["Manufacturer"]?.ToString() ?? "",
                            PnpDeviceId = device["PNPDeviceID"]?.ToString() ?? "",
                            Description = "Audio Device",
                            RequiresDriver = true
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Audio detection error: {ex.Message}");
                }

                return devices;
            });
        }

        /// <summary>
        /// Detect network adapters.
        /// </summary>
        public async Task<List<DetectedDevice>> DetectNetworkAsync()
        {
            return await Task.Run(() =>
            {
                var devices = new List<DetectedDevice>();

                try
                {
                    var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapter WHERE PhysicalAdapter=TRUE");
                    var collection = searcher.Get();

                    foreach (ManagementObject device in collection)
                    {
                        devices.Add(new DetectedDevice
                        {
                            DeviceId = Guid.NewGuid().ToString(),
                            DeviceName = device["Name"]?.ToString() ?? "Unknown Network",
                            DeviceType = "Network",
                            Status = device["NetConnectionStatus"]?.ToString() ?? "Connected",
                            Manufacturer = device["Manufacturer"]?.ToString() ?? "",
                            PnpDeviceId = device["PNPDeviceID"]?.ToString() ?? "",
                            Description = "Network Adapter",
                            RequiresDriver = true
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Network detection error: {ex.Message}");
                }

                return devices;
            });
        }

        /// <summary>
        /// Detect storage controllers.
        /// </summary>
        public async Task<List<DetectedDevice>> DetectStorageAsync()
        {
            return await Task.Run(() =>
            {
                var devices = new List<DetectedDevice>();

                try
                {
                    // SATA/SAS controllers
                    var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_SCSIController");
                    var collection = searcher.Get();

                    foreach (ManagementObject device in collection)
                    {
                        devices.Add(new DetectedDevice
                        {
                            DeviceId = Guid.NewGuid().ToString(),
                            DeviceName = device["Name"]?.ToString() ?? "Unknown Storage",
                            DeviceType = "Storage",
                            Status = "Active",
                            Manufacturer = device["Manufacturer"]?.ToString() ?? "",
                            PnpDeviceId = device["PNPDeviceID"]?.ToString() ?? "",
                            Description = "Storage Controller",
                            RequiresDriver = true
                        });
                    }

                    // NVMe controllers
                    var nvmeSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPDevice WHERE DeviceID LIKE '%NVME%'");
                    var nvmeCollection = nvmeSearcher.Get();

                    foreach (ManagementObject device in nvmeCollection)
                    {
                        devices.Add(new DetectedDevice
                        {
                            DeviceId = Guid.NewGuid().ToString(),
                            DeviceName = device["Name"]?.ToString() ?? "Unknown NVMe",
                            DeviceType = "Storage",
                            Status = "Active",
                            Manufacturer = device["Manufacturer"]?.ToString() ?? "",
                            PnpDeviceId = device["DeviceID"]?.ToString() ?? "",
                            Description = "NVMe Controller",
                            RequiresDriver = true
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Storage detection error: {ex.Message}");
                }

                return devices;
            });
        }

        /// <summary>
        /// Detect USB hubs and controllers.
        /// </summary>
        public async Task<List<DetectedDevice>> DetectUsbAsync()
        {
            return await Task.Run(() =>
            {
                var devices = new List<DetectedDevice>();

                try
                {
                    var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_USBHub");
                    var collection = searcher.Get();

                    foreach (ManagementObject device in collection)
                    {
                        devices.Add(new DetectedDevice
                        {
                            DeviceId = Guid.NewGuid().ToString(),
                            DeviceName = device["Name"]?.ToString() ?? "Unknown USB",
                            DeviceType = "USB",
                            Status = "Active",
                            Manufacturer = device["Manufacturer"]?.ToString() ?? "",
                            PnpDeviceId = device["PNPDeviceID"]?.ToString() ?? "",
                            Description = "USB Hub/Controller",
                            RequiresDriver = true
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"USB detection error: {ex.Message}");
                }

                return devices;
            });
        }

        /// <summary>
        /// Detect biometric devices.
        /// </summary>
        public async Task<List<DetectedDevice>> DetectBiometricsAsync()
        {
            return await Task.Run(() =>
            {
                var devices = new List<DetectedDevice>();

                try
                {
                    var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPDevice WHERE Name LIKE '%Biometric%' OR Name LIKE '%Fingerprint%' OR Name LIKE '%Camera%'");
                    var collection = searcher.Get();

                    foreach (ManagementObject device in collection)
                    {
                        var name = device["Name"]?.ToString() ?? "";
                        if (name.Contains("Biometric", StringComparison.OrdinalIgnoreCase) ||
                            name.Contains("Fingerprint", StringComparison.OrdinalIgnoreCase))
                        {
                            devices.Add(new DetectedDevice
                            {
                                DeviceId = Guid.NewGuid().ToString(),
                                DeviceName = name,
                                DeviceType = "Biometric",
                                Status = "Active",
                                Manufacturer = device["Manufacturer"]?.ToString() ?? "",
                                PnpDeviceId = device["DeviceID"]?.ToString() ?? "",
                                Description = "Biometric Device",
                                RequiresDriver = true
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Biometric detection error: {ex.Message}");
                }

                return devices;
            });
        }

        /// <summary>
        /// Detect wireless adapters.
        /// </summary>
        public async Task<List<DetectedDevice>> DetectWirelessAsync()
        {
            return await Task.Run(() =>
            {
                var devices = new List<DetectedDevice>();

                try
                {
                    var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapter WHERE PhysicalAdapter=TRUE AND Name LIKE '%Wireless%' OR Name LIKE '%WiFi%' OR Name LIKE '%WLAN%'");
                    var collection = searcher.Get();

                    foreach (ManagementObject device in collection)
                    {
                        devices.Add(new DetectedDevice
                        {
                            DeviceId = Guid.NewGuid().ToString(),
                            DeviceName = device["Name"]?.ToString() ?? "Unknown Wireless",
                            DeviceType = "Wireless",
                            Status = "Active",
                            Manufacturer = device["Manufacturer"]?.ToString() ?? "",
                            PnpDeviceId = device["PNPDeviceID"]?.ToString() ?? "",
                            Description = "Wireless Network Adapter",
                            RequiresDriver = true
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Wireless detection error: {ex.Message}");
                }

                return devices;
            });
        }

        /// <summary>
        /// Check if system has GPU.
        /// </summary>
        public async Task<bool> HasGpuAsync()
        {
            var gpus = await DetectGpuAsync();
            return gpus.Any(g => !g.DeviceName.Contains("RemoteFX", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Get specific device type.
        /// </summary>
        public async Task<List<DetectedDevice>> GetDevicesByTypeAsync(string deviceType)
        {
            var allDevices = await DetectAllHardwareAsync();
            return allDevices.Where(d => d.DeviceType.Equals(deviceType, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _semaphore?.Dispose();
            _disposed = true;
        }
    }
}
