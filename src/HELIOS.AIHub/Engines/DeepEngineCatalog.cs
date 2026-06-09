namespace HELIOS.AIHub.Engines;

/// <summary>
/// In-memory seed catalog for local X-Tier deep-engine artifacts.
/// </summary>
public sealed class DeepEngineCatalog
{
    private readonly IReadOnlyList<DeepEngineDefinition> _engines;

    public DeepEngineCatalog(IEnumerable<DeepEngineDefinition>? engines = null)
    {
        _engines = (engines ?? CreateSeedCatalog()).ToArray();
    }

    public IReadOnlyList<DeepEngineDefinition> GetEngines() => _engines;

    public DeepEngineDefinition? FindById(string engineId) =>
        _engines.FirstOrDefault(engine => string.Equals(engine.Id, engineId, StringComparison.OrdinalIgnoreCase));

    public static IReadOnlyList<DeepEngineDefinition> CreateSeedCatalog() =>
    [
        new("regression-core", "Regression Forecast Core", "regression", ["forecasting", "resource-prediction", "trend-analysis"], ["task", "data", "pipeline"], ["linear-regression", "gradient-boosting"], ["natural selection + bayesian hybrid strategy"]),
        new("symbolic-policy", "Symbolic Policy Host", "governance", ["policy", "rules", "explainability"], ["task", "async-event"], ["rules-engine", "constraint-solver"], ["security anomaly + drift detector hybrid strategy"]),
        new("geometry-optimizer", "Geometry Optimization Engine", "geometry", ["layout", "pathing", "spatial-optimization"], ["tensor", "pipeline"], ["mesh-analysis", "spatial-index"], ["graph attention + mesh consensus hybrid strategy"]),
        new("vision-guard", "Vision Guard Engine", "vision", ["image-inspection", "visual-anomaly", "ui-analysis"], ["tensor", "model"], ["cnn", "transformer-vision"], ["autoencoder + vector retrieval hybrid strategy"]),
        new("cluster-mesh", "Cluster Mesh Consensus", "mesh", ["fleet-consensus", "clustering", "topology"], ["fleet", "hybrid-mesh", "async-event"], ["k-means", "mesh-consensus"], ["graph attention + mesh consensus hybrid strategy"]),
        new("graph-attention", "Graph Attention Analyzer", "graph", ["dependency-analysis", "attack-paths", "fleet-graph"], ["model", "tensor", "hybrid-mesh"], ["gat", "graph-search"], ["graph attention + mesh consensus hybrid strategy"]),
        new("sequence-anomaly", "Recurrent Sequence Anomaly", "sequence", ["time-series", "security-events", "telemetry"], ["data", "model", "pipeline"], ["lstm", "temporal-autoencoder"], ["security anomaly + drift detector hybrid strategy"]),
        new("bandit-router", "Contextual Bandit Router", "routing", ["agent-routing", "model-selection", "online-learning"], ["task", "multi-LLM", "subagent"], ["epsilon-greedy", "ucb"], ["chaos + bandit hybrid strategy"]),
        new("compression-core", "Compression and Distillation Core", "compression", ["artifact-compression", "model-distillation", "memory-reduction"], ["data", "model", "tensor"], ["quantization", "distillation"], ["autoencoder + vector retrieval hybrid strategy"]),
        new("chaos-explorer", "Chaos Exploration Optimizer", "exploration", ["robustness", "fault-injection", "search"], ["task", "pipeline", "async-event"], ["chaos-testing", "evolutionary-search"], ["chaos + bandit hybrid strategy"]),
        new("bayes-tuner", "Bayesian Optimization Tuner", "optimization", ["hyperparameter-tuning", "cost-optimization", "planning"], ["task", "model", "pipeline"], ["bayesian-optimization", "surrogate-model"], ["natural selection + bayesian hybrid strategy"]),
        new("retrieval-memory", "Vector Retrieval Memory", "retrieval", ["memory-search", "semantic-recall", "grounding"], ["data", "multi-LLM", "async-event"], ["vector-search", "hybrid-rerank"], ["autoencoder + vector retrieval hybrid strategy"]),
        new("security-anomaly", "Security Anomaly Core", "security", ["threat-detection", "drift-guard", "egress-policy"], ["fleet", "async-event", "hybrid-mesh"], ["statistical-drift", "heuristic-anomaly"], ["security anomaly + drift detector hybrid strategy"]),
        new("memory-pressure", "Memory Pressure Optimizer", "memory-pressure", ["resource-control", "cache-sizing", "pressure-prediction"], ["data", "pipeline", "fleet"], ["pressure-model", "adaptive-cache"], ["natural selection + bayesian hybrid strategy"])
    ];
}

public sealed record DeepEngineDefinition(
    string Id,
    string Name,
    string Family,
    IReadOnlyList<string> Capabilities,
    IReadOnlyList<string> ParallelizationTypes,
    IReadOnlyList<string> SpecializationTools,
    IReadOnlyList<string> HybridizationStrategies);
