from __future__ import annotations

from dataclasses import asdict, dataclass
import json
from pathlib import Path


@dataclass
class ModelProfile:
    name: str
    family: str
    objective: str
    cadence_minutes: int
    backend: str


class MLRegistry:
    def __init__(self, path: str = "artifacts/aihub/ml-registry.json"):
        self.path = Path(path)
        self.path.parent.mkdir(parents=True, exist_ok=True)
        self.profiles: list[ModelProfile] = []

    def seed_default(self) -> None:
        self.profiles = [
            ModelProfile("contextual-bandit-router", "bandit", "model routing", 5, "python"),
            ModelProfile("autoencoder-shape-guard", "autoencoder", "shape compression + anomaly", 15, "python"),
            ModelProfile("drift-detector", "statistical", "feature drift", 10, "python"),
            ModelProfile("security-anomaly-core", "heuristic", "runtime anomaly security", 2, "cpp"),
            ModelProfile("integration-policy-host", "rules", "cross-service policy", 5, "csharp"),
            ModelProfile("gaussian-regression", "regression", "continuous signal fitting", 10, "python"),
            ModelProfile("linear-regression", "regression", "fast baseline predictions", 10, "python"),
            ModelProfile("knaa-routing", "mesh", "adaptive neighborhood routing", 4, "python"),
            ModelProfile("gnaa-graph-attention", "graph", "graph attention for fleet routing", 8, "python"),
            ModelProfile("rnaa-recurrent-anomaly", "sequence", "recurrent anomaly trend detection", 6, "python"),
            ModelProfile("chaos-engine", "exploration", "stochastic search + exploration", 7, "python"),
            ModelProfile("natural-selection-engine", "evolutionary", "candidate survival optimization", 7, "python"),
            ModelProfile("bayesian-optimizer", "optimization", "hyperparameter optimization", 9, "python"),
            ModelProfile("memory-pressure-optimizer", "optimization", "low-memory pressure controls", 3, "cpp"),
            ModelProfile("mesh-consensus-engine", "mesh", "cross-fleet consensus updates", 5, "csharp"),
        ]

    def save(self) -> None:
        self.path.write_text(json.dumps([asdict(p) for p in self.profiles], indent=2), encoding="utf-8")
