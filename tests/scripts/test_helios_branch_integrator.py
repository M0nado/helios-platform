#!/usr/bin/env python3
from __future__ import annotations

import importlib.util
import sys
import tempfile
import unittest
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]
MODULE_PATH = REPO_ROOT / "scripts" / "integration" / "helios_branch_integrator.py"
SPEC = importlib.util.spec_from_file_location("helios_branch_integrator", MODULE_PATH)
integrator = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = integrator
SPEC.loader.exec_module(integrator)


class HeliosBranchIntegratorTests(unittest.TestCase):
    def test_branch_priority_orders_control_before_hermes_and_he(self) -> None:
        self.assertEqual(integrator.branch_priority("origin/helios-control"), 10)
        self.assertEqual(integrator.branch_priority("origin/hermes-fleet-production"), 20)
        self.assertEqual(integrator.branch_priority("origin/feature/helios-ui"), 30)
        self.assertEqual(integrator.branch_priority("origin/he-xcore-specialist"), 54)
        self.assertIsNone(integrator.branch_priority("origin/main"))

    def test_lane_inventory_counts_stack_files_and_skips_build_outputs(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            (root / "src").mkdir()
            (root / "src" / "App.xaml").write_text("<Application />")
            (root / "src" / "Native.cpp").write_text("int main() { return 0; }")
            (root / "src" / "Forecast.fs").write_text("module Forecast")
            (root / "src" / "aihub.py").write_text("print('aihub')")
            (root / "infra.bicep").write_text("param location string")
            (root / "bin").mkdir()
            (root / "bin" / "Ignored.cs").write_text("class Ignored {}")

            inventory = integrator.lane_inventory(root)

        self.assertEqual(inventory["csharp_winui_frontend"]["count"], 1)
        self.assertEqual(inventory["cpp_performance_security_backend"]["count"], 1)
        self.assertEqual(inventory["fsharp_math_predictions_analytics"]["count"], 1)
        self.assertEqual(inventory["python_aihub_hermes_xcore_automation"]["count"], 1)
        self.assertEqual(inventory["azure_deployment"]["count"], 1)

    def test_merge_execute_requires_clean_working_tree(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            repo = Path(tmp)
            integrator.run(["git", "init"], repo)
            (repo / "README.md").write_text("dirty")
            refs = [integrator.BranchRef(name="feature/helios-ui", sha="abc123", source="local", priority=30)]
            with self.assertRaises(RuntimeError):
                integrator.merge_refs(repo, refs, execute=True)


if __name__ == "__main__":
    unittest.main()
