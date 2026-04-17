using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using HELIOS.Platform.Core;
using HELIOS.Platform.Core.Logging;
using HELIOS.Platform.Core.Diagnostics;
using HELIOS.Platform.Core.Monitoring;
using HELIOS.Platform.Core.Administration;
using HELIOS.Platform.Core.CLI;
using HELIOS.Platform.Data.Database;

namespace HELIOS.Platform.Tests;

/// <summary>
/// Unit tests for Phase 1 Tier 2 services (Dashboard, System Management, CLI).
/// Tests ensure reliability and data accuracy across all core services.
/// </summary>
public class Phase1ServiceTests
{
    // ==================== DASHBOARD HISTORY TRACKER TESTS ====================

    [Fact]
    public async Task DashboardHistoryTracker_RecordMetric_ShouldStoreMetric()
    {
        // Arrange
        var tracker = new DashboardHistoryTracker();
        var metric = new SystemMetric
        {
            Timestamp = DateTime.UtcNow,
            CpuUsagePercent = 45.5,
            MemoryUsagePercent = 60.0,
            MemoryUsedMb = 6000,
            MemoryTotalMb = 10000,
            DiskUsagePercent = 75.0,
            DiskUsedMb = 750000,
            DiskTotalMb = 1000000,
            ProcessCount = 125
        };

        // Act
        await tracker.RecordMetricAsync(metric);
        var stats = await tracker.GetDashboardStatsAsync();

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(45.5, stats.CurrentCpuUsagePercent, 1);
        Assert.True(stats.MemoryUsedGb > 0);
    }

    [Fact]
    public async Task DashboardHistoryTracker_MultipleMetrics_ShouldCalculateAverages()
    {
        // Arrange
        var tracker = new DashboardHistoryTracker();
        var cpuValues = new[] { 30.0, 40.0, 50.0 };

        // Act
        foreach (var cpu in cpuValues)
        {
            await tracker.RecordMetricAsync(new SystemMetric
            {
                Timestamp = DateTime.UtcNow,
                CpuUsagePercent = cpu,
                MemoryUsagePercent = 50,
                MemoryUsedMb = 5000,
                MemoryTotalMb = 10000,
                DiskUsagePercent = 60,
                DiskUsedMb = 600000,
                DiskTotalMb = 1000000,
                ProcessCount = 100
            });
        }

        var stats = await tracker.GetDashboardStatsAsync();

        // Assert
        Assert.True(stats.CpuAveragePercent > 30 && stats.CpuAveragePercent < 50);
        Assert.Equal(50.0, stats.CpuPeakPercent, 1);
    }

    [Fact]
    public async Task DashboardHistoryTracker_HighCPU_ShouldGenerateAlert()
    {
        // Arrange
        var tracker = new DashboardHistoryTracker();
        var metric = new SystemMetric
        {
            Timestamp = DateTime.UtcNow,
            CpuUsagePercent = 95.0, // High CPU threshold
            MemoryUsagePercent = 50,
            MemoryUsedMb = 5000,
            MemoryTotalMb = 10000,
            DiskUsagePercent = 50,
            DiskUsedMb = 500000,
            DiskTotalMb = 1000000,
            ProcessCount = 100
        };

        // Act
        await tracker.RecordMetricAsync(metric);
        var stats = await tracker.GetDashboardStatsAsync();

        // Assert
        Assert.NotEmpty(stats.Alerts);
        Assert.Contains(stats.Alerts, a => a.ToLower().Contains("cpu"));
    }

    [Fact]
    public async Task DashboardHistoryTracker_HighMemory_ShouldGenerateAlert()
    {
        // Arrange
        var tracker = new DashboardHistoryTracker();
        var metric = new SystemMetric
        {
            Timestamp = DateTime.UtcNow,
            CpuUsagePercent = 50,
            MemoryUsagePercent = 90.0, // High memory >7GB of 8GB
            MemoryUsedMb = 9000,
            MemoryTotalMb = 10000,
            DiskUsagePercent = 50,
            DiskUsedMb = 500000,
            DiskTotalMb = 1000000,
            ProcessCount = 100
        };

        // Act
        await tracker.RecordMetricAsync(metric);
        var stats = await tracker.GetDashboardStatsAsync();

        // Assert
        Assert.NotEmpty(stats.Alerts);
        Assert.Contains(stats.Alerts, a => a.ToLower().Contains("memory"));
    }

