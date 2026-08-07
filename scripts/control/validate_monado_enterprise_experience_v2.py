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


def _load_json(path: Path) -> dict:
    with path.open("r", encoding="utf-8") as handle:
        payload = json.load(handle)
    if not isinstance(payload, dict):
        raise ValueError(f"{path}: contract root must be an object")
    return payload


def _require(condition: bool, message: str, errors: list[str]) -> None:
    if not condition:
        errors.append(message)


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

    storage = _load_json(base / "storage.contract.v2.json")
    _require(storage.get("destructiveApplyDefault") is False, "storage destructiveApplyDefault must be false", errors)
    _require(storage.get("requireWhatIf") is True, "storage requireWhatIf must be true", errors)
    _require(storage.get("executionMode") == "proposal-only", "storage executionMode must be proposal-only", errors)
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
    sysadmin = next((entry for entry in profiles if isinstance(entry, Mapping) and entry.get("id") == "sysadmin"), None)
    _require(sysadmin is not None and sysadmin.get("administrator") is True, "sysadmin must be administrator", errors)
    _require(sysadmin is not None and sysadmin.get("hidden") is True and sysadmin.get("enabledByDefault") is False, "sysadmin hidden/disabled boundary is required", errors)
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
    chroma_profiles = chroma.get("profiles", {})
    _require(isinstance(chroma_profiles, Mapping), "chroma profiles must be an object", errors)
    _require(set(chroma_profiles.keys()) == EXPECTED_PROFILES, "chroma contract must define every expected profile", errors)

    alvis = _load_json(base / "alvis-tool-budgets.v2.json")
    _require(alvis.get("administratorDenied") is True, "ALVIS administratorDenied must be true", errors)
    _require(alvis.get("externalWriteRequiresApproval") is True, "ALVIS externalWriteRequiresApproval must be true", errors)
    alvis_profiles = alvis.get("profiles", {})
    _require(isinstance(alvis_profiles, Mapping), "ALVIS profiles must be an object", errors)
    _require(set(alvis_profiles.keys()) == EXPECTED_PROFILES, "ALVIS profiles must match expected profile set", errors)

    sync = _load_json(base / "synchronization.contract.v2.json")
    _require(sync.get("defaultMode") == "propose-and-validate-only", "synchronization defaultMode must remain propose-and-validate-only", errors)
    systems = sync.get("systems", {})
    _require(isinstance(systems, Mapping), "synchronization systems must be an object", errors)
    devops = systems.get("azure-devops", {})
    _require(isinstance(devops, Mapping) and str(devops.get("mode", "")).startswith("read-only"), "azure-devops must remain read-only", errors)
    required_envelope = {"eventId", "source", "eventType", "repository", "correlationId", "occurredAt", "payload"}
    envelope_fields = set(sync.get("envelope", {}).get("requiredFields", []))
    _require(required_envelope.issubset(envelope_fields), "synchronization envelope is missing required normalized fields", errors)

    ownership = _load_json(base / "repository-ownership.contract.v2.json")
    _require(ownership.get("canonicalPlatform") == "M0nado/helios-platform", "repository ownership canonicalPlatform mismatch", errors)
    codeowners_paths = ownership.get("codeownersReadyPaths", [])
    _require(isinstance(codeowners_paths, list) and len(codeowners_paths) >= 3, "repository ownership must define CODEOWNERS-ready paths", errors)

    openai_schema = _load_json(base / "openai-proposal.schema.v2.json")
    _require(openai_schema.get("type") == "object", "OpenAI proposal schema must define object root", errors)
    _require(openai_schema.get("additionalProperties") is False, "OpenAI proposal schema must fail closed on additional properties", errors)
    required = set(openai_schema.get("required", []))
    _require({"proposalId", "correlationId", "approval", "rollbackPlan", "expiresAtUtc"}.issubset(required), "OpenAI proposal schema must require proposal/approval/rollback fields", errors)

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
        profile_id = root.attrib.get("profileId")
        schema_version = root.attrib.get("schemaVersion")
        seen_ids.add(profile_id or "")
        _require(schema_version == "2.0.0", f"{path}: schemaVersion must be 2.0.0", errors)
        _require(profile_id in EXPECTED_PROFILES, f"{path}: profileId must be one of expected profiles", errors)

        for element in ("SemanticUi", "ServiceMode", "NetworkMode", "TelemetryClass", "AlvisMaxToolCallsPerPlan"):
            node = root.find(f"m:{element}", XML_NS)
            _require(node is not None and (node.text or "").strip() != "", f"{path}: missing {element}", errors)

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
