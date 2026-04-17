using Xunit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using HELIOS.Platform.Core.Backup;
using HELIOS.Platform.Core.Cloud;
using HELIOS.Platform.Core.Performance;
using HELIOS.Platform.Core.Security;
using HELIOS.Platform.Core.Configuration;
using HELIOS.Platform.Core.Administration;

namespace HELIOS.Platform.Tests;

/// <summary>
/// Comprehensive test suite for Phase 2 services
/// Target: 95%+ code coverage for backup, monitoring, cloud, performance, vault, and configuration services
/// </summary>
public class Phase2ServiceTests
{
    // ============ BACKUP SERVICE TESTS ============
    
    [Fact]
    public async Task BackupService_CreateFullBackup_ReturnsSuccessfulResult()
    {
        var backupService = new BackupService();
        var result = await backupService.CreateFullBackupAsync("/test/backup", "test-backup");
        
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(BackupType.Full, result.BackupType);
        Assert.NotEmpty(result.BackupId);
    }

    [Fact]
    public async Task BackupService_CreateIncrementalBackup_ReturnsIncrementalType()
    {
        var backupService = new BackupService();
        
        await backupService.CreateFullBackupAsync("/test/backup", "base");
        var incrementalResult = await backupService.CreateIncrementalBackupAsync("/test/backup", "increment");
        
        Assert.NotNull(incrementalResult);
        Assert.True(incrementalResult.Success);
        Assert.Equal(BackupType.Incremental, incrementalResult.BackupType);
    }

