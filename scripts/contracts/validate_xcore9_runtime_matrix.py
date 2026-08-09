#!/usr/bin/env python3
"""Dependency-free validator for the XCore9 runtime matrix contract."""

from __future__ import annotations

import argparse
import json
from pathlib import Path
import sys


REPOSITORY_ROOT = Path(__file__).resolve().parents[2]
MATRIX_PATH = REPOSITORY_ROOT / "monado" / "helios-control" / "config" / "xcore9-runtime-matrix.v1.json"
EXPECTED_MODES = {
    "local-windows",
    "local-docker",
    "hybrid-windows-docker-fleet",
}
REQUIRED_BOUNDARY_KEYS = {"identity", "network", "storage", "toolAccess", "secrets"}
REQUIRED_RESOURCE_KEYS = {
    "maxCpuCores",
    "maxMemoryGb",
    "maxGpuProcesses",
    "maxConcurrentDeepLearningJobs",
    "maxConcurrentAgentRuns",
}
REQUIRED_BASELINE_DENY = {
    "automatic-production-deploy",
    "automatic-rbac-change",
    "automatic-consent-grant",
    "automatic-merge",
    "cross-tenant-secret-reuse",
    "cross-mode-token-reuse",
    "unbounded-recursive-agents",
    "plaintext-secret-export",
    "bypass-protected-approval",
}
EXPECTED_OWNER_REPOSITORY = "M0nado/helios-platform"
EXPECTED_APPROVAL_BOUNDARY = "github-protected-environment"
REQUIRED_EVIDENCE_FIELDS = {"correlationId", "evidenceLinks"}
REQUIRED_STARTUP_ENVIRONMENT = {
    "HELIOS_EXECUTION_MODE": "dry-run",
    "HELIOS_CLOUD_RUNTIME_ONLY": "false",
    "HELIOS_LOCAL_RUNTIME_ALLOWED": "true",
}
EXPECTED_HEALTH_ENDPOINTS = {
    "local-windows": "http://127.0.0.1:5080/health/ready",
    "local-docker": "http://127.0.0.1:5081/health/ready",
    "hybrid-windows-docker-fleet": [
        "http://127.0.0.1:5080/health/ready",
        "http://127.0.0.1:5081/health/ready",
    ],
}
EXPECTED_STARTUP_COMMANDS = {
    "local-windows": "pwsh ./monado/helios-control/scripts/Start-HeliosLocal.ps1",
    "local-docker": (
        "docker build --file monado/helios-control/src/Helios.Connect.Api/Dockerfile --tag "
        "helios-connect:xcore9-local monado/helios-control && docker run --detach --rm --name "
        "helios-connect-xcore9-local --publish 127.0.0.1:5081:8080 --cpus 6 --memory 12g --env HELIOS_EXECUTION_MODE=dry-run "
        "--env HELIOS_CLOUD_RUNTIME_ONLY=false --env HELIOS_LOCAL_RUNTIME_ALLOWED=true helios-connect:xcore9-local"
    ),
    "hybrid-windows-docker-fleet": "pwsh ./monado/helios-control/scripts/Start-HeliosHybridRuntime.ps1",
}
EXPECTED_SMOKE_EVIDENCE_PATHS = {
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
}


def _append(errors: list[str], condition: bool, message: str) -> None:
    if not condition:
        errors.append(message)


def _is_positive_int(value: object) -> bool:
    return type(value) is int and value > 0


def load_manifest(root: Path) -> dict:
    path = root / "monado" / "helios-control" / "config" / "xcore9-runtime-matrix.v1.json"
    return json.loads(path.read_text(encoding="utf-8"))


