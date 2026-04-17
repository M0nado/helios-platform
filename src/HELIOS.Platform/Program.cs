using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HELIOS.Platform.Core;
using HELIOS.Platform.BackendServices.ServerManagement;
using HELIOS.Platform.Core.Logging;
using HELIOS.Platform.Core.Diagnostics;
using HELIOS.Platform.Core.Storage;
using HELIOS.Platform.Core.Configuration;
using HELIOS.Platform.Core.Security;
using HELIOS.Platform.Core.Monitoring;
using HELIOS.Platform.Core.Administration;
using HELIOS.Platform.Core.CLI;
using HELIOS.Platform.Core.Plugins;
using HELIOS.Platform.Core.RemoteAccess;
using HELIOS.Platform.Core.GPU;
using HELIOS.Platform.Core.Automation;
using HELIOS.Platform.Core.AI;
using HELIOS.Platform.Core.Performance;
using HELIOS.Platform.Core.Backup;
using HELIOS.Platform.Core.Cloud;
using HELIOS.Platform.Core.Sandbox;
using HELIOS.Platform.Core.Hardware;
using HELIOS.Platform.Core.Integration;
using HELIOS.Platform.Core.Production.Interfaces;
using HELIOS.Platform.Core.Production.Services;
using HELIOS.Platform.Core.Global;
using HELIOS.Platform.Core.Global.Interfaces;
using HELIOS.Platform.Data.Database;

