using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HELIOS.Platform.AI;
using HELIOS.Platform.Core.AI;
using HELIOS.Platform.Core.AI.Interfaces;
using HELIOS.Platform.Core.AI.Router;

namespace HELIOS.Platform.Core.AIHub
{
    public sealed class AIHubUnit
    {
        public string UnitId { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = string.Empty;
        public string MissionFamily { get; set; } = string.Empty;
        public string[] Capabilities { get; set; } = Array.Empty<string>();
        public double ReliabilityScore { get; set; } = 0.9;
        public double ExpectedLoad { get; set; } = 0.5;
    }

    public sealed class FleetRecommendation
    {
        public string Mission { get; set; } = string.Empty;
        public IReadOnlyList<AIHubUnit> Units { get; set; } = Array.Empty<AIHubUnit>();
        public IReadOnlyList<string> BalanceTips { get; set; } = Array.Empty<string>();
        public IReadOnlyList<string> MissionRecommendations { get; set; } = Array.Empty<string>();
    }

    public sealed class FleetTrainingResult
    {
        public IReadOnlyList<AIModel> Models { get; set; } = Array.Empty<AIModel>();
        public bool LearningStarted { get; set; }
    }

    public interface IAIHubScriptAdapter
    {
        Task<string> InvokeAsync(string operation, IReadOnlyDictionary<string, object> parameters, CancellationToken cancellationToken = default);
    }

    public sealed class NoOpAIHubScriptAdapter : IAIHubScriptAdapter
    {
        public Task<string> InvokeAsync(string operation, IReadOnlyDictionary<string, object> parameters, CancellationToken cancellationToken = default) =>
            Task.FromResult($"Script operation '{operation}' is available through an adapter and was not shelled from UI code.");
    }

    public sealed class AIHubUnitAgentAdapter : IAgent
    {
        private readonly AIHubUnit _unit;
        private readonly IReadOnlyList<ICapability> _capabilities;
        private long _totalRequests;
        private long _successfulRequests;
        private long _failedRequests;
        private double _averageExecutionTimeMs;

        public AIHubUnitAgentAdapter(AIHubUnit unit)
        {
            _unit = unit ?? throw new ArgumentNullException(nameof(unit));
            _capabilities = unit.Capabilities.Select(c => new AIHubCapability(c)).ToArray();
        }

        public AIHubUnit Unit => _unit;
        public string AgentId => _unit.UnitId;
        public string Name => _unit.Name;
        public AgentType AgentType => AgentType.Executor;
        public AgentStatus Status { get; private set; } = AgentStatus.Uninitialized;

        public Task InitializeAsync(CancellationToken cancellationToken = default) { Status = AgentStatus.Ready; return Task.CompletedTask; }
        public Task StartAsync(CancellationToken cancellationToken = default) { Status = AgentStatus.Running; return Task.CompletedTask; }
        public Task StopAsync(CancellationToken cancellationToken = default) { Status = AgentStatus.Stopped; return Task.CompletedTask; }

        public Task<AgentResult> ExecuteAsync(AgentRequest request, CancellationToken cancellationToken = default)
        {
            var sw = Stopwatch.StartNew();
            var matches = request.Parameters.TryGetValue("mission", out var mission) &&
                (HasCapability(Convert.ToString(mission) ?? string.Empty) || string.Equals(_unit.MissionFamily, Convert.ToString(mission), StringComparison.OrdinalIgnoreCase));
            sw.Stop();
            _totalRequests++;
            if (matches) _successfulRequests++; else _failedRequests++;
            _averageExecutionTimeMs = (_averageExecutionTimeMs + sw.Elapsed.TotalMilliseconds) / 2;
            return Task.FromResult(new AgentResult { RequestId = request.RequestId, Success = matches, ResultData = _unit, ErrorMessage = matches ? null : "Unit does not match mission.", ExecutionTime = sw.Elapsed });
        }