def validate_top_level(manifest: dict) -> list[str]:
    errors: list[str] = []
    _append(errors, manifest.get("schemaVersion") == 1, "schemaVersion must be 1")
    _append(errors, manifest.get("contractId") == "xcore9-runtime-matrix", "contractId must be xcore9-runtime-matrix")
    _append(errors, isinstance(manifest.get("version"), str) and bool(manifest["version"]), "version must be a non-empty string")
    _append(
        errors,
        manifest.get("ownerRepository") == EXPECTED_OWNER_REPOSITORY,
        f"ownerRepository must be exactly {EXPECTED_OWNER_REPOSITORY}",
    )
    _append(errors, manifest.get("defaultMode") in EXPECTED_MODES, "defaultMode must be one of the required runtime modes")
    _append(errors, manifest.get("defaultExecutionMode") == "validation-first", "defaultExecutionMode must be validation-first")

    governance = manifest.get("governance")
    if not isinstance(governance, dict):
        errors.append("governance must be an object")
        return errors

    _append(errors, governance.get("nonDestructiveDefault") is True, "governance.nonDestructiveDefault must be true")
    _append(
        errors,
        governance.get("productionMutationRequiresProtectedApproval") is True,
        "governance.productionMutationRequiresProtectedApproval must be true",
    )
    _append(
        errors,
        governance.get("approvalBoundary") == EXPECTED_APPROVAL_BOUNDARY,
        f"governance.approvalBoundary must be exactly {EXPECTED_APPROVAL_BOUNDARY}",
    )
    _append(
        errors,
        governance.get("crossTenantSecretReuseAllowed") is False,
        "governance.crossTenantSecretReuseAllowed must be false",
    )
    _append(
        errors,
        governance.get("crossModeTokenReuseAllowed") is False,
        "governance.crossModeTokenReuseAllowed must be false",
    )
    required_evidence_fields = governance.get("requiredEvidenceFields")
    _append(
        errors,
        isinstance(required_evidence_fields, list),
        "governance.requiredEvidenceFields must be a list",
    )
    if isinstance(required_evidence_fields, list):
        evidence_field_set = {field for field in required_evidence_fields if isinstance(field, str)}
        missing_evidence_fields = sorted(REQUIRED_EVIDENCE_FIELDS - evidence_field_set)
        _append(
            errors,
            not missing_evidence_fields,
            f"governance.requiredEvidenceFields missing mandatory fields: {missing_evidence_fields}",
        )

    required_deny = manifest.get("requiredDenyList")
    _append(errors, isinstance(required_deny, list) and len(required_deny) >= 5, "requiredDenyList must be a non-empty list")
    if isinstance(required_deny, list):
        _append(
            errors,
            all(isinstance(item, str) and item for item in required_deny),
            "requiredDenyList entries must be non-empty strings",
        )
        required_deny_set = {item for item in required_deny if isinstance(item, str)}
        missing_baseline = sorted(REQUIRED_BASELINE_DENY - required_deny_set)
        _append(
            errors,
            not missing_baseline,
            f"requiredDenyList missing mandatory baseline deny items: {missing_baseline}",
        )

    return errors


