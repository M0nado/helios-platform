from __future__ import annotations

import argparse
from dataclasses import asdict, dataclass
from datetime import datetime, timezone
import hashlib
import json
import os
from pathlib import Path
import random
import tempfile
from typing import Any

from .canonical_catalog import build_security_plan


_ALLOWED_SKILLS = (
    "routing",
    "security",
    "retrieval",
    "compression",
    "optimization",
    "observability",
)
_ALLOWED_NODES = (
    "node-01-inference",
    "node-02-feature-engineering",
    "node-03-training-sandbox",
)


@dataclass(frozen=True, slots=True)
class TrainingTaskProposal:
    task_id: str
    ordinal: int
    skill: str
    difficulty: float
    route_candidates: tuple[str, ...]
    evaluation: dict[str, float]
    execution_authority: str = "none"
    proposal_only: bool = True
    production_enabled: bool = False


def _task_id(*, seed: int, ordinal: int, skill: str, difficulty: float) -> str:
    material = f"{seed}:{ordinal}:{skill}:{difficulty:.4f}".encode("utf-8")
    return hashlib.sha256(material).hexdigest()[:20]


def build_training_proposal(
    *,
    cycles: int = 1,
    seed: int = 7,
    security_profile: str = "balanced",
) -> dict[str, Any]:
    if not 1 <= cycles <= 100:
        raise ValueError("cycles must be between 1 and 100")
    if not -(2**31) <= seed <= (2**31 - 1):
        raise ValueError("seed must fit in a signed 32-bit integer")

    security = build_security_plan(security_profile)
    randomizer = random.Random(seed)
    tasks: list[TrainingTaskProposal] = []
    for ordinal in range(1, cycles + 1):
        skill = _ALLOWED_SKILLS[(ordinal - 1) % len(_ALLOWED_SKILLS)]
        difficulty = round(0.35 + (randomizer.random() * 0.45), 4)
        quality_floor = round(max(0.5, 0.82 - (difficulty * 0.2)), 4)
        latency_budget_ms = float(round(120 + difficulty * 180))
        tasks.append(
            TrainingTaskProposal(
                task_id=_task_id(
                    seed=seed,
                    ordinal=ordinal,
                    skill=skill,
                    difficulty=difficulty,
                ),
                ordinal=ordinal,
                skill=skill,
                difficulty=difficulty,
                route_candidates=_ALLOWED_NODES,
                evaluation={
                    "minimumQuality": quality_floor,
                    "maximumLatencyMs": latency_budget_ms,
                    "maximumSecurityRisk": 0.1,
                },
            )
        )

    return {
        "schemaVersion": 1,
        "generatedUtc": datetime.now(timezone.utc).isoformat(),
        "seed": seed,
        "cycles": cycles,
        "securityPlan": asdict(security),
        "tasks": [asdict(task) for task in tasks],
        "executionMode": "proposal-only",
        "automaticExecution": False,
        "externalNetworkExecution": False,
        "privilegedExecution": False,
        "productionEnabled": False,
    }


def write_proposal(path: Path, payload: dict[str, Any]) -> str:
    destination = Path(path)
    destination.parent.mkdir(parents=True, exist_ok=True)
    encoded = (json.dumps(payload, indent=2, sort_keys=True) + "\n").encode("utf-8")
    descriptor, temp_name = tempfile.mkstemp(
        prefix=f".{destination.name}.",
        suffix=".tmp",
        dir=destination.parent,
    )
    temp_path = Path(temp_name)
    try:
        with os.fdopen(descriptor, "wb") as handle:
            handle.write(encoded)
            handle.flush()
            os.fsync(handle.fileno())
        os.replace(temp_path, destination)
    finally:
        temp_path.unlink(missing_ok=True)
    return hashlib.sha256(encoded).hexdigest()


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Generate a deterministic HELIOS Hermes/XCore training proposal."
    )
    parser.add_argument("--cycles", type=int, default=1)
    parser.add_argument("--seed", type=int, default=7)
    parser.add_argument(
        "--security-profile",
        choices=("balanced", "paranoid", "offline"),
        default="balanced",
    )
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()

    proposal = build_training_proposal(
        cycles=args.cycles,
        seed=args.seed,
        security_profile=args.security_profile,
    )
    result: dict[str, Any] = {"proposal": proposal}
    if args.output is not None:
        result["output"] = str(args.output)
        result["sha256"] = write_proposal(args.output, proposal)
    print(json.dumps(result, indent=2, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
