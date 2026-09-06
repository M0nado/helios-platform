from __future__ import annotations

from dataclasses import asdict, dataclass
from typing import Any


@dataclass(frozen=True, slots=True)
class SecurityOptimizationPlan:
    profile: str
    cpu_policy: str
    memory_policy: str
    egress_policy: str
    training_policy: str


@dataclass(frozen=True, slots=True)
class VMTarget:
    backend: str
    role: str
    gpu: bool
    memory_gb: int
    mutation_authority: str = "proposal-only"


@dataclass(frozen=True, slots=True)
class ModelProfile:
    name: str
    family: str
    objective: str
    cadence_minutes: int
    backend: str
    production_enabled: bool = False


@dataclass(frozen=True, slots=True)
class EngineSpec:
    name: str
    family: str
    tier: str
    backend: str
    supports_cuda: bool
    requires_cuda: bool
    objective: str
    memory_class: str
    production_enabled: bool = False


def build_security_plan(profile: str = "balanced") -> SecurityOptimizationPlan:
    normalized = profile.strip().lower()
    plans = {
        "balanced": SecurityOptimizationPlan(
            profile="balanced",
            cpu_policy="adaptive-priority",
            memory_policy="high-throughput-guarded",
            egress_policy="smart-allowlist",
            training_policy="checkpoint+drift-guard",
        ),
        "paranoid": SecurityOptimizationPlan(
            profile="paranoid",
            cpu_policy="bounded-high-priority",
            memory_policy="guarded",
            egress_policy="strict-allowlist",
            training_policy="signed-artifacts-only",
        ),
        "offline": SecurityOptimizationPlan(
            profile="offline",
            cpu_policy="bounded-local",
            memory_policy="guarded",
            egress_policy="deny",
            training_policy="local-signed-artifacts-only",
        ),
    }
    try:
        return plans[normalized]
    except KeyError as exc:
        raise ValueError("profile must be balanced, paranoid, or offline") from exc


def build_vm_topology() -> list[VMTarget]:
    return [
        VMTarget("docker", "gateway+api", True, 8),
        VMTarget("docker", "gui+control", False, 4),
        VMTarget("wsl2", "trainer", True, 16),
        VMTarget("hyperv", "security-isolation", False, 4),
    ]


def build_model_registry() -> list[ModelProfile]:
    return [
        ModelProfile("contextual-bandit-router", "bandit", "model routing", 5, "python"),
        ModelProfile("autoencoder-shape-guard", "autoencoder", "shape compression and anomaly", 15, "python"),
        ModelProfile("drift-detector", "statistical", "feature drift", 10, "python"),
        ModelProfile("security-anomaly-core", "heuristic", "runtime anomaly security", 2, "cpp"),
        ModelProfile("integration-policy-host", "rules", "cross-service policy", 5, "csharp"),
        ModelProfile("gaussian-regression", "regression", "continuous signal fitting", 10, "python"),
        ModelProfile("linear-regression", "regression", "fast baseline predictions", 10, "python"),
        ModelProfile("knaa-routing", "mesh", "adaptive neighborhood routing", 4, "python"),
        ModelProfile("gnaa-graph-attention", "graph", "graph attention for fleet routing", 8, "python"),
        ModelProfile("rnaa-recurrent-anomaly", "sequence", "recurrent anomaly trend detection", 6, "python"),
        ModelProfile("chaos-engine", "exploration", "controlled stochastic search", 7, "python"),
        ModelProfile("natural-selection-engine", "evolutionary", "candidate survival optimization", 7, "python"),
        ModelProfile("bayesian-optimizer", "optimization", "hyperparameter optimization", 9, "python"),
        ModelProfile("memory-pressure-optimizer", "optimization", "low-memory pressure controls", 3, "cpp"),
        ModelProfile("mesh-consensus-engine", "mesh", "cross-fleet consensus updates", 5, "csharp"),
    ]