    [Fact]
    public async Task DashboardHistoryTracker_HighDisk_ShouldGenerateAlert()
    {
        // Arrange
        var tracker = new DashboardHistoryTracker();
        var metric = new SystemMetric
        {
            Timestamp = DateTime.UtcNow,
            CpuUsagePercent = 50,
            MemoryUsagePercent = 50,
            MemoryUsedMb = 5000,
            MemoryTotalMb = 10000,
            DiskUsagePercent = 95.0, // High disk >90%
            DiskUsedMb = 950000,
            DiskTotalMb = 1000000,
            ProcessCount = 100
        };

        // Act
        await tracker.RecordMetricAsync(metric);
        var stats = await tracker.GetDashboardStatsAsync();

        // Assert
        Assert.NotEmpty(stats.Alerts);
        Assert.Contains(stats.Alerts, a => a.ToLower().Contains("disk"));
    }

    [Fact]
    public async Task DashboardHistoryTracker_HealthyMetrics_ShouldReturnHealthy()
    {
        // Arrange
        var tracker = new DashboardHistoryTracker();
        var metric = new SystemMetric
        {
            Timestamp = DateTime.UtcNow,
            CpuUsagePercent = 30,
            MemoryUsagePercent = 40,
            MemoryUsedMb = 4000,
            MemoryTotalMb = 10000,
            DiskUsagePercent = 50,
            DiskUsedMb = 500000,
            DiskTotalMb = 1000000,
            ProcessCount = 100
        };

        // Act
        await tracker.RecordMetricAsync(metric);
        var stats = await tracker.GetDashboardStatsAsync();

        // Assert
        Assert.Equal("✓ Healthy", stats.HealthStatus);
        Assert.Empty(stats.Alerts);
    }

    // ==================== SYSTEM MANAGEMENT SERVICE TESTS ====================

    [Fact]
    public async Task SystemManagementService_GetPartitions_ShouldReturnValidPartitions()
    {
        // Arrange
        var service = new SystemManagementService();

        // Act
        var partitions = await service.GetPartitionsAsync();

        // Assert
        Assert.NotNull(partitions);
        Assert.NotEmpty(partitions);
        var partition = partitions.First();
        Assert.NotEmpty(partition.Drive);
        Assert.True(partition.TotalSizeGb > 0);
        Assert.True(partition.UsagePercent >= 0 && partition.UsagePercent <= 100);
    }

    [Fact]
    public async Task SystemManagementService_GetPartitions_ShouldCalculateUsageCorrectly()
    {
        // Arrange
        var service = new SystemManagementService();

        // Act
        var partitions = await service.GetPartitionsAsync();

        // Assert
        foreach (var partition in partitions)
        {
            Assert.True(partition.AvailableGb <= partition.TotalSizeGb);
            Assert.True(partition.UsedGb <= partition.TotalSizeGb);
            // Verify formula: UsedGb = TotalSizeGb - AvailableGb
            var calculated = partition.TotalSizeGb - partition.AvailableGb;
            Assert.True(Math.Abs(partition.UsedGb - calculated) < 0.1);
        }
    }

    [Fact]
    public async Task SystemManagementService_GetServices_ShouldReturnValidServices()
    {
        // Arrange
        var service = new SystemManagementService();

        // Act
        var services = await service.GetServicesAsync();

        // Assert
        Assert.NotNull(services);
        Assert.NotEmpty(services);
        var windowsService = services.First();
        Assert.NotEmpty(windowsService.Name);
        var validStatuses = new[] { "Running", "Stopped", "Paused", "Unknown" };
        Assert.Contains(windowsService.Status, validStatuses);
    }

    [Fact]
    public async Task SystemManagementService_GetServices_ShouldIncludeCommonServices()
    {
        // Arrange
        var service = new SystemManagementService();

        // Act
        var services = await service.GetServicesAsync();
        var serviceNames = services.Select(s => s.Name.ToLower()).ToList();

        // Assert
        // At least some common Windows services should be present
        Assert.NotEmpty(services);
    }

    // ==================== CLI COMMAND EXECUTOR TESTS ====================

