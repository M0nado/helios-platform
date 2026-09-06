from __future__ import annotations

import importlib.util
import json
from pathlib import Path
import tempfile
import unittest


REPOSITORY_ROOT = Path(__file__).resolve().parents[2]
LEGACY_ROOT = REPOSITORY_ROOT / "python" / "x-tier"


def load_legacy_module(file_name: str, module_name: str):
    path = LEGACY_ROOT / file_name
    spec = importlib.util.spec_from_file_location(module_name, path)
    if spec is None or spec.loader is None:
        raise RuntimeError(f"Could not load {path}")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


class LegacyCompatibilityTests(unittest.TestCase):
    def test_security_optimizer_delegates_to_canonical_plan(self) -> None:
        module = load_legacy_module("security_optimizer.py", "legacy_security_optimizer")
        self.assertEqual(module.build_plan("balanced").egress_policy, "smart-allowlist")
        self.assertEqual(module.build_plan("offline").egress_policy, "deny")
        with self.assertRaises(ValueError):
            module.build_plan("unrestricted")

    def test_vm_orchestrator_is_proposal_only(self) -> None:
        module = load_legacy_module("vm_orchestrator.py", "legacy_vm_orchestrator")
        topology = module.VMOrchestrator().build_default_topology()
        self.assertEqual(len(topology), 4)
        self.assertTrue(all(item.mutation_authority == "proposal-only" for item in topology))

    def test_model_registry_writes_canonical_disabled_catalog_atomically(self) -> None:
        module = load_legacy_module("ml_registry.py", "legacy_ml_registry")
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "registry.json"
            registry = module.MLRegistry(str(path))
            registry.seed_default()
            digest = registry.save()
            data = json.loads(path.read_text(encoding="utf-8"))
            self.assertEqual(len(digest), 64)
            self.assertGreaterEqual(len(data), 15)
            self.assertTrue(all(item["production_enabled"] is False for item in data))
            self.assertFalse(any(path.parent.glob("*.tmp")))

    def test_engine_fabric_delegates_to_proposal_only_catalog(self) -> None:
        module = load_legacy_module("deep_engine_fabric.py", "legacy_engine_fabric")
        catalog = module.build_engine_catalog(cuda_enabled=False)
        self.assertFalse(catalog["productionEnabled"])
        recommendation = module.recommend_engine_mix(
            cuda_enabled=False,
            security_profile="paranoid",
            optimization_pressure=0.8,
            fleet_size=250,
        )
        self.assertTrue(recommendation["proposalOnly"])
        self.assertFalse(recommendation["productionEnabled"])

    def test_retired_entry_points_are_thin_safe_launchers(self) -> None:
        expected_targets = {
            "aihub_control_server.py": "secure_runtime.catalog_server",
            "ai.py": "aihub.secure_cli",
            "hermes_xcore_training_loop.py": "aihub.training_proposal",
            "hermes_xcore_training_loop_pseudo.py": "aihub.training_proposal",
            "build_super_outputs.py": "aihub.build_canonical_artifacts",
        }
        forbidden = (
            "0.0.0.0",
            "ThreadingHTTPServer",
            "subprocess.",
            "os.system(",
            "shell=True",
            "request.urlopen",
            "Start-Process",
        )
        for file_name, target in expected_targets.items():
            with self.subTest(file_name=file_name):
                text = (LEGACY_ROOT / file_name).read_text(encoding="utf-8")
                self.assertIn(target, text)
                for token in forbidden:
                    self.assertNotIn(token, text)


if __name__ == "__main__":
    unittest.main()