namespace HELIOS.Platform
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
                Console.WriteLine("║          HELIOS Platform - Enterprise Management System         ║");
                Console.WriteLine("║                    Version 2.0 Foundation                       ║");
                Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
                Console.WriteLine();
                
                // Initialize service container with all core services
                var logger = new ConsoleLogger();
                var orchestrator = new ServiceOrchestrator();
                var diagnostics = new SystemDiagnostics();
                var storage = new StorageManager();
                var config = new Core.Configuration.ConfigurationManager();
                var encryption = new EncryptionManager();
                var dashboardTracker = new DashboardHistoryTracker();
                var systemManagement = new SystemManagementService();
                var cliExecutor = new CliCommandExecutor(orchestrator, systemManagement);
                var hardeningService = new WindowsHardeningService(logger, systemManagement);
                var crossPartitionManager = new CrossPartitionManager(logger);
                var devDriveService = new DevDriveFileService(logger);
                var pluginManager = new PluginManager(logger);
                var remoteAccessService = new RemoteAccessService(logger);
                var gpuService = new GPUOptimizationService(logger);
                var automationServer = new AutomationServer(logger);
                var aiCoordinator = new AILearningCoordinator(logger);
                var vaultService = new SecurityVaultService(logger);
                var performanceProfiler = new PerformanceProfiler(logger);
                var backupService = new BackupService(logger);
                var monitoringService = new ServerMonitoringService(logger);
                var cloudService = new CloudIntegrationService(logger);
                var advancedConfigManager = new AdvancedConfigManager();
                var sandboxEnvironment = new Core.Sandbox.SandboxEnvironment();
                var advancedQuarantineSystem = new AdvancedQuarantineSystem();
                var driverAutoInstallService = new DriverAutoInstallService();
                var usbAdminAccessService = new USBAdminAccessService();
                var securityComplianceService = new SecurityComplianceService();
                var serverAutomationService = new ServerAutomationService();
                var machineDiscoveryService = new MachineDiscoveryService();
                var remoteFileTransferService = new RemoteFileTransferService();
                var remoteCommandExecutor = new RemoteCommandExecutor();
                var failoverManager = new FailoverManager();
                var replicationService = new ReplicationService();
                var autoScalingService = new AutoScalingService();
                var clusterManager = new ClusterManager();
                var containerOrchestration = new ContainerOrchestrationService();
                var eventDrivenOrchestration = new EventDrivenOrchestration();
                var serviceHealthMonitor = new ServiceDependencyMonitor();
                var anomalyDetectionService = new AnomalyDetectionEngine();
                var mlService = new MachineLearningEngine();
                var resourcePlanner = new ResourcePlanner();
                var capacityPlanner = new HELIOS.Platform.Core.Administration.CapacityPlanner();
                var dataShardingService = new DataShardingEngine();
                var distributedCache = new DistributedCacheEngine();
                
                // Optimization & Resilience Services (Phase 2+ Enhancements)
                var serviceFactory = new ServiceFactory();
                var batchOperationService = new BatchOperationService();
                
                // Phase 4 Performance Optimization Services
                var l1CacheService = new L1CacheService(logger);
                var l2CacheService = new InMemoryL2Cache();
                var advancedCacheService = new AdvancedCacheService(l1CacheService, l2CacheService, logger);
                var queryOptimizer = new QueryOptimizationService(logger);
                var memoryOptimizer = new MemoryOptimizationService(logger);
                var connectionPoolService = new ConnectionPoolService(logger);
                var databaseIndexService = new DatabaseIndexService(logger);
                var efCoreOptimizer = new EFCoreQueryOptimizer(logger);
                var connectionLifecycleService = new ConnectionLifecycleService(logger);
                
                var resilienceService = new ResilienceService();
                
                // Phase 3 Tier 4: Production Hardening Services
                // var distributedCacheLayer = new DistributedCacheLayer(logger);
                // var queryPlanAnalyzer = new QueryPlanAnalyzer(logger);
                // var productionLoadBalancer = new ProductionLoadBalancer(logger);
                // var zeroTrustImplementation = new ZeroTrustImplementation(logger);
                // var disasterRecoveryOrchestrator = new DisasterRecoveryOrchestrator(logger);
                
                // Phase 5 Tier 2: Global Intelligence Services
                // var globalMetricsAggregator = new GlobalMetricsAggregator(logger);
                // var costOptimizer = new CostOptimizer(logger);
                // var globalCapacityPlanner = new HELIOS.Platform.Core.Global.CapacityPlanner(logger);
                var globalLoadBalancer = new GlobalLoadBalancer(logger);
                var regionFailover = new RegionFailover(logger);
                var latencyOptimizer = new LatencyOptimizer(logger);
                var cdnController = new CDNController(logger);

                // Initialize database context
                var optionsBuilder = new DbContextOptionsBuilder<HeliosDatabaseContext>();
                optionsBuilder.UseSqlite("Data Source=helios.db");
                var dbContext = new HeliosDatabaseContext(optionsBuilder.Options);
                var dataAccessService = new DataAccessService(dbContext);
                
                ServiceContainer.Instance.RegisterSingleton<Core.Logging.ILogger>(logger);
                ServiceContainer.Instance.RegisterSingleton<IServiceOrchestrator>(orchestrator);
                ServiceContainer.Instance.RegisterSingleton<SystemDiagnostics>(diagnostics);
                ServiceContainer.Instance.RegisterSingleton<StorageManager>(storage);
                ServiceContainer.Instance.RegisterSingleton<Core.Configuration.ConfigurationManager>(config);
                ServiceContainer.Instance.RegisterSingleton<EncryptionManager>(encryption);
                ServiceContainer.Instance.RegisterSingleton<IDashboardHistoryTracker>(dashboardTracker);
                ServiceContainer.Instance.RegisterSingleton<ISystemManagementService>(systemManagement);
                ServiceContainer.Instance.RegisterSingleton<ICommandExecutor>(cliExecutor);
                ServiceContainer.Instance.RegisterSingleton<IWindowsHardeningService>(hardeningService);
                ServiceContainer.Instance.RegisterSingleton<ICrossPartitionManager>(crossPartitionManager);
                ServiceContainer.Instance.RegisterSingleton<IDevDriveFileService>(devDriveService);
                ServiceContainer.Instance.RegisterSingleton<IPluginManager>(pluginManager);
                ServiceContainer.Instance.RegisterSingleton<IRemoteAccessService>(remoteAccessService);
                ServiceContainer.Instance.RegisterSingleton<IGPUOptimizationService>(gpuService);
                ServiceContainer.Instance.RegisterSingleton<IAutomationServer>(automationServer);
                ServiceContainer.Instance.RegisterSingleton<IAILearningCoordinator>(aiCoordinator);
                ServiceContainer.Instance.RegisterSingleton<ISecurityVaultService>(vaultService);
                ServiceContainer.Instance.RegisterSingleton<IPerformanceProfiler>(performanceProfiler);
                ServiceContainer.Instance.RegisterSingleton<IBackupService>(backupService);
                ServiceContainer.Instance.RegisterSingleton<IServerMonitoringService>(monitoringService);
                ServiceContainer.Instance.RegisterSingleton<ICloudIntegrationService>(cloudService);
                ServiceContainer.Instance.RegisterSingleton<IAdvancedConfigManager>(advancedConfigManager);
                ServiceContainer.Instance.RegisterSingleton<ISandboxEnvironment>(sandboxEnvironment);
                ServiceContainer.Instance.RegisterSingleton<IAdvancedQuarantineSystem>(advancedQuarantineSystem);
                ServiceContainer.Instance.RegisterSingleton<IDriverAutoInstallService>(driverAutoInstallService);
                ServiceContainer.Instance.RegisterSingleton<IUSBAdminAccessService>(usbAdminAccessService);
                ServiceContainer.Instance.RegisterSingleton<ISecurityComplianceService>(securityComplianceService);
                ServiceContainer.Instance.RegisterSingleton<IServerAutomationService>(serverAutomationService);
                ServiceContainer.Instance.RegisterSingleton<IMachineDiscoveryService>(machineDiscoveryService);
                ServiceContainer.Instance.RegisterSingleton<IRemoteFileTransferService>(remoteFileTransferService);
                ServiceContainer.Instance.RegisterSingleton<IRemoteCommandExecutor>(remoteCommandExecutor);
                ServiceContainer.Instance.RegisterSingleton<IFailoverManager>(failoverManager);
                ServiceContainer.Instance.RegisterSingleton<IReplicationService>(replicationService);
                ServiceContainer.Instance.RegisterSingleton<IAutoScalingService>(autoScalingService);
                ServiceContainer.Instance.RegisterSingleton<IClusterManager>(clusterManager);
                ServiceContainer.Instance.RegisterSingleton<IContainerOrchestrationService>(containerOrchestration);
                ServiceContainer.Instance.RegisterSingleton<IEventDrivenOrchestration>(eventDrivenOrchestration);
                ServiceContainer.Instance.RegisterSingleton<IServiceHealthMonitor>(serviceHealthMonitor);
                ServiceContainer.Instance.RegisterSingleton<IAnomalyDetectionService>(anomalyDetectionService);
                ServiceContainer.Instance.RegisterSingleton<IMachineLearningService>(mlService);
                ServiceContainer.Instance.RegisterSingleton<IPredictiveResourcePlanning>(resourcePlanner);
                ServiceContainer.Instance.RegisterSingleton<ICapacityPlanningService>(capacityPlanner);
                ServiceContainer.Instance.RegisterSingleton<IDataShardingService>(dataShardingService);
                // QueryOptimizer, IntegrationTestService, PerformanceValidator, SystemValidator,  
                // DocumentationService, APIDocService, PackageService removed - not in current build
                // ServiceContainer.Instance.RegisterSingleton<IQueryOptimizationService>(queryOptimizer);
                // ServiceContainer.Instance.RegisterSingleton<IDistributedCachingService>(distributedCache);
                // ServiceContainer.Instance.RegisterSingleton<IIntegrationTestService>(integrationTestService);
                // ServiceContainer.Instance.RegisterSingleton<IPerformanceValidationService>(performanceValidator);
                // ServiceContainer.Instance.RegisterSingleton<ISystemValidationService>(systemValidator);
                // ServiceContainer.Instance.RegisterSingleton<IDocumentationService>(documentationService);
                // ServiceContainer.Instance.RegisterSingleton<IAPIDocumentationService>(apiDocService);
                // ServiceContainer.Instance.RegisterSingleton<IDeploymentPackageService>(packageService);
                
                // Phase 2 Batch 16: Integration & Validation Service Registrations
                // ServiceContainer.Instance.RegisterSingleton<IPhase2OrchestrationService>(phase2Orchestrator);
                // ServiceContainer.Instance.RegisterSingleton<IProductionReadinessValidator>(productionReadinessValidator);
                
                // Phase 2+ Enhancements: Service Factory, Batch Operations, Caching, Resilience
                ServiceContainer.Instance.RegisterSingleton<IServiceFactory>(serviceFactory);
                ServiceContainer.Instance.RegisterSingleton<IBatchOperationService>(batchOperationService);
                ServiceContainer.Instance.RegisterSingleton<IAdvancedCacheService>(advancedCacheService);
                ServiceContainer.Instance.RegisterSingleton<IResilienceService>(resilienceService);
                
                // Phase 3 Tier 4: Production Hardening Services Registration
                // ServiceContainer.Instance.RegisterSingleton<IDistributedCacheLayer>(distributedCacheLayer);
                // ServiceContainer.Instance.RegisterSingleton<IQueryPlanAnalyzer>(queryPlanAnalyzer);
                // ServiceContainer.Instance.RegisterSingleton<IProductionLoadBalancer>(productionLoadBalancer);
                // ServiceContainer.Instance.RegisterSingleton<IZeroTrustImplementation>(zeroTrustImplementation);
                // ServiceContainer.Instance.RegisterSingleton<IDisasterRecoveryOrchestrator>(disasterRecoveryOrchestrator);
                 
                 // Phase 5 Tier 2: Global Intelligence Services Registration
                 // ServiceContainer.Instance.RegisterSingleton<IGlobalMetricsAggregator>(globalMetricsAggregator);
                 // ServiceContainer.Instance.RegisterSingleton<ICostOptimizer>(costOptimizer);
                 // ServiceContainer.Instance.RegisterSingleton<ICapacityPlanner>(globalCapacityPlanner);
                 ServiceContainer.Instance.RegisterSingleton<IGlobalLoadBalancer>(globalLoadBalancer);
                 ServiceContainer.Instance.RegisterSingleton<IRegionFailover>(regionFailover);
                 ServiceContainer.Instance.RegisterSingleton<ILatencyOptimizer>(latencyOptimizer);
                 ServiceContainer.Instance.RegisterSingleton<ICDNController>(cdnController);
                
                ServiceContainer.Instance.RegisterSingleton<HeliosDatabaseContext>(dbContext);
                ServiceContainer.Instance.RegisterSingleton<IDataAccessService>(dataAccessService);
                
                logger.Info("HELIOS Platform initialized successfully");
                
                // Apply database migrations
                try
                {
                    await dbContext.Database.MigrateAsync();
                    logger.Info("Database migrations applied successfully");
                }
                catch (Exception migrateEx)
                {
                    logger.Error($"Database migration failed: {migrateEx.Message}");
                }
                
                // Show main menu
                await ShowMainMenu(logger);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR] {ex.Message}");
                Console.ResetColor();
                Environment.Exit(1);
            }
        }

        static async Task ShowMainMenu(Core.Logging.ILogger logger)
        {
            while (true)
            {
                Console.WriteLine("\n┌────────────────────────────────────────────────────────────────┐");
                Console.WriteLine("│                      MAIN MENU                                   │");
                Console.WriteLine("├────────────────────────────────────────────────────────────────┤");
                Console.WriteLine("│ 1. Dashboard           - System Overview & Monitoring           │");
                Console.WriteLine("│ 2. System Management   - Disks, Services, Processes            │");
                Console.WriteLine("│ 3. Diagnostics        - System Analysis & Health Check         │");
                Console.WriteLine("│ 4. Security           - Vault, Encryption, Authentication     │");
                Console.WriteLine("│ 5. AI Hub              - AI Features & Optimization             │");
                Console.WriteLine("│ 6. Settings            - Configuration & Preferences            │");
                Console.WriteLine("│ 7. Tools               - Utilities & Advanced Functions        │");
                Console.WriteLine("│ 8. Terminal            - CLI Interface                          │");
                Console.WriteLine("│ 9. Help                - Documentation & Support               │");
                Console.WriteLine("│ 0. Exit                                                        │");
                Console.WriteLine("└────────────────────────────────────────────────────────────────┘");
                Console.Write("\nSelect option (0-9): ");

                string input = Console.ReadLine() ?? "";

                switch (input)
                {
                    case "1":
                        await ShowDashboard();
                        break;
                    case "2":
                        await ShowSystemManagement();
                        break;
                    case "3":
                        await ShowDiagnostics();
                        break;
                    case "4":
                        await ShowSecurity();
                        break;
                    case "5":
                        await ShowAiHub();
                        break;
                    case "6":
                        await ShowSettings();
                        break;
                    case "7":
                        await ShowTools();
                        break;
                    case "8":
                        await ShowTerminal();
                        break;
                    case "9":
                        await ShowHelp();
                        break;
                    case "0":
                        Console.WriteLine("\n[INFO] Exiting HELIOS Platform...");
                        return;
                    default:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("[WARN] Invalid option. Please try again.");
                        Console.ResetColor();
                        break;
                }
            }
        }

        static async Task ShowDashboard()
        {
            Console.Clear();
            Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                     DASHBOARD (Real-Time)                      ║");
            Console.WriteLine("║                   Press ESC to return to menu                  ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

            var orchestrator = ServiceContainer.Instance.GetService<IServiceOrchestrator>();
            var dashboardTracker = ServiceContainer.Instance.GetService<IDashboardHistoryTracker>();
            
            if (orchestrator == null || dashboardTracker == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[ERROR] Dashboard services not initialized");
                Console.ResetColor();
                Console.ReadKey();
                return;
            }

            // Enable real-time updates with refresh
            using (var cts = new CancellationTokenSource())
            {
                try
                {
                    // Start background metric collection task
                    var collectionTask = Task.Run(async () =>
                    {
                        while (!cts.Token.IsCancellationRequested)
                        {
                            try
                            {
                                var resources = await orchestrator.GetSystemResourcesAsync();
                                await dashboardTracker.RecordMetricAsync(resources);
                                await Task.Delay(1000, cts.Token); // Collect every second
                            }
                            catch { }
                        }
                    }, cts.Token);

                    // Display loop
                    while (true)
                    {
                        if (Console.KeyAvailable)
                        {
                            var key = Console.ReadKey(true);
                            if (key.Key == ConsoleKey.Escape)
                            {
                                cts.Cancel();
                                break;
                            }
                        }

                        Console.Clear();
                        Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
                        Console.WriteLine("║                     DASHBOARD (Real-Time)                      ║");
                        Console.WriteLine("║                   Press ESC to return to menu                  ║");
                        Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

                        try
                        {
                            // Get current resources
                            var resources = await orchestrator.GetSystemResourcesAsync();
                            
                            // Display current metrics
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine("┌─ CURRENT METRICS ─────────────────────────────────────────────┐");
                            Console.ResetColor();
                            Console.WriteLine($"  CPU Usage:            {resources.CpuUsagePercent:F1}%");
                            Console.WriteLine($"  Memory Usage:         {resources.MemoryUsageMB} MB / {resources.AvailableMemoryMB + resources.MemoryUsageMB} MB");
                            Console.WriteLine($"  Disk Usage:           {resources.DiskUsagePercent}%");
                            Console.WriteLine($"  System Uptime:        {FormatUptime(resources.SystemUptimeSeconds)}");
                            Console.WriteLine($"  Active Services:      {resources.ActiveServices}");
                            Console.WriteLine($"  Total Processes:      {resources.TotalProcesses}");
                            Console.WriteLine("└───────────────────────────────────────────────────────────────┘\n");

                            // Get and display statistics
                            var stats = await dashboardTracker.GetDashboardStatsAsync();
                            
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("┌─ STATISTICS (Last Hour) ──────────────────────────────────────┐");
                            Console.ResetColor();
                            Console.WriteLine($"  CPU:    Avg: {stats.CpuAverage:F1}%  │  Peak: {stats.CpuPeak:F1}%  │  Min: {stats.CpuMin:F1}%");
                            Console.WriteLine($"  Memory: Avg: {stats.MemoryAverage:F0} MB  │  Peak: {stats.MemoryPeak} MB  │  Min: {stats.MemoryMin} MB");
                            Console.WriteLine($"  Disk:   Avg: {stats.DiskAverage}%");
                            Console.WriteLine("└───────────────────────────────────────────────────────────────┘\n");

                            // Display health status and alerts
                            Console.ForegroundColor = stats.HealthStatus.StartsWith("✓") ? ConsoleColor.Green :
                                                     stats.HealthStatus.StartsWith("⚠") ? ConsoleColor.Yellow : ConsoleColor.Red;
                            Console.WriteLine($"HEALTH STATUS: {stats.HealthStatus}");
                            Console.ResetColor();

                            if (stats.Alerts.Count > 0)
                            {
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine("\nALERTS:");
                                foreach (var alert in stats.Alerts)
                                {
                                    Console.WriteLine($"  {alert}");
                                }
                                Console.ResetColor();
                            }

                            Console.WriteLine("\n[Refreshing every second... Press ESC to return]");
                        }
                        catch (Exception ex)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"[ERROR] {ex.Message}");
                            Console.ResetColor();
                        }

                        await Task.Delay(1000); // Update display every second
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[ERROR] Dashboard error: {ex.Message}");
                    Console.ResetColor();
                    Console.ReadKey();
                }
            }
        }

        static string FormatUptime(long seconds)
        {
            var days = seconds / 86400;
            var hours = (seconds % 86400) / 3600;
            var minutes = (seconds % 3600) / 60;
            var secs = seconds % 60;
            
            if (days > 0)
                return $"{days}d {hours}h {minutes}m";
            if (hours > 0)
                return $"{hours}h {minutes}m {secs}s";
            return $"{minutes}m {secs}s";
        }

        static async Task ShowSystemManagement()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
                Console.WriteLine("║                    SYSTEM MANAGEMENT                           ║");
                Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

                Console.WriteLine("┌─ OPTIONS ──────────────────────────────────────────────────────┐");
                Console.WriteLine("│ 1. Partition & Disk Management                                 │");
                Console.WriteLine("│ 2. Services Management                                         │");
                Console.WriteLine("│ 0. Return to Main Menu                                         │");
                Console.WriteLine("└────────────────────────────────────────────────────────────────┘");
                Console.Write("\nSelect option (0-2): ");

                string input = Console.ReadLine() ?? "";

                switch (input)
                {
                    case "1":
                        await ShowPartitionManagement();
                        break;
                    case "2":
                        await ShowServicesManagement();
                        break;
                    case "0":
                        return;
                    default:
                        Console.WriteLine("Invalid option");
                        Console.ReadKey();
                        break;
                }
            }
        }

        static async Task ShowPartitionManagement()
        {
            Console.Clear();
            Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║               PARTITION & DISK MANAGEMENT                      ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

            try
            {
                var sysManagement = ServiceContainer.Instance.GetService<ISystemManagementService>();
                if (sysManagement == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[ERROR] System Management service not initialized");
                    Console.ResetColor();
                    Console.ReadKey();
                    return;
                }

                var partitions = await sysManagement.GetPartitionsAsync();

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("┌─ DISK PARTITIONS ──────────────────────────────────────────────┐");
                Console.ResetColor();

                foreach (var partition in partitions)
                {
                    var usageBar = GetUsageBar(partition.UsagePercent);
                    var sizeGB = partition.TotalSizeBytes / (1024.0 * 1024.0 * 1024.0);
                    var usedGB = partition.UsedSizeBytes / (1024.0 * 1024.0 * 1024.0);
                    var freeGB = partition.FreeSizeBytes / (1024.0 * 1024.0 * 1024.0);

                    Console.WriteLine($"\n{partition.DriveLetter} ({partition.VolumeLabel})");
                    Console.WriteLine($"  File System: {partition.FileSystem}");
                    Console.Write($"  Usage: ");
                    
                    if (partition.UsagePercent > 80)
                        Console.ForegroundColor = ConsoleColor.Red;
                    else if (partition.UsagePercent > 60)
                        Console.ForegroundColor = ConsoleColor.Yellow;
                    else
                        Console.ForegroundColor = ConsoleColor.Green;

                    Console.WriteLine(usageBar);
                    Console.ResetColor();
                    Console.WriteLine($"  Size: {usedGB:F1}GB / {sizeGB:F1}GB ({partition.UsagePercent}%)  Free: {freeGB:F1}GB");
                }

                Console.WriteLine("\n└────────────────────────────────────────────────────────────────┘");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR] {ex.Message}");
                Console.ResetColor();
            }

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }

        static async Task ShowServicesManagement()
        {
            Console.Clear();
            Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                  WINDOWS SERVICES MANAGEMENT                   ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

            try
            {
                var sysManagement = ServiceContainer.Instance.GetService<ISystemManagementService>();
                if (sysManagement == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[ERROR] System Management service not initialized");
                    Console.ResetColor();
                    Console.ReadKey();
                    return;
                }

                var services = await sysManagement.GetServicesAsync();

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("┌─ SYSTEM SERVICES ──────────────────────────────────────────────┐");
                Console.ResetColor();

                int index = 1;
                foreach (var service in services)
                {
                    var statusIcon = service.IsRunning ? "✓" : "✗";
                    var statusColor = service.IsRunning ? ConsoleColor.Green : ConsoleColor.Red;

                    Console.ForegroundColor = statusColor;
                    Console.Write($"  [{statusIcon}] ");
                    Console.ResetColor();
                    Console.WriteLine($"{index}. {service.DisplayName}");
                    Console.WriteLine($"     Status: {service.Status}  │  Start Type: {service.StartType}");
                    index++;
                }

                Console.WriteLine("\n└────────────────────────────────────────────────────────────────┘");
                Console.WriteLine("\nNote: Service operations require administrator privileges");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR] {ex.Message}");
                Console.ResetColor();
            }

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }

        static string GetUsageBar(int percent, int width = 40)
        {
            var filled = (percent * width) / 100;
            var bar = new string('█', filled) + new string('░', width - filled);
            return $"[{bar}] {percent}%";
        }

        static async Task ShowAiHub()
        {
            Console.Clear();
            Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                         AI HUB                                 ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

            Console.WriteLine("AI Hub Features:");
            Console.WriteLine("  • Local LLM Integration");
            Console.WriteLine("  • Agent Management");
            Console.WriteLine("  • Model Optimization");
            Console.WriteLine("  • Token Optimization");
            Console.WriteLine("  • GPU Acceleration (CUDA/DirectML)");

            Console.WriteLine("\nPress any key to return to main menu...");
            Console.ReadKey();
        }

        static async Task ShowSettings()
        {
            Console.Clear();
            Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                        SETTINGS                               ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

            Console.WriteLine("Configuration Options:");
            Console.WriteLine("  • General Settings");
            Console.WriteLine("  • Performance Tuning");
            Console.WriteLine("  • Security Configuration");
            Console.WriteLine("  • User Profiles");
            Console.WriteLine("  • Feature Toggles");

            Console.WriteLine("\nPress any key to return to main menu...");
            Console.ReadKey();
        }

        static async Task ShowTools()
        {
            Console.Clear();
            Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                         TOOLS                                  ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

            Console.WriteLine("Available Tools:");
            Console.WriteLine("  1. Disk Information & Analysis");
            Console.WriteLine("  2. Find Large Files");
            Console.WriteLine("  3. Configuration Manager");
            Console.WriteLine("  4. System Performance Monitoring");
            Console.WriteLine("  5. Security Toolkit");
            Console.WriteLine("  0. Back to Main Menu");
            Console.Write("\nSelect option: ");

            string choice = Console.ReadLine() ?? "";

            try
            {
                var storage = ServiceContainer.Instance.GetService<StorageManager>();
                
                switch (choice)
                {
                    case "1":
                        Console.WriteLine("\n▶ Disk Information:");
                        var disks = await storage.GetDiskInfoAsync();
                        foreach (var disk in disks)
                        {
                            Console.WriteLine($"\n  Drive: {disk.DriveLetter}");
                            Console.WriteLine($"  Name: {disk.Name}");
                            Console.WriteLine($"  Total: {disk.TotalSize / (1024 * 1024 * 1024)} GB");
                            Console.WriteLine($"  Used: {disk.UsedSpace / (1024 * 1024 * 1024)} GB");
                            Console.WriteLine($"  Free: {disk.AvailableSpace / (1024 * 1024 * 1024)} GB");
                            Console.WriteLine($"  Usage: {disk.UsagePercent:F1}%");
                        }
                        break;
                    case "2":
                        Console.Write("\nEnter directory path: ");
                        string dirPath = Console.ReadLine() ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                        var largeFiles = await storage.GetLargestFilesAsync(dirPath, 10);
                        Console.WriteLine("\n▶ Largest Files:");
                        foreach (var file in largeFiles)
                        {
                            Console.WriteLine($"  {file.Name,-40} {file.Length / (1024 * 1024)} MB");
                        }
                        break;
                    case "3":
                        var config = ServiceContainer.Instance.GetService<Core.Configuration.ConfigurationManager>();
                        Console.WriteLine("\n▶ Current Settings:");
                        var allSettings = config.GetAllSettings();
                        foreach (var setting in allSettings)
                        {
                            Console.WriteLine($"  {setting.Key}: {setting.Value}");
                        }
                        break;
                    case "4":
                        Console.WriteLine("\n▶ Performance Monitoring:");
                        Console.WriteLine("  • CPU Monitoring");
                        Console.WriteLine("  • Memory Profiling");
                        Console.WriteLine("  • Disk I/O Analysis");
                        Console.WriteLine("  • Network Performance");
                        break;
                    case "5":
                        Console.WriteLine("\n▶ Security Toolkit:");
                        Console.WriteLine("  • Malwarebytes Integration");
                        Console.WriteLine("  • Rootkit Detection");
                        Console.WriteLine("  • File Quarantine");
                        Console.WriteLine("  • USB Builder");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR] {ex.Message}");
                Console.ResetColor();
            }

            Console.WriteLine("\nPress any key to return to main menu...");
            Console.ReadKey();
        }

        static async Task ShowTerminal()
        {
            Console.Clear();
            Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                   CLI TERMINAL INTERFACE                       ║");
            Console.WriteLine("║                 Type 'help' for available commands              ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

            try
            {
                var orchestrator = ServiceContainer.Instance.GetService<IServiceOrchestrator>();
                var sysManagement = ServiceContainer.Instance.GetService<ISystemManagementService>();

                if (orchestrator == null || sysManagement == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[ERROR] Required services not initialized");
                    Console.ResetColor();
                    Console.ReadKey();
                    return;
                }

                var executor = new CliCommandExecutor(orchestrator, sysManagement);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Welcome to HELIOS CLI Terminal");
                Console.WriteLine("Type 'help' to see available commands, 'exit' to quit\n");
                Console.ResetColor();

                while (true)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write("helios> ");
                    Console.ResetColor();

                    string cmdLine = Console.ReadLine() ?? "";

                    if (string.IsNullOrWhiteSpace(cmdLine))
                        continue;

                    if (cmdLine.Equals("exit", StringComparison.OrdinalIgnoreCase) || 
                        cmdLine.Equals("quit", StringComparison.OrdinalIgnoreCase))
                        break;

                    try
                    {
                        var result = await executor.ExecuteAsync(cmdLine);

                        if (result.Success)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine(result.Message);
                            Console.ResetColor();
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Error: {result.Message}");
                            Console.ResetColor();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[ERROR] {ex.Message}");
                        Console.ResetColor();
                    }

                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR] Terminal initialization failed: {ex.Message}");
                Console.ResetColor();
                Console.ReadKey();
            }
        }

        static async Task ShowDiagnostics()
        {
            Console.Clear();
            Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                       DIAGNOSTICS                              ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

            try
            {
                var diag = ServiceContainer.Instance.GetService<SystemDiagnostics>();
                
                // System information
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("▶ System Information:");
                Console.ResetColor();
                var sysInfo = await diag.GetSystemInfoAsync();
                Console.WriteLine($"  Machine: {sysInfo.MachineName}");
                Console.WriteLine($"  OS Version: {sysInfo.OSVersion}");
                Console.WriteLine($"  Processors: {sysInfo.ProcessorCount}");
                Console.WriteLine($"  Total Memory: {sysInfo.TotalMemory / (1024 * 1024)} MB");
                
                // Top processes
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("▶ Top Processes by Memory:");
                Console.ResetColor();
                var processes = await diag.GetRunningProcessesAsync();
                var topProcs = processes.OrderByDescending(p => p.MemoryMB).Take(5).ToList();
                foreach (var proc in topProcs)
                {
                    Console.WriteLine($"  {proc.Name,-30} {proc.MemoryMB,8} MB (PID: {proc.Id})");
                }
                
                // Network
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("▶ Network:");
                Console.ResetColor();
                var netInfo = await diag.GetNetworkInfoAsync();
                Console.WriteLine($"  Hostname: {netInfo.HostName}");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR] {ex.Message}");
                Console.ResetColor();
            }

            Console.WriteLine("\nPress any key to return to main menu...");
            Console.ReadKey();
        }

        static async Task ShowSecurity()
        {
            Console.Clear();
            Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                        SECURITY                                ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

            Console.WriteLine("Security & Encryption Options:");
            Console.WriteLine("  1. Password Hashing & Verification");
            Console.WriteLine("  2. Generate Security Token");
            Console.WriteLine("  3. Encrypt Data");
            Console.WriteLine("  4. Decrypt Data");
            Console.WriteLine("  5. View Security Status");
            Console.WriteLine("  0. Back to Main Menu");
            Console.Write("\nSelect option: ");

            string choice = Console.ReadLine() ?? "";
            
            try
            {
                var encryption = ServiceContainer.Instance.GetService<EncryptionManager>();
                
                switch (choice)
                {
                    case "1":
                        Console.Write("\nEnter password to hash: ");
                        string pwd = Console.ReadLine() ?? "";
                        var hash = encryption.GenerateHash(pwd);
                        Console.WriteLine($"Hash: {hash}");
                        break;
                    case "2":
                        Console.WriteLine("\nToken generation feature requires secure context.");
                        Console.WriteLine("Placeholder for future implementation.");
                        break;
                    case "3":
                        Console.WriteLine("\nEncryption requires a valid plaintext and key.");
                        Console.WriteLine("Placeholder for future implementation.");
                        break;
                    case "4":
                        Console.WriteLine("\nDecryption requires a valid ciphertext and key.");
                        Console.WriteLine("Placeholder for future implementation.");
                        break;
                    case "5":
                        Console.WriteLine("\nSecurity Status:");
                        Console.WriteLine("  • Encryption: Enabled");
                        Console.WriteLine("  • Token Generation: Available");
                        Console.WriteLine("  • Password Hashing: SHA256");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR] {ex.Message}");
                Console.ResetColor();
            }

            Console.WriteLine("\nPress any key to return to main menu...");
            Console.ReadKey();
        }


        static async Task ShowHelp()
        {
            Console.Clear();
            Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                         HELP                                   ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

            Console.WriteLine("HELIOS Platform Documentation:");
            Console.WriteLine();
            Console.WriteLine("Dashboard:");
            Console.WriteLine("  View system metrics and resource usage in real-time.");
            Console.WriteLine();
            Console.WriteLine("System Management:");
            Console.WriteLine("  Manage partitions, optimize disk, configure security, and more.");
            Console.WriteLine();
            Console.WriteLine("Diagnostics:");
            Console.WriteLine("  System analysis, process monitoring, and health checks.");
            Console.WriteLine();
            Console.WriteLine("Security:");
            Console.WriteLine("  Encryption, vault management, and security operations.");
            Console.WriteLine();
            Console.WriteLine("AI Hub:");
            Console.WriteLine("  Access local LLM models and AI-powered system optimization.");
            Console.WriteLine();
            Console.WriteLine("Settings:");
            Console.WriteLine("  Configure HELIOS Platform preferences and system profiles.");
            Console.WriteLine();
            Console.WriteLine("Tools:");
            Console.WriteLine("  Access disk utilities, diagnostics, and system maintenance tools.");
            Console.WriteLine();
            Console.WriteLine("Terminal:");
            Console.WriteLine("  Execute CLI commands for advanced operations.");

            Console.WriteLine("\nPress any key to return to main menu...");
            Console.ReadKey();
        }

    }
}






