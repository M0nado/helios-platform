from __future__ import annotations

import hashlib
import json
from pathlib import Path
import tempfile
import unittest

from python.aihub.build_canonical_artifacts import build_artifacts
from python.aihub.canonical_catalog import (
    build_engine_catalog,
    build_model_registry,
    build_security_plan,
    build_vm_topology,
    recommend_engine_mix,
    serialize_catalog_bundle,
)


class CanonicalCatalogTests(unittest.TestCase):
    def test_security_profiles_fail_closed(self) -> None:
        self.assertEqual(build_security_plan("balanced").egress_policy, "smart-allowlist")
        self.assertEqual(build_security_plan("paranoid").training_policy, "signed-artifacts-only")
        self.assertEqual(build_security_plan("offline").egress_policy, "deny")
        with self.assertRaises(ValueError):
            build_security_plan("unrestricted")

    def test_vm_topology_is_proposal_only(self) -> None:
        topology = build_vm_topology()
        self.assertEqual(len(topology), 4)
        self.assertTrue(any(target.backend == "hyperv" for target in topology))
        self.assertTrue(all(target.mutation_authority == "proposal-only" for target in topology))

    def test_model_registry_is_disabled_for_production(self) -> None:
        registry = build_model_registry()
        self.assertGreaterEqual(len(registry), 15)
        self.assertTrue(any(profile.name == "security-anomaly-core" for profile in registry))
        self.assertTrue(all(profile.production_enabled is False for profile in registry))

    def test_cuda_filter_removes_only_required_engines(self) -> None:
        enabled = build_engine_catalog(cuda_enabled=True)
        disabled = build_engine_catalog(cuda_enabled=False)
        self.assertGreater(enabled["totalEngines"], disabled["totalEngines"])
        self.assertTrue(any(engine["name"] == "security-anomaly-core" for engine in disabled["engines"]))
        self.assertFalse(any(engine["requires_cuda"] for engine in disabled["engines"]))
        self.assertFalse(enabled["productionEnabled"])

    def test_engine_recommendation_is_bounded_and_proposal_only(self) -> None:
        recommendation = recommend_engine_mix(
            cuda_enabled=True,
            security_profile="paranoid",
            optimization_pressure=0.9,
            fleet_size=250,
        )
        names = {engine["name"] for engine in recommendation["selectedEngines"]}
        self.assertIn("security-anomaly-core", names)
        self.assertIn("mesh-consensus-engine", names)
        self.assertTrue(recommendation["proposalOnly"])
        self.assertFalse(recommendation["productionEnabled"])

        with self.assertRaises(ValueError):
            recommend_engine_mix(
                cuda_enabled=False,
                security_profile="balanced",
                optimization_pressure=1.1,
                fleet_size=1,
            )
        with self.assertRaises(ValueError):
            recommend_engine_mix(
                cuda_enabled=False,
                security_profile="root",
                optimization_pressure=0.5,
                fleet_size=1,
            )

    def test_bundle_serializes_without_runtime_authority(self) -> None:
        bundle = serialize_catalog_bundle(cuda_enabled=False)
        self.assertFalse(bundle["productionEnabled"])
        self.assertEqual(bundle["securityPlans"]["offline"]["egress_policy"], "deny")
        self.assertTrue(all(target["mutation_authority"] == "proposal-only" for target in bundle["vmTopology"]))


class CanonicalArtifactTests(unittest.TestCase):
    def test_artifacts_are_atomic_complete_and_hash_locked(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            result = build_artifacts(root, cuda_enabled=False)
            self.assertFalse(result["productionEnabled"])
            self.assertEqual(len(result["files"]), 5)

            manifest_path = root / "manifest.json"
            manifest_bytes = manifest_path.read_bytes()
            self.assertEqual(
                hashlib.sha256(manifest_bytes).hexdigest(),
                result["manifestSha256"],
            )

            manifest = json.loads(manifest_bytes)
            for entry in manifest["files"]:
                path = root / entry["path"]
                self.assertTrue(path.is_file())
                self.assertEqual(path.stat().st_size, entry["bytes"])
                self.assertEqual(hashlib.sha256(path.read_bytes()).hexdigest(), entry["sha256"])
            self.assertFalse(any(root.glob("*.tmp")))


if __name__ == "__main__":
    unittest.main()
