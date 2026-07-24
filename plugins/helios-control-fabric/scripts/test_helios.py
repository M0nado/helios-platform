import importlib.util
import json
import os
import unittest
from pathlib import Path
from unittest.mock import patch


SCRIPT = Path(__file__).with_name("helios.py")
SPEC = importlib.util.spec_from_file_location("helios_cli", SCRIPT)
assert SPEC and SPEC.loader
HELIOS = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(HELIOS)


class HeliosCliTests(unittest.TestCase):
    def test_targets_are_canonical(self) -> None:
        targets = HELIOS.read_targets()
        self.assertEqual(
            targets["authorities"]["github"],
            "https://github.com/M0nado/helios-platform",
        )
        self.assertEqual(targets["state"]["azure"], "not-live")

    def test_plan_is_non_executing(self) -> None:
        plan = HELIOS.release_plan("azure-dev")
        self.assertEqual(plan["executionMode"], "plan-only")
        self.assertIn("Azure deployment approval", plan["administratorGates"])
        self.assertEqual(
            plan["federationSubject"],
            "repo:M0nado/helios-platform:environment:azure-dev",
        )

    def test_oidc_contract_is_secretless_and_exact(self) -> None:
        with patch.dict(os.environ, {}, clear=True):
            contract = HELIOS.oidc_contract("azure-dev")
        self.assertEqual(
            contract["selectedSubject"],
            "repo:M0nado/helios-platform:environment:azure-dev",
        )
        self.assertEqual(contract["audience"], "api://AzureADTokenExchange")
        self.assertFalse(contract["secretsStoredInGitHub"])
        self.assertFalse(contract["automaticRoleAssignment"])
        self.assertTrue(all(not value for value in contract["configuredVariables"].values()))

    def test_devops_sync_remains_read_only(self) -> None:
        plan = HELIOS.devops_sync_plan()
        self.assertTrue(plan["azureDevOps"]["readOnly"])
        self.assertFalse(plan["synchronization"]["automaticWrites"])
        self.assertFalse(plan["synchronization"]["bidirectionalMerge"])
        self.assertEqual(plan["executionMode"], "plan-only")

    def test_runner_topology_keeps_self_hosted_disabled(self) -> None:
        topology = HELIOS.runner_plan()
        self.assertEqual(topology["validation"]["provider"], "github-hosted")
        self.assertFalse(topology["selfHosted"]["enabled"])
        self.assertTrue(topology["release"]["immutableImageRequired"])

    def test_edge_plan_requires_private_link_and_separate_approval(self) -> None:
        plan = HELIOS.edge_plan("azure-dev")
        self.assertEqual(plan["targetEdge"]["service"], "Azure Front Door Premium")
        self.assertEqual(plan["targetEdge"]["connectivity"], "Private Link")
        self.assertFalse(plan["automaticApply"])
        self.assertEqual(plan["workflow"]["apply"], "separate protected-environment approval")

    def test_all_assets_are_json(self) -> None:
        for name in (
            "connections.json",
            "oidc.json",
            "devops-sync.json",
            "runner-topology.json",
            "edge-runtime.json",
            "microsoft-mcp.template.json",
        ):
            json.loads((HELIOS.ASSETS / name).read_text(encoding="utf-8"))

    def test_invalid_environment_fails(self) -> None:
        for function in (HELIOS.release_plan, HELIOS.oidc_contract, HELIOS.edge_plan):
            with self.subTest(function=function.__name__):
                with self.assertRaises(ValueError):
                    function("production-now")


if __name__ == "__main__":
    unittest.main()
