#!/usr/bin/env python3
"""Fail-closed validation for Monado enterprise experience fabric v2 contracts."""

from __future__ import annotations

import argparse
import json
import sys
import xml.etree.ElementTree as ET
from collections.abc import Mapping
from pathlib import Path


REPOSITORY_ROOT = Path(__file__).resolve().parents[2]
BASE = REPOSITORY_ROOT / "config" / "monadoblade" / "experience-fabric"

EXPECTED_JSON = {
    "monado-enterprise-experience-fabric.v2.json",
    "storage.contract.v2.json",
    "profile-catalog.v2.json",
    "profile-experience.contract.v2.json",
    "chroma-wyvern.contract.v2.json",
    "alvis-tool-budgets.v2.json",
    "repository-ownership.contract.v2.json",
    "synchronization.contract.v2.json",
    "openai-proposal.schema.v2.json",
}

EXPECTED_PROFILES = {
    "core",
    "developer",
    "gamer",
    "studio",
    "personal",
    "sysops",
    "ai-server",
    "sysadmin",
}

XML_NS = {"m": "https://helios-platform.dev/schemas/monado-profile-v2"}
EXPECTED_CONTRACT_KEYS = {
    "storage",
    "profileCatalog",
    "profileExperience",
    "chromaWyvern",
    "alvisBudget",
    "repositoryOwnership",
    "synchronization",
    "openAiProposalSchema",
    "profileXmlSchema",
}
EXPECTED_AZURE_DEVOPS_MODE = "read-only-mirror-until-separate-approved-identity"
EXPECTED_ENVELOPE_FIELDS = {
    "eventId",
    "source",
    "eventType",
    "repository",
    "correlationId",
    "environment",
    "occurredAt",
    "dataClassification",
    "links",
    "payload",
}


def _load_json(path: Path) -> dict:
    with path.open("r", encoding="utf-8") as handle:
        payload = json.load(handle)
    if not isinstance(payload, dict):
        raise ValueError(f"{path}: contract root must be an object")
    return payload


def _require(condition: bool, message: str, errors: list[str]) -> None:
    if not condition:
        errors.append(message)


def _resolve_contract_path(base: Path, relative_path: str) -> Path:
    normalized = Path(relative_path.replace("\\", "/"))
    if normalized.parts[:3] == ("config", "monadoblade", "experience-fabric"):
        return base / Path(*normalized.parts[3:])
    return base / normalized


