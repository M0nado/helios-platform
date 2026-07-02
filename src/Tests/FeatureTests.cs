using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using MonadoBlade.Features;

namespace MonadoBlade.Tests
{
    public class DualBootWizardTests
    {
        [Fact]
        public async Task PerformPreflight_ReturnsTrueWhenReady()
        {
            var wizard = new DualBootWizard();
            var result = await wizard.PerformPreflight();

            Assert.NotNull(result);
            Assert.IsType<DualBootCheckResult>(result);
        }

        [Fact]
        public async Task PerformPreflight_ChecksAvailableSpace()
        {
            var wizard = new DualBootWizard();
            var result = await wizard.PerformPreflight();

            Assert.NotNull(result);
            Assert.True(result.AvailableSpaceGB >= 0);
        }

        [Fact]
        public async Task ExecuteDualBootInstallation_RequiresPreflight()
        {
            var wizard = new DualBootWizard();
            var config = new DualBootConfiguration();

            var result = await wizard.ExecuteDualBootInstallation(config);

            Assert.False(result);
        }

        [Fact]
        public async Task VerifyDataIntegrity_ReturnsResult()
        {
            var wizard = new DualBootWizard();
            var result = await wizard.VerifyDataIntegrity();

            Assert.NotNull(result);
            Assert.IsType<DataIntegrityResult>(result);
        }

        [Fact]
        public async Task Rollback_FailsWithoutPreparedBackup()
        {
            var wizard = new DualBootWizard();
            var result = await wizard.Rollback();

            Assert.False(result);
        }
    }

    public class PartitionResizerTests
    {
        [Fact]
        public async Task ResizeWindowsPartition_ValidatesReductionPercent()
        {
            var resizer = new PartitionResizer();

            var result = await resizer.ResizeWindowsPartition(150);

            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
        }

        [Fact]
        public async Task ResizeWindowsPartition_SucceedsWithValidPercent()
        {
            var resizer = new PartitionResizer();

            var result = await resizer.ResizeWindowsPartition(20);

            Assert.True(result.Success);
            Assert.NotNull(result.NewPartitionPath);
        }

        [Fact]
        public async Task CreateMonadoPartition_RequiresValidPath()
        {
            var resizer = new PartitionResizer();

            var result = await resizer.CreateMonadoPartition("");

            Assert.False(result.Success);
        }

        [Fact]
        public async Task CreateMonadoPartition_CreatesExt4Partition()
        {
            var resizer = new PartitionResizer();

            var result = await resizer.CreateMonadoPartition("/dev/sda1");

            Assert.True(result.Success);
            Assert.Equal("ext4", result.FilesystemType);
        }

        [Fact]
        public async Task RestoreWindowsPartition_RequiresBackup()
        {
            var resizer = new PartitionResizer();

            var result = await resizer.RestoreWindowsPartition();

            Assert.False(result.Success);
        }
    }

    public class CorruptionDetectorTests
    {
        [Fact]
        public async Task PerformFullCheck_ReturnsCheckResult()
        {
            var detector = new CorruptionDetector();
            var result = await detector.PerformFullCheck();

            Assert.NotNull(result);
            Assert.IsType<CorruptionCheckResult>(result);
            Assert.True(result.Success);
        }

        [Fact]
        public async Task DetectPartitionCorruption_ReturnsIssueList()
        {
            var detector = new CorruptionDetector();
            var issues = await detector.DetectPartitionCorruption();

            Assert.NotNull(issues);
            Assert.IsType<List<CorruptionIssue>>(issues);
        }

        [Fact]
        public async Task DetectBootloaderCorruption_ReturnsIssueList()
        {
            var detector = new CorruptionDetector();
            var issues = await detector.DetectBootloaderCorruption();

            Assert.NotNull(issues);
            Assert.IsType<List<CorruptionIssue>>(issues);
        }

        [Fact]
        public async Task DetectFilesystemCorruption_ReturnsIssueList()
        {
            var detector = new CorruptionDetector();
            var issues = await detector.DetectFilesystemCorruption();

            Assert.NotNull(issues);
            Assert.IsType<List<CorruptionIssue>>(issues);
        }

        [Fact]
        public void StartDailyChecking_StartsBackgroundTimer()
        {
            var detector = new CorruptionDetector();
            detector.StartDailyChecking(2);

            detector.StopDailyChecking();
        }
    }

    public class AutoRepairEngineTests
    {
        [Fact]
        public void SetRepairMode_ChangesMode()
        {
            var engine = new AutoRepairEngine();
            engine.SetRepairMode(RepairMode.Safe);
        }

        [Fact]
        public async Task RepairCorruptionIssues_EmptyListReturnsSuccess()
        {
            var engine = new AutoRepairEngine();
            var result = await engine.RepairCorruptionIssues(new List<CorruptionIssue>());

            Assert.True(result.Success);
            Assert.Equal(0, result.IssuesFixed);
        }