def validate_mode(mode: dict, required_deny: set[str], mode_ids: set[str]) -> list[str]:
    errors: list[str] = []
    mode_id = mode.get("id")
    if not isinstance(mode_id, str) or not mode_id:
        return ["mode.id must be a non-empty string"]

    _append(errors, mode_id in EXPECTED_MODES, f"{mode_id}: unexpected runtime mode id")

    boundaries = mode.get("boundaries")
    _append(errors, isinstance(boundaries, dict), f"{mode_id}: boundaries must be an object")
    if isinstance(boundaries, dict):
        _append(
            errors,
            REQUIRED_BOUNDARY_KEYS.issubset(boundaries.keys()),
            f"{mode_id}: boundaries must include {sorted(REQUIRED_BOUNDARY_KEYS)}",
        )
        identity = boundaries.get("identity", {})
        if isinstance(identity, dict):
            _append(
                errors,
                identity.get("crossTenantTokenReuseAllowed") is False,
                f"{mode_id}: boundaries.identity.crossTenantTokenReuseAllowed must be false",
            )
        else:
            errors.append(f"{mode_id}: boundaries.identity must be an object")

        network = boundaries.get("network", {})
        if isinstance(network, dict):
            _append(
                errors,
                network.get("publicIngressAllowed") is False,
                f"{mode_id}: boundaries.network.publicIngressAllowed must be false",
            )
        else:
            errors.append(f"{mode_id}: boundaries.network must be an object")

        storage = boundaries.get("storage", {})
        if isinstance(storage, dict):
            _append(
                errors,
                storage.get("destructiveDiskOperationsAllowed") is False,
                f"{mode_id}: boundaries.storage.destructiveDiskOperationsAllowed must be false",
            )
        else:
            errors.append(f"{mode_id}: boundaries.storage must be an object")

        tool_access = boundaries.get("toolAccess", {})
        if isinstance(tool_access, dict):
            _append(
                errors,
                tool_access.get("localMcpReadOnlyRequired") is True,
                f"{mode_id}: boundaries.toolAccess.localMcpReadOnlyRequired must be true",
            )
        else:
            errors.append(f"{mode_id}: boundaries.toolAccess must be an object")

        secrets = boundaries.get("secrets", {})
        if isinstance(secrets, dict):
            _append(
                errors,
                secrets.get("crossModeReuseAllowed") is False,
                f"{mode_id}: boundaries.secrets.crossModeReuseAllowed must be false",
            )
            _append(
                errors,
                secrets.get("plaintextInRepositoryAllowed") is False,
                f"{mode_id}: boundaries.secrets.plaintextInRepositoryAllowed must be false",
            )
            _append(
                errors,
                isinstance(secrets.get("scopeId"), str) and bool(secrets["scopeId"]),
                f"{mode_id}: boundaries.secrets.scopeId must be a non-empty string",
            )
        else:
            errors.append(f"{mode_id}: boundaries.secrets must be an object")

    startup = mode.get("startupContract")
    _append(errors, isinstance(startup, dict), f"{mode_id}: startupContract must be an object")
    if isinstance(startup, dict):
        expected_command = EXPECTED_STARTUP_COMMANDS.get(mode_id)
        _append(
            errors,
            isinstance(startup.get("command"), str) and startup.get("command") == expected_command,
            f"{mode_id}: startupContract.command must be exactly {expected_command}",
        )
        _append(
            errors,
            isinstance(startup.get("requiredEnvironment"), dict),
            f"{mode_id}: startupContract.requiredEnvironment must be an object",
        )
        if isinstance(startup.get("requiredEnvironment"), dict):
            required_environment = startup["requiredEnvironment"]
            _append(
                errors,
                required_environment == REQUIRED_STARTUP_ENVIRONMENT,
                f"{mode_id}: startupContract.requiredEnvironment must be exactly {REQUIRED_STARTUP_ENVIRONMENT}",
            )
            _append(
                errors,
                required_environment.get("HELIOS_EXECUTION_MODE") == "dry-run",
                f"{mode_id}: startupContract.requiredEnvironment.HELIOS_EXECUTION_MODE must be dry-run",
            )
        _append(
            errors,
            _is_positive_int(startup.get("startupTimeoutSeconds")),
            f"{mode_id}: startupContract.startupTimeoutSeconds must be a positive integer",
        )

    health = mode.get("healthContract")
    _append(errors, isinstance(health, dict), f"{mode_id}: healthContract must be an object")
    if isinstance(health, dict):
        expected_method = "MULTI" if mode_id == "hybrid-windows-docker-fleet" else "GET"
        _append(
            errors,
            health.get("method") == expected_method,
            f"{mode_id}: healthContract.method must be exactly {expected_method}",
        )
        _append(
            errors,
            health.get("expectedStatusCode") == 200,
            f"{mode_id}: healthContract.expectedStatusCode must be 200",
        )
        _append(
            errors,
            _is_positive_int(health.get("probeTimeoutSeconds")),
            f"{mode_id}: healthContract.probeTimeoutSeconds must be a positive integer",
        )
        _append(
            errors,
            _is_positive_int(health.get("maxStartupSeconds")),
            f"{mode_id}: healthContract.maxStartupSeconds must be a positive integer",
        )
        if mode_id == "hybrid-windows-docker-fleet":
            endpoints = health.get("endpoints")
            _append(
                errors,
                isinstance(endpoints, list) and len(endpoints) == 2 and all(isinstance(endpoint, str) for endpoint in endpoints),
                f"{mode_id}: healthContract.endpoints must define exactly two endpoint URLs",
            )
            if isinstance(endpoints, list) and len(endpoints) == 2 and all(isinstance(endpoint, str) for endpoint in endpoints):
                expected_hybrid_endpoints = EXPECTED_HEALTH_ENDPOINTS["hybrid-windows-docker-fleet"]
                _append(
                    errors,
                    endpoints == expected_hybrid_endpoints,
                    f"{mode_id}: healthContract.endpoints must be exactly {expected_hybrid_endpoints}",
                )
                _append(
                    errors,
                    len(set(endpoints)) == 2,
                    f"{mode_id}: healthContract.endpoints must contain two distinct loopback endpoints",
                )
        else:
            expected_endpoint = EXPECTED_HEALTH_ENDPOINTS.get(mode_id)
            _append(
                errors,
                isinstance(health.get("endpoint"), str) and health.get("endpoint") == expected_endpoint,
                f"{mode_id}: healthContract.endpoint must be exactly {expected_endpoint}",
            )

    artifact = mode.get("artifactPinning")
    _append(errors, isinstance(artifact, dict), f"{mode_id}: artifactPinning must be an object")
    if isinstance(artifact, dict):
        _append(errors, artifact.get("algorithm") == "sha256", f"{mode_id}: artifactPinning.algorithm must be sha256")
        _append(
            errors,
            artifact.get("immutableReferenceRequired") is True,
            f"{mode_id}: artifactPinning.immutableReferenceRequired must be true",
        )

    resource = mode.get("resourceEnvelope")
    _append(errors, isinstance(resource, dict), f"{mode_id}: resourceEnvelope must be an object")
    if isinstance(resource, dict):
        _append(
            errors,
            REQUIRED_RESOURCE_KEYS.issubset(resource.keys()),
            f"{mode_id}: resourceEnvelope must include {sorted(REQUIRED_RESOURCE_KEYS)}",
        )
        for key in REQUIRED_RESOURCE_KEYS:
            _append(
                errors,
                _is_positive_int(resource.get(key)),
                f"{mode_id}: resourceEnvelope.{key} must be a positive integer",
            )
        if _is_positive_int(resource.get("maxConcurrentDeepLearningJobs")) and _is_positive_int(resource.get("maxConcurrentAgentRuns")):
            _append(
                errors,
                resource["maxConcurrentDeepLearningJobs"] <= resource["maxConcurrentAgentRuns"],
                f"{mode_id}: maxConcurrentDeepLearningJobs cannot exceed maxConcurrentAgentRuns",
            )
        if mode_id == "hybrid-windows-docker-fleet":
            _append(
                errors,
                _is_positive_int(resource.get("maxCpuCores")) and resource["maxCpuCores"] >= 2,
                f"{mode_id}: resourceEnvelope.maxCpuCores must be >= 2 to split limits across runtimes",
            )
            _append(
                errors,
                _is_positive_int(resource.get("maxMemoryGb")) and resource["maxMemoryGb"] >= 2,
                f"{mode_id}: resourceEnvelope.maxMemoryGb must be >= 2 to split limits across runtimes",
            )

    rollback = mode.get("rollback")
    _append(errors, isinstance(rollback, dict), f"{mode_id}: rollback must be an object")
    if isinstance(rollback, dict):
        _append(
            errors,
            rollback.get("requiresProtectedApprovalForLiveMutation") is True,
            f"{mode_id}: rollback.requiresProtectedApprovalForLiveMutation must be true",
        )
        _append(
            errors,
            isinstance(rollback.get("failureDomain"), str) and bool(rollback["failureDomain"]),
            f"{mode_id}: rollback.failureDomain must be a non-empty string",
        )
        _append(
            errors,
            isinstance(rollback.get("strategy"), str) and bool(rollback["strategy"]),
            f"{mode_id}: rollback.strategy must be a non-empty string",
        )
        _append(
            errors,
            isinstance(rollback.get("isolationBoundary"), str) and bool(rollback["isolationBoundary"]),
            f"{mode_id}: rollback.isolationBoundary must be a non-empty string",
        )

    disallowed = mode.get("disallowedOperations")
    _append(errors, isinstance(disallowed, list), f"{mode_id}: disallowedOperations must be a list")
    if isinstance(disallowed, list):
        operation_set = {item for item in disallowed if isinstance(item, str)}
        missing_baseline_deny = sorted(REQUIRED_BASELINE_DENY - operation_set)
        _append(
            errors,
            not missing_baseline_deny,
            f"{mode_id}: disallowedOperations missing mandatory baseline deny items: {missing_baseline_deny}",
        )
        missing = sorted(required_deny - operation_set)
        _append(errors, not missing, f"{mode_id}: disallowedOperations missing required deny items: {missing}")
        _append(
            errors,
            len(operation_set) > len(required_deny),
            f"{mode_id}: disallowedOperations must include at least one mode-specific deny entry",
        )

    smoke_evidence = mode.get("smokeEvidence")
    _append(errors, isinstance(smoke_evidence, dict), f"{mode_id}: smokeEvidence must be an object")
    if isinstance(smoke_evidence, dict):
        expected_paths = EXPECTED_SMOKE_EVIDENCE_PATHS.get(mode_id, {})
        for key in ("summary", "data"):
            value = smoke_evidence.get(key)
            expected_value = expected_paths.get(key)
            _append(
                errors,
                isinstance(value, str) and value == expected_value,
                f"{mode_id}: smokeEvidence.{key} must be exactly {expected_value}",
            )

    _append(
        errors,
        mode_id in mode_ids,
        f"{mode_id}: runtime mode not listed in expected modes",
    )
    return errors