def validate_contracts(base: Path = BASE) -> list[str]:
    errors: list[str] = []
    for name in sorted(EXPECTED_JSON):
        path = base / name
        if not path.exists():
            errors.append(f"missing contract file: {path}")

    if errors:
        return errors

    root = _load_json(base / "monado-enterprise-experience-fabric.v2.json")
    _require(root.get("status") == "authoritative-proposal-only", "root status must remain authoritative-proposal-only", errors)
    execution = root.get("execution", {})
    _require(isinstance(execution, Mapping), "root execution must be an object", errors)
    _require(execution.get("destructiveApplyDefault") is False, "destructive apply must be disabled by default", errors)
    _require(execution.get("runtimeSideEffectsAllowed") is False, "runtime side effects must be disabled", errors)
    contracts = root.get("contracts", {})
    _require(isinstance(contracts, Mapping), "root contracts must be an object", errors)
    if isinstance(contracts, Mapping):
        _require(EXPECTED_CONTRACT_KEYS.issubset(contracts.keys()), "root contracts mapping is incomplete", errors)
        for key in EXPECTED_CONTRACT_KEYS:
            path = contracts.get(key)
            _require(isinstance(path, str) and path.strip() != "", f"root contracts.{key} must be a non-empty path", errors)
            if isinstance(path, str) and path.strip() != "":
                _require(_resolve_contract_path(base, path).is_file(), f"root contracts.{key} points to a missing file", errors)

    storage = _load_json(base / "storage.contract.v2.json")
    _require(storage.get("destructiveApplyDefault") is False, "storage destructiveApplyDefault must be false", errors)
    _require(storage.get("requireWhatIf") is True, "storage requireWhatIf must be true", errors)
    _require(storage.get("executionMode") == "proposal-only", "storage executionMode must be proposal-only", errors)
    exact_size_lock = storage.get("exactSizeLock", {})
    _require(isinstance(exact_size_lock, Mapping), "storage exactSizeLock must be an object", errors)
    _require(
        isinstance(exact_size_lock, Mapping) and exact_size_lock.get("status") == "unresolved",
        "storage exactSizeLock.status must remain unresolved",
        errors,
    )
    _require(
        isinstance(exact_size_lock, Mapping) and exact_size_lock.get("allowMutationBeforeResolution") is False,
        "storage exactSizeLock must deny mutation before resolution",
        errors,
    )
    guardrails = storage.get("guardrails", {})
    _require(isinstance(guardrails, Mapping), "storage guardrails must be an object", errors)
    for key in (
        "denyBlindDiskSelection",
        "denyDirectDiskMutationFromRuntime",
        "requireBackupEvidenceBeforeAnyApply",
        "requireRollbackPlanBeforeAnyApply",
    ):
        _require(
            isinstance(guardrails, Mapping) and guardrails.get(key) is True,
            f"storage guardrail {key} must be true",
            errors,
        )
    topology = storage.get("topology", {})
    _require(isinstance(topology, Mapping), "storage topology must be an object", errors)
    disk0 = topology.get("disk0", {})
    disk1 = topology.get("disk1", {})
    _require(isinstance(disk0, Mapping), "storage disk0 must be an object", errors)
    _require(isinstance(disk1, Mapping), "storage disk1 must be an object", errors)
    disk0_letters = {item.get("targetLetter") for item in disk0.get("partitions", []) if isinstance(item, Mapping)}
    _require({"C", "R", "X"}.issubset(disk0_letters), "disk0 must define C/R/X partitions", errors)
    vhdx_items = [item for item in disk1.get("vhdx", []) if isinstance(item, Mapping)]
    devdrive = next((item for item in vhdx_items if item.get("id") == "devdrive"), None)
    vault = next((item for item in vhdx_items if item.get("id") == "vault"), None)
    _require(devdrive is not None and devdrive.get("targetLetter") == "D", "devdrive vhdx must target D", errors)
    _require(vault is not None and vault.get("targetLetter") == "V", "vault vhdx must target V", errors)
    _require(vault is not None and vault.get("autoMount") is False, "vault vhdx must never auto-mount", errors)

    catalog = _load_json(base / "profile-catalog.v2.json")
    profiles = catalog.get("profiles", [])
    ids = {entry.get("id") for entry in profiles if isinstance(entry, Mapping)}
    _require(ids == EXPECTED_PROFILES, f"profile set mismatch: {sorted(ids)}", errors)
    administrators = [entry for entry in profiles if isinstance(entry, Mapping) and entry.get("administrator") is True]
    _require(
        len(administrators) == 1 and administrators[0].get("id") == "sysadmin",
        "sysadmin must be the sole administrator profile",
        errors,
    )
    sysadmin = next((entry for entry in profiles if isinstance(entry, Mapping) and entry.get("id") == "sysadmin"), None)
    _require(sysadmin is not None and sysadmin.get("administrator") is True, "sysadmin must be administrator", errors)
    _require(sysadmin is not None and sysadmin.get("hidden") is True and sysadmin.get("enabledByDefault") is False, "sysadmin hidden/disabled boundary is required", errors)
    activation = sysadmin.get("activation", {}) if isinstance(sysadmin, Mapping) else {}
    _require(isinstance(activation, Mapping), "sysadmin activation must be an object", errors)
    if isinstance(activation, Mapping):
        _require(activation.get("physicalPresenceRequired") is True, "sysadmin activation requires physical presence", errors)
        _require(activation.get("remoteActivationDenied") is True, "sysadmin activation must deny remote activation", errors)
        _require(activation.get("cloudActivationDenied") is True, "sysadmin activation must deny cloud activation", errors)
        _require(activation.get("aiActivationDenied") is True, "sysadmin activation must deny AI activation", errors)
        _require(activation.get("minimumFactors", 0) >= 2, "sysadmin activation requires at least two local factors", errors)
    states = set(catalog.get("states", []))
    overlays = set(catalog.get("overlays", []))
    _require({"recovery", "quarantine"}.issubset(states), "recovery and quarantine must be states", errors)
    _require("airgap" in overlays, "airgap must be an overlay", errors)

    experience = _load_json(base / "profile-experience.contract.v2.json")
    profile_experience = experience.get("profiles", {})
    _require(isinstance(profile_experience, Mapping), "profile experience profiles must be an object", errors)
    _require(set(profile_experience.keys()) == EXPECTED_PROFILES, "profile experience must define every expected profile", errors)
    common_core = experience.get("commonCoreInstall", {})
    _require(common_core.get("singleInstallAuthority") is True, "common core must be single install authority", errors)
    _require(common_core.get("profileLinksOnly") is True, "common core must use profile links only", errors)

    chroma = _load_json(base / "chroma-wyvern.contract.v2.json")
    safe_default = chroma.get("safeDefault", {})
    _require(isinstance(safe_default, Mapping), "chroma safeDefault must be an object", errors)
    _require(
        isinstance(safe_default, Mapping) and safe_default.get("deviceWritesDeniedWithoutApproval") is True,
        "chroma safeDefault must deny device writes without approval",
        errors,
    )
    _require(
        isinstance(safe_default, Mapping) and safe_default.get("persistentKernelDriverInstallDeniedAtRuntime") is True,
        "chroma safeDefault must deny runtime kernel driver installs",
        errors,
    )
    chroma_profiles = chroma.get("profiles", {})
    _require(isinstance(chroma_profiles, Mapping), "chroma profiles must be an object", errors)
    _require(set(chroma_profiles.keys()) == EXPECTED_PROFILES, "chroma contract must define every expected profile", errors)

    alvis = _load_json(base / "alvis-tool-budgets.v2.json")
    _require(alvis.get("administratorDenied") is True, "ALVIS administratorDenied must be true", errors)
    _require(alvis.get("externalWriteRequiresApproval") is True, "ALVIS externalWriteRequiresApproval must be true", errors)
    alvis_profiles = alvis.get("profiles", {})
    _require(isinstance(alvis_profiles, Mapping), "ALVIS profiles must be an object", errors)
    _require(set(alvis_profiles.keys()) == EXPECTED_PROFILES, "ALVIS profiles must match expected profile set", errors)
    if isinstance(alvis_profiles, Mapping):
        for profile_id in EXPECTED_PROFILES:
            policy = alvis_profiles.get(profile_id)
            _require(isinstance(policy, Mapping), f"ALVIS profile {profile_id} must define a policy object", errors)
            if not isinstance(policy, Mapping):
                continue
            max_calls = policy.get("maxToolCallsPerPlan")
            _require(
                isinstance(max_calls, int) and max_calls > 0,
                f"ALVIS profile {profile_id} maxToolCallsPerPlan must be a positive integer",
                errors,
            )
            _require(
                isinstance(policy.get("allowPrivilegedProposal"), bool),
                f"ALVIS profile {profile_id} allowPrivilegedProposal must be boolean",
                errors,
            )
            if profile_id == "sysadmin":
                _require(
                    policy.get("applyDeniedWithoutExplicitApproval") is True,
                    "ALVIS sysadmin applyDeniedWithoutExplicitApproval must be true",
                    errors,
                )

    sync = _load_json(base / "synchronization.contract.v2.json")
    _require(sync.get("defaultMode") == "propose-and-validate-only", "synchronization defaultMode must remain propose-and-validate-only", errors)
    systems = sync.get("systems", {})
    _require(isinstance(systems, Mapping), "synchronization systems must be an object", errors)
    devops = systems.get("azure-devops", {})
    _require(
        isinstance(devops, Mapping) and devops.get("mode") == EXPECTED_AZURE_DEVOPS_MODE,
        "azure-devops mode must match the approved read-only mirror mode",
        errors,
    )
    envelope_fields = set(sync.get("envelope", {}).get("requiredFields", []))
    _require(EXPECTED_ENVELOPE_FIELDS.issubset(envelope_fields), "synchronization envelope is missing required normalized fields", errors)

    ownership = _load_json(base / "repository-ownership.contract.v2.json")
    _require(ownership.get("canonicalPlatform") == "M0nado/helios-platform", "repository ownership canonicalPlatform mismatch", errors)
    codeowners_paths = ownership.get("codeownersReadyPaths", [])
    _require(isinstance(codeowners_paths, list) and len(codeowners_paths) >= 3, "repository ownership must define CODEOWNERS-ready paths", errors)

    openai_schema = _load_json(base / "openai-proposal.schema.v2.json")
    _require(openai_schema.get("type") == "object", "OpenAI proposal schema must define object root", errors)
    _require(openai_schema.get("additionalProperties") is False, "OpenAI proposal schema must fail closed on additional properties", errors)
    required = set(openai_schema.get("required", []))
    _require({"proposalId", "correlationId", "approval", "rollbackPlan", "expiresAtUtc"}.issubset(required), "OpenAI proposal schema must require proposal/approval/rollback fields", errors)
    schema_guards = openai_schema.get("allOf", [])
    _require(isinstance(schema_guards, list) and len(schema_guards) > 0, "OpenAI proposal schema must define conditional safety guards", errors)

    return errors


