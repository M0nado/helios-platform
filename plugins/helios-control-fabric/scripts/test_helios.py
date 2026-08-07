import importlib.util
import io
import json
import os
import unittest
from contextlib import redirect_stderr
from pathlib import Path
from unittest.mock import Mock, patch


SCRIPT = Path(__file__).with_name("helios.py")
SPEC = importlib.util.spec_from_file_location("helios_cli", SCRIPT)
assert SPEC and SPEC.loader
HELIOS = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(HELIOS)

REPOSITORY_INFO = {
    "id": 1207349837,
    "name": "helios-platform",
    "owner": {
        "id": 274244942,
        "login": "M0nado",
    },
}


def github_reader(*, immutable: bool = True, use_default: bool = True) -> Mock:
    reader = Mock()

    def read(endpoint: str) -> dict:
        if endpoint == "repos/M0nado/helios-platform":
            return REPOSITORY_INFO
        if endpoint == (
            "repos/M0nado/helios-platform/actions/oidc/customization/sub"
        ):
            return {
                "use_default": use_default,
                "use_immutable_subject": immutable,
            }
        raise AssertionError(f"unexpected GitHub endpoint: {endpoint}")

    reader.side_effect = read
    return reader


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
        self.assertIn(
            "effective default/immutable subject policy",
            plan["federationSubjectResolution"],
        )
        self.assertNotIn("federationSubject", plan)

    def test_oidc_contract_is_secretless_and_exact(self) -> None:
        with patch.dict(os.environ, {}, clear=True):
            contract = HELIOS.oidc_contract(
                "azure-dev",
                api_reader=github_reader(),
            )
        self.assertEqual(
            contract["selectedSubject"],
            (
                "repo:M0nado@274244942/"
                "helios-platform@1207349837:environment:azure-dev"
            ),
        )
        self.assertTrue(contract["useImmutableSubject"])
        self.assertEqual(contract["ownerId"], "274244942")
        self.assertEqual(contract["repositoryId"], "1207349837")
        self.assertEqual(contract["audience"], "api://AzureADTokenExchange")
        self.assertFalse(contract["secretsStoredInGitHub"])
        self.assertFalse(contract["automaticRoleAssignment"])
        self.assertTrue(all(not value for value in contract["configuredVariables"].values()))

    def test_oidc_contract_honors_github_legacy_default_when_effective(self) -> None:
        contract = HELIOS.oidc_contract(
            "azure-test",
            api_reader=github_reader(immutable=False),
        )
        self.assertEqual(
            contract["selectedSubject"],
            "repo:M0nado/helios-platform:environment:azure-test",
        )
        self.assertFalse(contract["useImmutableSubject"])

    def test_oidc_contract_rejects_custom_subject_templates(self) -> None:
        with self.assertRaisesRegex(RuntimeError, "customized OIDC"):
            HELIOS.oidc_contract(
                "azure-dev",
                api_reader=github_reader(use_default=False),
            )

    def test_oidc_contract_rejects_missing_policy_signal(self) -> None:
        def reader(endpoint: str) -> dict:
            if endpoint == "repos/M0nado/helios-platform":
                return REPOSITORY_INFO
            return {"use_default": True}

        with self.assertRaisesRegex(RuntimeError, "use_immutable_subject"):
            HELIOS.oidc_contract("azure-dev", api_reader=reader)

    def test_oidc_contract_rejects_missing_repository_ids(self) -> None:
        repository_info = {
            "name": "helios-platform",
            "owner": {"login": "M0nado"},
        }

        def reader(endpoint: str) -> dict:
            if endpoint == "repos/M0nado/helios-platform":
                return repository_info
            return {"use_default": True, "use_immutable_subject": True}

        with self.assertRaisesRegex(RuntimeError, "immutable owner ID"):
            HELIOS.oidc_contract("azure-dev", api_reader=reader)

    def test_static_oidc_asset_contains_no_subject_values(self) -> None:
        asset = HELIOS.read_asset("oidc.json")
        self.assertNotIn("subjects", asset)
        self.assertTrue(asset["subjectResolution"]["failClosed"])
        self.assertTrue(
            asset["subjectResolution"]["requireImmutablePolicySignal"]
        )

    def test_oidc_cli_fails_cleanly_without_github_cli(self) -> None:
        stderr = io.StringIO()
        with patch.object(HELIOS.shutil, "which", return_value=None):
            with redirect_stderr(stderr):
                result = HELIOS.main(
                    ["oidc", "--environment", "azure-dev", "--json"]
                )
        self.assertEqual(result, 2)
        self.assertIn("GitHub CLI is required", stderr.getvalue())

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

    def test_enterprise_fleet_contract_is_complete_and_blocked(self) -> None:
        plan = HELIOS.enterprise_fleet_plan()
        self.assertEqual(plan["defaultExecutionMode"], "what-if-only")
        self.assertEqual(plan["agentCount"], 20)
        self.assertEqual(plan["connectorCount"], 12)
        self.assertFalse(plan["productionProvisioningAllowed"])
        self.assertEqual(plan["blockingIssue"], 162)

    def test_enterprise_fleet_contract_supports_explicit_repository_root(self) -> None:
        repository_root = Path(__file__).resolve().parents[3]
        with patch.dict(
            os.environ,
            {"HELIOS_REPOSITORY_ROOT": str(repository_root)},
            clear=False,
        ):
            plan = HELIOS.enterprise_fleet_plan()
        self.assertEqual(plan["defaultExecutionMode"], "what-if-only")
        self.assertEqual(plan["agentCount"], 20)

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