def validate_manifest(manifest: dict) -> list[str]:
    errors = validate_top_level(manifest)
    required_deny = manifest.get("requiredDenyList")
    if not isinstance(required_deny, list):
        return errors
    required_deny_set = {item for item in required_deny if isinstance(item, str)}

    modes = manifest.get("modes")
    if not isinstance(modes, list):
        errors.append("modes must be a list")
        return errors

    mode_ids = [mode.get("id") for mode in modes if isinstance(mode, dict)]
    _append(errors, len(mode_ids) == len(set(mode_ids)), "mode ids must be unique")
    discovered_modes = {mode_id for mode_id in mode_ids if isinstance(mode_id, str)}
    _append(
        errors,
        discovered_modes == EXPECTED_MODES,
        f"modes must be exactly {sorted(EXPECTED_MODES)}; got {sorted(discovered_modes)}",
    )

    if isinstance(manifest.get("defaultMode"), str):
        default_mode_id = manifest["defaultMode"]
        default_mode = next((mode for mode in modes if isinstance(mode, dict) and mode.get("id") == default_mode_id), None)
        if isinstance(default_mode, dict):
            _append(
                errors,
                default_mode.get("enabledByDefault") is True,
                "defaultMode entry must have enabledByDefault=true",
            )

    scope_ids: list[str] = []
    for mode in modes:
        if not isinstance(mode, dict):
            errors.append("each mode entry must be an object")
            continue
        errors.extend(validate_mode(mode, required_deny_set, EXPECTED_MODES))
        boundaries = mode.get("boundaries", {})
        if isinstance(boundaries, dict):
            secrets = boundaries.get("secrets", {})
            if isinstance(secrets, dict) and isinstance(secrets.get("scopeId"), str):
                scope_ids.append(secrets["scopeId"])

    _append(
        errors,
        len(scope_ids) == len(set(scope_ids)) and len(scope_ids) == len(EXPECTED_MODES),
        "boundaries.secrets.scopeId must be unique per mode and present for all modes",
    )
    return errors


def validate_matrix(root: Path) -> list[str]:
    manifest = load_manifest(root)
    return validate_manifest(manifest)


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--root",
        type=Path,
        default=REPOSITORY_ROOT,
        help="Repository root path (defaults to auto-detected root).",
    )
    args = parser.parse_args(argv)
    root = args.root.resolve()
    errors = validate_matrix(root)
    if errors:
        print("XCore9 runtime matrix validation failed:")
        for error in errors:
            print(f"- {error}")
        return 1
    print("XCore9 runtime matrix validation passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
