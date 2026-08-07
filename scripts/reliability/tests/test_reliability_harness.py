from __future__ import annotations

import sys
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / "scripts" / "reliability"))

from reliability_harness import (  # noqa: E402
    ControlledFailure,
    ControlledFailureInjector,
    FailureScenario,
    OperationObservation,
    assess_repeated_failure_evidence,
    burn_rate,
    load_scenarios,
    prioritize_burn_rates,
    summarize,
)


class ReliabilityHarnessTests(unittest.TestCase):
    def test_summary_records_required_metrics_and_separates_degradation(self) -> None:
        report = summarize(
            [
                OperationObservation("one", "healthy", 10),
                OperationObservation(
                    "two", "degraded", 30, errors=1, retries=2, dead_letters=1,
                    queue_age_seconds=8, cancellation_attempted=True,
                    cancellation_succeeded=True, restart_attempted=True,
                    restart_succeeded=True, detection_seconds=2, recovery_seconds=9,
                    rollback_attempted=True, rollback_succeeded=True,
                    restore_attempted=True, restore_succeeded=True,
                    expected_cleanup_resources=("lease-2",), cleaned_resources=("lease-2",),
                ),
                OperationObservation("three", "outage", 60, errors=1),
            ]
        )
        self.assertEqual(report["availability"], 1 / 3)
        self.assertEqual(report["successfulServiceRate"], 2 / 3)
        self.assertEqual(report["degradedRate"], 1 / 3)
        self.assertEqual(report["outageRate"], 1 / 3)
        self.assertEqual(report["retryRate"], 2 / 3)
        self.assertEqual(report["deadLetterCount"], 1)
        self.assertEqual(report["cleanupCompleteness"], 1)
        self.assertEqual(report["cancellationSuccess"], 1)
        self.assertEqual(report["meanTimeToRecoverSeconds"], 9)

    def test_injector_is_development_only_and_bounded(self) -> None:
        with self.assertRaises(PermissionError):
            ControlledFailureInjector("production")
        injector = ControlledFailureInjector("development")
        scenario = FailureScenario("queue-delay", "queue", "delay")
        with self.assertRaises(ControlledFailure):
            injector.run(scenario, lambda: "ok")
        self.assertEqual(injector.run(scenario, lambda: "ok"), "ok")

    def test_manifest_covers_each_failure_target(self) -> None:
        scenarios = load_scenarios(ROOT / "config/reliability/development-failure-scenarios.json")
        self.assertEqual(
            {scenario.target for scenario in scenarios},
            {"provider", "worker", "queue", "storage", "identity", "network", "deployment"},
        )

    def test_repeated_effects_require_unique_ids_and_no_orphans(self) -> None:
        passing = assess_repeated_failure_evidence(
            [
                {"durableEffectId": "effect-1", "effectCreated": True},
                {"durableEffectId": "effect-1", "effectCreated": False},
            ]
        )
        self.assertTrue(passing["passed"])
        failing = assess_repeated_failure_evidence(
            [
                {"durableEffectId": "effect-1", "effectCreated": True},
                {
                    "durableEffectId": "effect-2",
                    "effectCreated": True,
                    "orphanedResources": ["lease-9"],
                },
            ]
        )
        self.assertFalse(failing["passed"])
        self.assertEqual(failing["effectCreationCount"], 2)
        self.assertEqual(failing["orphanedResources"], ["lease-9"])

    def test_burn_rate_drives_priority(self) -> None:
        self.assertAlmostEqual(burn_rate(2, 1000, 0.999), 2)
        fast = prioritize_burn_rates({"5m": 15, "1h": 14.4, "30m": 3, "6h": 2})
        self.assertEqual(fast["priority"], "critical")
        slow = prioritize_burn_rates({"5m": 2, "1h": 2, "30m": 6, "6h": 7})
        self.assertEqual(slow["priority"], "high")


if __name__ == "__main__":
    unittest.main()
