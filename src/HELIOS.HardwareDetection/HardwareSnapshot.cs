namespace HELIOS.HardwareDetection;

public sealed record HardwareSnapshot(
    DateTimeOffset CapturedAt,
    string MachineName,
    string OperatingSystem,
    IReadOnlyList<ProcessorInfo> Processors,
    IReadOnlyList<DeviceInfo> DisplayAdapters,
    IReadOnlyList<DeviceInfo> NetworkAdapters,
    IReadOnlyList<DeviceInfo> StorageDevices,
    bool? SecureBootEnabled,
    bool? TpmPresent,
    bool? RazerHardwareDetected,
    bool? NvidiaHardwareDetected,
    bool? IntelHardwareDetected);

public sealed record ProcessorInfo(string Name, int LogicalProcessors, int Cores);

public sealed record DeviceInfo(string Name, string? Manufacturer, string? PnpDeviceId, string? DriverVersion);
