import importlib.util
import io
import json
import os
import unittest
from contextlib import redirect_stderr, redirect_stdout
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

    def test_setup_bundle_covers_all_requested_surfaces(self) -> None:
        bundle = HELIOS.full_setup_bundle("azure-dev")
        self.assertEqual(bundle["executionMode"], "plan-only")
        self.assertEqual(
            bundle["setupBundle"],
            "files-modules-services-environments-integrations",
        )
        self.assertGreater(bundle["files"]["total"], 0)
        self.assertIn(
            "monado/helios-control/config/integrations.json",
            [item["path"] for item in bundle["files"]["required"]],
        )
        self.assertTrue(
            any(
                item["name"] == "Helios.Connect.Api"
                for item in bundle["modules"]["items"]
            )
        )
        self.assertGreater(bundle["services"]["total"], 0)
        self.assertIn(
            "github",
            [item["id"] for item in bundle["integrations"]["destinations"]],
        )
        self.assertTrue(
            bundle["environments"]["selectedEnvironmentCoverage"]["identityBindings"]
        )
        self.assertTrue(
            bundle["environments"]["selectedEnvironmentCoverage"]["automationAllowed"]
        )

    def test_setup_bundle_handles_missing_control_configs(self) -> None:
        missing = ["monado/helios-control/config/cloud-runtime.json"]
        with patch.object(
            HELIOS,
            "setup_file_inventory",
            return_value={
                "required": [{"path": missing[0], "present": False}],
                "total": 1,
                "presentCount": 0,
                "missing": missing,
            },
        ):
            bundle = HELIOS.full_setup_bundle("azure-dev")
        self.assertIn("warnings", bundle)
        self.assertFalse(bundle["services"]["available"])
        self.assertEqual(bundle["integrations"]["routeCount"], 0)
        self.assertEqual(
            bundle["warnings"][0],
            "Missing required control config files: monado/helios-control/config/cloud-runtime.json",
        )

    def test_setup_human_output_includes_route_status(self) -> None:
        bundle = HELIOS.full_setup_bundle("azure-dev")
        output = io.StringIO()
        with redirect_stdout(output):
            HELIOS.print_human(bundle)
        self.assertIn("routes:", output.getvalue())
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
        for function in (
            HELIOS.release_plan,
            HELIOS.oidc_contract,
            HELIOS.edge_plan,
            HELIOS.full_setup_bundle,
        ):
            with self.subTest(function=function.__name__):
                with self.assertRaises(ValueError):
                    function("production-now")


if __name__ == "__main__":
    unittest.main()
