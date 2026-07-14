// ═══════════════════════════════════════════════════════════════════════════
// Sample Plugin 1: Performance Analyzer
// Demonstrates IAnalyzer plugin implementation
// ═══════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HELIOS.Platform.Plugins.Interfaces;

namespace HELIOS.Platform.Plugins.Samples
{
    [Plugin(
        id: "helios.analyzer.performance",
        name: "Performance Analyzer",
        version: "1.0.0",
        author: "HELIOS Team",
        description: "Analyzes system performance metrics and identifies bottlenecks",
        category: PluginCategory.Analyzer)]
    public class PerformanceAnalyzerPlugin : IAnalyzer
    {
        private IPluginContext _context;
        private bool _disposed;

        public string Id => "helios.analyzer.performance";
        public string Name => "Performance Analyzer";
        public string Version => "1.0.0";
        public string Author => "HELIOS Team";
        public string Description => "Analyzes system performance metrics and identifies bottlenecks";
        public PluginCategory Category => PluginCategory.Analyzer;
        public IReadOnlyList<string> Dependencies => new List<string>();
        public PluginStatus Status { get; private set; } = PluginStatus.NotLoaded;
        public string AnalysisScope => "System Performance";

        public event EventHandler<PluginStatusChangedEventArgs> StatusChanged;

        public async Task InitializeAsync(IPluginContext context, CancellationToken cancellationToken = default)
        {
            _context = context;
            Status = PluginStatus.Loaded;
            _context.Logger.LogInformation("Performance Analyzer initialized");
            await Task.CompletedTask;
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            Status = PluginStatus.Running;
            _context.Logger.LogInformation("Performance Analyzer started");
            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            Status = PluginStatus.Loaded;
            _context.Logger.LogInformation("Performance Analyzer stopped");
            await Task.CompletedTask;
        }

        public HELIOS.Platform.Plugins.Interfaces.IPluginConfiguration GetConfiguration()
        {
            return null;
        }

        public async Task<bool> ValidateAsync(CancellationToken cancellationToken = default)
        {
            return await Task.FromResult(true);
        }

        public async Task<AnalysisResult> AnalyzeAsync(
            object target,
            IAnalysisOptions options = null,
            CancellationToken cancellationToken = default)
        {
            _context.Logger.LogInformation("Starting performance analysis");

            var result = new AnalysisResult
            {
                AnalyzerId = Id,
                AnalysisTime = DateTime.UtcNow,
                IsSuccessful = true,
                ScopeAnalyzed = AnalysisScope,
                ExecutionTime = TimeSpan.FromMilliseconds(150),
                Findings = new()
                {
                    new AnalysisFinding
                    {
                        Id = "perf-001",
                        Title = "High CPU Usage",
                        Description = "CPU usage is elevated",
                        Severity = FindingSeverity.Warning,
                        Category = "CPU"
                    },
                    new AnalysisFinding
                    {
                        Id = "perf-002",
                        Title = "Memory Pressure",
                        Description = "Memory utilization is high",
                        Severity = FindingSeverity.Warning,
                        Category = "Memory"
                    }
                }
            };

            return await Task.FromResult(result);
        }

        public IReadOnlyList<string> GetSupportedOptions()
        {
            return new[] { "detailed", "baseline-comparison", "custom-metrics" };
        }

        public async Task<bool> CanAnalyzeAsync(object target, CancellationToken cancellationToken = default)
        {
            return await Task.FromResult(target != null);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }
    }
}
