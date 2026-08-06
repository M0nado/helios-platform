#!/usr/bin/env python3
"""Development-only reliability measurement and controlled fault harness."""

from __future__ import annotations

import argparse
import json
import random
import statistics
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Callable, Iterable, Mapping


SAFE_ENVIRONMENTS = frozenset({"local", "development", "test"})
FAILURE_TARGETS = frozenset(
    {"provider", "worker", "queue", "storage", "identity", "network", "deployment"}
)
OUTCOMES = frozenset({"healthy", "degraded", "outage"})


@dataclass(frozen=True)
class OperationObservation:
    operation_id: str
    outcome: str
    latency_ms: float
    errors: int = 0
    retries: int = 0
    dead_letters: int = 0
    queue_age_seconds: float = 0
    cancellation_attempted: bool = False
    cancellation_succeeded: bool = False
    restart_attempted: bool = False
    restart_succeeded: bool = False
    detection_seconds: float | None = None
    recovery_seconds: float | None = None
    rollback_attempted: bool = False
    rollback_succeeded: bool = False
    restore_attempted: bool = False
    restore_succeeded: bool = False
    expected_cleanup_resources: tuple[str, ...] = ()
    cleaned_resources: tuple[str, ...] = ()

    def __post_init__(self) -> None:
        if self.outcome not in OUTCOMES:
            raise ValueError(f"outcome must be one of {sorted(OUTCOMES)}")
        numeric = (
            self.latency_ms,
            self.queue_age_seconds,
            self.errors,
            self.retries,
            self.dead_letters,
            *(value for value in (self.detection_seconds, self.recovery_seconds) if value is not None),
        )
        if any(value < 0 for value in numeric):
            raise ValueError("metric values cannot be negative")
        # Reject non-finite and negative timing metrics.
        #
        # When telemetry contains NaN/infinite latency or a negative detection or
        # recovery duration, the observation can pass validation and emit invalid
        # evidence or non-standard JSON tokens (for example NaN).

def _rate(successes: int, attempts: int) -> float | None:
    return successes / attempts if attempts else None


def summarize(observations: Iterable[OperationObservation]) -> dict[str, Any]:
    """Produce explicitly separated service, recovery, and outcome metrics."""
    items = list(observations)
    if not items:
        raise ValueError("at least one observation is required")
    total = len(items)
    counts = {outcome: sum(item.outcome == outcome for item in items) for outcome in OUTCOMES}
    expected = set().union(*(set(item.expected_cleanup_resources) for item in items))
    cleaned = set().union(*(set(item.cleaned_resources) for item in items))

    def attempts(name: str) -> list[OperationObservation]:
        return [item for item in items if getattr(item, f"{name}_attempted")]

    def success_rate(name: str) -> float | None:
        attempted = attempts(name)
        return _rate(sum(getattr(item, f"{name}_succeeded") for item in attempted), len(attempted))

    return {
        "sampleCount": total,
        "availability": counts["healthy"] / total,
        "successfulServiceRate": (counts["healthy"] + counts["degraded"]) / total,
        "degradedRate": counts["degraded"] / total,
        "outageRate": counts["outage"] / total,
        "latencyMs": {
            "mean": statistics.fmean(item.latency_ms for item in items),
            "max": max(item.latency_ms for item in items),
        },
        "errorRate": sum(item.errors for item in items) / total,
        "retryRate": sum(item.retries for item in items) / total,
        "deadLetterCount": sum(item.dead_letters for item in items),
        "queueAgeSeconds": {
            "mean": statistics.fmean(item.queue_age_seconds for item in items),
            "max": max(item.queue_age_seconds for item in items),
        },
        "cancellationSuccess": success_rate("cancellation"),
        "restartSuccess": success_rate("restart"),
        "meanTimeToDetectSeconds": _optional_mean(item.detection_seconds for item in items),
        "meanTimeToRecoverSeconds": _optional_mean(item.recovery_seconds for item in items),
        "rollbackSuccess": success_rate("rollback"),
        "restoreSuccess": success_rate("restore"),
        "cleanupCompleteness": _rate(len(expected & cleaned), len(expected)),
        "orphanedResources": sorted(expected - cleaned),
    }


def _optional_mean(values: Iterable[float | None]) -> float | None:
    present = [value for value in values if value is not None]
    return statistics.fmean(present) if present else None


