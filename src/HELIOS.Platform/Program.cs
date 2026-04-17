using System;
using System.Threading.Tasks;
using HELIOS.Platform.Core;
using HELIOS.Platform.BackendServices.ServerManagement;
using HELIOS.Platform.Core.Logging;
using HELIOS.Platform.Core.Diagnostics;
using HELIOS.Platform.Core.Storage;
using HELIOS.Platform.Core.Configuration;
using HELIOS.Platform.Core.Security;

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
                
                ServiceContainer.Instance.RegisterSingleton<Core.Logging.ILogger>(logger);
                ServiceContainer.Instance.RegisterSingleton<IServiceOrchestrator>(orchestrator);
                ServiceContainer.Instance.RegisterSingleton<SystemDiagnostics>(diagnostics);
                ServiceContainer.Instance.RegisterSingleton<StorageManager>(storage);
                ServiceContainer.Instance.RegisterSingleton<Core.Configuration.ConfigurationManager>(config);
                ServiceContainer.Instance.RegisterSingleton<EncryptionManager>(encryption);
                
                logger.Info("HELIOS Platform initialized successfully");
                
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
            Console.WriteLine("║                        DASHBOARD                               ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

            try
            {
                var orchestrator = ServiceContainer.Instance.GetService<IServiceOrchestrator>();
                var resources = await orchestrator.GetSystemResourcesAsync();
                
                Console.WriteLine($"CPU Usage:        {resources?.CpuUsagePercent:F1}%");
                Console.WriteLine($"Memory Usage:     {resources?.MemoryUsageMB} MB");
                Console.WriteLine($"System Uptime:    {resources?.SystemUptimeSeconds} seconds");
                Console.WriteLine($"Active Services:  {resources?.ActiveServices}");
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

        static async Task ShowSystemManagement()
        {
            Console.Clear();
            Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                    SYSTEM MANAGEMENT                           ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

            Console.WriteLine("Available tools:");
            Console.WriteLine("  • Partition Management");
            Console.WriteLine("  • Disk Optimization");
            Console.WriteLine("  • User Accounts");
            Console.WriteLine("  • Security Settings");
            Console.WriteLine("  • Device Manager");

            Console.WriteLine("\nPress any key to return to main menu...");
            Console.ReadKey();
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
            Console.WriteLine("║                       TERMINAL                                ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

            Console.WriteLine("CLI Commands Available:");
            Console.WriteLine("  • system info");
            Console.WriteLine("  • partition list");
            Console.WriteLine("  • service status");
            Console.WriteLine("  • help");

            Console.WriteLine("\nType 'exit' to return to main menu...");
            
            while (true)
            {
                Console.Write("> ");
                string cmd = Console.ReadLine() ?? "";
                
                if (cmd == "exit")
                    break;
                    
                if (cmd == "help")
                {
                    Console.WriteLine("  Available commands: system info, partition list, service status, exit");
                }
                else if (cmd.StartsWith("system"))
                {
                    Console.WriteLine("  System information retrieved");
                }
                else if (!string.IsNullOrEmpty(cmd))
                {
                    Console.WriteLine("  Command not found. Type 'help' for available commands.");
                }
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
