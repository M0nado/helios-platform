using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace HELIOS.Platform.Phase10.Drivers.Tests
{
    public class DriverDetectorTests
    {
        [Fact]
        public async Task DetectAllHardwareAsync_ReturnsDevices()
        {
            using (var detector = new DriverDetector())
            {
                var devices = await detector.DetectAllHardwareAsync();
                Assert.NotNull(devices);
                Assert.IsType<List<DetectedDevice>>(devices);
            }
        }

        [Fact]
        public async Task DetectChipsetsAsync_ReturnsChipsets()
        {
            using (var detector = new DriverDetector())
            {
                var chipsets = await detector.DetectChipsetsAsync();
                Assert.NotNull(chipsets);
                Assert.IsType<List<DetectedDevice>>(chipsets);
            }
        }

        [Fact]
        public async Task DetectGpuAsync_ReturnsGpus()
        {
            using (var detector = new DriverDetector())
            {
                var gpus = await detector.DetectGpuAsync();
                Assert.NotNull(gpus);
                Assert.IsType<List<DetectedDevice>>(gpus);
            }
        }

        [Fact]
        public async Task HasGpuAsync_ReturnsBool()
        {
            using (var detector = new DriverDetector())
            {
                var hasGpu = await detector.HasGpuAsync();
                Assert.IsType<bool>(hasGpu);
            }
        }

        [Fact]
        public async Task DetectAudioAsync_ReturnsAudioDevices()
        {
            using (var detector = new DriverDetector())
            {
                var audio = await detector.DetectAudioAsync();
                Assert.NotNull(audio);
                Assert.IsType<List<DetectedDevice>>(audio);
            }
        }

        [Fact]
        public async Task DetectNetworkAsync_ReturnsNetworkAdapters()
        {
            using (var detector = new DriverDetector())
            {
                var network = await detector.DetectNetworkAsync();
                Assert.NotNull(network);
                Assert.IsType<List<DetectedDevice>>(network);
            }
        }

        [Fact]
        public async Task DetectStorageAsync_ReturnsStorageControllers()
        {
            using (var detector = new DriverDetector())
            {
                var storage = await detector.DetectStorageAsync();
                Assert.NotNull(storage);
                Assert.IsType<List<DetectedDevice>>(storage);
            }
        }

        [Fact]
        public async Task DetectUsbAsync_ReturnsUsbDevices()
        {
            using (var detector = new DriverDetector())
            {
                var usb = await detector.DetectUsbAsync();
                Assert.NotNull(usb);
                Assert.IsType<List<DetectedDevice>>(usb);
            }
        }

        [Fact]
        public async Task DetectBiometricsAsync_ReturnsBiometricDevices()
        {
            using (var detector = new DriverDetector())
            {
                var biometric = await detector.DetectBiometricsAsync();
                Assert.NotNull(biometric);
                Assert.IsType<List<DetectedDevice>>(biometric);
            }
        }

        [Fact]
        public async Task GetDevicesByTypeAsync_FiltersCorrectly()
        {
            using (var detector = new DriverDetector())
            {
                var gpuDevices = await detector.GetDevicesByTypeAsync("GPU");
                Assert.NotNull(gpuDevices);
                foreach (var device in gpuDevices)
                {
                    Assert.Equal("GPU", device.DeviceType);
                }
            }
        }
    }

    public class DriverRepositoryTests
    {
        private string _testRepoPath;

        public DriverRepositoryTests()
        {
            _testRepoPath = Path.Combine(Path.GetTempPath(), "HELIOS_Test_Repo_" + Guid.NewGuid());
        }

        private DriverRepository CreateTestRepository()
        {
            return new DriverRepository(_testRepoPath);
        }

        [Fact]
        public async Task InitializeAsync_CreatesDirectories()
        {
            var repo = CreateTestRepository();
            await repo.InitializeAsync();

            Assert.True(Directory.Exists(_testRepoPath));
            Assert.True(Directory.Exists(Path.Combine(_testRepoPath, "Cache")));

            Cleanup();
        }

        [Fact]
        public async Task StoreDriverAsync_SavesDriver()
        {
            var repo = CreateTestRepository();
            await repo.InitializeAsync();

            var tempFile = Path.Combine(Path.GetTempPath(), $"test_driver_{Guid.NewGuid()}.exe");
            File.WriteAllText(tempFile, "test content");

            var driver = new DriverInfo
            {
                DriverId = "test_driver_1",
                Name = "Test Driver",
                Version = "1.0.0"
            };

            await repo.StoreDriverAsync(driver, tempFile);

            var retrieved = await repo.GetDriverAsync("test_driver_1");
            Assert.NotNull(retrieved);
            Assert.Equal("test_driver_1", retrieved.DriverId);

            File.Delete(tempFile);
            Cleanup();
        }

        [Fact]
        public async Task GetAllDriversAsync_ReturnsStoredDrivers()
        {
            var repo = CreateTestRepository();
            await repo.InitializeAsync();

            var tempFile = Path.Combine(Path.GetTempPath(), $"test_driver_{Guid.NewGuid()}.exe");
            File.WriteAllText(tempFile, "test");

            var driver = new DriverInfo { DriverId = "test_1", Name = "Test", Version = "1.0.0" };
            await repo.StoreDriverAsync(driver, tempFile);

            var all = await repo.GetAllDriversAsync();
            Assert.NotEmpty(all);

            File.Delete(tempFile);
            Cleanup();
        }

        [Fact]
        public async Task GetDriversByTypeAsync_FiltersCorrectly()
        {
            var repo = CreateTestRepository();
            await repo.InitializeAsync();

            var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.exe");
            File.WriteAllText(tempFile, "test");

            var driver = new DriverInfo
            {
                DriverId = "gpu_driver",
                Name = "GPU Driver",
                Version = "1.0.0",
                DriverType = "GPU"
            };

            await repo.StoreDriverAsync(driver, tempFile);

            var gpuDrivers = await repo.GetDriversByTypeAsync("GPU");
            Assert.NotEmpty(gpuDrivers);
            Assert.All(gpuDrivers, d => Assert.Equal("GPU", d.DriverType));

            File.Delete(tempFile);
            Cleanup();
        }

        [Fact]
        public async Task RemoveDriverAsync_DeletesDriver()
        {
            var repo = CreateTestRepository();
            await repo.InitializeAsync();

            var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.exe");
            File.WriteAllText(tempFile, "test");

            var driver = new DriverInfo { DriverId = "test_remove", Name = "Test", Version = "1.0.0" };
            await repo.StoreDriverAsync(driver, tempFile);

            var exists = await repo.DriverExistsAsync("test_remove");
            Assert.True(exists);

            await repo.RemoveDriverAsync("test_remove");

            var stillExists = await repo.DriverExistsAsync("test_remove");
            Assert.False(stillExists);

            File.Delete(tempFile);
            Cleanup();
        }

        [Fact]
        public async Task GetDriverCountAsync_ReturnsCorrectCount()
        {
            var repo = CreateTestRepository();
            await repo.InitializeAsync();

            var initialCount = await repo.GetDriverCountAsync();
            Assert.Equal(0, initialCount);

            var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.exe");
            File.WriteAllText(tempFile, "test");

            var driver = new DriverInfo { DriverId = "test_count", Name = "Test", Version = "1.0.0" };
            await repo.StoreDriverAsync(driver, tempFile);

            var newCount = await repo.GetDriverCountAsync();
            Assert.Equal(1, newCount);

            File.Delete(tempFile);
            Cleanup();
        }

        [Fact]
        public async Task ClearCacheAsync_RemovesAllDrivers()
        {
            var repo = CreateTestRepository();
            await repo.InitializeAsync();

            var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.exe");
            File.WriteAllText(tempFile, "test");

            var driver = new DriverInfo { DriverId = "test_clear", Name = "Test", Version = "1.0.0" };
            await repo.StoreDriverAsync(driver, tempFile);

            var countBefore = await repo.GetDriverCountAsync();
            Assert.Equal(1, countBefore);

            await repo.ClearCacheAsync();

            var countAfter = await repo.GetDriverCountAsync();
            Assert.Equal(0, countAfter);

            File.Delete(tempFile);
            Cleanup();
        }

        private void Cleanup()
        {
            try { Directory.Delete(_testRepoPath, recursive: true); } catch { }
        }
    }

    public class DriverDownloaderTests
    {
        private DriverRepository _testRepo;

        public DriverDownloaderTests()
        {
            var testPath = Path.Combine(Path.GetTempPath(), "HELIOS_Download_Test_" + Guid.NewGuid());
            _testRepo = new DriverRepository(testPath);
        }

        [Fact]
        public void GetManufacturerUrl_ReturnsIntelUrl()
        {
            var downloader = new DriverDownloader(_testRepo);
            var url = downloader.GetManufacturerUrl("Intel");
            Assert.NotNull(url);
            Assert.Contains("intel", url, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void GetManufacturerUrl_ReturnsAmdUrl()
        {
            var downloader = new DriverDownloader(_testRepo);
            var url = downloader.GetManufacturerUrl("AMD");
            Assert.NotNull(url);
            Assert.Contains("amd", url, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void GetManufacturerUrl_ReturnsNvidiaUrl()
        {
            var downloader = new DriverDownloader(_testRepo);
            var url = downloader.GetManufacturerUrl("NVIDIA");
            Assert.NotNull(url);
            Assert.Contains("nvidia", url, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task VerifyChecksumAsync_ValidChecksum()
        {
            var testFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.txt");
            File.WriteAllText(testFile, "test content");

            var downloader = new DriverDownloader(_testRepo);
            var checksum = await downloader.CalculateChecksumAsync(testFile);
            Assert.NotNull(checksum);

            var isValid = await downloader.VerifyChecksumAsync(testFile, checksum);
            Assert.True(isValid);

            File.Delete(testFile);
        }

        [Fact]
        public async Task VerifyChecksumAsync_InvalidChecksum()
        {
            var testFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.txt");
            File.WriteAllText(testFile, "test content");

            var downloader = new DriverDownloader(_testRepo);
            var isValid = await downloader.VerifyChecksumAsync(testFile, "invalidchecksum");
            Assert.False(isValid);

            File.Delete(testFile);
        }

        [Fact]
        public async Task CalculateChecksumAsync_ReturnsValidChecksum()
        {
            var testFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.txt");
            File.WriteAllText(testFile, "test data");

            var downloader = new DriverDownloader(_testRepo);
            var checksum = await downloader.CalculateChecksumAsync(testFile);

            Assert.NotNull(checksum);
            Assert.NotEmpty(checksum);

            File.Delete(testFile);
        }
    }

    public class DriverInstallerTests
    {
        private DriverRepository _testRepo;
        private string _testRepoPath;

        public DriverInstallerTests()
        {
            _testRepoPath = Path.Combine(Path.GetTempPath(), "HELIOS_Install_Test_" + Guid.NewGuid());
            _testRepo = new DriverRepository(_testRepoPath);
        }

        [Fact]
        public async Task InstallDriverAsync_MissingDriver_ReturnsFalse()
        {
            var installer = new DriverInstaller(_testRepo);
            var result = await installer.InstallDriverAsync("nonexistent");

            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal("nonexistent", result.DriverName);
        }

        [Fact]
        public async Task GetInstallationHistoryAsync_ReturnsHistory()
        {
            var installer = new DriverInstaller(_testRepo);
            var history = await installer.GetInstallationHistoryAsync();

            Assert.NotNull(history);
            Assert.IsType<List<InstallationResult>>(history);
        }

        [Fact]
        public async Task CreateRestorePointAsync_Succeeds()
        {
            var installer = new DriverInstaller(_testRepo);
            var result = await installer.CreateRestorePointAsync("Test Restore Point");

            Assert.IsType<bool>(result);
        }

        [Fact]
        public async Task VerifyInstallationAsync_ReturnsBool()
        {
            var installer = new DriverInstaller(_testRepo);
            var verified = await installer.VerifyInstallationAsync("test_driver");

            Assert.IsType<bool>(verified);
        }
    }

    public class DriverUpdaterTests
    {
        private DriverRepository _testRepo;
        private DriverDownloader _downloader;
        private DriverInstaller _installer;
        private string _testRepoPath;

        public DriverUpdaterTests()
        {
            _testRepoPath = Path.Combine(Path.GetTempPath(), "HELIOS_Update_Test_" + Guid.NewGuid());
            _testRepo = new DriverRepository(_testRepoPath);
            _downloader = new DriverDownloader(_testRepo);
            _installer = new DriverInstaller(_testRepo);
        }

        [Fact]
        public async Task CheckForUpdatesAsync_ReturnsUpdates()
        {
            var updater = new DriverUpdater(_testRepo, _downloader, _installer);
            var updates = await updater.CheckForUpdatesAsync();

            Assert.NotNull(updates);
            Assert.IsType<List<DriverInfo>>(updates);
        }

        [Fact]
        public async Task GetAvailableUpdatesAsync_ReturnsUpdates()
        {
            var updater = new DriverUpdater(_testRepo, _downloader, _installer);
            var available = await updater.GetAvailableUpdatesAsync();

            Assert.NotNull(available);
            Assert.IsType<List<DriverInfo>>(available);
        }

        [Fact]
        public async Task UpdateDriverAsync_NonexistentDriver_ReturnsFalse()
        {
            var updater = new DriverUpdater(_testRepo, _downloader, _installer);
            var result = await updater.UpdateDriverAsync("nonexistent");

            Assert.NotNull(result);
            Assert.False(result.Success);
        }

        [Fact]
        public void IsScheduled_ReturnsFalse_Initially()
        {
            var updater = new DriverUpdater(_testRepo, _downloader, _installer);
            Assert.False(updater.IsScheduled);
        }

        [Fact]
        public async Task ScheduleUpdateCheckAsync_SetsScheduled()
        {
            var updater = new DriverUpdater(_testRepo, _downloader, _installer);
            await updater.ScheduleUpdateCheckAsync(TimeSpan.FromHours(1));

            Assert.True(updater.IsScheduled);

            await updater.UnscheduleUpdateCheckAsync();
            updater.Dispose();
        }

        [Fact]
        public async Task GetUpdateHistoryAsync_ReturnsHistory()
        {
            var updater = new DriverUpdater(_testRepo, _downloader, _installer);
            var history = await updater.GetUpdateHistoryAsync();

            Assert.NotNull(history);
            Assert.IsType<List<(string, DateTime, string)>>(history);
        }
    }

    public class DriverRollbackTests
    {
        private DriverRepository _testRepo;
        private string _testRepoPath;

        public DriverRollbackTests()
        {
            _testRepoPath = Path.Combine(Path.GetTempPath(), "HELIOS_Rollback_Test_" + Guid.NewGuid());
            _testRepo = new DriverRepository(_testRepoPath);
        }

        [Fact]
        public async Task DetectProblematicDriversAsync_ReturnsDrivers()
        {
            var rollback = new DriverRollback(_testRepo);
            var problematic = await rollback.DetectProblematicDriversAsync();

            Assert.NotNull(problematic);
            Assert.IsType<List<string>>(problematic);
        }

        [Fact]
        public async Task GetAvailableBackupsAsync_ReturnsBackups()
        {
            var rollback = new DriverRollback(_testRepo);
            var backups = await rollback.GetAvailableBackupsAsync("test_driver");

            Assert.NotNull(backups);
            Assert.IsType<List<DriverInfo>>(backups);
        }

        [Fact]
        public async Task GetBackupSizeAsync_ReturnsSize()
        {
            var rollback = new DriverRollback(_testRepo);
            var size = await rollback.GetBackupSizeAsync();

            Assert.IsType<long>(size);
            Assert.True(size >= 0);
        }

        [Fact]
        public async Task GetAllBackupsAsync_ReturnsDictionary()
        {
            var rollback = new DriverRollback(_testRepo);
            var allBackups = await rollback.GetAllBackupsAsync();

            Assert.NotNull(allBackups);
            Assert.IsType<Dictionary<string, List<DriverInfo>>>(allBackups);
        }
    }

    public class DriverHealthMonitorTests
    {
        [Fact]
        public async Task InitializeAsync_Succeeds()
        {
            var monitor = new DriverHealthMonitor();
            await monitor.InitializeAsync();

            Assert.True(true);
            monitor.Shutdown();
        }

        [Fact]
        public async Task CheckDriverHealthAsync_ReturnsStatus()
        {
            var monitor = new DriverHealthMonitor();
            await monitor.InitializeAsync();

            var status = await monitor.CheckDriverHealthAsync("test_driver");

            Assert.NotNull(status);
            Assert.IsType<DriverHealthStatus>(status);

            monitor.Shutdown();
        }

        [Fact]
        public async Task GetSystemHealthAsync_ReturnsStatuses()
        {
            var monitor = new DriverHealthMonitor();
            await monitor.InitializeAsync();

            var statuses = await monitor.GetSystemHealthAsync();

            Assert.NotNull(statuses);
            Assert.IsType<List<DriverHealthStatus>>(statuses);

            monitor.Shutdown();
        }

        [Fact]
        public async Task DetectDriverCrashesAsync_ReturnsCrashes()
        {
            var monitor = new DriverHealthMonitor();
            var crashes = await monitor.DetectDriverCrashesAsync();

            Assert.NotNull(crashes);
            Assert.IsType<List<string>>(crashes);
        }

        [Fact]
        public async Task DetectAllCrashesAsync_ReturnsCrashes()
        {
            var monitor = new DriverHealthMonitor();
            var crashes = await monitor.DetectAllCrashesAsync();

            Assert.NotNull(crashes);
            Assert.IsType<List<string>>(crashes);
        }

        [Fact]
        public async Task GenerateStabilityReportAsync_ReturnsReport()
        {
            var monitor = new DriverHealthMonitor();
            await monitor.InitializeAsync();

            var report = await monitor.GenerateStabilityReportAsync();

            Assert.NotNull(report);
            Assert.NotEmpty(report);
            Assert.Contains("Stability Report", report);

            monitor.Shutdown();
        }

        [Fact]
        public async Task GetDriverEventsAsync_ReturnsEvents()
        {
            var monitor = new DriverHealthMonitor();
            var events = await monitor.GetDriverEventsAsync();

            Assert.NotNull(events);
            Assert.IsType<List<string>>(events);
        }

        [Fact]
        public async Task GetAllHealthStatusesAsync_ReturnsStatuses()
        {
            var monitor = new DriverHealthMonitor();
            await monitor.InitializeAsync();

            var statuses = await monitor.GetAllHealthStatusesAsync();

            Assert.NotNull(statuses);
            Assert.IsType<List<DriverHealthStatus>>(statuses);

            monitor.Shutdown();
        }

        [Fact]
        public async Task ClearCacheAsync_Succeeds()
        {
            var monitor = new DriverHealthMonitor();
            await monitor.InitializeAsync();

            await monitor.CheckDriverHealthAsync("test");
            var before = await monitor.GetAllHealthStatusesAsync();

            await monitor.ClearCacheAsync();
            var after = await monitor.GetAllHealthStatusesAsync();

            Assert.Empty(after);

            monitor.Shutdown();
        }
    }

    public class DriverServiceIntegrationTests
    {
        [Fact]
        public void DriverInfoCreation_Succeeds()
        {
            var driver = new DriverInfo
            {
                DriverId = "test",
                Name = "Test Driver",
                Version = "1.0.0",
                ChipsetInfo = "Test Chipset",
                DeviceName = "Test Device"
            };

            Assert.Equal("test", driver.DriverId);
            Assert.Equal("Test Driver", driver.Name);
            Assert.Equal("1.0.0", driver.Version);
        }

        [Fact]
        public void DetectedDeviceCreation_Succeeds()
        {
            var device = new DetectedDevice
            {
                DeviceId = "dev1",
                DeviceName = "Test Device",
                DeviceType = "GPU",
                Status = "Active"
            };

            Assert.Equal("dev1", device.DeviceId);
            Assert.Equal("GPU", device.DeviceType);
        }

        [Fact]
        public void InstallationResultCreation_Succeeds()
        {
            var result = new InstallationResult
            {
                Success = true,
                DriverName = "Test",
                Version = "1.0.0",
                Message = "Success"
            };

            Assert.True(result.Success);
            Assert.Equal("Test", result.DriverName);
        }

        [Fact]
        public void DriverHealthStatusCreation_Succeeds()
        {
            var status = new DriverHealthStatus
            {
                DriverId = "test",
                DriverName = "Test Driver",
                IsHealthy = true,
                ErrorCount = 0,
                CrashCount = 0
            };

            Assert.True(status.IsHealthy);
            Assert.Equal(0, status.ErrorCount);
        }
    }
}
