using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Power;
using Windows.System;
using Windows.System.Power;

namespace HELIOS.Platform.SystemIntegration
{
    /// <summary>
    /// Low-level device control and system metrics collection.
    /// Integrates with Windows.Devices APIs for device enumeration,
    /// power management, and thermal monitoring.
    /// </summary>
    public class DeviceController : IDisposable
    {
        [DllImport("Kernel32.dll", SetLastError = true)]
        private static extern bool GetSystemPowerStatus(out SystemPowerStatus sps);

        [DllImport("powrprof.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetSuspendState(
            [MarshalAs(UnmanagedType.Bool)] bool hibernate,
            [MarshalAs(UnmanagedType.Bool)] bool forceCritical,
            [MarshalAs(UnmanagedType.Bool)] bool disableWakeEvent);

        [StructLayout(LayoutKind.Sequential)]
        private struct SystemPowerStatus
        {
            public byte ACLineStatus;
            public byte BatteryFlag;
            public byte BatteryLifePercent;
            public byte SystemStatusFlag;
            public uint BatteryLifeTime;
            public uint BatteryFullLifeTime;
        }

        private List<DeviceInfo> _devices;
        private bool _disposed;
        private BatteryReport _batteryReport;
        private SystemMetrics _systemMetrics;
        private readonly Func<PowerAction, bool> _powerActionAuthorizer;
        private readonly Action<string> _powerActionAudit;

        public event EventHandler<DeviceStateChangedEventArgs> DeviceStateChanged;
        public event EventHandler<PowerStateChangedEventArgs> PowerStateChanged;
        public event EventHandler<ThermalThrottlingEventArgs> ThermalThrottlingDetected;

        public DeviceController()
            : this(_ => false, message => Debug.WriteLine(message))
        {
        }

        /// <summary>
        /// Creates a controller with an explicit authorization/confirmation boundary for
        /// destructive power operations. The parameterless constructor denies them.
        /// </summary>
        public DeviceController(
            Func<PowerAction, bool> powerActionAuthorizer,
            Action<string>? powerActionAudit = null)
        {
            _powerActionAuthorizer = powerActionAuthorizer ?? throw new ArgumentNullException(nameof(powerActionAuthorizer));
            _powerActionAudit = powerActionAudit ?? (message => Debug.WriteLine(message));
            _devices = new List<DeviceInfo>();
            _systemMetrics = new SystemMetrics();
            InitializeDeviceMonitoring();
        }

        /// <summary>
        /// Initializes device monitoring and event subscriptions.
        /// </summary>
        private void InitializeDeviceMonitoring()
        {
            try
            {
                // Subscribe to power state changes
                PowerManager.BatteryStatusChanged += PowerManager_BatteryStatusChanged;
                PowerManager.PowerSupplyStatusChanged += PowerManager_PowerSupplyStatusChanged;
                PowerManager.RemainingChargePercentChanged += PowerManager_RemainingChargePercentChanged;
                PowerManager.EnergySaverStatusChanged += PowerManager_EnergySaverStatusChanged;

                Debug.WriteLine("[DeviceController] Device monitoring initialized");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DeviceController] Error initializing device monitoring: {ex.Message}");
            }
        }

        /// <summary>
        /// Enumerates all devices matching the specified device selector.
        /// </summary>
        public async Task<IList<DeviceInfo>> EnumerateDevicesAsync(DeviceCategory category)
        {
            try
            {
                var selector = GetDeviceSelector(category);
                var deviceInfoCollection = await DeviceInformation.FindAllAsync(selector);

                var discoveredDevices = new List<DeviceInfo>();

                foreach (var deviceInfo in deviceInfoCollection)
                {
                    var device = new DeviceInfo
                    {
                        Id = deviceInfo.Id,
                        Name = deviceInfo.Name,
                        Category = category,
                        IsEnabled = deviceInfo.IsEnabled,
                        Status = MapDeviceStatus(deviceInfo.Kind),
                        DiscoveredTime = DateTime.UtcNow
                    };

                    discoveredDevices.Add(device);
                }

                _devices.AddRange(discoveredDevices);
                Debug.WriteLine($"[DeviceController] Enumerated {discoveredDevices.Count} devices in {category}");

                return discoveredDevices;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DeviceController] Error enumerating devices: {ex.Message}");
                return new List<DeviceInfo>();
            }
        }

