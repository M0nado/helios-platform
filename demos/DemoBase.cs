using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HELIOS.Platform.Demos
{
    /// <summary>
    /// Base class for all demo applications with common functionality.
    /// </summary>
    public abstract class DemoBase
    {
        protected string DemoName { get; set; } = "Demo";
        protected string LogFilePath { get; set; }
        protected StringBuilder LogBuffer { get; set; } = new();
        protected Stopwatch ExecutionTimer { get; set; } = new();
        protected Dictionary<string, object> Metrics { get; set; } = new();
        protected List<string> DeployedComponents { get; set; } = new();
        protected bool Success { get; set; } = true;

        protected DemoBase(string demoName)
        {
            DemoName = demoName;
            LogFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "helios-demos",
                $"{DateTime.Now:yyyy-MM-dd}",
                $"{demoName}_{DateTime.Now:HH-mm-ss}.log"
            );

            Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath)!);
        }

        /// <summary>
        /// Execute the demo application.
        /// </summary>
        public async Task<DemoResult> RunAsync()
        {
            ExecutionTimer.Start();
            LogSection($"═══ HELIOS Platform Demo: {DemoName} ═══");
            LogLine($"Start Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            LogLine($"Log File: {LogFilePath}");
            LogLine();

            try
            {
                await ExecuteDemoAsync();
                ExecutionTimer.Stop();
                await GenerateReportAsync();
                return CreateResult();
            }
            catch (Exception ex)
            {
                Success = false;
                LogError($"Demo failed: {ex.Message}");
                LogError(ex.StackTrace!);
                ExecutionTimer.Stop();
                await GenerateReportAsync();
                return CreateResult();
            }
        }

        protected abstract Task ExecuteDemoAsync();

        /// <summary>
        /// Simulate system metrics before deployment.
        /// </summary>
        protected SystemMetrics GetSystemMetrics(string label)
        {
            return new SystemMetrics
            {
                Label = label,
                Timestamp = DateTime.Now,
                CPUUsage = 15 + Random.Shared.Next(20),
                MemoryUsageMB = 2000 + Random.Shared.Next(2000),
                DiskUsagePercent = 45 + Random.Shared.Next(20),
                ProcessCount = 150 + Random.Shared.Next(50),
                ThreadCount = 1000 + Random.Shared.Next(500),
                HandleCount = 5000 + Random.Shared.Next(2000)
            };
        }

        /// <summary>
        /// Simulate component deployment.
        /// </summary>
        protected async Task DeployComponentAsync(string componentName, int delayMs = 800)
        {
            LogLine($"  ⟳ Deploying {componentName}...", ConsoleColor.Cyan);
            await Task.Delay(delayMs);
            DeployedComponents.Add(componentName);
            LogLine($"  ✓ {componentName} deployed successfully", ConsoleColor.Green);
        }

        /// <summary>
        /// Simulate phase deployment.
        /// </summary>
        protected async Task DeployPhaseAsync(int phaseNumber, string phaseName, List<string> components)
        {
            LogSection($"Phase {phaseNumber}: {phaseName}");
            foreach (var component in components)
            {
                await DeployComponentAsync(component);
                Metrics[$"{component}_Status"] = "Healthy";
            }
            LogLine($"Phase {phaseNumber} complete", ConsoleColor.Green);
            LogLine();
        }

        /// <summary>
        /// Generate before/after comparison.
        /// </summary>
        protected void GenerateComparison(SystemMetrics before, SystemMetrics after, string title)
        {
            LogSection(title);
            LogLine($"{"Metric",-25} {"Before",-15} {"After",-15} {"Change",-15}");
            LogLine(new string('─', 70));

            var cpuChange = after.CPUUsage - before.CPUUsage;
            var memoryChange = after.MemoryUsageMB - before.MemoryUsageMB;
            var diskChange = after.DiskUsagePercent - before.DiskUsagePercent;

            LogLine($"{"CPU Usage %",-25} {before.CPUUsage,-15} {after.CPUUsage,-15} {cpuChange:+0;-#;0}%");
            LogLine($"{"Memory (MB)",-25} {before.MemoryUsageMB,-15} {after.MemoryUsageMB,-15} {memoryChange:+0;-#;0} MB");
            LogLine($"{"Disk Usage %",-25} {before.DiskUsagePercent,-15} {after.DiskUsagePercent,-15} {diskChange:+0;-#;0}%");
            LogLine($"{"Processes",-25} {before.ProcessCount,-15} {after.ProcessCount,-15} {after.ProcessCount - before.ProcessCount:+0;-#;0}");
            LogLine();
        }

        /// <summary>
        /// Create a JSON report.
        /// </summary>
        protected string GenerateJsonReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"demoName\": \"{DemoName}\",");
            sb.AppendLine($"  \"executionTime\": \"{ExecutionTimer.Elapsed:hh\\:mm\\:ss}\",");
            sb.AppendLine($"  \"success\": {Success.ToString().ToLower()},");
            sb.AppendLine($"  \"components\": [");
            sb.AppendLine(string.Join(",\n", DeployedComponents.Select(c => $"    \"{c}\"")));
            sb.AppendLine("  ],");
            sb.AppendLine($"  \"metrics\": {{");

            var metricLines = Metrics.Select(kvp => $"    \"{kvp.Key}\": {FormatMetricValue(kvp.Value)}");
            sb.AppendLine(string.Join(",\n", metricLines));

            sb.AppendLine("  }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private string FormatMetricValue(object value)
        {
            return value switch
            {
                string s => $"\"{s}\"",
                bool b => b.ToString().ToLower(),
                _ => value.ToString()!
            };
        }

        /// <summary>
        /// Generate comprehensive report.
        /// </summary>
        protected async Task GenerateReportAsync()
        {
            var reportPath = LogFilePath.Replace(".log", ".report.txt");
            var jsonPath = LogFilePath.Replace(".log", ".json");

            // Write text report
            using (var writer = new StreamWriter(reportPath))
            {
                await writer.WriteLineAsync("═══════════════════════════════════════════════════════════");
                await writer.WriteLineAsync($"HELIOS Platform Demo Report: {DemoName}");
                await writer.WriteLineAsync("═══════════════════════════════════════════════════════════");
                await writer.WriteLineAsync();
                await writer.WriteLineAsync($"Start Time:      {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                await writer.WriteLineAsync($"Duration:        {ExecutionTimer.Elapsed:hh\\:mm\\:ss}");
                await writer.WriteLineAsync($"Status:          {(Success ? "✓ SUCCESS" : "✗ FAILED")}");
                await writer.WriteLineAsync();

                await writer.WriteLineAsync("DEPLOYED COMPONENTS:");
                foreach (var component in DeployedComponents)
                {
                    await writer.WriteLineAsync($"  ✓ {component}");
                }
                await writer.WriteLineAsync();

                await writer.WriteLineAsync("METRICS:");
                foreach (var metric in Metrics)
                {
                    await writer.WriteLineAsync($"  {metric.Key}: {metric.Value}");
                }
                await writer.WriteLineAsync();

                await writer.WriteLineAsync(LogBuffer.ToString());
            }

            // Write JSON report
            File.WriteAllText(jsonPath, GenerateJsonReport());

            LogLine($"✓ Reports generated:", ConsoleColor.Green);
            LogLine($"  Text: {reportPath}");
            LogLine($"  JSON: {jsonPath}");
        }

        private DemoResult CreateResult()
        {
            return new DemoResult
            {
                DemoName = DemoName,
                Success = Success,
                Duration = ExecutionTimer.Elapsed,
                ComponentsDeployed = DeployedComponents.Count,
                LogFile = LogFilePath,
                Timestamp = DateTime.Now
            };
        }

        protected void LogSection(string text)
        {
            var line = $"\n{text}";
            LogLine(line);
            LogLine(new string('─', text.Length));
        }

        protected void LogLine(string? text = "", ConsoleColor color = ConsoleColor.White)
        {
            var message = text ?? "";
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
            LogBuffer.AppendLine(message);
        }

        protected void LogError(string text)
        {
            LogLine(text, ConsoleColor.Red);
        }

        protected void LogSuccess(string text)
        {
            LogLine(text, ConsoleColor.Green);
        }

        protected void LogWarning(string text)
        {
            LogLine(text, ConsoleColor.Yellow);
        }

        protected void LogInfo(string text)
        {
            LogLine(text, ConsoleColor.Cyan);
        }

        protected async Task ShowProgressAsync(int steps, string taskName)
        {
            for (int i = 1; i <= steps; i++)
            {
                var percent = (i * 100) / steps;
                var filled = (percent / 5);
                var empty = 20 - filled;
                var bar = new string('█', filled) + new string('░', empty);
                LogLine($"  {taskName}: [{bar}] {percent}%", ConsoleColor.Cyan);
                await Task.Delay(200);
            }
        }
    }

    /// <summary>
    /// System metrics snapshot.
    /// </summary>
    public class SystemMetrics
    {
        public string Label { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public int CPUUsage { get; set; }
        public int MemoryUsageMB { get; set; }
        public int DiskUsagePercent { get; set; }
        public int ProcessCount { get; set; }
        public int ThreadCount { get; set; }
        public int HandleCount { get; set; }
    }

    /// <summary>
    /// Demo execution result.
    /// </summary>
    public class DemoResult
    {
        public string DemoName { get; set; } = "";
        public bool Success { get; set; }
        public TimeSpan Duration { get; set; }
        public int ComponentsDeployed { get; set; }
        public string LogFile { get; set; } = "";
        public DateTime Timestamp { get; set; }

        public override string ToString()
        {
            return $"{DemoName}: {(Success ? "✓" : "✗")} ({Duration.TotalSeconds:F1}s)";
        }
    }
}
