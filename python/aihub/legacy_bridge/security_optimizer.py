from __future__ import annotations

from dataclasses import dataclass


@dataclass
class SecurityOptimizationPlan:
    profile: str
    cpu_policy: str
    memory_policy: str
    egress_policy: str
    training_policy: str


def build_plan(profile: str = "balanced") -> SecurityOptimizationPlan:
    if profile == "paranoid":
        return SecurityOptimizationPlan(
            profile="paranoid",
            cpu_policy="bounded-high-priority",
            memory_policy="guarded",
            egress_policy="strict-allowlist",
            training_policy="signed-artifacts-only",
        )
    return SecurityOptimizationPlan(
        profile="balanced",
        cpu_policy="adaptive-priority",
        memory_policy="high-throughput-guarded",
        egress_policy="smart-allowlist",
        training_policy="checkpoint+drift-guard",
    )