def _engine_specs() -> list[EngineSpec]:
    return [
        EngineSpec("gaussian-regression", "regression", "core", "python", True, False, "continuous signal fitting", "medium"),
        EngineSpec("linear-regression", "regression", "core", "python", True, False, "baseline predictive fit", "low"),
        EngineSpec("ridge-regression", "regression", "core", "python", True, False, "regularized trend learning", "low"),
        EngineSpec("lasso-regression", "regression", "core", "python", True, False, "sparse feature selection", "low"),
        EngineSpec("elasticnet-regression", "regression", "core", "python", True, False, "hybrid sparse regularization", "low"),
        EngineSpec("extreme-calculus-solver", "symbolic", "advanced", "csharp", False, False, "differential optimization", "medium"),
        EngineSpec("geometry-topology-optimizer", "geometry", "advanced", "csharp", False, False, "spatial route shaping", "medium"),
        EngineSpec("gaussian-blur-anomaly", "vision", "support", "cpp", True, False, "noise suppression and anomaly preparation", "low"),
        EngineSpec("knn-clustering", "clustering", "core", "python", True, False, "nearest-neighbor grouping", "medium"),
        EngineSpec("knaa-routing", "mesh", "advanced", "python", True, False, "k-neighbor adaptive allocation", "high"),
        EngineSpec("gnaa-graph-attention", "graph", "advanced", "python", True, True, "graph neural attention policy", "high"),
        EngineSpec("rnaa-recurrent-anomaly", "sequence", "advanced", "python", True, False, "temporal anomaly scoring", "high"),
        EngineSpec("rnn-sequence-predictor", "sequence", "core", "python", True, False, "sequence forecasting", "high"),
        EngineSpec("transformer-router", "routing", "advanced", "python", True, True, "LLM route selection", "high"),
        EngineSpec("contextual-bandit-router", "routing", "core", "python", True, False, "online model selection", "low"),
        EngineSpec("autoencoder-shape-compressor", "compression", "core", "python", True, False, "latent compression", "medium"),
        EngineSpec("variational-autoencoder", "compression", "advanced", "python", True, True, "distributional latent spaces", "high"),
        EngineSpec("pca-fast-compressor", "compression", "support", "cpp", False, False, "low-memory dimension reduction", "low"),
        EngineSpec("drift-detector", "governance", "core", "python", False, False, "data drift guardrails", "low"),
        EngineSpec("chaos-engine", "exploration", "experimental", "python", True, False, "controlled stochastic exploration", "medium"),
        EngineSpec("natural-selection-engine", "evolutionary", "advanced", "python", True, False, "candidate survival optimization", "medium"),
        EngineSpec("genetic-policy-search", "evolutionary", "advanced", "python", True, False, "policy mutation and crossover", "medium"),
        EngineSpec("bayesian-optimizer", "optimization", "core", "python", True, False, "hyperparameter optimization", "medium"),
        EngineSpec("simulated-annealing", "optimization", "support", "cpp", False, False, "global objective search", "low"),
        EngineSpec("multi-armed-bandit-swarm", "routing", "advanced", "python", True, False, "swarm routing policy", "medium"),
        EngineSpec("mesh-consensus-engine", "mesh", "advanced", "csharp", False, False, "fleet consensus propagation", "medium"),
        EngineSpec("sql-pattern-miner", "analytics", "core", "python", False, False, "training signal extraction", "low"),
        EngineSpec("vector-retrieval-ranker", "retrieval", "core", "python", True, False, "semantic ranking", "medium"),
        EngineSpec("security-anomaly-core", "security", "core", "cpp", False, False, "runtime threat scoring", "low"),
        EngineSpec("memory-pressure-optimizer", "optimization", "support", "cpp", False, False, "memory pressure control", "low"),
    ]


