from __future__ import annotations

import copy
from pathlib import Path
import sys
import unittest


REPOSITORY_ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(REPOSITORY_ROOT / "scripts" / "contracts"))

from validate_xcore9_runtime_matrix import (  # noqa: E402
    EXPECTED_APPROVAL_BOUNDARY,
    EXPECTED_ARTIFACT_PINNING,
    EXPECTED_MODE_ENABLED_BY_DEFAULT,
    EXPECTED_MODES,
    EXPECTED_RESOURCE_LIMITS,
    REQUIRED_BASELINE_DENY,
    EXPECTED_OWNER_REPOSITORY,
    EXPECTED_SMOKE_EVIDENCE_PATHS,
    EXPECTED_STARTUP_COMMANDS,
    REQUIRED_STARTUP_TOOLS,
    REQUIRED_STARTUP_ENVIRONMENT,
    load_manifest,
    validate_manifest,
    validate_matrix,
)


class XCore9RuntimeMatrixValidationTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.manifest = load_manifest(REPOSITORY_ROOT)

    def test_canonical_manifest_is_valid(self) -> None:
        self.assertEqual(validate_matrix(REPOSITORY_ROOT), [])

    def test_modes_fail_when_required_mode_is_missing(self) -> None:
        candidate = copy.deepcopy(self.manifest)
        candidate["modes"] = [
            mode for mode in candidate["modes"] if mode["id"] != "local-docker"
        ]
        errors = validate_manifest(candidate)
        self.assertTrue(any("modes must be exactly" in error for error in errors))

    def test_manifest_fails_when_default_mode_changes(self) -> None:
        candidate = copy.deepcopy(self.manifest)
        candidate["defaultMode"] = "local-docker"
        errors = validate_manifest(candidate)
        self.assertTrue(any("defaultMode must be local-windows" in error for error in errors))

    def test_mode_fails_when_enabled_by_default_is_changed(self) -> None:
        candidate = copy.deepcopy(self.manifest)
        docker_mode = next(mode for mode in candidate["modes"] if mode["id"] == "local-docker")
        docker_mode["enabledByDefault"] = True
        errors = validate_manifest(candidate)
        self.assertTrue(any("local-docker: enabledByDefault must be false" in error for error in errors))

    def test_mode_fails_when_boundary_is_missing(self) -> None:
        candidate = copy.deepcopy(self.manifest)
        local = next(mode for mode in candidate["modes"] if mode["id"] == "local-windows")
        local["boundaries"].pop("network")
        errors = validate_manifest(candidate)
        self.assertTrue(any("boundaries must include" in error for error in errors))

    def test_mode_fails_when_required_startup_tool_is_removed(self) -> None:
        candidate = copy.deepcopy(self.manifest)
        docker_mode = next(mode for mode in candidate["modes"] if mode["id"] == "local-docker")
        docker_mode["boundaries"]["toolAccess"]["allowedTools"] = ["pwsh", "dotnet"]
        errors = validate_manifest(candidate)
        self.assertTrue(any("allowedTools missing startup tools" in error for error in errors))

    def test_mode_fails_when_required_deny_item_is_removed(self) -> None:
        candidate = copy.deepcopy(self.manifest)
        local = next(mode for mode in candidate["modes"] if mode["id"] == "local-windows")
        local["disallowedOperations"].remove("automatic-merge")
        errors = validate_manifest(candidate)
        self.assertTrue(any("missing required deny items" in error for error in errors))

    def test_mode_fails_when_artifact_pinning_subject_changes(self) -> None:
        candidate = copy.deepcopy(self.manifest)
        local = next(mode for mode in candidate["modes"] if mode["id"] == "local-windows")
        local["artifactPinning"]["subject"] = "Changed.Subject.dll"
        errors = validate_manifest(candidate)
        self.assertTrue(any("artifactPinning.subject must be exactly" in error for error in errors))

    def test_manifest_fails_when_baseline_deny_item_is_removed(self) -> None:
        candidate = copy.deepcopy(self.manifest)
        candidate["requiredDenyList"].remove("automatic-rbac-change")
        errors = validate_manifest(candidate)
        self.assertTrue(any("requiredDenyList missing mandatory baseline deny items" in error for error in errors))

    def test_mode_fails_when_startup_is_not_dry_run(self) -> None:
        candidate = copy.deepcopy(self.manifest)
        local = next(mode for mode in candidate["modes"] if mode["id"] == "local-docker")
        local["startupContract"]["requiredEnvironment"]["HELIOS_EXECUTION_MODE"] = "live"
        errors = validate_manifest(candidate)
        self.assertTrue(any("HELIOS_EXECUTION_MODE must be dry-run" in error for error in errors))

    def test_mode_fails_when_required_environment_changes(self) -> None:
        candidate = copy.deepcopy(self.manifest)
        local = next(mode for mode in candidate["modes"] if mode["id"] == "local-docker")
        local["startupContract"]["requiredEnvironment"]["HELIOS_CLOUD_RUNTIME_ONLY"] = "true"
        errors = validate_manifest(candidate)
        self.assertTrue(any("requiredEnvironment must be exactly" in error for error in errors))

    def test_mode_fails_when_startup_command_changes(self) -> None:
        candidate = copy.deepcopy(self.manifest)
        local = next(mode for mode in candidate["modes"] if mode["id"] == "local-docker")
        local["startupContract"]["command"] = "docker run --rm ubuntu:latest"
        errors = validate_manifest(candidate)
        self.assertTrue(any("startupContract.command must be exactly" in error for error in errors))

    def test_mode_fails_when_loopback_endpoint_is_modified(self) -> None:
        candidate = copy.deepcopy(self.manifest)
        local = next(mode for mode in candidate["modes"] if mode["id"] == "local-docker")
        local["healthContract"]["endpoint"] = "http://127.0.0.1:5999/health/ready"
        errors = validate_manifest(candidate)
        self.assertTrue(any("healthContract.endpoint must be exactly" in error for error in errors))

    def test_mode_fails_when_health_method_changes(self) -> None:
        candidate = copy.deepcopy(self.manifest)
        local = next(mode for mode in candidate["modes"] if mode["id"] == "local-windows")
        local["healthContract"]["method"] = "POST"
        errors = validate_manifest(candidate)
        self.assertTrue(any("healthContract.method must be exactly GET" in error for error in errors))

    def test_hybrid_mode_fails_when_endpoint_order_changes(self) -> None:
        candidate = copy.deepcopy(self.manifest)
        hybrid = next(mode for mode in candidate["modes"] if mode["id"] == "hybrid-windows-docker-fleet")
        hybrid["healthContract"]["endpoints"] = list(reversed(hybrid["healthContract"]["endpoints"]))
        errors = validate_manifest(candidate)
        self.assertTrue(any("healthContract.endpoints must be exactly" in error for error in errors))

    def test_hybrid_mode_fails_when_health_method_changes(self) -> None:
        candidate = copy.deepcopy(self.manifest)
        hybrid = next(mode for mode in candidate["modes"] if mode["id"] == "hybrid-windows-docker-fleet")
        hybrid["healthContract"]["method"] = "GET"
        errors = validate_manifest(candidate)
        self.assertTrue(any("healthContract.method must be exactly MULTI" in error for error in errors))

    def test_mode_fails_when_smoke_evidence_path_changes(self) -> None:
        candidate = copy.deepcopy(self.manifest)
        local = next(mode for mode in candidate["modes"] if mode["id"] == "local-docker")
        local["smokeEvidence"]["data"] = "monado/helios-control/docs/evidence/xcore9-runtime-matrix/renamed.json"
        errors = validate_manifest(candidate)
        self.assertTrue(any("smokeEvidence.data must be exactly" in error for error in errors))

    def test_mode_fails_when_startup_timeout_is_boolean(self) -> None:
        candidate = copy.deepcopy(self.manifest)
        local = next(mode for mode in candidate["modes"] if mode["id"] == "local-docker")
        local["startupContract"]["startupTimeoutSeconds"] = True
        errors = validate_manifest(candidate)
        self.assertTrue(any("startupTimeoutSeconds must be a positive integer" in error for error in errors))

    def test_mode_fails_when_startup_timeout_is_less_than_health_timeout(self) -> None:
        candidate = copy.deepcopy(self.manifest)
        local = next(mode for mode in candidate["modes"] if mode["id"] == "local-docker")
        local["startupContract"]["startupTimeoutSeconds"] = 100
        local["healthContract"]["maxStartupSeconds"] = 120
        errors = validate_manifest(candidate)
        self.assertTrue(any("startupTimeoutSeconds must be >= healthContract.maxStartupSeconds" in error for error in errors))

    def test_hybrid_mode_fails_when_resources_cannot_be_split(self) -> None:
        candidate = copy.deepcopy(self.manifest)
        hybrid = next(mode for mode in candidate["modes"] if mode["id"] == "hybrid-windows-docker-fleet")
        hybrid["resourceEnvelope"]["maxCpuCores"] = 1
        hybrid["resourceEnvelope"]["maxMemoryGb"] = 1
        errors = validate_manifest(candidate)
        self.assertTrue(any("maxCpuCores must be >= 2 to split limits across runtimes" in error for error in errors))
        self.assertTrue(any("maxMemoryGb must be >= 2 to split limits across runtimes" in error for error in errors))

    def test_mode_fails_when_resource_limits_do_not_match_expected_startup_limits(self) -> None:
        candidate = copy.deepcopy(self.manifest)
        docker_mode = next(mode for mode in candidate["modes"] if mode["id"] == "local-docker")
        docker_mode["resourceEnvelope"]["maxCpuCores"] = 1
        docker_mode["resourceEnvelope"]["maxMemoryGb"] = 1
        errors = validate_manifest(candidate)
        self.assertTrue(any("local-docker: resourceEnvelope.maxCpuCores must be exactly 6" in error for error in errors))
        self.assertTrue(any("local-docker: resourceEnvelope.maxMemoryGb must be exactly 12" in error for error in errors))

    def test_manifest_fails_when_secret_scope_is_reused(self) -> None:
        candidate = copy.deepcopy(self.manifest)
        docker = next(mode for mode in candidate["modes"] if mode["id"] == "local-docker")
        hybrid = next(mode for mode in candidate["modes"] if mode["id"] == "hybrid-windows-docker-fleet")
        hybrid["boundaries"]["secrets"]["scopeId"] = docker["boundaries"]["secrets"]["scopeId"]
        errors = validate_manifest(candidate)
        self.assertTrue(any("scopeId must be unique per mode" in error for error in errors))

    def test_manifest_fails_when_governance_flags_are_weakened(self) -> None:
        candidate = copy.deepcopy(self.manifest)
        candidate["governance"]["productionMutationRequiresProtectedApproval"] = False
        errors = validate_manifest(candidate)
        self.assertTrue(any("productionMutationRequiresProtectedApproval must be true" in error for error in errors))

    def test_manifest_fails_when_approval_boundary_changes(self) -> None:
        candidate = copy.deepcopy(self.manifest)
        candidate["governance"]["approvalBoundary"] = "none"
        errors = validate_manifest(candidate)
        self.assertTrue(any("approvalBoundary must be exactly" in error for error in errors))

    def test_manifest_fails_when_owner_repository_changes(self) -> None:
        candidate = copy.deepcopy(self.manifest)
        candidate["ownerRepository"] = "Heli0s-Dynamics/adaptive-multibrain-bootstrap"
        errors = validate_manifest(candidate)
        self.assertTrue(any("ownerRepository must be exactly" in error for error in errors))

    def test_mode_fails_when_public_ingress_is_enabled(self) -> None:
        candidate = copy.deepcopy(self.manifest)
        local = next(mode for mode in candidate["modes"] if mode["id"] == "local-windows")
        local["boundaries"]["network"]["publicIngressAllowed"] = True
        errors = validate_manifest(candidate)
        self.assertTrue(any("publicIngressAllowed must be false" in error for error in errors))

    def test_mode_fails_when_rollback_strategy_is_missing(self) -> None:
        candidate = copy.deepcopy(self.manifest)
        docker = next(mode for mode in candidate["modes"] if mode["id"] == "local-docker")
        docker["rollback"].pop("strategy")
        errors = validate_manifest(candidate)
        self.assertTrue(any("rollback.strategy must be a non-empty string" in error for error in errors))

    def test_baseline_deny_list_is_stable(self) -> None:
        self.assertSetEqual(
            REQUIRED_BASELINE_DENY,
            {
                "automatic-production-deploy",
                "automatic-rbac-change",
                "automatic-consent-grant",
                "automatic-merge",
                "cross-tenant-secret-reuse",
                "cross-mode-token-reuse",
                "unbounded-recursive-agents",
                "plaintext-secret-export",
                "bypass-protected-approval",
            },
        )

    def test_expected_modes_are_stable(self) -> None:
        self.assertSetEqual(
            EXPECTED_MODES,
            {"local-windows", "local-docker", "hybrid-windows-docker-fleet"},
        )

    def test_expected_owner_and_boundary_are_stable(self) -> None:
        self.assertEqual(EXPECTED_OWNER_REPOSITORY, "M0nado/helios-platform")
        self.assertEqual(EXPECTED_APPROVAL_BOUNDARY, "github-protected-environment")

    def test_expected_startup_commands_are_stable(self) -> None:
        self.assertDictEqual(
            EXPECTED_STARTUP_COMMANDS,
            {
                "local-windows": "pwsh ./monado/helios-control/scripts/Start-HeliosLocal.ps1",
                "local-docker": (
                    "docker build --file monado/helios-control/src/Helios.Connect.Api/Dockerfile --tag "
                    "helios-connect:xcore9-local monado/helios-control && docker run --detach --rm --name "
                    "helios-connect-xcore9-local --publish 127.0.0.1:5081:8080 --cpus 6 --memory 12g --env HELIOS_EXECUTION_MODE=dry-run "
                    "--env HELIOS_CLOUD_RUNTIME_ONLY=false --env HELIOS_LOCAL_RUNTIME_ALLOWED=true helios-connect:xcore9-local"
                ),
                "hybrid-windows-docker-fleet": "pwsh ./monado/helios-control/scripts/Start-HeliosHybridRuntime.ps1",
            },
        )

    def test_required_startup_environment_is_stable(self) -> None:
        self.assertDictEqual(
            REQUIRED_STARTUP_ENVIRONMENT,
            {
                "HELIOS_EXECUTION_MODE": "dry-run",
                "HELIOS_CLOUD_RUNTIME_ONLY": "false",
                "HELIOS_LOCAL_RUNTIME_ALLOWED": "true",
            },
        )

    def test_expected_startup_tools_are_stable(self) -> None:
        self.assertDictEqual(
            REQUIRED_STARTUP_TOOLS,
            {
                "local-windows": {"pwsh", "dotnet"},
                "local-docker": {"pwsh", "docker"},
                "hybrid-windows-docker-fleet": {"pwsh", "dotnet", "docker"},
            },
        )

    def test_expected_enabled_by_default_values_are_stable(self) -> None:
        self.assertDictEqual(
            EXPECTED_MODE_ENABLED_BY_DEFAULT,
            {
                "local-windows": True,
                "local-docker": False,
                "hybrid-windows-docker-fleet": False,
            },
        )

    def test_expected_resource_limits_are_stable(self) -> None:
        self.assertDictEqual(
            EXPECTED_RESOURCE_LIMITS,
            {
                "local-windows": {"maxCpuCores": 8, "maxMemoryGb": 16},
                "local-docker": {"maxCpuCores": 6, "maxMemoryGb": 12},
                "hybrid-windows-docker-fleet": {"maxCpuCores": 12, "maxMemoryGb": 24},
            },
        )

    def test_expected_artifact_pinning_is_stable(self) -> None:
        self.assertDictEqual(
            EXPECTED_ARTIFACT_PINNING,
            {
                "local-windows": {
                    "type": "binary-hash",
                    "subject": "Helios.Connect.Api.dll",
                    "referenceSource": "dotnet-publish-output",
                },
                "local-docker": {
                    "type": "image-digest",
                    "subject": "helios-connect:xcore9-local",
                    "referenceSource": "docker-image-inspect",
                },
                "hybrid-windows-docker-fleet": {
                    "type": "binary-hash-and-image-digest",
                    "subject": "Helios.Connect.Api.dll + helios-connect:xcore9-local",
                    "referenceSource": "dotnet-publish-output-and-docker-image-inspect",
                },
            },
        )

    def test_expected_smoke_evidence_paths_are_stable(self) -> None:
        self.assertDictEqual(
            EXPECTED_SMOKE_EVIDENCE_PATHS,
            {
                "local-windows": {
                    "summary": "monado/helios-control/docs/evidence/xcore9-runtime-matrix/local-windows-smoke.md",
                    "data": "monado/helios-control/docs/evidence/xcore9-runtime-matrix/local-windows-smoke.json",
                },
                "local-docker": {
                    "summary": "monado/helios-control/docs/evidence/xcore9-runtime-matrix/local-docker-smoke.md",
                    "data": "monado/helios-control/docs/evidence/xcore9-runtime-matrix/local-docker-smoke.json",
                },
                "hybrid-windows-docker-fleet": {
                    "summary": "monado/helios-control/docs/evidence/xcore9-runtime-matrix/hybrid-windows-docker-fleet-smoke.md",
                    "data": "monado/helios-control/docs/evidence/xcore9-runtime-matrix/hybrid-windows-docker-fleet-smoke.json",
                },
            },
        )


if __name__ == "__main__":
    unittest.main()
