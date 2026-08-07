import importlib.util
import io
import json
import os
import tempfile
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

    def test_integration_plan_is_dry_run_with_hermes_routes(self) -> None:
        plan = HELIOS.integration_plan()
        self.assertEqual(plan["integrationExecutionMode"], "dry-run")
        self.assertEqual(plan["executionMode"], "plan-only")
        self.assertEqual(plan["routes"]["enabled"], 0)
        self.assertIn("hermes.training.candidate", plan["routes"]["hermesEvents"])
        self.assertIn("microsoftAgents", plan["destinations"])

    def test_fleet_plan_exposes_provider_and_workflow_contract(self) -> None:
        plan = HELIOS.fleet_plan()
        self.assertEqual(plan["name"], "helios-local-agent-fleet")
        self.assertEqual(plan["fleetExecutionMode"], "dry-run")
        self.assertFalse(plan["automaticProviderRuns"])
        self.assertEqual(plan["executionMode"], "plan-only")
        self.assertEqual(len(plan["providers"]), 4)
        self.assertIn("xcore-evaluator", plan["workflow"])
        self.assertIn("automatic-merge", plan["forbidden"])

    def test_hermes_plan_is_bounded_and_candidate_only(self) -> None:
        plan = HELIOS.hermes_plan()
        self.assertEqual(plan["runtime"], "microsoft-foundry-agent-service")
        self.assertEqual(plan["runtimeState"], "planned-admin-gate")
        self.assertEqual(plan["orchestrator"]["id"], "hermes-orchestrator")
        self.assertFalse(plan["learningPolicy"]["automaticProductionMutation"])
        self.assertFalse(plan["learningPolicy"]["rawCopilotConversationsAsTrainingData"])
        self.assertEqual(plan["executionMode"], "plan-only")

    def test_xcore9_plan_matches_runner_and_agent_contract(self) -> None:
        plan = HELIOS.xcore9_plan()
        self.assertEqual(plan["state"], "standby")
        self.assertEqual(plan["runnerGroup"]["name"], "hermes-xcore9-local")
        self.assertEqual(plan["runnerGroup"]["fleet"], "hermes")
        self.assertEqual(len(plan["runnerGroup"]["runners"]), 4)
        self.assertEqual(plan["teacher"]["id"], "xcore-teacher")
        self.assertEqual(plan["evaluator"]["id"], "xcore-evaluator")
        self.assertGreaterEqual(len(plan["xcoreProfiles"]), 1)
        self.assertEqual(plan["executionMode"], "plan-only")

    def test_aihub_plan_contains_language_and_engine_matrix(self) -> None:
        plan = HELIOS.aihub_plan()
        self.assertEqual(plan["contract"], "aihub-learning-matrix")
        self.assertEqual(plan["state"], "candidate-only")
        self.assertEqual(plan["executionMode"], "plan-only")
        self.assertIn("codex-api", [engine["id"] for engine in plan["providerEngines"]])
        self.assertIn("openai-responses", [engine["id"] for engine in plan["providerEngines"]])
        self.assertIn("claude-api", [engine["id"] for engine in plan["providerEngines"]])
        self.assertEqual(set(plan["languageCoverage"]), {"csharp", "fsharp", "cpp", "python"})

    def test_aihub_plan_can_focus_on_single_language(self) -> None:
        plan = HELIOS.aihub_plan("csharp")
        self.assertEqual(plan["selectedLanguage"], "csharp")
        self.assertIn("primaryEngines", plan["playbook"])
        self.assertIn("codex-api", plan["playbook"]["primaryEngines"])
        self.assertNotIn("languagePlaybooks", plan)

    def test_aihub_plan_rejects_invalid_language(self) -> None:
        with self.assertRaises(ValueError):
            HELIOS.aihub_plan("java")

    def test_code_engine_plan_covers_provider_matrix(self) -> None:
        plan = HELIOS.code_engine_plan()
        self.assertEqual(plan["model"], "provider-neutral-mcp-boundary")
        self.assertGreaterEqual(len(plan["engines"]), 6)
        self.assertEqual(len(plan["localFleetEngines"]), 4)
        self.assertIn("codex-api", [engine["id"] for engine in plan["engines"]])
        self.assertEqual(set(plan["languageCoverage"]), {"csharp", "fsharp", "cpp", "python"})
        self.assertIn("integration-builder", plan["defaultWorkflow"])
        self.assertEqual(plan["executionMode"], "plan-only")

    def test_benchmarking_plan_is_xcore_guarded(self) -> None:
        plan = HELIOS.benchmarking_plan()
        self.assertEqual(plan["status"], "candidate-only")
        self.assertEqual(plan["owner"], "xcore-evaluator")
        self.assertIn("quality", plan["metrics"])
        self.assertFalse(plan["stores"]["cloudStoresImplemented"])
        self.assertFalse(plan["gates"]["automaticProductionMutation"])
        self.assertTrue(plan["gates"]["rollbackRequired"])
        self.assertIn("benchmark-pass-threshold", plan["gates"]["promotion"])
        self.assertEqual(set(plan["languageCoverage"]), {"csharp", "fsharp", "cpp", "python"})
        self.assertEqual(plan["executionMode"], "plan-only")

    def test_local_autoscaling_contract_is_bounded(self) -> None:
        plan = HELIOS.build_local_autoscaling_contract("azure-dev")
        self.assertEqual(plan["executionMode"], "automatic-local")
        self.assertEqual(plan["strategy"], "bounded-local-fleet-autoscaling")
        self.assertEqual(plan["targets"]["localAgentsDesired"], 4)
        self.assertEqual(plan["targets"]["cloudAgentsDesiredWhenConfigured"], 2)
        self.assertEqual(plan["targets"]["xcore9RunnersDesired"], 4)
        self.assertEqual(plan["queuePolicy"]["scaleOutPendingTaskThreshold"], 4)
        self.assertEqual(plan["parallelismContract"]["maxConcurrentModelCalls"], 8)
        self.assertEqual(plan["cloudMutation"], "disabled-without-protected-approval")

    def test_activate_local_contract_writes_runtime_artifacts(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            with patch.object(HELIOS, "LOCAL_AUTOMATION_ROOT", Path(temp_dir)):
                activated = HELIOS.activate_local_contract(
                    "integration",
                    HELIOS.integration_plan(),
                    "azure-dev",
                )
            self.assertEqual(activated["executionMode"], "automatic-local")
            self.assertIn("automation", activated)
            self.assertEqual(activated["automation"]["mode"], "automatic-local")
            contract_file = Path(temp_dir) / "azure-dev" / "integration.auto.json"
            autoscaling_file = Path(temp_dir) / "azure-dev" / "autoscaling.auto.json"
            self.assertTrue(contract_file.exists())
            self.assertTrue(autoscaling_file.exists())

    def test_activate_local_contract_rejects_invalid_environment(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            with patch.object(HELIOS, "LOCAL_AUTOMATION_ROOT", Path(temp_dir)):
                with self.assertRaises(ValueError):
                    HELIOS.activate_local_contract(
                        "integration",
                        HELIOS.integration_plan(),
                        "production-now",
                    )

    def test_integration_cli_auto_local_uses_automatic_mode(self) -> None:
        stdout = io.StringIO()
        with tempfile.TemporaryDirectory() as temp_dir:
            with patch.object(HELIOS, "LOCAL_AUTOMATION_ROOT", Path(temp_dir)):
                with redirect_stdout(stdout):
                    result = HELIOS.main(
                        [
                            "integration",
                            "--environment",
                            "azure-dev",
                            "--auto-local",
                            "--json",
                        ]
                    )
        self.assertEqual(result, 0)
        payload = json.loads(stdout.getvalue())
        self.assertEqual(payload["executionMode"], "automatic-local")
        self.assertIn("automation", payload)
        self.assertEqual(payload["automation"]["mode"], "automatic-local")

    def test_setup_all_auto_local_enables_automatic_local_sections(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            with patch.object(HELIOS, "LOCAL_AUTOMATION_ROOT", Path(temp_dir)):
                with patch.dict(os.environ, {}, clear=True):
                    bundle = HELIOS.setup_all(
                        "azure-dev",
                        api_reader=github_reader(),
                        auto_local=True,
                    )
        self.assertEqual(bundle["executionMode"], "automatic-local")
        self.assertEqual(bundle["integration"]["executionMode"], "automatic-local")
        self.assertEqual(bundle["fleet"]["executionMode"], "automatic-local")
        self.assertEqual(bundle["hermes"]["executionMode"], "automatic-local")
        self.assertEqual(bundle["xcore9"]["executionMode"], "automatic-local")
        self.assertEqual(bundle["aihub"]["executionMode"], "automatic-local")
        self.assertEqual(bundle["codeEngine"]["executionMode"], "automatic-local")
        self.assertEqual(bundle["benchmarking"]["executionMode"], "automatic-local")
        self.assertIn("automation", bundle)

    def test_setup_all_bundle_covers_all_contracts(self) -> None:
        with patch.dict(os.environ, {}, clear=True):
            bundle = HELIOS.setup_all("azure-dev", api_reader=github_reader())
        self.assertEqual(bundle["executionMode"], "plan-only")
        self.assertEqual(bundle["environment"], "azure-dev")
        self.assertIsInstance(bundle["doctor"]["requiredToolsReady"], bool)
        self.assertIn("authorities", bundle["targets"])
        self.assertEqual(bundle["plan"]["executionMode"], "plan-only")
        self.assertEqual(bundle["oidc"]["executionMode"], "plan-only")
        self.assertEqual(bundle["devopsSync"]["executionMode"], "plan-only")
        self.assertEqual(bundle["runners"]["executionMode"], "plan-only")
        self.assertEqual(bundle["edge"]["executionMode"], "plan-only")
        self.assertEqual(bundle["integration"]["executionMode"], "plan-only")
        self.assertEqual(bundle["fleet"]["executionMode"], "plan-only")
        self.assertEqual(bundle["hermes"]["executionMode"], "plan-only")
        self.assertEqual(bundle["xcore9"]["executionMode"], "plan-only")
        self.assertEqual(bundle["aihub"]["executionMode"], "plan-only")
        self.assertEqual(bundle["codeEngine"]["executionMode"], "plan-only")
        self.assertEqual(bundle["benchmarking"]["executionMode"], "plan-only")

    def test_setup_all_cli_json_includes_all_sections(self) -> None:
        stdout = io.StringIO()
        with patch.dict(os.environ, {}, clear=True):
            with patch.object(HELIOS, "run_gh_api", github_reader()):
                with redirect_stdout(stdout):
                    result = HELIOS.main(
                        ["setup-all", "--environment", "azure-dev", "--json"]
                    )
        self.assertEqual(result, 0)
        payload = json.loads(stdout.getvalue())
        for key in (
            "doctor",
            "targets",
            "plan",
            "oidc",
            "devopsSync",
            "runners",
            "edge",
            "integration",
            "fleet",
            "hermes",
            "xcore9",
            "aihub",
            "codeEngine",
            "benchmarking",
        ):
            self.assertIn(key, payload)

    def test_runner_topology_configures_xcore9_local_hermes_fleet(self) -> None:
        topology = HELIOS.runner_plan()
        self.assertEqual(topology["validation"]["provider"], "github-hosted")
        self.assertTrue(topology["selfHosted"]["enabled"])
        self.assertEqual(
            topology["selfHosted"]["activationMode"],
            "manual-protected-environment-approval",
        )
        group = next(
            item
            for item in topology["selfHosted"]["groups"]
            if item["name"] == "hermes-xcore9-local"
        )
        self.assertEqual(group["fleet"], "hermes")
        self.assertEqual(len(group["runners"]), 4)
        self.assertIn("xcore9", group["labels"])
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
        for name in (
            "integrations.json",
            "agent-fleet.json",
            "microsoft-agents.json",
            "aihub-learning-matrix.json",
        ):
            json.loads((HELIOS.MONADO_CONFIG / name).read_text(encoding="utf-8"))

    def test_invalid_environment_fails(self) -> None:
        for function in (
            HELIOS.release_plan,
            HELIOS.oidc_contract,
            HELIOS.edge_plan,
            HELIOS.build_local_autoscaling_contract,
            HELIOS.setup_all,
        ):
            with self.subTest(function=function.__name__):
                with self.assertRaises(ValueError):
                    function("production-now")


if __name__ == "__main__":
    unittest.main()