def validate_profile_xml(base: Path = BASE) -> list[str]:
    errors: list[str] = []
    xml_root = base / "xml"
    xsd_path = xml_root / "profile-manifest.v2.xsd"
    if not xsd_path.exists():
        return [f"missing XSD: {xsd_path}"]

    xsd_text = xsd_path.read_text(encoding="utf-8")
    if "<xs:element name=\"ProfileManifest\"" not in xsd_text:
        errors.append("XSD must define ProfileManifest root element")

    xml_files = sorted(xml_root.glob("*.profile.v2.xml"))
    if len(xml_files) != 8:
        errors.append(f"expected 8 profile XML manifests, found {len(xml_files)}")
        return errors

    seen_ids: set[str] = set()
    for path in xml_files:
        try:
            tree = ET.parse(path)
        except ET.ParseError as exc:
            errors.append(f"{path}: invalid XML: {exc}")
            continue
        root = tree.getroot()
        _require(
            root.tag == "{https://helios-platform.dev/schemas/monado-profile-v2}ProfileManifest",
            f"{path}: root element must be ProfileManifest in the monado-profile-v2 namespace",
            errors,
        )
        profile_id = root.attrib.get("profileId")
        schema_version = root.attrib.get("schemaVersion")
        seen_ids.add(profile_id or "")
        _require(schema_version == "2.0.0", f"{path}: schemaVersion must be 2.0.0", errors)
        _require(profile_id in EXPECTED_PROFILES, f"{path}: profileId must be one of expected profiles", errors)

        for element in ("SemanticUi", "ServiceMode", "NetworkMode", "TelemetryClass", "AlvisMaxToolCallsPerPlan"):
            node = root.find(f"m:{element}", XML_NS)
            _require(node is not None and (node.text or "").strip() != "", f"{path}: missing {element}", errors)
        max_calls_node = root.find("m:AlvisMaxToolCallsPerPlan", XML_NS)
        max_calls_text = (max_calls_node.text or "").strip() if max_calls_node is not None else ""
        _require(
            max_calls_text.isdigit() and int(max_calls_text) > 0,
            f"{path}: AlvisMaxToolCallsPerPlan must be a positive integer",
            errors,
        )

    _require(seen_ids == EXPECTED_PROFILES, f"XML profile set mismatch: {sorted(seen_ids)}", errors)
    return errors


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base", type=Path, default=BASE)
    args = parser.parse_args(argv)

    errors = validate_contracts(args.base.resolve())
    errors.extend(validate_profile_xml(args.base.resolve()))
    if errors:
        print("Monado v2 contract validation failed:", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1
    print("Monado v2 contract validation passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