        public IReadOnlyList<ICapability> GetCapabilities() => _capabilities;
        public bool HasCapability(string capabilityName) => _unit.Capabilities.Any(c => string.Equals(c, capabilityName, StringComparison.OrdinalIgnoreCase));
        public AgentMetrics GetMetrics() => new() { TotalRequestsProcessed = _totalRequests, SuccessfulRequests = _successfulRequests, FailedRequests = _failedRequests, AverageExecutionTimeMs = _averageExecutionTimeMs, PeakMemoryMb = 64, LastHealthCheckTime = DateTime.UtcNow, IsHealthy = _unit.ReliabilityScore >= 0.5 };
        public Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default) => Task.FromResult(_unit.ReliabilityScore >= 0.5);

        private sealed class AIHubCapability : ICapability
        {
            public AIHubCapability(string name) => CapabilityName = name;
            public string CapabilityName { get; }
            public string Description => $"AIHub capability: {CapabilityName}";
            public Version Version { get; } = new(1, 0, 0);
            public Task InitializeAsync() => Task.CompletedTask;
        }
    }

    public sealed class AIHubFleetService
    {
        private readonly IRouter _router;
        private readonly IAILearningCoordinator _learningCoordinator;
        private readonly UsageAnalyzer _usageAnalyzer;
        private readonly PredictiveOptimizer _predictiveOptimizer;
        private readonly PerformancePredictor _performancePredictor;
        private readonly SmartResourceAllocator _resourceAllocator;
        private readonly IAIHubScriptAdapter _scriptAdapter;
        private readonly List<AIHubUnit> _units;

        public AIHubFleetService(IRouter? router = null, IAILearningCoordinator? learningCoordinator = null, UsageAnalyzer? usageAnalyzer = null, PredictiveOptimizer? predictiveOptimizer = null, PerformancePredictor? performancePredictor = null, SmartResourceAllocator? resourceAllocator = null, IAIHubScriptAdapter? scriptAdapter = null)
        {
            _router = router ?? new InMemoryAIHubRouter();
            _learningCoordinator = learningCoordinator ?? new AILearningCoordinator();
            _usageAnalyzer = usageAnalyzer ?? new UsageAnalyzer();
            _predictiveOptimizer = predictiveOptimizer ?? new PredictiveOptimizer();
            _performancePredictor = performancePredictor ?? new PerformancePredictor();
            _resourceAllocator = resourceAllocator ?? new SmartResourceAllocator();
            _scriptAdapter = scriptAdapter ?? new NoOpAIHubScriptAdapter();
            _units = CreateDefaultUnits();
            foreach (var unit in _units) _router.RegisterAgent(new AIHubUnitAgentAdapter(unit), unit.Capabilities.Concat(new[] { unit.MissionFamily }).ToArray());
        }

        public IReadOnlyList<AIHubUnit> Units => _units;
        public Task<string> InvokeScriptAdapterAsync(string operation, IReadOnlyDictionary<string, object> parameters, CancellationToken cancellationToken = default) => _scriptAdapter.InvokeAsync(operation, parameters, cancellationToken);

        public async Task<FleetTrainingResult> TrainUnitsAsync(CancellationToken cancellationToken = default)
        {
            var started = await _learningCoordinator.StartLearningAsync();
            var models = new List<AIModel>();
            foreach (var unit in _units)
            {
                var data = unit.Capabilities.Select((_, i) => new TrainingData { Features = new Dictionary<string, double> { ["reliability"] = unit.ReliabilityScore, ["load"] = unit.ExpectedLoad, ["capabilityIndex"] = i }, Label = unit.ReliabilityScore }).ToList();
                var model = await _learningCoordinator.TrainModelAsync($"aihub-{unit.UnitId}", data);
                await _learningCoordinator.DeployModelAsync(model.Name);
                models.Add(model);
            }
            return new FleetTrainingResult { LearningStarted = started, Models = models };
        }