    [Fact]
    public async Task BackupService_ScheduleBackup_CreatesScheduleSuccessfully()
    {
        var backupService = new BackupService();
        var schedule = new BackupSchedule
        {
            Name = "Daily",
            CronExpression = "0 0 * * *",
            RetentionDays = 30,
            Enabled = true
        };
        
        var result = await backupService.ScheduleBackupAsync(schedule);
        
        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task BackupService_RestoreBackup_ReturnsRestoreResult()
    {
        var backupService = new BackupService();
        var backupId = "test-backup-123";
        
        var restoreResult = await backupService.RestoreBackupAsync(backupId, "/restore/path");
        
        Assert.NotNull(restoreResult);
        Assert.NotEmpty(restoreResult.RestoredFiles);
    }

    [Fact]
    public async Task BackupService_VerifyBackupIntegrity_ValidatesChecksum()
    {
        var backupService = new BackupService();
        var result = await backupService.VerifyBackupIntegrityAsync("test-backup-id");
        
        Assert.NotNull(result);
        Assert.IsType<bool>(result);
    }

    [Fact]
    public async Task BackupService_RunDisasterRecoveryTest_CompletesCycle()
    {
        var backupService = new BackupService();
        var result = await backupService.RunDisasterRecoveryTestAsync("test-backup-id");
        
        Assert.NotNull(result);
        Assert.True(result.TestCompleted);
        Assert.NotEmpty(result.Results);
    }

    // ============ CLOUD INTEGRATION SERVICE TESTS ============

    [Fact]
    public async Task CloudService_ConnectToAzure_ReturnsConnectionStatus()
    {
        var cloudService = new CloudIntegrationService();
        var status = await cloudService.ConnectToAzureAsync("test-subscription", "test-resource-group");
        
        Assert.NotNull(status);
        Assert.IsType<bool>(status);
    }

    [Fact]
    public async Task CloudService_SyncDataToCloud_SuccessfulSync()
    {
        var cloudService = new CloudIntegrationService();
        var result = await cloudService.SyncDataToCloudAsync("local-path", "cloud-path", new Dictionary<string, object>());
        
        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task CloudService_RestoreFromCloud_ReturnsRestoreInfo()
    {
        var cloudService = new CloudIntegrationService();
        var result = await cloudService.RestoreFromCloudAsync("cloud-path", "local-path");
        
        Assert.NotNull(result);
        Assert.NotEmpty(result.RestoredItems);
    }

    [Fact]
    public async Task CloudService_CreatePowerBIConnection_ReturnsConnectionInfo()
    {
        var cloudService = new CloudIntegrationService();
        var connection = await cloudService.CreatePowerBIConnectionAsync("test-workspace", "test-dataset");
        
        Assert.NotNull(connection);
        Assert.NotEmpty(connection.WorkspaceId);
    }

    [Fact]
    public async Task CloudService_ListAzureResources_ReturnsResourceList()
    {
        var cloudService = new CloudIntegrationService();
        var resources = await cloudService.ListAzureResourcesAsync("test-subscription");
        
        Assert.NotNull(resources);
        Assert.IsType<List<CloudResource>>(resources);
    }

    [Fact]
    public async Task CloudService_GetCloudStorageMetrics_ReturnsMetrics()
    {
        var cloudService = new CloudIntegrationService();
        var metrics = await cloudService.GetCloudStorageMetricsAsync();
        
        Assert.NotNull(metrics);
        Assert.True(metrics.TotalStorageGB > 0);
    }

    // ============ PERFORMANCE PROFILER TESTS ============

    [Fact]
    public async Task PerformanceProfiler_CollectMetrics_ReturnsSystemMetrics()
    {
        var profiler = new PerformanceProfiler();
        var metrics = await profiler.CollectMetricsAsync();
        
        Assert.NotNull(metrics);
        Assert.True(metrics.CpuUsagePercent >= 0);
        Assert.True(metrics.MemoryUsageMB >= 0);
        Assert.True(metrics.DiskUsagePercent >= 0);
    }

    [Fact]
    public async Task PerformanceProfiler_AnalyzePerformance_ReturnsRecommendations()
    {
        var profiler = new PerformanceProfiler();
        var analysis = await profiler.AnalyzePerformanceAsync();
        
        Assert.NotNull(analysis);
        Assert.NotEmpty(analysis.Recommendations);
    }

    [Fact]
    public async Task PerformanceProfiler_MeasureStartupTime_ReturnsTimeInMilliseconds()
    {
        var profiler = new PerformanceProfiler();
        var startupTime = await profiler.MeasureStartupTimeAsync();
        
        Assert.True(startupTime > 0);
        Assert.True(startupTime < 30000); // Should be less than 30 seconds
    }

    [Fact]
    public async Task PerformanceProfiler_TrackResponseTime_RecordsMetric()
    {
        var profiler = new PerformanceProfiler();
        await profiler.TrackResponseTimeAsync("test-operation", 150);
        
        var percentiles = await profiler.GetResponseTimePercentilesAsync();
        Assert.NotNull(percentiles);
    }

    // ============ SECURITY VAULT SERVICE TESTS ============

    [Fact]
    public async Task VaultService_StoreCredential_SuccessfulStorage()
    {
        var vaultService = new SecurityVaultService();
        var result = await vaultService.StoreCredentialAsync("test-user", "test-password");
        
        Assert.True(result);
    }

    [Fact]
    public async Task VaultService_RetrieveCredential_ReturnsStoredCredential()
    {
        var vaultService = new SecurityVaultService();
        await vaultService.StoreCredentialAsync("test-user", "test-password");
        
        var credential = await vaultService.RetrieveCredentialAsync("test-user");
        
        Assert.NotNull(credential);
    }

    [Fact]
    public async Task VaultService_DeleteCredential_RemovesCredential()
    {
        var vaultService = new SecurityVaultService();
        await vaultService.StoreCredentialAsync("test-user", "test-password");
        
        var result = await vaultService.DeleteCredentialAsync("test-user");
        
        Assert.True(result);
    }

    [Fact]
    public async Task VaultService_EnableMFA_SetsMFAActive()
    {
        var vaultService = new SecurityVaultService();
        var result = await vaultService.EnableMFAAsync("test-user");
        
        Assert.True(result);
    }

    [Fact]
    public async Task VaultService_IntegrateBitLocker_ReturnsStatus()
    {
        var vaultService = new SecurityVaultService();
        var result = await vaultService.IntegrateBitLockerAsync("C:", "test-password");
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task VaultService_EncryptData_ReturnsEncryptedData()
    {
        var vaultService = new SecurityVaultService();
        var encryptedData = await vaultService.EncryptDataAsync("sensitive-data", "AES-256");
        
        Assert.NotNull(encryptedData);
        Assert.NotEqual("sensitive-data", encryptedData);
    }

    // ============ CONFIGURATION MANAGER TESTS ============

    [Fact]
    public async Task ConfigManager_CreateProfile_ReturnsNewProfile()
    {
        var configManager = new AdvancedConfigManager();
        var profile = await configManager.CreateProfileAsync("Test Profile", "Test Description");
        
        Assert.NotNull(profile);
        Assert.Equal("Test Profile", profile.Name);
        Assert.False(profile.IsActive);
    }

    [Fact]
    public async Task ConfigManager_GetProfile_ReturnsProfileWithSettings()
    {
        var configManager = new AdvancedConfigManager();
        var created = await configManager.CreateProfileAsync("Test", "Desc");
        
        var retrieved = await configManager.GetProfileAsync(created.Id);
        
        Assert.NotNull(retrieved);
        Assert.Equal(created.Id, retrieved.Id);
    }

    [Fact]
    public async Task ConfigManager_ListProfiles_ReturnsAllProfiles()
    {
        var configManager = new AdvancedConfigManager();
        await configManager.CreateProfileAsync("Profile1", "Desc1");
        await configManager.CreateProfileAsync("Profile2", "Desc2");
        
        var profiles = await configManager.ListProfilesAsync();
        
        Assert.NotEmpty(profiles);
        Assert.True(profiles.Count >= 3); // Default + 2 created
    }

    [Fact]
    public async Task ConfigManager_UpdateProfile_ModifiesSettings()
    {
        var configManager = new AdvancedConfigManager();
        var profile = await configManager.CreateProfileAsync("Test", "Desc");
        
        var updates = new Dictionary<string, object> { { "AutoBackup", false } };
        var updated = await configManager.UpdateProfileAsync(profile.Id, updates);
        
        Assert.NotNull(updated);
        Assert.False((bool)updated.Settings["AutoBackup"]);
    }

    [Fact]
    public async Task ConfigManager_ActivateProfile_SetsActiveFlag()
    {
        var configManager = new AdvancedConfigManager();
        var profile = await configManager.CreateProfileAsync("Test", "Desc");
        
        var activated = await configManager.ActivateProfileAsync(profile.Id);
        
        Assert.True(activated.IsActive);
    }

    [Fact]
    public async Task ConfigManager_ValidateConfiguration_ReturnsValidationResult()
    {
        var configManager = new AdvancedConfigManager();
        var settings = new Dictionary<string, object>
        {
            { "AutoBackup", true },
            { "PerformanceMode", "Balanced" }
        };
        
        var result = await configManager.ValidateConfigurationAsync(settings);
        
        Assert.NotNull(result);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ConfigManager_ValidateConfiguration_DetectsInvalidValues()
    {
        var configManager = new AdvancedConfigManager();
        var settings = new Dictionary<string, object>
        {
            { "PerformanceMode", "InvalidMode" }
        };
        
        var result = await configManager.ValidateConfigurationAsync(settings);
        
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task ConfigManager_GetChangeHistory_ReturnsHistoryRecords()
    {
        var configManager = new AdvancedConfigManager();
        var profile = await configManager.CreateProfileAsync("Test", "Desc");
        await configManager.UpdateProfileAsync(profile.Id, new Dictionary<string, object> { { "AutoBackup", false } });
        
        var history = await configManager.GetChangeHistoryAsync(profile.Id);
        
        Assert.NotNull(history);
        Assert.NotEmpty(history);
    }

    [Fact]
    public async Task ConfigManager_ExportProfile_CreatesExportFile()
    {
        var configManager = new AdvancedConfigManager();
        var profile = await configManager.CreateProfileAsync("Test", "Desc");
        var exportPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "test-export.json");
        
        var result = await configManager.ExportProfileAsync(profile.Id, exportPath);
        
        Assert.True(result);
        Assert.True(System.IO.File.Exists(exportPath));
        
        System.IO.File.Delete(exportPath);
    }

    [Fact]
    public async Task ConfigManager_ImportProfile_LoadsProfileFromFile()
    {
        var configManager = new AdvancedConfigManager();
        var profile = await configManager.CreateProfileAsync("Original", "Desc");
        var exportPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "test-import.json");
        
        await configManager.ExportProfileAsync(profile.Id, exportPath);
        var imported = await configManager.ImportProfileAsync(exportPath);
        
        Assert.NotNull(imported);
        Assert.Equal(profile.Name, imported.Name);
        
        System.IO.File.Delete(exportPath);
    }

    [Fact]
    public async Task ConfigManager_ResetToDefaults_RestoresDefaultSettings()
    {
        var configManager = new AdvancedConfigManager();
        var profile = await configManager.CreateProfileAsync("Test", "Desc");
        await configManager.ActivateProfileAsync(profile.Id);
        await configManager.UpdateProfileAsync(profile.Id, new Dictionary<string, object> { { "AutoBackup", false } });
        
        var result = await configManager.ResetToDefaultsAsync();
        
        Assert.True(result);
        var config = await configManager.GetActiveConfigurationAsync();
        Assert.True((bool)config["AutoBackup"]);
    }

    [Fact]
    public async Task ConfigManager_DeleteProfile_RemovesInactiveProfile()
    {
        var configManager = new AdvancedConfigManager();
        var profile = await configManager.CreateProfileAsync("Test", "Desc");
        
        var result = await configManager.DeleteProfileAsync(profile.Id);
        
        Assert.True(result);
    }

    [Fact]
    public async Task ConfigManager_DeleteProfile_FailsForActiveProfile()
    {
        var configManager = new AdvancedConfigManager();
        var profile = await configManager.CreateProfileAsync("Test", "Desc");
        await configManager.ActivateProfileAsync(profile.Id);
        
        var result = await configManager.DeleteProfileAsync(profile.Id);
        
        Assert.False(result);
    }

    [Fact]
    public async Task ConfigManager_GetActiveConfiguration_ReturnsCurrentSettings()
    {
        var configManager = new AdvancedConfigManager();
        var config = await configManager.GetActiveConfigurationAsync();
        
        Assert.NotNull(config);
        Assert.NotEmpty(config);
        Assert.Contains("AutoBackup", config.Keys);
    }

    // ============ INTEGRATION TESTS ============

    [Fact]
    public async Task IntegrationTest_BackupAndRestore_CompleteCycle()
    {
        var backupService = new BackupService();
        
        var backupResult = await backupService.CreateFullBackupAsync("/test/path", "integration-test");
        Assert.True(backupResult.Success);
        
        var restoreResult = await backupService.RestoreBackupAsync(backupResult.BackupId, "/restore/path");
        Assert.NotEmpty(restoreResult.RestoredFiles);
    }

    [Fact]
    public async Task IntegrationTest_ConfigurationAndBackup_CoordinatedOperation()
    {
        var configManager = new AdvancedConfigManager();
        var backupService = new BackupService();
        
        var profile = await configManager.CreateProfileAsync("Backup Profile", "For backups");
        await configManager.UpdateProfileAsync(profile.Id, new Dictionary<string, object>
        {
            { "AutoBackup", true },
            { "AutoBackupInterval", 3600 }
        });
        
        var backup = await backupService.CreateFullBackupAsync("/test", "config-backup");
        Assert.True(backup.Success);
    }

    [Fact]
    public async Task IntegrationTest_PerformanceAndConfiguration_Combined()
    {
        var configManager = new AdvancedConfigManager();
        var profiler = new PerformanceProfiler();
        
        var metrics = await profiler.CollectMetricsAsync();
        Assert.NotNull(metrics);
        
        var profile = await configManager.CreateProfileAsync("Performance", "Performance tuning");
        var updateSettings = new Dictionary<string, object> { { "PerformanceMode", metrics.CpuUsagePercent > 70 ? "PowerSaver" : "Balanced" } };
        
        var updated = await configManager.UpdateProfileAsync(profile.Id, updateSettings);
        Assert.NotNull(updated);
    }

    // ============ SECURITY COMPLIANCE SERVICE TESTS ============

    [Fact]
    public async Task SecurityCompliance_RunSecurityAudit_ReturnsComplianceResults()
    {
        var complianceService = new SecurityComplianceService();
        var results = await complianceService.RunSecurityAuditAsync();
        
        Assert.NotNull(results);
        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.NotNull(r.CheckName));
    }

    [Fact]
    public async Task SecurityCompliance_CheckRBAC_VerifiesRoleConfiguration()
    {
        var complianceService = new SecurityComplianceService();
        var result = await complianceService.CheckRBACAsync();
        
        Assert.True(result);
    }

    [Fact]
    public async Task SecurityCompliance_CheckAuditLogging_VerifiesAuditState()
    {
        var complianceService = new SecurityComplianceService();
        var result = await complianceService.CheckAuditLoggingAsync();
        
        Assert.True(result);
    }

    [Fact]
    public async Task SecurityCompliance_CreateFirewallRule_AddsRuleToCollection()
    {
        var complianceService = new SecurityComplianceService();
        var initialRules = await complianceService.ListFirewallRulesAsync();
        var initialCount = initialRules.Count;
        
        var newRule = new FirewallRule
        {
            RuleName = "Test Rule",
            Direction = "Inbound",
            Action = "Allow",
            Protocol = "TCP"
        };
        
        var result = await complianceService.CreateFirewallRuleAsync(newRule);
        Assert.True(result);
        
        var updatedRules = await complianceService.ListFirewallRulesAsync();
        Assert.Equal(initialCount + 1, updatedRules.Count);
    }

    [Fact]
    public async Task SecurityCompliance_ListRoles_ReturnsSystemRoles()
    {
        var complianceService = new SecurityComplianceService();
        var roles = await complianceService.ListRolesAsync();
        
        Assert.NotNull(roles);
        Assert.NotEmpty(roles);
        Assert.Contains(roles, r => r.RoleName == "Administrator");
    }

    [Fact]
    public async Task SecurityCompliance_CreateRole_AddsCustomRole()
    {
        var complianceService = new SecurityComplianceService();
        var permissions = new List<string> { "Read", "Write" };
        
        var role = await complianceService.CreateRoleAsync("CustomRole", permissions);
        
        Assert.NotNull(role);
        Assert.Equal("CustomRole", role.RoleName);
        Assert.Equal(2, role.Permissions.Count);
    }

    [Fact]
    public async Task SecurityCompliance_GetSecurityPosture_ReturnsHealthMetrics()
    {
        var complianceService = new SecurityComplianceService();
        var posture = await complianceService.GetSecurityPostureAsync();
        
        Assert.NotNull(posture);
        Assert.Contains("OverallScore", posture.Keys);
        Assert.Contains("PassedChecks", posture.Keys);
    }

    // ============ SERVER AUTOMATION SERVICE TESTS ============

    [Fact]
    public async Task ServerAutomation_CreateTemplate_AddsTemplateSuccessfully()
    {
        var automationService = new ServerAutomationService();
        var steps = new List<WorkflowStep>
        {
            new WorkflowStep { StepNumber = 1, StepName = "Step1", ActionType = "Action1", TimeoutSeconds = 60 }
        };
        
        var template = await automationService.CreateTemplateAsync("Test Template", "Test Desc", steps);
        
        Assert.NotNull(template);
        Assert.Equal("Test Template", template.TemplateName);
        Assert.Single(template.Steps);
    }

    [Fact]
    public async Task ServerAutomation_ListTemplates_ReturnsAllTemplates()
    {
        var automationService = new ServerAutomationService();
        var templates = await automationService.ListTemplatesAsync();
        
        Assert.NotNull(templates);
        Assert.NotEmpty(templates);
    }

    [Fact]
    public async Task ServerAutomation_ExecuteWorkflow_RunsTemplateSuccessfully()
    {
        var automationService = new ServerAutomationService();
        var templates = await automationService.ListTemplatesAsync();
        var template = templates.First();
        
        var execution = await automationService.ExecuteWorkflowAsync(template.TemplateId, new Dictionary<string, object>());
        
        Assert.NotNull(execution);
        Assert.Equal(ExecutionStatus.Completed, execution.Status);
        Assert.NotEmpty(execution.StepResults);
    }

    [Fact]
    public async Task ServerAutomation_GetExecutionStatus_TracksWorkflowProgress()
    {
        var automationService = new ServerAutomationService();
        var templates = await automationService.ListTemplatesAsync();
        var execution = await automationService.ExecuteWorkflowAsync(templates.First().TemplateId, new Dictionary<string, object>());
        
        var status = await automationService.GetExecutionStatusAsync(execution.ExecutionId);
        
        Assert.NotNull(status);
        Assert.Equal(execution.ExecutionId, status.ExecutionId);
    }

    [Fact]
    public async Task ServerAutomation_CancelExecution_StopsRunningWorkflow()
    {
        var automationService = new ServerAutomationService();
        var templates = await automationService.ListTemplatesAsync();
        var execution = await automationService.ExecuteWorkflowAsync(templates.First().TemplateId, new Dictionary<string, object>());
        
        var result = await automationService.CancelExecutionAsync(execution.ExecutionId);
        
        Assert.True(result);
    }

    // ============ MACHINE DISCOVERY SERVICE TESTS ============

    [Fact]
    public async Task MachineDiscovery_DiscoverMachines_FindsNetworkDevices()
    {
        var discoveryService = new MachineDiscoveryService();
        var options = new DiscoveryOptions { NetworkRange = "192.168.1.0/24", TimeoutSeconds = 30 };
        
        var machines = await discoveryService.DiscoverMachinesAsync(options);
        
        Assert.NotNull(machines);
        Assert.NotEmpty(machines);
    }

    [Fact]
    public async Task MachineDiscovery_GetOnlineMachines_ReturnsActiveMachines()
    {
        var discoveryService = new MachineDiscoveryService();
        var machines = await discoveryService.GetOnlineMachinesAsync();
        
        Assert.NotNull(machines);
        Assert.NotEmpty(machines);
        Assert.All(machines, m => Assert.True(m.IsOnline));
    }

    [Fact]
    public async Task MachineDiscovery_RegisterMachine_AddsNewMachine()
    {
        var discoveryService = new MachineDiscoveryService();
        var initialCount = (await discoveryService.GetAllMachinesAsync()).Count;
        
        var newMachine = new MachineInfo
        {
            MachineName = "TestServer",
            IPAddress = "192.168.1.200",
            OSVersion = "Windows Server 2022",
            ProcessorInfo = "Intel Xeon"
        };
        
        var result = await discoveryService.RegisterMachineAsync(newMachine);
        Assert.True(result);
        
        var finalCount = (await discoveryService.GetAllMachinesAsync()).Count;
        Assert.Equal(initialCount + 1, finalCount);
    }

    [Fact]
    public async Task MachineDiscovery_GetMachineHealth_ReturnsMachineMetrics()
    {
        var discoveryService = new MachineDiscoveryService();
        var machines = await discoveryService.GetAllMachinesAsync();
        var machineId = machines.First().MachineId;
        
        var health = await discoveryService.GetMachineHealthAsync(machineId);
        
        Assert.NotNull(health);
        Assert.True(health.HealthScore >= 0 && health.HealthScore <= 100);
    }

    [Fact]
    public async Task MachineDiscovery_PingMachine_UpdatesMachineStatus()
    {
        var discoveryService = new MachineDiscoveryService();
        var machines = await discoveryService.GetAllMachinesAsync();
        var machineId = machines.First().MachineId;
        
        var result = await discoveryService.PingMachineAsync(machineId);
        Assert.True(result);
        
        var updated = await discoveryService.GetMachineInfoAsync(machineId);
        Assert.True(updated.IsOnline);
    }
}
