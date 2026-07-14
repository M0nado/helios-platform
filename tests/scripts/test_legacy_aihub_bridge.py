from __future__ import annotations

import json
import tempfile
import unittest
from pathlib import Path

from python.aihub.legacy_bridge.ml_registry import MLRegistry
from python.aihub.legacy_bridge.security_optimizer import build_plan
from python.aihub.legacy_bridge.vm_orchestrator import VMOrchestrator
from python.aihub.legacy_bridge.winre_conversation_integrator import classify, parse_transcript


class LegacyBridgeTests(unittest.TestCase):
    def test_security_profiles(self) -> None:
        self.assertEqual(build_plan("balanced").egress_policy, "smart-allowlist")
        self.assertEqual(build_plan("paranoid").training_policy, "signed-artifacts-only")

    def test_vm_topology(self) -> None:
        topology = VMOrchestrator().build_default_topology()
        self.assertEqual(len(topology), 4)
        self.assertTrue(any(item.backend == "hyperv" for item in topology))

    def test_registry_roundtrip(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            path = Path(temp_dir) / "registry.json"
            registry = MLRegistry(str(path))
            registry.seed_default()
            registry.save()
            data = json.loads(path.read_text(encoding="utf-8"))
            self.assertGreaterEqual(len(data), 10)
            self.assertTrue(any(item["name"] == "security-anomaly-core" for item in data))

    def test_transcript_classification(self) -> None:
        tags = classify("Use WinRE, Docker, GPU compression, and firewall quarantine")
        self.assertIn("winre", tags)
        self.assertIn("runtime", tags)
        self.assertIn("security", tags)
        payload = parse_transcript(["You said", "Build AIHub with Docker", "Copilot said", "Milestone complete"])
        self.assertEqual(payload["summary"]["total_turns"], 2)


if __name__ == "__main__":
    unittest.main()
