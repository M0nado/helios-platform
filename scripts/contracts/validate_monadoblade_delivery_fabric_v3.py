#!/usr/bin/env python3
"""Dependency-free validation for Monadoblade six-profile delivery fabric v3."""

from __future__ import annotations

import argparse
import json
from pathlib import Path
import sys
import xml.etree.ElementTree as ET


REPOSITORY_ROOT = Path(__file__).resolve().parents[2]
V3_ROOT = REPOSITORY_ROOT / "config" / "monado-enterprise" / "v3"
PROFILE_MANIFEST_DIR = V3_ROOT / "profile-manifests"
XSD_PATH = REPOSITORY_ROOT / "schemas" / "monado-enterprise" / "v3" / "profile-manifest.v3.xsd"

EXPECTED_PROFILES = {
    "core",
    "developer",
    "studio",
    "gamer",
    "ai-server",
    "sysadmin",
}
EXPECTED_WORKFLOWS = {"recovery", "quarantine"}
EXPECTED_LIBRARY_SURFACES = {
    "policy",
    "evidence",
    "control-client",
    "shellkit",
    "renderer",
    "chroma",
    "wyvern",
    "usb-device-broker-requests",
}
REQUIRED_V1_PROFILE_IDS = {
    "developer",
    "sysadmin",
    "sysops",
    "gamer",
    "studio",
    "personal",
    "server-background",
}
REQUIRED_V2_PROFILE_IDS = {
    "core",
    "developer",
    "gamer",
    "studio",
    "personal",
    "sysops",
    "ai-server",
    "sysadmin",
}


