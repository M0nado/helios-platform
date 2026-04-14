using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace HELIOS.Platform.Demos
{
    /// <summary>
    /// HELIOS Platform Demo Launcher
    /// Interactive application for running all 7 demo scenarios.
    /// </summary>
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            PrintHeader();

            // If arguments provided, run specific demo
            if (args.Length > 0)
            {
                await RunSpecificDemoAsync(args[0]);
            }
            else
            {
                await RunInteractiveMenuAsync();
            }
        }

        static void PrintHeader()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(@"
╔════════════════════════════════════════════════════════════════╗
║                                                                ║
║     HELIOS PLATFORM - DEMO APPLICATION SUITE                  ║
║                                                                ║
║     Complete Windows Ecosystem Demonstration                  ║
║     7 Interactive Scenarios • Production-Ready                ║
║                                                                ║
╚════════════════════════════════════════════════════════════════╝
");
            Console.ResetColor();
        }

        static async Task RunInteractiveMenuAsync()
        {
            while (true)
            {
                Console.WriteLine("\n" + new string('─', 65));
                Console.WriteLine("AVAILABLE DEMOS:\n");

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("1. Quick Start Demo");
                Console.ResetColor();
                Console.WriteLine("   Fresh Windows 11 Pro setup with Phase 1 deployment");
                Console.WriteLine("   • 7 components • Real-time progress • System metrics");
                Console.WriteLine("   Duration: ~8 minutes\n");

                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("2. Gaming Optimization Demo");
                Console.ResetColor();
                Console.WriteLine("   Pre-configured gaming environment with benchmarking");
                Console.WriteLine("   • GPU optimization • FPS improvements • Gaming profile");
                Console.WriteLine("   Duration: ~10 minutes\n");

                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("3. Developer Setup Demo");
                Console.ResetColor();
                Console.WriteLine("   Full-stack development environment configuration");
                Console.WriteLine("   • VS Code • Docker • Node.js • Python • .NET");
                Console.WriteLine("   Duration: ~12 minutes\n");

                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("4. Security Hardening Demo");
                Console.ResetColor();
                Console.WriteLine("   Enterprise security configuration and compliance");
                Console.WriteLine("   • AppLocker • Firewall • Encryption • Policies");
                Console.WriteLine("   Duration: ~10 minutes\n");

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("5. Multi-Phase Deployment Demo");
                Console.ResetColor();
                Console.WriteLine("   All 7 phases with progress tracking and rollback");
                Console.WriteLine("   • Phase 0-7 execution • Monitoring • Rollback demo");
                Console.WriteLine("   Duration: ~15 minutes\n");

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("6. Enterprise Deployment Demo");
                Console.ResetColor();
                Console.WriteLine("   Full enterprise-grade production deployment");
                Console.WriteLine("   • HA setup • Monitoring • Reports • Compliance");
                Console.WriteLine("   Duration: ~14 minutes\n");

                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("7. Custom Configuration Demo");
                Console.ResetColor();
                Console.WriteLine("   User-configurable deployment with component selection");
                Console.WriteLine("   • Tier selection • Options • Estimation • Export");
                Console.WriteLine("   Duration: ~9 minutes\n");

                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("8. Run All Demos (Sequential)");
                Console.ResetColor();
                Console.WriteLine("   Execute all 7 demos in sequence");
                Console.WriteLine("   Duration: ~90 minutes\n");

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("9. Exit");
                Console.ResetColor();

                Console.Write("\nSelect demo to run [1-9]: ");
                var choice = Console.ReadLine();

                Console.WriteLine();

                switch (choice)
                {
                    case "1":
                        await RunDemoAsync(new QuickStartDemo());
                        break;
                    case "2":
                        await RunDemoAsync(new GamingOptimizationDemo());
                        break;
                    case "3":
                        await RunDemoAsync(new DeveloperSetupDemo());
                        break;
                    case "4":
                        await RunDemoAsync(new SecurityHardeningDemo());
                        break;
                    case "5":
                        await RunDemoAsync(new MultiPhaseDemo());
                        break;
                    case "6":
                        await RunDemoAsync(new EnterpriseDemo());
                        break;
                    case "7":
                        await RunDemoAsync(new CustomConfigDemo());
                        break;
                    case "8":
                        await RunAllDemosAsync();
                        break;
                    case "9":
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Thank you for using HELIOS Platform Demo Suite!");
                        Console.ResetColor();
                        return;
                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Invalid selection. Please try again.");
                        Console.ResetColor();
                        continue;
                }

                Console.WriteLine("\nPress any key to return to menu...");
                Console.ReadKey();
                Console.Clear();
            }
        }

        static async Task RunDemoAsync(DemoBase demo)
        {
            Console.Clear();
            var result = await demo.RunAsync();

            Console.WriteLine();
            Console.WriteLine(new string('═', 70));
            Console.ForegroundColor = result.Success ? ConsoleColor.Green : ConsoleColor.Red;
            Console.WriteLine($"Demo completed: {result}");
            Console.ResetColor();
            Console.WriteLine($"Report saved to: {result.LogFile}");
            Console.WriteLine(new string('═', 70));
        }

        static async Task RunAllDemosAsync()
        {
            Console.Clear();
            var demos = new DemoBase[]
            {
                new QuickStartDemo(),
                new GamingOptimizationDemo(),
                new DeveloperSetupDemo(),
                new SecurityHardeningDemo(),
                new MultiPhaseDemo(),
                new EnterpriseDemo(),
                new CustomConfigDemo()
            };

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║           RUNNING ALL 7 DEMOS IN SEQUENCE                      ║");
            Console.WriteLine("║           Estimated duration: ~90 minutes                      ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
            Console.ResetColor();

            var results = new List<DemoResult>();
            var overallTimer = Stopwatch.StartNew();

            for (int i = 0; i < demos.Length; i++)
            {
                Console.WriteLine($"\n\n{'─',70}");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"RUNNING DEMO {i + 1} OF 7: {demos[i].GetType().Name.Replace("Demo", "")}");
                Console.ResetColor();
                Console.WriteLine(new string('─', 70));

                var result = await demos[i].RunAsync();
                results.Add(result);

                if (i < demos.Length - 1)
                {
                    Console.WriteLine("\nPress any key to continue to next demo...");
                    Console.ReadKey();
                    Console.Clear();
                }
            }

            overallTimer.Stop();

            // Print summary
            Console.Clear();
            PrintHeader();
            Console.WriteLine("\n" + new string('═', 70));
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("DEMO SUITE EXECUTION SUMMARY");
            Console.ResetColor();
            Console.WriteLine(new string('═', 70));

            Console.WriteLine($"\n{"Demo",-40} {"Status",-12} {"Duration",-12}");
            Console.WriteLine(new string('─', 70));

            int successCount = 0;
            foreach (var result in results)
            {
                var statusStr = result.Success ? "✓ SUCCESS" : "✗ FAILED";
                var statusColor = result.Success ? ConsoleColor.Green : ConsoleColor.Red;
                
                Console.ForegroundColor = statusColor;
                Console.WriteLine($"{result.DemoName,-40} {statusStr,-12} {result.Duration.TotalSeconds:F1}s");
                Console.ResetColor();

                if (result.Success) successCount++;
            }

            Console.WriteLine(new string('─', 70));
            Console.WriteLine($"{"Total Time:",-40} {overallTimer.Elapsed:hh\\:mm\\:ss}");
            Console.WriteLine($"{"Success Rate:",-40} {successCount}/{results.Count} ({(successCount * 100 / results.Count)}%)");
            Console.WriteLine($"{"Components Deployed:",-40} {results.Sum(r => r.ComponentsDeployed)}");
            Console.WriteLine(new string('═', 70));

            Console.WriteLine("\nAll logs are saved in: C:\\Users\\<username>\\helios-demos\\");
            Console.WriteLine("Each demo generated:");
            Console.WriteLine("  • .log file (execution log)");
            Console.WriteLine("  • .report.txt file (text report)");
            Console.WriteLine("  • .json file (machine-readable report)");
        }

        static async Task RunSpecificDemoAsync(string demoName)
        {
            DemoBase? demo = demoName.ToLower() switch
            {
                "quickstart" or "1" => new QuickStartDemo(),
                "gaming" or "2" => new GamingOptimizationDemo(),
                "developer" or "3" => new DeveloperSetupDemo(),
                "security" or "4" => new SecurityHardeningDemo(),
                "multiphase" or "5" => new MultiPhaseDemo(),
                "enterprise" or "6" => new EnterpriseDemo(),
                "custom" or "7" => new CustomConfigDemo(),
                "all" => null,
                _ => null
            };

            if (demoName.ToLower() == "all" || demoName == "8")
            {
                await RunAllDemosAsync();
            }
            else if (demo != null)
            {
                Console.Clear();
                var result = await demo.RunAsync();
                Console.WriteLine($"\nDemo completed: {result}");
                Console.WriteLine($"Log: {result.LogFile}");
            }
            else
            {
                Console.WriteLine($"Unknown demo: {demoName}");
                Console.WriteLine("Valid options: quickstart, gaming, developer, security, multiphase, enterprise, custom, all");
            }
        }
    }
}
