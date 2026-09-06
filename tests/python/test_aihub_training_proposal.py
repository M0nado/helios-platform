from __future__ import annotations

import hashlib
import json
from pathlib import Path
import tempfile
import unittest

from python.aihub.training_proposal import (
    build_training_proposal,
    write_proposal,
)


class TrainingProposalTests(unittest.TestCase):
    def test_proposal_is_deterministic_except_timestamp(self) -> None:
        first = build_training_proposal(
            cycles=6,
            seed=42,
            security_profile="balanced",
        )
        second = build_training_proposal(
            cycles=6,
            seed=42,
            security_profile="balanced",
        )
        first.pop("generatedUtc")
        second.pop("generatedUtc")
        self.assertEqual(first, second)
        self.assertEqual(len(first["tasks"]), 6)
        self.assertEqual(
            [task["skill"] for task in first["tasks"]],
            [
                "routing",
                "security",
                "retrieval",
                "compression",
                "optimization",
                "observability",
            ],
        )

    def test_proposal_has_no_execution_authority(self) -> None:
        proposal = build_training_proposal(
            cycles=3,
            seed=7,
            security_profile="paranoid",
        )
        self.assertEqual(proposal["executionMode"], "proposal-only")
        self.assertFalse(proposal["automaticExecution"])
        self.assertFalse(proposal["externalNetworkExecution"])
        self.assertFalse(proposal["privilegedExecution"])
        self.assertFalse(proposal["productionEnabled"])
        self.assertEqual(proposal["securityPlan"]["egress_policy"], "strict-allowlist")
        self.assertTrue(
            all(task["execution_authority"] == "none" for task in proposal["tasks"])
        )
        self.assertTrue(all(task["proposal_only"] for task in proposal["tasks"]))
        self.assertTrue(
            all(task["production_enabled"] is False for task in proposal["tasks"])
        )

    def test_offline_profile_denies_egress(self) -> None:
        proposal = build_training_proposal(
            cycles=1,
            seed=1,
            security_profile="offline",
        )
        self.assertEqual(proposal["securityPlan"]["egress_policy"], "deny")

    def test_invalid_bounds_fail_closed(self) -> None:
        for cycles in (0, 101):
            with self.subTest(cycles=cycles):
                with self.assertRaises(ValueError):
                    build_training_proposal(cycles=cycles)
        for seed in (-(2**31) - 1, 2**31):
            with self.subTest(seed=seed):
                with self.assertRaises(ValueError):
                    build_training_proposal(seed=seed)
        with self.assertRaises(ValueError):
            build_training_proposal(security_profile="unrestricted")

    def test_atomic_output_is_hash_locked(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            destination = Path(directory) / "training-proposal.json"
            proposal = build_training_proposal(
                cycles=2,
                seed=11,
                security_profile="balanced",
            )
            digest = write_proposal(destination, proposal)
            self.assertEqual(
                digest,
                hashlib.sha256(destination.read_bytes()).hexdigest(),
            )
            self.assertEqual(
                json.loads(destination.read_text(encoding="utf-8"))["cycles"],
                2,
            )
            self.assertFalse(any(destination.parent.glob("*.tmp")))


if __name__ == "__main__":
    unittest.main()