def build_engine_catalog(cuda_enabled: bool = True) -> dict[str, Any]:
    specs = [spec for spec in _engine_specs() if cuda_enabled or not spec.requires_cuda]
    engines = [asdict(spec) for spec in specs]
    by_family: dict[str, int] = {}
    by_backend: dict[str, int] = {}
    for engine in engines:
        by_family[engine["family"]] = by_family.get(engine["family"], 0) + 1
        by_backend[engine["backend"]] = by_backend.get(engine["backend"], 0) + 1
    return {
        "totalEngines": len(engines),
        "cudaEnabled": cuda_enabled,
        "families": by_family,
        "backends": by_backend,
        "majorParallelizationTypes": [
            "task-parallel",
            "data-parallel",
            "pipeline-parallel",
            "tensor-parallel",
            "model-parallel",
            "fleet-swarm-parallel",
            "multi-llm-routing-parallel",
            "subagent-specialist-parallel",
            "hybrid-mesh-parallel",
            "async-event-parallel",
        ],
        "hybridizationStrategies": [
            "chaos+bandit",
            "natural-selection+bayesian",
            "graph-attention+mesh-consensus",
            "autoencoder+vector-retrieval",
            "security-anomaly+drift-detector",
        ],
        "engines": engines,
        "productionEnabled": False,
    }


def recommend_engine_mix(
    *,
    cuda_enabled: bool,
    security_profile: str,
    optimization_pressure: float,
    fleet_size: int,
) -> dict[str, Any]:
    if not 0.0 <= optimization_pressure <= 1.0:
        raise ValueError("optimization_pressure must be between 0 and 1")
    if not 0 <= fleet_size <= 100_000:
        raise ValueError("fleet_size must be between 0 and 100000")
    normalized_security = security_profile.strip().lower()
    if normalized_security not in {"balanced", "paranoid", "offline"}:
        raise ValueError("security_profile must be balanced, paranoid, or offline")

    catalog = build_engine_catalog(cuda_enabled=cuda_enabled)
    engines: list[dict[str, Any]] = catalog["engines"]
    selected: list[dict[str, Any]] = []

    for family in ("routing", "security", "optimization", "compression", "analytics", "retrieval"):
        selected.extend(engine for engine in engines if engine["family"] == family and engine["tier"] == "core")
        if not any(engine["family"] == family for engine in selected):
            first = next((engine for engine in engines if engine["family"] == family), None)
            if first is not None:
                selected.append(first)

    if optimization_pressure >= 0.7:
        selected.extend(
            engine
            for engine in engines
            if engine["name"] in {
                "chaos-engine",
                "natural-selection-engine",
                "bayesian-optimizer",
            }
        )
    if normalized_security in {"paranoid", "offline"}:
        selected.extend(engine for engine in engines if engine["family"] in {"security", "governance"})
    if fleet_size >= 200:
        selected.extend(
            engine
            for engine in engines
            if engine["name"] in {
                "mesh-consensus-engine",
                "multi-armed-bandit-swarm",
                "gnaa-graph-attention",
            }
        )

    unique = {engine["name"]: engine for engine in selected}
    high_memory = sum(engine["memory_class"] == "high" for engine in unique.values())
    memory_score = max(40.0, round(100.0 - (high_memory * 1.8), 2))
    return {
        "selectedCount": len(unique),
        "selectedEngines": list(unique.values()),
        "expectedMemoryEfficiencyScore": memory_score,
        "majorParallelizationTypes": catalog["majorParallelizationTypes"],
        "hybridizationStrategies": catalog["hybridizationStrategies"],
        "securityProfile": normalized_security,
        "optimizationPressure": optimization_pressure,
        "fleetSize": fleet_size,
        "proposalOnly": True,
        "productionEnabled": False,
    }


def serialize_catalog_bundle(*, cuda_enabled: bool = True) -> dict[str, Any]:
    return {
        "securityPlans": {
            name: asdict(build_security_plan(name))
            for name in ("balanced", "paranoid", "offline")
        },
        "vmTopology": [asdict(target) for target in build_vm_topology()],
        "modelRegistry": [asdict(profile) for profile in build_model_registry()],
        "engineCatalog": build_engine_catalog(cuda_enabled=cuda_enabled),
        "productionEnabled": False,
    }
