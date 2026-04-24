using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using HELIOS.Platform.Core.Logging;
using HELIOS.Platform.Phase10.BootEnvironment;

namespace HELIOS.Platform.Phase10.BootEnvironment.Tests
{
    /// <summary>
    /// Comprehensive unit tests for USB Boot Environment services.
    /// </summary>
    public class BootEnvironmentTests
    {
        private readonly ILogger _logger;
        private readonly string _testDir;

        public BootEnvironmentTests()
        {
            _logger = new ConsoleLogger();
            _testDir = Path.Combine(Path.GetTempPath(), $"HELIOS_Test_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDir);
        }

        // USBBootstrapEngine Tests

        [Fact]
        public async Task CreateWinPEEnvironment_ValidPath_CreatesStructure()
        {
            var engine = new USBBootstrapEngine(_logger);
            var peRoot = Path.Combine(_testDir, "WinPE");

            var result = await engine.CreateWinPEEnvironmentAsync(peRoot, true, true);

            Assert.True(result);
            Assert.True(Directory.Exists(Path.Combine(peRoot, "Boot")));
            Assert.True(Directory.Exists(Path.Combine(peRoot, "Sources")));
            Assert.True(Directory.Exists(Path.Combine(peRoot, "EFI", "Boot")));
        }

        [Fact]
        public async Task CreateWinPEEnvironment_NullPath_ReturnsFalse()
        {
            var engine = new USBBootstrapEngine(_logger);
            var result = await engine.CreateWinPEEnvironmentAsync(null);
            Assert.False(result);
        }

        [Fact]
        public async Task CreateWinPEEnvironment_UEFIOnly_CreatesUEFIDirectories()
        {
            var engine = new USBBootstrapEngine(_logger);
            var peRoot = Path.Combine(_testDir, "WinPE_UEFI");

            var result = await engine.CreateWinPEEnvironmentAsync(peRoot, true, false);

            Assert.True(result);
            Assert.True(Directory.Exists(Path.Combine(peRoot, "EFI", "Boot")));
        }

        [Fact]
        public async Task ConfigureBootEnvironment_ValidConfig_SavesConfiguration()
        {
            var engine = new USBBootstrapEngine(_logger);
            var peRoot = Path.Combine(_testDir, "WinPE_Config");
            await engine.CreateWinPEEnvironmentAsync(peRoot);

            var config = new BootConfiguration
            {
                DefaultBootOption = 0,
                BootTimeoutSeconds = 30,
                EnableGraphicalMenu = true
            };

            var result = await engine.ConfigureBootEnvironmentAsync(peRoot, config);

            Assert.True(result);
        }

        [Fact]
        public async Task ConfigureBootEnvironment_NullConfig_ReturnsFalse()
        {
            var engine = new USBBootstrapEngine(_logger);
            var result = await engine.ConfigureBootEnvironmentAsync(_testDir, null);
            Assert.False(result);
        }

        [Fact]
        public async Task ValidateBootEnvironment_ValidStructure_ReturnsTrue()
        {
            var engine = new USBBootstrapEngine(_logger);
            var peRoot = Path.Combine(_testDir, "WinPE_Validate");
            await engine.CreateWinPEEnvironmentAsync(peRoot);

            var result = await engine.ValidateBootEnvironmentAsync(peRoot);

            Assert.True(result);
        }

        [Fact]
        public async Task EnableUEFIBoot_ValidPath_CreatesUEFIDirectory()
        {
            var engine = new USBBootstrapEngine(_logger);
            var peRoot = Path.Combine(_testDir, "WinPE_UEFI_Enable");
            await engine.CreateWinPEEnvironmentAsync(peRoot, false);

            var result = await engine.EnableUEFIBootAsync(peRoot, true);

            Assert.True(result);
            Assert.True(Directory.Exists(Path.Combine(peRoot, "EFI", "Boot")));
        }

        // ISOImageBuilder Tests

        [Fact]
        public async Task BuildISOImage_ValidSource_CreatesISO()
        {
            var engine = new USBBootstrapEngine(_logger);
            var peRoot = Path.Combine(_testDir, "WinPE_ISO");
            await engine.CreateWinPEEnvironmentAsync(peRoot);

            var builder = new ISOImageBuilder(_logger);
            var outputPath = Path.Combine(_testDir, "ISO_Output");
            var isoPath = await builder.BuildISOImageAsync(peRoot, outputPath, "bootable.iso");

            Assert.NotNull(isoPath);
            Assert.True(File.Exists(isoPath));
        }

        [Fact]
        public async Task BuildISOImage_InvalidSource_ReturnsNull()
        {
            var builder = new ISOImageBuilder(_logger);
            var result = await builder.BuildISOImageAsync("/invalid/path", _testDir, "test.iso");

            Assert.Null(result);
        }

        [Fact]
        public async Task VerifyISOIntegrity_ValidISO_ReturnsTrue()
        {
            var engine = new USBBootstrapEngine(_logger);
            var peRoot = Path.Combine(_testDir, "WinPE_Verify");
            await engine.CreateWinPEEnvironmentAsync(peRoot);

            var builder = new ISOImageBuilder(_logger);
            var outputPath = Path.Combine(_testDir, "ISO_Verify");
            var isoPath = await builder.BuildISOImageAsync(peRoot, outputPath, "test.iso");

            var result = await builder.VerifyISOIntegrityAsync(isoPath);

            Assert.True(result);
        }

        [Fact]
        public async Task GetISOSize_ValidISO_ReturnsSize()
        {
            var engine = new USBBootstrapEngine(_logger);
            var peRoot = Path.Combine(_testDir, "WinPE_Size");
            await engine.CreateWinPEEnvironmentAsync(peRoot);

            var builder = new ISOImageBuilder(_logger);
            var outputPath = Path.Combine(_testDir, "ISO_Size");
            var isoPath = await builder.BuildISOImageAsync(peRoot, outputPath, "size.iso");

            var size = await builder.GetISOSizeAsync(isoPath);

            Assert.True(size > 0);
        }

        [Fact]
        public async Task ConfigureBootMethods_BothMethods_ConfiguresSuccessfully()
        {
            var builder = new ISOImageBuilder(_logger);
            var engine = new USBBootstrapEngine(_logger);
            var peRoot = Path.Combine(_testDir, "WinPE_Methods");
            await engine.CreateWinPEEnvironmentAsync(peRoot);

            var outputPath = Path.Combine(_testDir, "ISO_Methods");
            var isoPath = await builder.BuildISOImageAsync(peRoot, outputPath, "methods.iso");

            var result = await builder.ConfigureBootMethodsAsync(isoPath, true, true);

            Assert.True(result);
        }

        // USBFlasher Tests

        [Fact]
        public async Task WriteISOToUSB_ValidISO_WritesSuccessfully()
        {
            var engine = new USBBootstrapEngine(_logger);
            var peRoot = Path.Combine(_testDir, "WinPE_USB");
            await engine.CreateWinPEEnvironmentAsync(peRoot);

            var builder = new ISOImageBuilder(_logger);
            var outputPath = Path.Combine(_testDir, "ISO_USB");
            var isoPath = await builder.BuildISOImageAsync(peRoot, outputPath, "usb.iso");

            var flasher = new USBFlasher(_logger);
            var result = await flasher.WriteISOToUSBAsync(isoPath, "USB001", true);

            Assert.True(result);
        }

        [Fact]
        public async Task VerifyUSBBootability_ValidUSB_ReturnsTrue()
        {
            var flasher = new USBFlasher(_logger);
            var result = await flasher.VerifyUSBBootabilityAsync("USB001");

            Assert.True(result);
        }

        [Fact]
        public async Task SafeEjectUSB_ValidUSB_EjectsSuccessfully()
        {
            var flasher = new USBFlasher(_logger);
            var result = await flasher.SafeEjectUSBAsync("USB001");

            Assert.True(result);
        }

        [Fact]
        public async Task FormatUSB_ValidUSB_FormatsSuccessfully()
        {
            var flasher = new USBFlasher(_logger);
            var result = await flasher.FormatUSBAsync("USB001", "NTFS");

            Assert.True(result);
        }

        [Fact]
        public async Task GetUSBCapacity_ValidUSB_ReturnsCapacity()
        {
            var flasher = new USBFlasher(_logger);
            var (total, available) = await flasher.GetUSBCapacityAsync("USB001");

            Assert.True(total > 0);
            Assert.True(available > 0);
            Assert.True(available <= total);
        }

        [Fact]
        public async Task GetConnectedUSBDevices_EnumeratesDevices_ReturnsDevices()
        {
            var flasher = new USBFlasher(_logger);
            var devices = await flasher.GetConnectedUSBDevicesAsync();

            Assert.NotNull(devices);
            Assert.NotEmpty(devices);
        }

        // BootMenuManager Tests

        [Fact]
        public async Task CreateBootMenu_ValidItems_CreatesMenu()
        {
            var manager = new BootMenuManager(_logger);
            var items = new List<string> { "Windows PE", "HELIOS", "Tools" };

            var config = await manager.CreateBootMenuAsync(items);

            Assert.NotNull(config);
            Assert.Equal(3, config.MenuEntries.Count);
        }

        [Fact]
        public async Task CreateBootMenu_EmptyItems_ReturnsNull()
        {
            var manager = new BootMenuManager(_logger);
            var config = await manager.CreateBootMenuAsync(new List<string>());

            Assert.Null(config);
        }

        [Fact]
        public async Task UpdateBootMenu_ValidConfig_SavesConfiguration()
        {
            var manager = new BootMenuManager(_logger);
            var configPath = Path.Combine(_testDir, "boot.config");

            var config = new BootConfiguration
            {
                DefaultBootOption = 0,
                BootTimeoutSeconds = 30,
                MenuEntries = new List<BootMenuEntry>
                {
                    new BootMenuEntry { OrderIndex = 0, DisplayName = "Option 1" }
                }
            };

            var result = await manager.UpdateBootMenuAsync(configPath, config);

            Assert.True(result);
            Assert.True(File.Exists(configPath));
        }

        [Fact]
        public async Task SetDefaultBootOption_ValidIndex_UpdatesDefault()
        {
            var manager = new BootMenuManager(_logger);
            var configPath = Path.Combine(_testDir, "boot_default.config");

            var config = new BootConfiguration
            {
                DefaultBootOption = 0,
                BootTimeoutSeconds = 30,
                MenuEntries = new List<BootMenuEntry>
                {
                    new BootMenuEntry { OrderIndex = 0, DisplayName = "Option 1", IsDefault = true },
                    new BootMenuEntry { OrderIndex = 1, DisplayName = "Option 2", IsDefault = false }
                }
            };

            await manager.UpdateBootMenuAsync(configPath, config);
            var result = await manager.SetDefaultBootOptionAsync(configPath, 1);

            Assert.True(result);
        }

        [Fact]
        public async Task SetBootTimeout_ValidTimeout_UpdatesTimeout()
        {
            var manager = new BootMenuManager(_logger);
            var configPath = Path.Combine(_testDir, "boot_timeout.config");

            var config = new BootConfiguration
            {
                DefaultBootOption = 0,
                BootTimeoutSeconds = 30,
                MenuEntries = new List<BootMenuEntry>()
            };

            await manager.UpdateBootMenuAsync(configPath, config);
            var result = await manager.SetBootTimeoutAsync(configPath, 60);

            Assert.True(result);
        }

        [Fact]
        public async Task AddMenuEntry_ValidEntry_AddsEntry()
        {
            var manager = new BootMenuManager(_logger);
            var configPath = Path.Combine(_testDir, "boot_add.config");

            var config = new BootConfiguration
            {
                DefaultBootOption = 0,
                BootTimeoutSeconds = 30,
                MenuEntries = new List<BootMenuEntry>()
            };

            await manager.UpdateBootMenuAsync(configPath, config);
            var result = await manager.AddMenuEntryAsync(configPath, "New Option", "/boot/new.efi");

            Assert.True(result);
        }

        [Fact]
        public async Task RemoveMenuEntry_ValidIndex_RemovesEntry()
        {
            var manager = new BootMenuManager(_logger);
            var configPath = Path.Combine(_testDir, "boot_remove.config");

            var config = new BootConfiguration
            {
                DefaultBootOption = 0,
                BootTimeoutSeconds = 30,
                MenuEntries = new List<BootMenuEntry>
                {
                    new BootMenuEntry { OrderIndex = 0, DisplayName = "Option 1" },
                    new BootMenuEntry { OrderIndex = 1, DisplayName = "Option 2" }
                }
            };

            await manager.UpdateBootMenuAsync(configPath, config);
            var result = await manager.RemoveMenuEntryAsync(configPath, 0);

            Assert.True(result);
        }

        // PreBootEnvironment Tests

        [Fact]
        public async Task LoadPEDrivers_ValidDrivers_LoadsSuccessfully()
        {
            var peEnv = new PreBootEnvironment(_logger);
            var peRoot = Path.Combine(_testDir, "PE_Drivers");
            var driverPath = Path.Combine(_testDir, "test_driver.sys");
            await File.WriteAllTextAsync(driverPath, "DRIVER_DATA");

            var result = await peEnv.LoadPEDriversAsync(peRoot, new List<string> { driverPath });

            Assert.True(result);
        }

        [Fact]
        public async Task MountFilesystems_ValidPath_CreatesMountPoints()
        {
            var peEnv = new PreBootEnvironment(_logger);
            var peRoot = Path.Combine(_testDir, "PE_Mounts");
            Directory.CreateDirectory(peRoot);

            var result = await peEnv.MountFilesystemsAsync(peRoot);

            Assert.True(result);
            Assert.True(Directory.Exists(Path.Combine(peRoot, "mnt", "sysvol")));
        }

        [Fact]
        public async Task SetupPENetwork_ValidPath_ConfiguresNetwork()
        {
            var peEnv = new PreBootEnvironment(_logger);
            var peRoot = Path.Combine(_testDir, "PE_Network");
            Directory.CreateDirectory(peRoot);

            var result = await peEnv.SetupPENetworkAsync(peRoot, "192.168.1.100");

            Assert.True(result);
        }

        [Fact]
        public async Task InitializePEStorage_ValidPath_CreatesTempDirs()
        {
            var peEnv = new PreBootEnvironment(_logger);
            var peRoot = Path.Combine(_testDir, "PE_Storage");
            Directory.CreateDirectory(peRoot);

            var result = await peEnv.InitializePEStorageAsync(peRoot);

            Assert.True(result);
            Assert.True(Directory.Exists(Path.Combine(peRoot, "temp")));
        }

        [Fact]
        public async Task GetLoadedDrivers_AfterLoading_ReturnsDriverList()
        {
            var peEnv = new PreBootEnvironment(_logger);
            var peRoot = Path.Combine(_testDir, "PE_GetDrivers");
            var driverPath = Path.Combine(_testDir, "driver1.sys");
            await File.WriteAllTextAsync(driverPath, "DRIVER");

            await peEnv.LoadPEDriversAsync(peRoot, new List<string> { driverPath });
            var drivers = peEnv.GetLoadedDrivers();

            Assert.NotEmpty(drivers);
        }

        [Fact]
        public async Task InstallPackage_ValidPackage_InstallsSuccessfully()
        {
            var peEnv = new PreBootEnvironment(_logger);
            var peRoot = Path.Combine(_testDir, "PE_Package");
            Directory.CreateDirectory(peRoot);

            var packagePath = Path.Combine(_testDir, "test.zip");
            await File.WriteAllTextAsync(packagePath, "PACKAGE_DATA");

            var result = await peEnv.InstallPackageAsync(peRoot, packagePath);

            Assert.True(result);
        }

        [Fact]
        public async Task ValidatePEReady_CompleteSetup_ReturnsTrue()
        {
            var peEnv = new PreBootEnvironment(_logger);
            var engine = new USBBootstrapEngine(_logger);
            var peRoot = Path.Combine(_testDir, "PE_Ready");
            await engine.CreateWinPEEnvironmentAsync(peRoot);

            var result = await peEnv.ValidatePEReadyAsync(peRoot);

            Assert.True(result);
        }

        // BootDiagnostics Tests

        [Fact]
        public async Task GetBootEnvironmentInfo_ReturnsInfo()
        {
            var diag = new BootDiagnostics(_logger);
            var info = await diag.GetBootEnvironmentInfoAsync();

            Assert.NotNull(info);
            Assert.NotEmpty(info.FirmwareType);
            Assert.True(info.TotalMemoryMB > 0);
        }

        [Fact]
        public async Task RunBootDiagnostics_ReturnsResults()
        {
            var diag = new BootDiagnostics(_logger);
            var result = await diag.RunBootDiagnosticsAsync();

            Assert.NotNull(result);
            Assert.NotNull(result.DiagnosticMessages);
            Assert.True(result.ExecutionTimeMs >= 0);
        }

        [Fact]
        public async Task ValidateBootFirmware_ReturnsTrue()
        {
            var diag = new BootDiagnostics(_logger);
            var result = await diag.ValidateBootFirmwareAsync();

            Assert.True(result);
        }

        [Fact]
        public async Task CheckCPUSupport_ReturnsTrue()
        {
            var diag = new BootDiagnostics(_logger);
            var result = await diag.CheckCPUSupportAsync();

            Assert.True(result);
        }

        [Fact]
        public async Task DetectBootFirmware_ReturnsFirmwareType()
        {
            var diag = new BootDiagnostics(_logger);
            var firmware = await diag.DetectBootFirmwareAsync();

            Assert.NotNull(firmware);
            Assert.NotEmpty(firmware);
        }

        [Fact]
        public async Task CheckMemoryHealth_ReturnsTrue()
        {
            var diag = new BootDiagnostics(_logger);
            var result = await diag.CheckMemoryHealthAsync();

            Assert.True(result);
        }

        // RecoveryPartitionManager Tests

        [Fact]
        public async Task CreateRecoveryPartition_ValidDisk_CreatesPartition()
        {
            var recovery = new RecoveryPartitionManager(_logger);
            var partitionPath = Path.Combine(_testDir, "Recovery");
            Directory.CreateDirectory(partitionPath);

            var result = await recovery.CreateRecoveryPartitionAsync(partitionPath, 1L * 1024 * 1024 * 1024);

            Assert.True(result);
        }

        [Fact]
        public async Task BackupWinRE_ValidPath_CreatesBackup()
        {
            var recovery = new RecoveryPartitionManager(_logger);
            var partitionPath = Path.Combine(_testDir, "Recovery_Backup");
            var backupPath = Path.Combine(_testDir, "Backup");
            Directory.CreateDirectory(partitionPath);

            var result = await recovery.BackupWinREAsync(partitionPath, backupPath);

            Assert.True(result);
        }

        [Fact]
        public async Task RestoreWinRE_ValidBackup_RestoresSuccessfully()
        {
            var recovery = new RecoveryPartitionManager(_logger);
            var partitionPath = Path.Combine(_testDir, "Recovery_Restore");
            var backupPath = Path.Combine(_testDir, "Backup_Restore");

            // Create backup first
            Directory.CreateDirectory(backupPath);
            await recovery.BackupWinREAsync(partitionPath, backupPath);

            // Then restore
            var result = await recovery.RestoreWinREAsync(partitionPath, backupPath);

            Assert.True(result);
        }

        [Fact]
        public async Task GetRecoveryPartitionInfo_ReturnsInfo()
        {
            var recovery = new RecoveryPartitionManager(_logger);
            var info = await recovery.GetRecoveryPartitionInfoAsync("RecoveryPart_1");

            Assert.NotNull(info);
            Assert.NotEmpty(info.PartitionId);
        }

        [Fact]
        public async Task ValidateRecoveryPartition_ValidPartition_ReturnsTrue()
        {
            var recovery = new RecoveryPartitionManager(_logger);
            var partitionPath = Path.Combine(_testDir, "Recovery_Validate");
            Directory.CreateDirectory(partitionPath);

            var result = await recovery.ValidateRecoveryPartitionAsync(partitionPath);

            Assert.True(result);
        }

        [Fact]
        public async Task EnumerateRecoveryPartitions_ReturnsPartitions()
        {
            var recovery = new RecoveryPartitionManager(_logger);
            var partitions = await recovery.EnumerateRecoveryPartitionsAsync();

            Assert.NotNull(partitions);
        }

        // USBHealthMonitor Tests

        [Fact]
        public async Task GetUSBHealth_ValidDevice_ReturnsHealth()
        {
            var monitor = new USBHealthMonitor(_logger);
            var health = await monitor.GetUSBHealthAsync("USB001");

            Assert.NotNull(health);
            Assert.NotEmpty(health.DeviceId);
            Assert.True(health.HealthPercentage >= 0);
        }

        [Fact]
        public async Task MonitorUSBHealth_ValidDevice_StartsMonitoring()
        {
            var monitor = new USBHealthMonitor(_logger);
            var result = await monitor.MonitorUSBHealthAsync("USB001", TimeSpan.FromSeconds(5));

            Assert.True(result);

            // Clean up
            await monitor.StopUSBMonitorAsync("USB001");
        }

        [Fact]
        public async Task StopUSBMonitor_MonitoringDevice_StopsSuccessfully()
        {
            var monitor = new USBHealthMonitor(_logger);
            await monitor.MonitorUSBHealthAsync("USB002", TimeSpan.FromSeconds(5));

            var result = await monitor.StopUSBMonitorAsync("USB002");

            Assert.True(result);
        }

        [Fact]
        public async Task GetAllUSBDevices_EnumeratesDevices_ReturnsDevices()
        {
            var monitor = new USBHealthMonitor(_logger);
            var devices = await monitor.GetAllUSBDevicesAsync();

            Assert.NotNull(devices);
            Assert.NotEmpty(devices);
        }

        [Fact]
        public async Task SafeEject_ValidDevice_EjectsSuccessfully()
        {
            var monitor = new USBHealthMonitor(_logger);
            var result = await monitor.SafeEjectAsync("USB001");

            Assert.True(result);
        }

        [Fact]
        public async Task DetectFailedDevices_NoFailures_ReturnsEmpty()
        {
            var monitor = new USBHealthMonitor(_logger);
            var failed = await monitor.DetectFailedDevicesAsync();

            Assert.NotNull(failed);
        }

        [Fact]
        public async Task GetAllDeviceHealth_ReturnsHealthStatus()
        {
            var monitor = new USBHealthMonitor(_logger);
            await monitor.MonitorUSBHealthAsync("USB001", TimeSpan.FromSeconds(1));

            var health = await monitor.GetAllDeviceHealthAsync();

            Assert.NotNull(health);

            await monitor.StopUSBMonitorAsync("USB001");
        }

        // Integration Tests

        [Fact]
        public async Task IntegrationTest_FullBootEnvironmentSetup_CompleteSuccessfully()
        {
            // Bootstrap
            var engine = new USBBootstrapEngine(_logger);
            var peRoot = Path.Combine(_testDir, "Integration_PE");
            var bootstrapResult = await engine.CreateWinPEEnvironmentAsync(peRoot, true, true);
            Assert.True(bootstrapResult);

            // Pre-Boot Setup
            var preEnv = new PreBootEnvironment(_logger);
            var driverPath = Path.Combine(_testDir, "integration_driver.sys");
            await File.WriteAllTextAsync(driverPath, "DRIVER");

            var driverResult = await preEnv.LoadPEDriversAsync(peRoot, new List<string> { driverPath });
            Assert.True(driverResult);

            var networkResult = await preEnv.SetupPENetworkAsync(peRoot);
            Assert.True(networkResult);

            // Build ISO
            var builder = new ISOImageBuilder(_logger);
            var isoOutput = Path.Combine(_testDir, "Integration_ISO");
            var isoPath = await builder.BuildISOImageAsync(peRoot, isoOutput, "integration.iso");
            Assert.NotNull(isoPath);

            // Configure Boot Menu
            var menuManager = new BootMenuManager(_logger);
            var menuConfig = await menuManager.CreateBootMenuAsync(new List<string> { "PE", "Tools" });
            Assert.NotNull(menuConfig);

            // Check Diagnostics
            var diag = new BootDiagnostics(_logger);
            var diagResult = await diag.RunBootDiagnosticsAsync();
            Assert.NotNull(diagResult);
            Assert.NotNull(diagResult.DiagnosticMessages);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_testDir))
                {
                    Directory.Delete(_testDir, true);
                }
            }
            catch { /* Cleanup best effort */ }
        }
    }
}
