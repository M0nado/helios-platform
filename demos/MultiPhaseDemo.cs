using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Demos
{
    /// <summary>
    /// Demo 5: Multi-Phase Deployment Demo
    /// Shows all 7 phases executing sequentially from Phase 0 through Phase 7.
    /// Includes real-time monitoring, progress tracking, and rollback demonstration.
    /// </summary>
    public class MultiPhaseDemo : DemoBase
    {
        private Dictionary<int, PhaseInfo> phases = new();
        private DateTime phaseStartTime;

        public MultiPhaseDemo() : base("MultiPhaseDemo") { }

        protected override async Task ExecuteDemoAsync()
        {
            LogSection("Multi-Phase HELIOS Deployment");
            LogLine("Target: Full Enterprise Deployment (All 7 Phases)", ConsoleColor.Magenta);
            LogLine();

            InitializePhases();
            await DeployAllPhasesAsync();
            LogLine();

            DisplayPhaseTimings();
            LogLine();

            await DemonstrateRollback();
            LogLine();

            DisplayDeploymentMetrics();
            LogLine();

            LogSuccess("✓ Multi-phase deployment demonstration completed");
        }

        private void InitializePhases()
        {
            phases = new()
            {
                { 0, new PhaseInfo { Number = 0, Name = "Validation", Components = new List<string> { "System Check", "Prerequisites", "Dependencies" } } },
                { 1, new PhaseInfo { Number = 1, Name = "Foundation", Components = new List<string> { "Monado Engine", "Core Libraries" } } },
                { 2, new PhaseInfo { Number = 2, Name = "Security", Components = new List<string> { "Security System", "Firewall", "Encryption" } } },
                { 3, new PhaseInfo { Number = 3, Name = "Dashboard", Components = new List<string> { "GUI Dashboard", "Monitoring", "Real-time Updates" } } },
                { 4, new PhaseInfo { Number = 4, Name = "Automation", Components = new List<string> { "Build Agents", "CI/CD Pipeline" } } },
                { 5, new PhaseInfo { Number = 5, Name = "Intelligence", Components = new List<string> { "AI Orchestrator", "ML Models" } } },
                { 6, new PhaseInfo { Number = 6, Name = "Development", Components = new List<string> { "DevAI Hub", "Developer Tools" } } },
                { 7, new PhaseInfo { Number = 7, Name = "Software Stack", Components = new List<string> { "45 Development Tools", "Frameworks", "Libraries" } } }
            };
        }

        private async Task DeployAllPhasesAsync()
        {
            LogSection("Phase Progression");
            LogLine();

            for (int phase = 0; phase <= 7; phase++)
            {
                await DeployPhaseAsync(phase);
                if (phase < 7)
                {
                    LogLine(); // Add spacing between phases
                }
            }
        }

        private async Task DeployPhaseAsync(int phaseNum)
        {
            if (!phases.TryGetValue(phaseNum, out var phase))
                return;

            phaseStartTime = DateTime.Now;
            phase.StartTime = phaseStartTime;

            LogLine($"┌─ PHASE {phase.Number}: {phase.Name} " + new string('─', 50), ConsoleColor.Magenta);

            var completedPercentage = (phaseNum * 14); // ~14% per phase
            var bar = new string('█', phaseNum + 1) + new string('░', 7 - phaseNum);
            LogLine($"│ Overall Progress: [{bar}] {completedPercentage}%", ConsoleColor.Cyan);
            LogLine($"│", ConsoleColor.Magenta);

            int componentIndex = 0;
            foreach (var component in phase.Components)
            {
                componentIndex++;
                var componentPercent = (componentIndex * 100) / phase.Components.Count;
                LogLine($"│ [{componentIndex}/{phase.Components.Count}] {componentPercent}%: {component}", ConsoleColor.Cyan);
                await DeployComponentAsync(component, 500);
                Metrics[$"Phase{phaseNum}_{component}"] = "Deployed";
            }

            phase.EndTime = DateTime.Now;
            phase.Duration = phase.EndTime.Value - phase.StartTime.Value;

            LogSuccess($"│ ✓ Phase {phaseNum} completed in {phase.Duration?.TotalSeconds:F1}s");
            LogLine($"└" + new string('─', 56), ConsoleColor.Magenta);
        }

        private void DisplayPhaseTimings()
        {
            LogSection("Phase Execution Timeline");
            LogLine($"{"Phase",-8} {"Name",-20} {"Duration",-12} {"Status",-12} {"Resources",-15}");
            LogLine(new string('─', 70));

            TimeSpan totalDuration = TimeSpan.Zero;
            foreach (var phase in phases.Values)
            {
                var duration = phase.Duration ?? TimeSpan.Zero;
                totalDuration += duration;
                var color = phase.Number <= 7 ? ConsoleColor.Green : ConsoleColor.Yellow;
                LogLine($"{phase.Number,-8} {phase.Name,-20} {duration.TotalSeconds:F2}s {"-12",-12} ✓ Active", color);
            }

            LogLine(new string('─', 70));
            LogLine($"{"TOTAL",-8} {"All Phases",-20} {totalDuration.TotalSeconds:F2}s total execution time", ConsoleColor.Cyan);
            Metrics["TotalDeploymentTime"] = totalDuration.TotalSeconds;
        }

        private async Task DemonstrateRollback()
        {
            LogSection("Rollback Demonstration");
            LogLine("Rolling back from Phase 7 to Phase 4...", ConsoleColor.Yellow);
            LogLine();

            for (int p = 7; p >= 4; p--)
            {
                LogLine($"  Rolling back Phase {p}...", ConsoleColor.Yellow);
                await Task.Delay(400);
                LogSuccess($"  ✓ Phase {p} rolled back");
            }

            LogLine();
            LogSuccess("✓ Rollback completed successfully");
            LogLine("  Deployment reverted to Phase 4: Automation");
            LogLine("  No data loss. Ready to redeploy from Phase 4.");
        }

        private void DisplayDeploymentMetrics()
        {
            LogSection("Deployment Metrics & Performance");
            LogLine($"{"Metric",-35} {"Value",-25}");
            LogLine(new string('─', 60));
            LogLine($"{"Total Phases Completed",-35} {8,-25}");
            LogLine($"{"Total Components Deployed",-35} {DeployedComponents.Count,-25}");
            LogLine($"{"Total Execution Time",-35} {ExecutionTimer.Elapsed:hh\\:mm\\:ss,-25}");
            LogLine($"{"Average Phase Duration",-35} {(ExecutionTimer.Elapsed.TotalSeconds / 8):F2}s");
            LogLine($"{"Resource Utilization",-35} {"CPU: 65%, Memory: 45%",-25}");
            LogLine($"{"Success Rate",-35} {"100%",-25}");
            LogLine($"{"Errors Encountered",-35} {"0",-25}");
            LogLine();

            Metrics["PhasesCompleted"] = 8;
            Metrics["ComponentsDeployed"] = DeployedComponents.Count;
            Metrics["SuccessRate"] = "100%";
        }

        private class PhaseInfo
        {
            public int Number { get; set; }
            public string Name { get; set; } = "";
            public List<string> Components { get; set; } = new();
            public DateTime? StartTime { get; set; }
            public DateTime? EndTime { get; set; }
            public TimeSpan? Duration { get; set; }
        }
    }
}
