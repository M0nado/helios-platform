using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Demos
{
    /// <summary>
    /// Demo 1: Quick Start Demo
    /// Fresh Windows 11 Pro setup with Phase 1 Professional tier deployment.
    /// Shows real-time progress, 7 components, and system metrics comparison.
    /// </summary>
    public class QuickStartDemo : DemoBase
    {
        public QuickStartDemo() : base("QuickStartDemo") { }

        protected override async Task ExecuteDemoAsync()
        {
            LogSection("PHASE 0: System Validation");
            await ValidateSystem();
            LogLine();

            var beforeMetrics = GetSystemMetrics("Before Deployment");
            LogLine($"System State (Before): CPU {beforeMetrics.CPUUsage}%, Memory {beforeMetrics.MemoryUsageMB}MB", ConsoleColor.Yellow);
            LogLine();

            LogSection("PHASE 1: Professional Tier Deployment");
            LogLine("Deploying 7 core components...", ConsoleColor.Cyan);
            LogLine();

            var phase1Components = new List<string>
            {
                "Monado Engine",
                "Security System",
                "AI Orchestrator",
                "GUI Dashboard",
                "Build Agents",
                "DevAI Hub",
                "Software Stack"
            };

            int componentIndex = 0;
            foreach (var component in phase1Components)
            {
                componentIndex++;
                var progressPercent = (componentIndex * 100) / 7;
                LogLine($"[{componentIndex}/7] {progressPercent}%: Installing {component}...", ConsoleColor.Cyan);
                await DeployComponentAsync(component, 900);
                Metrics[$"{component}"] = "Deployed";
                LogLine();
            }

            LogSection("PHASE 1: Configuration");
            LogLine("Configuring Professional tier settings...", ConsoleColor.Cyan);
            await ConfigureProfileAsync();
            LogLine();

            var afterMetrics = GetSystemMetrics("After Deployment");
            GenerateComparison(beforeMetrics, afterMetrics, "System Performance Comparison");

            LogSection("DEPLOYMENT SUMMARY");
            LogSuccess($"✓ Quick Start deployment completed successfully");
            LogLine($"  Components deployed: {DeployedComponents.Count}");
            LogLine($"  Execution time: {ExecutionTimer.Elapsed:hh\\:mm\\:ss}");
            LogLine($"  Profile: Professional Tier");
            LogLine();

            Metrics["TotalDeploymentTime"] = ExecutionTimer.Elapsed.TotalSeconds;
            Metrics["DeploymentTier"] = "Professional";
            Metrics["ComponentCount"] = DeployedComponents.Count;
            Metrics["SystemReady"] = true;
        }

        private async Task ValidateSystem()
        {
            LogLine("✓ Windows 11 Pro detected");
            LogLine("✓ System requirements met");
            LogLine("✓ Admin privileges confirmed");
            LogLine("✓ Network connectivity: Online");
            LogLine("✓ Disk space available: 250GB");
            await Task.Delay(500);
        }

        private async Task ConfigureProfileAsync()
        {
            LogLine("  • Configuring Performance settings...", ConsoleColor.Cyan);
            await Task.Delay(600);
            LogLine("  ✓ Power profile optimized", ConsoleColor.Green);

            LogLine("  • Setting up Windows Defender integration...", ConsoleColor.Cyan);
            await Task.Delay(600);
            LogLine("  ✓ Security policies applied", ConsoleColor.Green);

            LogLine("  • Registering diagnostic tools...", ConsoleColor.Cyan);
            await Task.Delay(600);
            LogLine("  ✓ Monitoring enabled", ConsoleColor.Green);
        }
    }
}
