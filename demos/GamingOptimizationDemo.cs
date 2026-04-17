using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Demos
{
    /// <summary>
    /// Demo 2: Gaming Optimization Demo
    /// Pre-configured gaming environment with performance optimization.
    /// Shows FPS improvements, gaming tools installation, and benchmarking.
    /// </summary>
    public class GamingOptimizationDemo : DemoBase
    {
        private double baselineFPS = 45.0;
        private double optimizedFPS = 145.0;

        public GamingOptimizationDemo() : base("GamingOptimizationDemo") { }

        protected override async Task ExecuteDemoAsync()
        {
            LogSection("Gaming Optimization Demo");
            LogLine("Profile: High-End Gaming PC", ConsoleColor.Magenta);
            LogLine();

            LogSection("PHASE 0: Gaming Environment Validation");
            await ValidateGamingSetup();
            LogLine();

            var beforeBench = GetGamingBenchmarks("before");
            DisplayGamingMetrics(beforeBench, "Baseline Performance");
            LogLine();

            LogSection("PHASE 1: Gaming Tool Installation");
            var gamingTools = new List<string>
            {
                "NVIDIA GeForce Experience",
                "AMD Radeon Software",
                "Intel XTU (Extreme Tuning)",
                "GPU-Z",
                "FrameView Performance Monitor",
                "VSync Controller"
            };

            int toolIndex = 0;
            foreach (var tool in gamingTools)
            {
                toolIndex++;
                LogLine($"[{toolIndex}/6] Installing {tool}...", ConsoleColor.Cyan);
                await DeployComponentAsync(tool, 700);
                LogLine();
            }

            LogSection("PHASE 2: System Optimization");
            await OptimizeForGaming();
            LogLine();

            LogSection("PHASE 3: GPU Configuration");
            await ConfigureGPUSettings();
            LogLine();

            LogSection("PHASE 4: Performance Benchmarking");
            await RunGamingBenchmarks();
            LogLine();

            var afterBench = GetGamingBenchmarks("after");
            DisplayPerformanceComparison(beforeBench, afterBench);
            LogLine();

            LogSection("GAMING PROFILE SAVED");
            LogSuccess("✓ Gaming profile 'HighEnd_Ultra' saved successfully");
            LogLine("  Export path: C:\\Users\\Public\\HELIOS\\Gaming\\HighEnd_Ultra.profile");
            LogLine();

            Metrics["BaselineFPS"] = baselineFPS;
            Metrics["OptimizedFPS"] = optimizedFPS;
            Metrics["FPSImprovement"] = ((optimizedFPS - baselineFPS) / baselineFPS * 100).ToString("F1") + "%";
            Metrics["GamingProfile"] = "HighEnd_Ultra";
        }

        private async Task ValidateGamingSetup()
        {
            LogLine("✓ NVIDIA RTX 4090 detected");
            LogLine("✓ 64GB DDR5 RAM detected");
            LogLine("✓ 1TB NVMe SSD detected");
            LogLine("✓ 360Hz Gaming Monitor detected");
            LogLine("✓ Gaming peripherals: Configured");
            await Task.Delay(400);
        }

        private async Task OptimizeForGaming()
        {
            LogLine("  • Disabling background services...", ConsoleColor.Cyan);
            await Task.Delay(500);
            LogLine("  ✓ 12 services disabled", ConsoleColor.Green);

            LogLine("  • Optimizing CPU scheduling...", ConsoleColor.Cyan);
            await Task.Delay(500);
            LogLine("  ✓ GPU has priority access", ConsoleColor.Green);

            LogLine("  • Disabling Windows updates during gaming...", ConsoleColor.Cyan);
            await Task.Delay(500);
            LogLine("  ✓ Interruptions prevented", ConsoleColor.Green);

            LogLine("  • Enabling GPU overclock profiles...", ConsoleColor.Cyan);
            await Task.Delay(500);
            LogLine("  ✓ +15% GPU performance available", ConsoleColor.Green);
        }

        private async Task ConfigureGPUSettings()
        {
            LogLine("  • NVIDIA Control Panel settings:", ConsoleColor.Cyan);
            LogLine("    - Power management: Maximum performance");
            LogLine("    - Texture filtering: High quality");
            LogLine("    - NVIDIA GeForce Experience: Optimized");
            await Task.Delay(600);
            LogLine("  ✓ GPU configured", ConsoleColor.Green);
        }

        private async Task RunGamingBenchmarks()
        {
            await ShowProgressAsync(10, "3DMark Fire Strike");
            LogLine("  Score: 47,890 points", ConsoleColor.Green);
            LogLine();

            await ShowProgressAsync(10, "GFXBench Aztec Ruins");
            LogLine("  FPS: 185 fps", ConsoleColor.Green);
            LogLine();

            await ShowProgressAsync(10, "Heaven Benchmark");
            LogLine("  FPS: 156 fps", ConsoleColor.Green);
        }

        private void DisplayGamingMetrics(GamingBenchmark bench, string label)
        {
            LogSection(label);
            LogLine($"{"Metric",-30} {"Value",-20}");
            LogLine(new string('─', 50));
            LogLine($"{"Average FPS",-30} {bench.AverageFPS,-20:F1}");
            LogLine($"{"Min FPS",-30} {bench.MinFPS,-20:F1}");
            LogLine($"{"Max FPS",-30} {bench.MaxFPS,-20:F1}");
            LogLine($"{"GPU Temperature",-30} {bench.GPUTempC,-20:F1}°C");
            LogLine($"{"GPU Memory",-30} {bench.GPUMemoryMB,-20}MB");
            LogLine($"{"Frame Time (avg)",-30} {bench.FrameTimeMs,-20:F2}ms");
            LogLine($"{"Power Consumption",-30} {bench.PowerDrawW,-20:F1}W");
        }

        private void DisplayPerformanceComparison(GamingBenchmark before, GamingBenchmark after)
        {
            LogSection("Performance Improvement");
            LogLine($"{"Metric",-25} {"Before",-12} {"After",-12} {"Gain",-12}");
            LogLine(new string('─', 60));

            var fpsGain = after.AverageFPS - before.AverageFPS;
            var tempReduction = before.GPUTempC - after.GPUTempC;
            var powerReduction = before.PowerDrawW - after.PowerDrawW;

            LogLine($"{"Avg FPS",-25} {before.AverageFPS,-12:F1} {after.AverageFPS,-12:F1} {fpsGain:+0;-#;0} fps", ConsoleColor.Green);
            LogLine($"{"GPU Temperature",-25} {before.GPUTempC,-12:F1}°C {after.GPUTempC,-12:F1}°C {tempReduction:+0;-#;0}°C", ConsoleColor.Green);
            LogLine($"{"Power Draw",-25} {before.PowerDrawW,-12:F1}W {after.PowerDrawW,-12:F1}W {powerReduction:+0;-#;0}W", ConsoleColor.Green);
            LogLine();
            LogSuccess($"✓ Overall gaming performance improved by {(fpsGain / before.AverageFPS * 100):F1}%");
        }

        private GamingBenchmark GetGamingBenchmarks(string when)
        {
            if (when == "before")
            {
                return new GamingBenchmark
                {
                    AverageFPS = baselineFPS,
                    MinFPS = baselineFPS - 8,
                    MaxFPS = baselineFPS + 5,
                    GPUTempC = 78,
                    GPUMemoryMB = 8192,
                    FrameTimeMs = 22.2,
                    PowerDrawW = 320
                };
            }
            else
            {
                return new GamingBenchmark
                {
                    AverageFPS = optimizedFPS,
                    MinFPS = optimizedFPS - 5,
                    MaxFPS = optimizedFPS + 8,
                    GPUTempC = 62,
                    GPUMemoryMB = 10240,
                    FrameTimeMs = 6.9,
                    PowerDrawW = 310
                };
            }
        }

        private class GamingBenchmark
        {
            public double AverageFPS { get; set; }
            public double MinFPS { get; set; }
            public double MaxFPS { get; set; }
            public double GPUTempC { get; set; }
            public int GPUMemoryMB { get; set; }
            public double FrameTimeMs { get; set; }
            public double PowerDrawW { get; set; }
        }
    }
}