        public async Task<FleetRecommendation> RecommendFleetAsync(string mission, int maxUnits = 3, CancellationToken cancellationToken = default)
        {
            var routing = await _router.RouteToMultipleAsync(new AgentRoutingRequest { RequiredTags = new[] { mission }, RoutingHints = new Dictionary<string, object> { ["mission"] = mission } }, maxUnits, cancellationToken);
            var units = routing.SelectedAgents.Select(a => ((AIHubUnitAgentAdapter)a.Agent).Unit).ToArray();
            SeedAnalytics(mission, units);
            var optimizerTips = await _predictiveOptimizer.AnalyzeAndRecommend();
            await _performancePredictor.TrainModels();
            var predictions = _performancePredictor.PredictNext();
            var allocation = await _resourceAllocator.AllocateResources(units.DefaultIfEmpty().Average(u => u?.ExpectedLoad ?? 0.5), mission);
            var balanceTips = optimizerTips.Select(t => t.Description).Append($"Allocate {allocation.ThreadPoolSize} worker threads and {allocation.CacheSize / (1024 * 1024)} MB cache for {mission}.").ToArray();
            var missionRecommendations = units.Select(u => $"Use {u.Name} for {mission} via router-selected capabilities: {string.Join(", ", u.Capabilities)}.").Concat(predictions.Select(p => $"{p.MetricName}: {p.Status} ({p.Confidence:P0} confidence)." )).ToArray();
            return new FleetRecommendation { Mission = mission, Units = units, BalanceTips = balanceTips, MissionRecommendations = missionRecommendations };
        }

        private void SeedAnalytics(string mission, IReadOnlyList<AIHubUnit> units)
        {
            for (var i = 0; i < 12; i++)
            {
                var load = Math.Clamp(units.DefaultIfEmpty().Average(u => u?.ExpectedLoad ?? 0.5) + (i % 3 * 0.1), 0, 1);
                _usageAnalyzer.RecordUsage(new UsageAnalyzer.UsageEvent { FeatureName = mission, Success = true, DurationMs = 40 + i, Timestamp = DateTime.UtcNow.AddMinutes(-i) });
                _predictiveOptimizer.RecordMetric(new PredictiveOptimizer.PerformanceMetric { Timestamp = DateTime.UtcNow.AddMinutes(-i), CpuUsage = load, MemoryUsage = load, DiskIo = 0.6, ThreadCount = 64, RequestCount = 100 + i, AverageResponseTime = 120 + i });
                _performancePredictor.RecordSnapshot(new PerformancePredictor.PerformanceSnapshot { Timestamp = DateTime.UtcNow.AddMinutes(-i), CpuUsage = load, MemoryUsage = load, DiskIoUsage = 0.4, NetworkUsage = 0.4, AverageResponseTime = 120 + i, ThroughputRps = 100 + i, ErrorCount = 0 });
            }
            _resourceAllocator.UpdateResourceMetrics(0.65, 0.6, 8);
        }

        private static List<AIHubUnit> CreateDefaultUnits() => new()
        {
            new AIHubUnit { UnitId = "azure", Name = "Azure Integration Specialist", MissionFamily = "azure", Capabilities = new[] { "azure", "cloud", "infrastructure" }, ExpectedLoad = 0.7 },
            new AIHubUnit { UnitId = "docker", Name = "Docker Container Specialist", MissionFamily = "docker", Capabilities = new[] { "docker", "containers", "backend" }, ExpectedLoad = 0.6 },
            new AIHubUnit { UnitId = "github", Name = "GitHub Automation Specialist", MissionFamily = "github", Capabilities = new[] { "github", "repository", "ci" }, ExpectedLoad = 0.5 },
            new AIHubUnit { UnitId = "security", Name = "Security Optimization Specialist", MissionFamily = "security", Capabilities = new[] { "security", "compliance", "threat" }, ExpectedLoad = 0.8 },
            new AIHubUnit { UnitId = "analytics", Name = "F# Analytics Specialist", MissionFamily = "analytics", Capabilities = new[] { "analytics", "prediction", "learning" }, ExpectedLoad = 0.7 },
            new AIHubUnit { UnitId = "frontend", Name = "Frontend WinUI Specialist", MissionFamily = "frontend", Capabilities = new[] { "frontend", "winui", "ui" }, ExpectedLoad = 0.4 },
            new AIHubUnit { UnitId = "backend", Name = "C++ Backend Performance Specialist", MissionFamily = "backend", Capabilities = new[] { "backend", "performance", "services" }, ExpectedLoad = 0.75 }
        };
    }
}