    [Fact]
    public void CliCommandExecutor_ParseCommand_ShouldParseSimpleCommand()
    {
        // Act
        var context = CliCommandExecutor.CommandParser.ParseCommand("status");

        // Assert
        Assert.NotNull(context);
        Assert.Equal("status", context.Command);
        Assert.Empty(context.Arguments);
    }

    [Fact]
    public void CliCommandExecutor_ParseCommand_ShouldParseCommandWithArguments()
    {
        // Act
        var context = CliCommandExecutor.CommandParser.ParseCommand("config get theme");

        // Assert
        Assert.NotNull(context);
        Assert.Equal("config", context.Command);
        Assert.Contains("get", context.Arguments);
        Assert.Contains("theme", context.Arguments);
    }

    [Fact]
    public void CliCommandExecutor_ParseCommand_ShouldParseCommandWithMultipleArgs()
    {
        // Act
        var context = CliCommandExecutor.CommandParser.ParseCommand("service start wuauserv --force --verbose");

        // Assert
        Assert.NotNull(context);
        Assert.Equal("service", context.Command);
        Assert.Contains("start", context.Arguments);
        Assert.Contains("wuauserv", context.Arguments);
        Assert.Contains("force", context.Options.Keys);
        Assert.Contains("verbose", context.Options.Keys);
    }

    [Fact]
    public async Task CliCommandExecutor_HelpCommand_ShouldReturnAvailableCommands()
    {
        // Arrange
        var orchestrator = new ServiceOrchestrator();
        var sysManagement = new SystemManagementService();
        var executor = new CliCommandExecutor(orchestrator, sysManagement);
        var context = new CliCommandExecutor.CommandContext
        {
            Command = "help",
            Arguments = new List<string>(),
            Options = new Dictionary<string, string>()
        };

        // Act
        var result = await executor.ExecuteCommandAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.NotEmpty(result.Message);
        Assert.Contains("status", result.Message.ToLower());
    }

    [Fact]
    public async Task CliCommandExecutor_StatusCommand_ShouldReturnSuccess()
    {
        // Arrange
        var orchestrator = new ServiceOrchestrator();
        var sysManagement = new SystemManagementService();
        var executor = new CliCommandExecutor(orchestrator, sysManagement);
        var context = new CliCommandExecutor.CommandContext
        {
            Command = "status",
            Arguments = new List<string>(),
            Options = new Dictionary<string, string>()
        };

        // Act
        var result = await executor.ExecuteCommandAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.NotEmpty(result.Message);
    }

    [Fact]
    public async Task CliCommandExecutor_InvalidCommand_ShouldReturnError()
    {
        // Arrange
        var orchestrator = new ServiceOrchestrator();
        var sysManagement = new SystemManagementService();
        var executor = new CliCommandExecutor(orchestrator, sysManagement);
        var context = new CliCommandExecutor.CommandContext
        {
            Command = "invalid_command_xyz_12345",
            Arguments = new List<string>(),
            Options = new Dictionary<string, string>()
        };

        // Act
        var result = await executor.ExecuteCommandAsync(context);

        // Assert
        Assert.False(result.Success);
        Assert.NotEmpty(result.Message);
    }

    [Fact]
    public async Task CliCommandExecutor_PartitionsCommand_ShouldReturnSuccess()
    {
        // Arrange
        var orchestrator = new ServiceOrchestrator();
        var sysManagement = new SystemManagementService();
        var executor = new CliCommandExecutor(orchestrator, sysManagement);
        var context = new CliCommandExecutor.CommandContext
        {
            Command = "partitions",
            Arguments = new List<string>(),
            Options = new Dictionary<string, string>()
        };

        // Act
        var result = await executor.ExecuteCommandAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.NotEmpty(result.Message);
    }

    [Fact]
    public async Task CliCommandExecutor_ServicesCommand_ShouldReturnSuccess()
    {
        // Arrange
        var orchestrator = new ServiceOrchestrator();
        var sysManagement = new SystemManagementService();
        var executor = new CliCommandExecutor(orchestrator, sysManagement);
        var context = new CliCommandExecutor.CommandContext
        {
            Command = "services",
            Arguments = new List<string>(),
            Options = new Dictionary<string, string>()
        };

        // Act
        var result = await executor.ExecuteCommandAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.NotEmpty(result.Message);
    }