def burn_rate(error_events: int, total_events: int, slo_target: float) -> float:
    """Return consumed error budget; >1 means the SLO budget is burning too fast."""
    if total_events <= 0 or not 0 < slo_target < 1:
        raise ValueError("total_events must be positive and slo_target must be between 0 and 1")
    return (error_events / total_events) / (1 - slo_target)


def prioritize_burn_rates(windows: Mapping[str, float]) -> dict[str, Any]:
    """Classify multi-window burn evidence using standard fast/slow thresholds."""
    required_windows = {"5m", "30m", "1h", "6h"}
    missing_windows = required_windows.difference(windows)
    if missing_windows:
        raise ValueError(f"missing burn-rate windows: {sorted(missing_windows)}")
    fast = windows["1h"] >= 14.4 and windows["5m"] >= 14.4
    slow = windows["6h"] >= 6 and windows["30m"] >= 6
    priority = "critical" if fast else "high" if slow else "normal"
    return {"priority": priority, "fastBurn": fast, "slowBurn": slow, "windows": dict(windows)}


@dataclass(frozen=True)
class FailureScenario:
    scenario_id: str
    target: str
    failure: str
    probability: float = 1.0
    max_injections: int = 1

    def __post_init__(self) -> None:
        if self.target not in FAILURE_TARGETS:
            raise ValueError(f"target must be one of {sorted(FAILURE_TARGETS)}")
        if (
            not 0 <= self.probability <= 1
            or isinstance(self.max_injections, bool)
            or not isinstance(self.max_injections, int)
            or self.max_injections < 1
        ):
            raise ValueError(
                "probability must be between 0 and 1 and max_injections must be a positive integer"
            )


class ControlledFailureInjector:
    """Fail-closed injector that cannot run against staging or production."""

    def __init__(self, environment: str, seed: int = 0) -> None:
        if environment not in SAFE_ENVIRONMENTS:
            raise PermissionError("controlled failures are restricted to local/development/test")
        self.environment = environment
        self._random = random.Random(seed)
        self._counts: dict[str, int] = {}

    def run(self, scenario: FailureScenario, operation: Callable[[], Any]) -> Any:
        count = self._counts.get(scenario.scenario_id, 0)
        inject = count < scenario.max_injections and self._random.random() < scenario.probability
        if inject:
            self._counts[scenario.scenario_id] = count + 1
            raise ControlledFailure(scenario)
        return operation()


class ControlledFailure(RuntimeError):
    def __init__(self, scenario: FailureScenario) -> None:
        self.scenario = scenario
        super().__init__(f"controlled {scenario.target} failure: {scenario.failure}")


def assess_repeated_failure_evidence(records: Iterable[Mapping[str, Any]]) -> dict[str, Any]:
    """Require replay evidence to prove effects are unique and cleanup is complete."""
    rows = list(records)
    effect_ids = [str(row["durableEffectId"]) for row in rows if row.get("durableEffectId")]
    creations = [row for row in rows if row.get("effectCreated") is True]
    orphan_ids = sorted({str(value) for row in rows for value in row.get("orphanedResources", [])})
    repeated = len(rows) >= 2
    passed = repeated and len(set(effect_ids)) == 1 and len(creations) <= 1 and not orphan_ids
    return {
        "passed": passed,
        "attemptCount": len(rows),
        "uniqueEffectCount": len(set(effect_ids)),
        "effectCreationCount": len(creations),
        "durableEffectIds": sorted(set(effect_ids)),
        "orphanedResources": orphan_ids,
        "requirements": {
            "minimumAttempts": 2,
            "exactlyOneDurableEffect": True,
            "maximumEffectCreations": 1,
            "zeroOrphans": True,
        },
    }


def load_scenarios(path: Path) -> list[FailureScenario]:
    document = json.loads(path.read_text(encoding="utf-8"))
    if document.get("environment") not in SAFE_ENVIRONMENTS:
        raise PermissionError("scenario manifest must be restricted to a safe environment")
    return [FailureScenario(**item) for item in document["scenarios"]]


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("observations", type=Path, help="JSON array of observation objects")
    parser.add_argument("--slo", type=float, default=0.999)
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()
    if not 0 < args.slo < 1:
        parser.error("--slo must be between 0 and 1")
    observations = [OperationObservation(**item) for item in json.loads(args.observations.read_text())]
    report = summarize(observations)
    report.update({"sloTarget": args.slo, "generatedAt": datetime.now(timezone.utc).isoformat()})
    rendered = json.dumps(report, indent=2, sort_keys=True)
    if args.output:
        args.output.write_text(rendered + "\n", encoding="utf-8")
    else:
        print(rendered)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