        /// <summary>
        /// Gets current system power state.
        /// </summary>
        public PowerInfo GetPowerState()
        {
            try
            {
                var powerInfo = new PowerInfo();

                // Get battery info
                _batteryReport = Battery.AggregateBattery.GetReport();

                if (_batteryReport != null)
                {
                    powerInfo.BatteryCapacityRemaining = _batteryReport.RemainingCapacityInMilliwattHours ?? 0;
                    powerInfo.BatteryCapacityFull = _batteryReport.FullChargeCapacityInMilliwattHours ?? 0;
                    powerInfo.BatteryPercentage = powerInfo.BatteryCapacityFull > 0
                        ? (int)Math.Clamp(
                            Math.Round((double)powerInfo.BatteryCapacityRemaining / powerInfo.BatteryCapacityFull * 100),
                            0,
                            100)
                        : 0;
                }

                // Get AC power status
                if (GetSystemPowerStatus(out var sps))
                {
                    powerInfo.IsOnAC = sps.ACLineStatus == 1;
                    powerInfo.IsBatteryPresent = sps.BatteryFlag != 255;
                    powerInfo.BatteryPercentage = sps.BatteryLifePercent;
                }

                // Get energy saver status
                powerInfo.IsEnergySaverEnabled = PowerManager.EnergySaverStatus == EnergySaverStatus.On;

                // Get power supply status
                powerInfo.PowerSupplyStatus = PowerManager.PowerSupplyStatus.ToString();

                Debug.WriteLine($"[DeviceController] Power State: AC={powerInfo.IsOnAC}, Battery={powerInfo.BatteryPercentage}%");

                return powerInfo;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DeviceController] Error getting power state: {ex.Message}");
                return new PowerInfo();
            }
        }

        /// <summary>
        /// Collects comprehensive system metrics.
        /// </summary>
        public SystemMetrics CollectSystemMetrics()
        {
            try
            {
                var metrics = new SystemMetrics
                {
                    Timestamp = DateTime.UtcNow,
                    ProcessorCount = Environment.ProcessorCount,
                    TotalMemory = GC.GetTotalMemory(false),
                    WorkingSet = Process.GetCurrentProcess().WorkingSet64
                };

                // Add CPU usage (simplified)
                var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                metrics.CpuUsagePercent = cpuCounter.NextValue();

                // Add available memory
                var availableMemory = new PerformanceCounter("Memory", "Available MBytes");
                metrics.AvailableMemory = (long)availableMemory.NextValue() * 1024 * 1024;

                // Add power information
                metrics.PowerState = GetPowerState();

                Debug.WriteLine($"[DeviceController] System Metrics: CPU={metrics.CpuUsagePercent}%, Memory={metrics.TotalMemory / (1024 * 1024)}MB");

                return metrics;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DeviceController] Error collecting system metrics: {ex.Message}");
                return _systemMetrics;
            }
        }

        /// <summary>
        /// Monitors thermal state and detects thermal throttling.
        /// </summary>
        public ThermalState MonitorThermalState()
        {
            try
            {
                var thermalState = ThermalState.Normal;

                // In a real implementation, you would query actual temperature sensors
                // This is a simplified example showing the structure

                // Check for thermal throttling indicators
                var powerState = GetPowerState();
                var metrics = CollectSystemMetrics();

                // If on battery and CPU usage is high, might indicate thermal issues
                if (!powerState.IsOnAC && metrics.CpuUsagePercent > 80)
                {
                    thermalState = ThermalState.Throttling;
                    ThermalThrottlingDetected?.Invoke(this, new ThermalThrottlingEventArgs
                    {
                        State = thermalState,
                        CpuUsage = metrics.CpuUsagePercent
                    });
                }

                Debug.WriteLine($"[DeviceController] Thermal State: {thermalState}");
                return thermalState;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DeviceController] Error monitoring thermal state: {ex.Message}");
                return ThermalState.Unknown;
            }
        }

        /// <summary>
        /// Changes device power state (e.g., sleep, hibernate).
        /// </summary>
        public Task SetPowerStateAsync(PowerAction action)
        {
            return SetPowerStateAsync(action, CancellationToken.None);
        }

        public async Task SetPowerStateAsync(PowerAction action, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!Enum.IsDefined(action))
            {
                throw new ArgumentOutOfRangeException(nameof(action), action, "Unknown power action.");
            }

            if (!OperatingSystem.IsWindows())
            {
                throw new PlatformNotSupportedException("Power actions are supported only on Windows.");
            }

            if (!_powerActionAuthorizer(action))
            {
                _powerActionAudit($"[DeviceController] Denied power action: {action}");
                throw new UnauthorizedAccessException(
                    $"Power action '{action}' requires explicit host authorization and confirmation.");
            }

            _powerActionAudit($"[DeviceController] Authorized power action: {action}");

            try
            {
                switch (action)
                {
                    case PowerAction.Sleep:
                        await Task.Run(() => SuspendSystem(hibernate: false), cancellationToken);
                        break;
                    case PowerAction.Hibernate:
                        await Task.Run(() => SuspendSystem(hibernate: true), cancellationToken);
                        break;
                    case PowerAction.Shutdown:
                        // Note: Shutdown requires admin privileges
                        ShutdownManager.BeginShutdown(ShutdownKind.Shutdown, TimeSpan.FromSeconds(30));
                        break;
                    case PowerAction.Restart:
                        ShutdownManager.BeginShutdown(ShutdownKind.Restart, TimeSpan.FromSeconds(30));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(action), action, "Unknown power action.");
                }

                Debug.WriteLine($"[DeviceController] Power action initiated: {action}");
                _powerActionAudit($"[DeviceController] Initiated power action: {action}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DeviceController] Error setting power state: {ex.Message}");
                _powerActionAudit($"[DeviceController] Failed power action {action}: {ex.Message}");
                throw;
            }
        }

        private static void SuspendSystem(bool hibernate)
        {
            if (!SetSuspendState(hibernate, forceCritical: false, disableWakeEvent: false))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        private string GetDeviceSelector(DeviceCategory category)
        {
            return category switch
            {
                DeviceCategory.Audio => DeviceInformation.GetAqsFilterFromDeviceClass(DeviceClass.AudioRender),
                DeviceCategory.Camera => DeviceInformation.GetAqsFilterFromDeviceClass(DeviceClass.VideoCapture),
                DeviceCategory.Network => "System.Devices.NetworkDevice",
                DeviceCategory.Storage => DeviceInformation.GetAqsFilterFromDeviceClass(DeviceClass.PortableStorageDevice),
                _ => ""
            };
        }

        private string MapDeviceStatus(DeviceInformationKind kind)
        {
            return kind switch
            {
                DeviceInformationKind.Device => "Online",
                DeviceInformationKind.DeviceContainer => "Container",
                _ => "Unknown"
            };
        }

        private void PowerManager_BatteryStatusChanged(object sender, object e)
        {
            var powerState = GetPowerState();
            PowerStateChanged?.Invoke(this, new PowerStateChangedEventArgs { PowerInfo = powerState });
            Debug.WriteLine("[DeviceController] Battery status changed");
        }

        private void PowerManager_PowerSupplyStatusChanged(object sender, object e)
        {
            var powerState = GetPowerState();
            PowerStateChanged?.Invoke(this, new PowerStateChangedEventArgs { PowerInfo = powerState });
            Debug.WriteLine("[DeviceController] Power supply status changed");
        }

        private void PowerManager_RemainingChargePercentChanged(object sender, object e)
        {
            var powerState = GetPowerState();
            PowerStateChanged?.Invoke(this, new PowerStateChangedEventArgs { PowerInfo = powerState });
        }

        private void PowerManager_EnergySaverStatusChanged(object sender, object e)
        {
            var powerState = GetPowerState();
            PowerStateChanged?.Invoke(this, new PowerStateChangedEventArgs { PowerInfo = powerState });
            Debug.WriteLine("[DeviceController] Energy saver status changed");
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                PowerManager.BatteryStatusChanged -= PowerManager_BatteryStatusChanged;
                PowerManager.PowerSupplyStatusChanged -= PowerManager_PowerSupplyStatusChanged;
                PowerManager.RemainingChargePercentChanged -= PowerManager_RemainingChargePercentChanged;
                PowerManager.EnergySaverStatusChanged -= PowerManager_EnergySaverStatusChanged;

                _disposed = true;
                Debug.WriteLine("[DeviceController] Device controller disposed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DeviceController] Error during disposal: {ex.Message}");
            }
        }
    }

    public class DeviceInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public DeviceCategory Category { get; set; }
        public bool IsEnabled { get; set; }
        public string Status { get; set; }
        public DateTime DiscoveredTime { get; set; }
    }

    public class PowerInfo
    {
        public bool IsOnAC { get; set; }
        public bool IsBatteryPresent { get; set; }
        public int BatteryPercentage { get; set; }
        public long BatteryCapacityRemaining { get; set; }
        public long BatteryCapacityFull { get; set; }
        public bool IsEnergySaverEnabled { get; set; }
        public string PowerSupplyStatus { get; set; }
    }

    public class SystemMetrics
    {
        public DateTime Timestamp { get; set; }
        public int ProcessorCount { get; set; }
        public long TotalMemory { get; set; }
        public long AvailableMemory { get; set; }
        public long WorkingSet { get; set; }
        public float CpuUsagePercent { get; set; }
        public PowerInfo PowerState { get; set; }
    }

    public enum DeviceCategory
    {
        Audio,
        Camera,
        Network,
        Storage,
        Other
    }

    public enum PowerAction
    {
        Sleep,
        Hibernate,
        Shutdown,
        Restart
    }

    public enum ThermalState
    {
        Normal,
        Warm,
        Hot,
        Throttling,
        Unknown
    }

    public class PowerStateChangedEventArgs : EventArgs
    {
        public PowerInfo PowerInfo { get; set; }
    }

    public class DeviceStateChangedEventArgs : EventArgs
    {
        public string DeviceId { get; set; }
        public string NewState { get; set; }
    }

    public class ThermalThrottlingEventArgs : EventArgs
    {
        public ThermalState State { get; set; }
        public float CpuUsage { get; set; }
    }
}