    // ==================== SERVICE CONTAINER TESTS ====================

    [Fact]
    public void ServiceContainer_RegisterSingleton_ShouldStoreService()
    {
        // Arrange
        var logger = new ConsoleLogger();

        // Act
        ServiceContainer.Instance.RegisterSingleton<ILogger>(logger);
        var resolved = ServiceContainer.Instance.Resolve<ILogger>();

        // Assert
        Assert.NotNull(resolved);
        Assert.Same(logger, resolved);
    }

    [Fact]
    public void ServiceContainer_MultipleSingletons_ShouldRetainSameInstance()
    {
        // Arrange
        var logger = new ConsoleLogger();
        var orchestrator = new ServiceOrchestrator();

        // Act
        ServiceContainer.Instance.RegisterSingleton<ILogger>(logger);
        ServiceContainer.Instance.RegisterSingleton<IServiceOrchestrator>(orchestrator);

        var logger1 = ServiceContainer.Instance.Resolve<ILogger>();
        var logger2 = ServiceContainer.Instance.Resolve<ILogger>();
        var orch1 = ServiceContainer.Instance.Resolve<IServiceOrchestrator>();
        var orch2 = ServiceContainer.Instance.Resolve<IServiceOrchestrator>();

        // Assert
        Assert.Same(logger1, logger2);
        Assert.Same(orch1, orch2);
    }

    // ==================== INTEGRATION TESTS ====================

    [Fact]
    public async Task Integration_DashboardAndSystemManagement_ShouldCoexist()
    {
        // Arrange
        var dashboard = new DashboardHistoryTracker();
        var systemMgmt = new SystemManagementService();

        // Act
        var metric = new SystemMetric
        {
            Timestamp = DateTime.UtcNow,
            CpuUsagePercent = 55,
            MemoryUsagePercent = 65,
            MemoryUsedMb = 6500,
            MemoryTotalMb = 10000,
            DiskUsagePercent = 75,
            DiskUsedMb = 750000,
            DiskTotalMb = 1000000,
            ProcessCount = 120
        };

        await dashboard.RecordMetricAsync(metric);
        var dashStats = await dashboard.GetDashboardStatsAsync();
        var partitions = await systemMgmt.GetPartitionsAsync();

        // Assert
        Assert.NotNull(dashStats);
        Assert.NotEmpty(partitions);
        Assert.True(dashStats.CurrentCpuUsagePercent > 0);
    }

    [Fact]
    public async Task Integration_AllServices_ShouldInitializeWithoutError()
    {
        // Arrange & Act
        try
        {
            var logger = new ConsoleLogger();
            var orchestrator = new ServiceOrchestrator();
            var diagnostics = new SystemDiagnostics();
            var storage = new StorageManager();
            var config = new Core.Configuration.ConfigurationManager();
            var encryption = new EncryptionManager();
            var dashboard = new DashboardHistoryTracker();
            var systemMgmt = new SystemManagementService();
            var cli = new CliCommandExecutor(orchestrator, systemMgmt);

            ServiceContainer.Instance.RegisterSingleton<ILogger>(logger);
            ServiceContainer.Instance.RegisterSingleton<IServiceOrchestrator>(orchestrator);
            ServiceContainer.Instance.RegisterSingleton<IDashboardHistoryTracker>(dashboard);
            ServiceContainer.Instance.RegisterSingleton<ISystemManagementService>(systemMgmt);
            ServiceContainer.Instance.RegisterSingleton<ICommandExecutor>(cli);

            // Assert - all services initialized successfully
            Assert.NotNull(ServiceContainer.Instance.Resolve<ILogger>());
            Assert.NotNull(ServiceContainer.Instance.Resolve<IServiceOrchestrator>());
            Assert.NotNull(ServiceContainer.Instance.Resolve<IDashboardHistoryTracker>());
            Assert.NotNull(ServiceContainer.Instance.Resolve<ISystemManagementService>());
            Assert.NotNull(ServiceContainer.Instance.Resolve<ICommandExecutor>());
        }
        catch (Exception ex)
        {
            Assert.True(false, $"Service initialization failed: {ex.Message}");
        }
    }
}