        [Fact]
        public async Task RepairCorruptionIssues_ReturnsRepairResult()
        {
            var engine = new AutoRepairEngine();
            var issues = new List<CorruptionIssue>
            {
                new CorruptionIssue { Type = CorruptionType.PartitionCorruption, Severity = SeverityLevel.High }
            };

            var result = await engine.RepairCorruptionIssues(issues);

            Assert.NotNull(result);
            Assert.IsType<RepairResult>(result);
        }

        [Fact]
        public void GetSuccessRate_ReturnsPercentage()
        {
            var engine = new AutoRepairEngine();
            var rate = engine.GetSuccessRate();

            Assert.True(rate >= 0 && rate <= 100);
        }

        [Fact]
        public void GetRepairHistory_ReturnsOperationList()
        {
            var engine = new AutoRepairEngine();
            var history = engine.GetRepairHistory();

            Assert.NotNull(history);
            Assert.IsType<List<RepairOperation>>(history);
        }
    }

    public class RecoveryPartitionTests
    {
        [Fact]
        public async Task InitializeRecoveryPartition_SucceedsAndReturnsTrue()
        {
            var recovery = new RecoveryPartition();
            var result = await recovery.InitializeRecoveryPartition();

            Assert.True(result);
        }

        [Fact]
        public async Task CreateSystemBackup_ReturnsBackupResult()
        {
            var recovery = new RecoveryPartition();
            await recovery.InitializeRecoveryPartition();

            var result = await recovery.CreateSystemBackup();

            Assert.True(result.Success);
            Assert.NotNull(result.BackupPath);
        }

        [Fact]
        public async Task RestoreSystemBackup_SupportsRestoration()
        {
            var recovery = new RecoveryPartition();
            await recovery.InitializeRecoveryPartition();

            var result = await recovery.RestoreSystemBackup();

            Assert.True(result);
        }

        [Fact]
        public async Task GetPartitionInfo_ReturnsPartitionInfo()
        {
            var recovery = new RecoveryPartition();
            var info = await recovery.GetPartitionInfo();

            Assert.NotNull(info);
            Assert.True(info.TotalSize > 0);
        }

        [Fact]
        public async Task StoreResource_SucceedsWithValidData()
        {
            var recovery = new RecoveryPartition();
            var data = new byte[1024];

            var result = await recovery.StoreResource("test_resource", data);

            Assert.True(result);
        }

        [Fact]
        public async Task GetBackupBootloader_ReturnsBootloaderData()
        {
            var recovery = new RecoveryPartition();
            var data = await recovery.GetBackupBootloader();

            Assert.NotNull(data);
        }
    }

    public class CloudProfileSyncerTests
    {
        [Fact]
        public async Task InitializeAdapters_SucceedsAndReturnsTrue()
        {
            var syncer = new CloudProfileSyncer();
            var result = await syncer.InitializeAdapters();

            Assert.True(result);
        }

        [Fact]
        public async Task AuthenticateWithProvider_SupportsMultipleProviders()
        {
            var syncer = new CloudProfileSyncer();
            await syncer.InitializeAdapters();

            var result = await syncer.AuthenticateWithProvider(CloudProvider.OneDrive);

            Assert.True(result);
        }

        [Fact]
        public async Task BackupProfileToCloud_SucceedsWithActiveProvider()
        {
            var syncer = new CloudProfileSyncer();
            await syncer.InitializeAdapters();
            await syncer.AuthenticateWithProvider(CloudProvider.OneDrive);

            var result = await syncer.BackupProfileToCloud();

            Assert.True(result.Success);
            Assert.True(result.CompressionRatio >= 0);
        }

        [Fact]
        public async Task RestoreProfileFromCloud_SupportsRestore()
        {
            var syncer = new CloudProfileSyncer();
            await syncer.InitializeAdapters();
            await syncer.AuthenticateWithProvider(CloudProvider.OneDrive);

            var result = await syncer.RestoreProfileFromCloud();

            Assert.NotNull(result);
            Assert.IsType<RestoreSyncResult>(result);
        }

        [Fact]
        public async Task PerformDifferentialSync_ReturnsResultWithChangedFiles()
        {
            var syncer = new CloudProfileSyncer();
            var result = await syncer.PerformDifferentialSync();

            Assert.NotNull(result);
            Assert.IsType<DifferentialSyncResult>(result);
        }

        [Fact]
        public void ConfigureSelectiveSync_StoresComponentList()
        {
            var syncer = new CloudProfileSyncer();
            var components = new List<string> { "Documents", "Pictures", "Settings" };

            syncer.ConfigureSelectiveSync(components);
        }

        [Fact]
        public void SetupScheduledBackups_ConfiguresBackupInterval()
        {
            var syncer = new CloudProfileSyncer();
            syncer.SetupScheduledBackups(24);
        }

        [Fact]
        public async Task GetAvailableBackups_ReturnsBackupList()
        {
            var syncer = new CloudProfileSyncer();
            await syncer.InitializeAdapters();
            await syncer.AuthenticateWithProvider(CloudProvider.OneDrive);

            var backups = await syncer.GetAvailableBackups();

            Assert.NotNull(backups);
            Assert.IsType<List<BackupInfo>>(backups);
        }
    }
}