def _read_json(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8"))


def _resolve_contract_path(root: Path, relative_path: str) -> Path:
    return root / Path(relative_path.replace("\\", "/"))


def _append(errors: list[str], condition: bool, message: str) -> None:
    if not condition:
        errors.append(message)


def validate_index(index: dict, root: Path) -> list[str]:
    errors: list[str] = []
    _append(errors, index.get("schemaVersion") == "3.0.0", "index.schemaVersion must be 3.0.0")
    _append(errors, index.get("status") == "authoritative", "index.status must be authoritative")
    _append(errors, index.get("executionMode") == "proposal-only", "index.executionMode must be proposal-only")
    _append(errors, index.get("destructiveApplyDefault") is False, "index.destructiveApplyDefault must be false")
    _append(errors, index.get("requiresApprovalEvidence") is True, "index.requiresApprovalEvidence must be true")

    contracts = index.get("contracts", {})
    required_contract_keys = {
        "profiles",
        "experience",
        "storage",
        "integrationProjection",
        "repositoryMap",
        "libraries",
    }
    _append(
        errors,
        isinstance(contracts, dict) and required_contract_keys.issubset(contracts.keys()),
        "index.contracts must include profiles/experience/storage/integrationProjection/repositoryMap/libraries",
    )
    if isinstance(contracts, dict):
        for key in required_contract_keys:
            value = contracts.get(key)
            if not isinstance(value, str):
                errors.append(f"index.contracts.{key} must be a string path")
                continue
            if not _resolve_contract_path(root, value).is_file():
                errors.append(f"index.contracts.{key} points to missing file: {value}")

    migration_map = index.get("migrationMap")
    if isinstance(migration_map, str):
        _append(errors, _resolve_contract_path(root, migration_map).is_file(), "index.migrationMap must exist")
    else:
        errors.append("index.migrationMap must be a string path")

    manifest_directory = index.get("profileManifestDirectory")
    if isinstance(manifest_directory, str):
        _append(
            errors,
            _resolve_contract_path(root, manifest_directory).is_dir(),
            "index.profileManifestDirectory must exist",
        )
    else:
        errors.append("index.profileManifestDirectory must be a string path")

    manifest_schema = index.get("profileManifestSchema")
    if isinstance(manifest_schema, str):
        _append(
            errors,
            _resolve_contract_path(root, manifest_schema).is_file(),
            "index.profileManifestSchema must exist",
        )
    else:
        errors.append("index.profileManifestSchema must be a string path")

    related = index.get("relatedIssues", [])
    _append(
        errors,
        isinstance(related, list) and 207 in related,
        "index.relatedIssues must include issue #207",
    )

    gates = index.get("governanceGates", [])
    _append(errors, isinstance(gates, list) and len(gates) >= 1, "index.governanceGates must be non-empty")
    if isinstance(gates, list):
        gate_areas = {gate.get("area") for gate in gates if isinstance(gate, dict)}
        _append(
            errors,
            {"gui-and-usb-extraction", "runner-topology-evidence", "deployment-evidence"}.issubset(gate_areas),
            "index.governanceGates must include GUI/USB, runner topology, and deployment gates",
        )
    return errors


def validate_profiles_contract(profiles_contract: dict) -> list[str]:
    errors: list[str] = []
    _append(errors, profiles_contract.get("defaultProfile") == "core", "profiles.defaultProfile must be core")

    profiles = profiles_contract.get("profiles", [])
    profile_ids = {profile.get("id") for profile in profiles if isinstance(profile, dict)}
    _append(
        errors,
        profile_ids == EXPECTED_PROFILES,
        f"profiles set mismatch: expected {sorted(EXPECTED_PROFILES)} got {sorted(profile_ids)}",
    )

    admins = [profile for profile in profiles if isinstance(profile, dict) and profile.get("administrator") is True]
    _append(errors, len(admins) == 1, "exactly one administrator profile is required")
    if admins:
        sysadmin = admins[0]
        _append(errors, sysadmin.get("id") == "sysadmin", "sysadmin must be the only administrator")
        _append(errors, sysadmin.get("hidden") is True, "sysadmin must be hidden")
        _append(errors, sysadmin.get("enabledByDefault") is False, "sysadmin must be disabled by default")
        _append(errors, sysadmin.get("localOnly") is True, "sysadmin must be local-only")
        _append(errors, sysadmin.get("offlineRequired") is True, "sysadmin must require offline operation")
        activation = sysadmin.get("activation", {})
        if isinstance(activation, dict):
            _append(errors, activation.get("physicalPresenceRequired") is True, "sysadmin requires physical presence")
            _append(errors, activation.get("minimumFactors", 0) >= 2, "sysadmin requires at least two factors")
            _append(errors, activation.get("remoteActivationDenied") is True, "sysadmin must deny remote activation")
            _append(errors, activation.get("cloudActivationDenied") is True, "sysadmin must deny cloud activation")
            _append(errors, activation.get("aiActivationDenied") is True, "sysadmin must deny AI activation")
        else:
            errors.append("sysadmin.activation must be an object")

    workflows = profiles_contract.get("workflows", [])
    workflow_ids = {workflow.get("id") for workflow in workflows if isinstance(workflow, dict)}
    _append(errors, workflow_ids == EXPECTED_WORKFLOWS, "workflows must be recovery and quarantine")
    for workflow in workflows:
        if not isinstance(workflow, dict):
            continue
        _append(errors, workflow.get("kind") == "sysadmin-workflow", "workflow kind must be sysadmin-workflow")
        _append(errors, workflow.get("interactiveProfile") == "sysadmin", "workflow interactiveProfile must be sysadmin")
        _append(errors, workflow.get("entryRequiresApproval") is True, "workflow entryRequiresApproval must be true")

    invariants = profiles_contract.get("invariants", {})
    for key in (
        "profileCountIsSix",
        "onlySysAdminIsAdministrator",
        "sysAdminHiddenAndDisabledByDefault",
        "sysAdminIsLocalOfflineOnly",
        "recoveryAndQuarantineAreWorkflowsNotProfiles",
    ):
        _append(errors, isinstance(invariants, dict) and invariants.get(key) is True, f"invariant {key} must be true")
    return errors


def validate_experience_contract(experience_contract: dict) -> list[str]:
    errors: list[str] = []

    shell = experience_contract.get("postAuthShell", {})
    _append(
        errors,
        isinstance(shell, dict) and shell.get("runsAfterWindowsAuthentication") is True,
        "experience.postAuthShell must run after Windows authentication",
    )
    _append(
        errors,
        isinstance(shell, dict) and shell.get("replacesWindowsCredentialProvider") is False,
        "experience.postAuthShell must not replace Windows credential provider",
    )

    state_machine = shell.get("stateMachine", {}) if isinstance(shell, dict) else {}
    states = state_machine.get("states", []) if isinstance(state_machine, dict) else []
    required_states = {"safe-boot", "identity-verified", "wheel-select", "shell-active", "safe-neutral-blocked"}
    _append(
        errors,
        isinstance(states, list) and required_states.issubset(set(states)),
        "experience state machine must include safe-boot, identity-verified, wheel-select, shell-active, safe-neutral-blocked",
    )

    alvis = experience_contract.get("alvis", {})
    _append(errors, isinstance(alvis, dict) and alvis.get("assistantId") == "ALVIS", "experience.ALVIS assistant id mismatch")
    _append(errors, isinstance(alvis, dict) and alvis.get("mode") == "reactive-assistant", "experience.ALVIS mode must be reactive-assistant")
    _append(errors, isinstance(alvis, dict) and alvis.get("administrator") is False, "experience.ALVIS must not be administrator")
    _append(errors, isinstance(alvis, dict) and alvis.get("executorToolsAllowed") is False, "experience.ALVIS executor tools must be disabled")

    classes = alvis.get("toolClasses", {}) if isinstance(alvis, dict) else {}
    if isinstance(classes, dict):
        _append(
            errors,
            set(classes.get("readOnlyPrefixes", [])) == {"search_", "fetch_"},
            "experience.ALVIS readOnlyPrefixes must be search_ and fetch_",
        )
        _append(
            errors,
            set(classes.get("planOnlyPrefixes", [])) == {"plan_"},
            "experience.ALVIS planOnlyPrefixes must be plan_",
        )
        _append(
            errors,
            set(classes.get("approvalPendingPrefixes", [])) == {"request_"},
            "experience.ALVIS approvalPendingPrefixes must be request_",
        )
    else:
        errors.append("experience.ALVIS toolClasses must be an object")

    profile_map = experience_contract.get("profiles", {})
    _append(
        errors,
        isinstance(profile_map, dict) and EXPECTED_PROFILES.issubset(profile_map.keys()),
        "experience.profiles must include all six profiles",
    )

    dedicated = experience_contract.get("dedicatedTargets", {})
    _append(
        errors,
        isinstance(dedicated, dict) and dedicated.get("linkagePolicy") == "link-not-duplicate",
        "experience.dedicatedTargets.linkagePolicy must be link-not-duplicate",
    )
    _append(
        errors,
        isinstance(dedicated, dict) and dedicated.get("guiGateIssue") == 149,
        "experience.dedicatedTargets.guiGateIssue must be 149",
    )
    _append(
        errors,
        isinstance(dedicated, dict) and dedicated.get("usbWizardGateIssue") == 149,
        "experience.dedicatedTargets.usbWizardGateIssue must be 149",
    )
    return errors


def validate_storage_contract(storage_contract: dict) -> list[str]:
    errors: list[str] = []
    policy = storage_contract.get("executionPolicy", {})
    _append(errors, isinstance(policy, dict) and policy.get("mode") == "proposal-only", "storage.mode must be proposal-only")
    _append(errors, isinstance(policy, dict) and policy.get("destructiveApplyDefault") is False, "storage.destructiveApplyDefault must be false")

    routes = storage_contract.get("usbWizardRoutes", {})
    if isinstance(routes, dict):
        inventory = routes.get("inventory", {})
        request = routes.get("requestStoragePlan", {})
        _append(errors, isinstance(inventory, dict) and inventory.get("mode") == "dry-run", "usb inventory route must be dry-run")
        _append(errors, isinstance(inventory, dict) and inventory.get("writesEnabled") is False, "usb inventory route must be read-only")
        _append(errors, isinstance(request, dict) and request.get("mode") == "proposal-only", "usb request route must be proposal-only")
        _append(errors, isinstance(request, dict) and request.get("writesEnabled") is False, "usb request route must be read-only")
        _append(errors, isinstance(request, dict) and request.get("requiresApproval") is True, "usb request route must require approval")
        _append(errors, routes.get("applyRouteEnabled") is False, "usb apply route must be disabled")
    else:
        errors.append("storage.usbWizardRoutes must be an object")

    ownership = storage_contract.get("workflowOwnership", {})
    _append(errors, isinstance(ownership, dict) and ownership.get("recovery") == "sysadmin", "storage.recovery workflow owner must be sysadmin")
    _append(errors, isinstance(ownership, dict) and ownership.get("quarantine") == "sysadmin", "storage.quarantine workflow owner must be sysadmin")

    guardrails = storage_contract.get("guardrails", {})
    _append(
        errors,
        isinstance(guardrails, dict) and guardrails.get("denyPhysicalUsbWritesFromWizard") is True,
        "storage guardrail denyPhysicalUsbWritesFromWizard must be true",
    )

    forbidden = storage_contract.get("forbiddenRuntimeActions", [])
    _append(errors, isinstance(forbidden, list) and "physical-usb-write" in forbidden, "storage forbiddenRuntimeActions must include physical-usb-write")
    _append(errors, isinstance(forbidden, list) and "vhdx-apply-from-runtime-disk" in forbidden, "storage forbiddenRuntimeActions must include vhdx-apply-from-runtime-disk")
    return errors


def validate_projection_contract(projection_contract: dict) -> list[str]:
    errors: list[str] = []
    _append(errors, projection_contract.get("executionMode") == "projection-only", "projection.executionMode must be projection-only")
    _append(errors, projection_contract.get("directExternalDeliveryEnabled") is False, "projection.directExternalDeliveryEnabled must be false")
    _append(errors, projection_contract.get("executionTriggersAllowed") is False, "projection.executionTriggersAllowed must be false")

    surfaces = projection_contract.get("surfaces", {})
    required_surfaces = {"linear", "slack", "teams", "sharepoint", "azure-devops"}
    _append(
        errors,
        isinstance(surfaces, dict) and required_surfaces.issubset(surfaces.keys()),
        "projection.surfaces must include linear/slack/teams/sharepoint/azure-devops",
    )
    if isinstance(surfaces, dict):
        for name in required_surfaces:
            surface = surfaces.get(name, {})
            _append(errors, isinstance(surface, dict) and surface.get("projectionOnly") is True, f"projection surface {name} must be projectionOnly")
            _append(errors, isinstance(surface, dict) and surface.get("triggersExecution") is False, f"projection surface {name} must not trigger execution")

    rules = projection_contract.get("projectionRules", [])
    _append(errors, isinstance(rules, list) and len(rules) >= 1, "projectionRules must contain at least one route")
    if isinstance(rules, list):
        for rule in rules:
            if not isinstance(rule, dict):
                continue
            _append(errors, rule.get("source") == "github", "projection rule source must be github")
            _append(errors, rule.get("payloadMode") == "links-only", "projection rule payloadMode must be links-only")
            targets = rule.get("targets", [])
            _append(
                errors,
                isinstance(targets, list) and set(targets).issubset(required_surfaces),
                "projection rule targets must be allowed projection surfaces",
            )
    return errors


def validate_repository_map_contract(repository_map: dict) -> list[str]:
    errors: list[str] = []
    _append(
        errors,
        repository_map.get("canonicalRepository") == "M0nado/helios-platform",
        "repositoryMap canonicalRepository must be M0nado/helios-platform",
    )

    dedicated = repository_map.get("dedicatedTargets", {})
    gui = dedicated.get("gui", {}) if isinstance(dedicated, dict) else {}
    usb = dedicated.get("usbWizard", {}) if isinstance(dedicated, dict) else {}
    _append(
        errors,
        isinstance(gui, dict) and gui.get("integrationMode") == "linked-not-duplicated",
        "repositoryMap GUI integration mode must be linked-not-duplicated",
    )
    _append(
        errors,
        isinstance(usb, dict) and usb.get("integrationMode") == "linked-not-duplicated",
        "repositoryMap USB integration mode must be linked-not-duplicated",
    )

    status_sources = repository_map.get("projectStatusSources", {})
    _append(
        errors,
        isinstance(status_sources, dict) and str(status_sources.get("githubIssue", "")).endswith("/issues/207"),
        "repositoryMap projectStatusSources.githubIssue must point to issue #207",
    )

    constraints = repository_map.get("constraints", {})
    _append(
        errors,
        isinstance(constraints, dict) and constraints.get("noExecutionTriggerFromProjectionSurfaces") is True,
        "repositoryMap constraint noExecutionTriggerFromProjectionSurfaces must be true",
    )
    return errors


def validate_libraries_contract(libraries_contract: dict) -> list[str]:
    errors: list[str] = []
    libraries = libraries_contract.get("libraries", [])
    surfaces = {library.get("surface") for library in libraries if isinstance(library, dict)}
    _append(
        errors,
        surfaces == EXPECTED_LIBRARY_SURFACES,
        f"library surfaces mismatch: expected {sorted(EXPECTED_LIBRARY_SURFACES)} got {sorted(surfaces)}",
    )

    invariants = libraries_contract.get("invariants", {})
    for key in ("allSurfacesVersioned", "allSurfacesReusable", "executorSurfaceExcluded"):
        _append(errors, isinstance(invariants, dict) and invariants.get(key) is True, f"library invariant {key} must be true")
    return errors


def validate_migration_map_contract(migration_map_contract: dict) -> list[str]:
    errors: list[str] = []
    sources = migration_map_contract.get("legacySources", [])
    source_versions = {source.get("version") for source in sources if isinstance(source, dict)}
    _append(errors, {"v1", "v2"}.issubset(source_versions), "migration map must include v1 and v2 sources")

    mappings = migration_map_contract.get("mappings", [])
    v1_ids = {
        mapping.get("sourceProfileId")
        for mapping in mappings
        if isinstance(mapping, dict) and mapping.get("sourceVersion") == "v1"
    }
    v2_ids = {
        mapping.get("sourceProfileId")
        for mapping in mappings
        if isinstance(mapping, dict) and mapping.get("sourceVersion") == "v2"
    }
    _append(errors, REQUIRED_V1_PROFILE_IDS.issubset(v1_ids), "migration map must cover all required v1 profile IDs")
    _append(errors, REQUIRED_V2_PROFILE_IDS.issubset(v2_ids), "migration map must cover all required v2 profile IDs")

    invariants = migration_map_contract.get("invariants", {})
    for key in ("legacyArtifactsRemainUnmodified", "noHistoryRewrite", "targetProfileSetIsSix"):
        _append(errors, isinstance(invariants, dict) and invariants.get(key) is True, f"migration invariant {key} must be true")
    return errors


def validate_profile_manifests(manifest_directory: Path, expected_profiles: set[str]) -> list[str]:
    errors: list[str] = []
    _append(errors, manifest_directory.is_dir(), "profile manifest directory is missing")
    _append(errors, XSD_PATH.is_file(), "profile manifest XSD is missing")
    if errors:
        return errors

    manifest_files = sorted(manifest_directory.glob("*.xml"))
    _append(errors, len(manifest_files) == len(expected_profiles), "profile manifest count must match expected profile count")

    discovered_profiles: set[str] = set()
    for manifest in manifest_files:
        try:
            root = ET.fromstring(manifest.read_text(encoding="utf-8"))
        except ET.ParseError as exc:
            errors.append(f"{manifest.name}: XML parse error: {exc}")
            continue
        if root.tag != "MonadoProfileManifest":
            errors.append(f"{manifest.name}: root element must be MonadoProfileManifest")
            continue
        profile_id = root.attrib.get("profileId")
        version = root.attrib.get("version")
        if profile_id:
            discovered_profiles.add(profile_id)
        _append(errors, version == "3.0.0", f"{manifest.name}: version must be 3.0.0")

        required_children = {
            "DisplayName",
            "Glyph",
            "SemanticUi",
            "Theme",
            "ChromaProfile",
            "WyvernAudioProfile",
            "ServiceBudget",
            "ProcessBudget",
            "NetworkPolicy",
            "TelemetryPolicy",
            "AlvisToolBudget",
            "StateModel",
        }
        children = {child.tag for child in root}
        missing_children = sorted(required_children - children)
        if missing_children:
            errors.append(f"{manifest.name}: missing required elements: {', '.join(missing_children)}")

    _append(errors, discovered_profiles == expected_profiles, "profile manifest IDs must match expected six-profile set")
    return errors


def load_contract_bundle(root: Path) -> dict[str, dict]:
    index = _read_json(root / "config" / "monado-enterprise" / "v3" / "index.json")
    contracts = index["contracts"]
    return {
        "index": index,
        "profiles": _read_json(_resolve_contract_path(root, contracts["profiles"])),
        "experience": _read_json(_resolve_contract_path(root, contracts["experience"])),
        "storage": _read_json(_resolve_contract_path(root, contracts["storage"])),
        "integrationProjection": _read_json(_resolve_contract_path(root, contracts["integrationProjection"])),
        "repositoryMap": _read_json(_resolve_contract_path(root, contracts["repositoryMap"])),
        "libraries": _read_json(_resolve_contract_path(root, contracts["libraries"])),
        "migrationMap": _read_json(_resolve_contract_path(root, index["migrationMap"])),
    }


def validate_contract_bundle(root: Path) -> list[str]:
    errors: list[str] = []
    bundle = load_contract_bundle(root)
    index = bundle["index"]
    errors.extend(validate_index(index, root))
    errors.extend(validate_profiles_contract(bundle["profiles"]))
    errors.extend(validate_experience_contract(bundle["experience"]))
    errors.extend(validate_storage_contract(bundle["storage"]))
    errors.extend(validate_projection_contract(bundle["integrationProjection"]))
    errors.extend(validate_repository_map_contract(bundle["repositoryMap"]))
    errors.extend(validate_libraries_contract(bundle["libraries"]))
    errors.extend(validate_migration_map_contract(bundle["migrationMap"]))
    errors.extend(validate_profile_manifests(_resolve_contract_path(root, index["profileManifestDirectory"]), EXPECTED_PROFILES))
    return errors


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
    errors = validate_contract_bundle(root)
    if errors:
        print("Monadoblade six-profile delivery fabric v3 validation failed:")
        for error in errors:
            print(f"- {error}")
        return 1
    print("Monadoblade six-profile delivery fabric v3 validation passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
