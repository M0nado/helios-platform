#!/usr/bin/env python3
"""Dependency-free validation for Monado enterprise experience fabric v2 contracts."""

from __future__ import annotations

import argparse
import json
from pathlib import Path
import sys
import xml.etree.ElementTree as ET


REPOSITORY_ROOT = Path(__file__).resolve().parents[2]
V2_ROOT = REPOSITORY_ROOT / "config" / "monado-enterprise" / "v2"
PROFILE_MANIFEST_DIR = V2_ROOT / "profile-manifests"
XSD_PATH = REPOSITORY_ROOT / "schemas" / "monado-enterprise" / "v2" / "profile-manifest.v2.xsd"

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
EXPECTED_STATE_IDS = {"recovery", "quarantine"}
EXPECTED_OVERLAY_IDS = {"airgap"}
XML_SCHEMA_NAMESPACE = {"xs": "http://www.w3.org/2001/XMLSchema"}
XML_BOOLEAN_LITERALS = {"true", "false", "1", "0"}


def _read_json(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8"))


def _resolve_contract_path(root: Path, relative_path: str) -> Path:
    return root / Path(relative_path.replace("\\", "/"))


def _append(errors: list[str], condition: bool, message: str) -> None:
    if not condition:
        errors.append(message)


def _is_non_negative_integer(value: str) -> bool:
    return value.isdigit()


def _is_xsd_boolean(value: str) -> bool:
    return value in XML_BOOLEAN_LITERALS


def _load_manifest_schema_constraints(xsd_path: Path) -> dict[str, object]:
    try:
        schema_root = ET.parse(xsd_path).getroot()
    except ET.ParseError as exc:  # pragma: no cover - defensive parse failure
        raise ValueError(f"profile manifest XSD parse error: {exc}") from exc

    profile_enum_nodes = schema_root.findall(
        "./xs:simpleType[@name='ProfileIdType']/xs:restriction/xs:enumeration",
        XML_SCHEMA_NAMESPACE,
    )
    profile_ids = {
        node.attrib.get("value", "")
        for node in profile_enum_nodes
        if node.attrib.get("value")
    }
    if not profile_ids:
        raise ValueError("profile manifest XSD does not define ProfileIdType enumerations")

    root_element = schema_root.find(
        "./xs:element[@name='MonadoProfileManifest']",
        XML_SCHEMA_NAMESPACE,
    )
    if root_element is None:
        raise ValueError("profile manifest XSD is missing MonadoProfileManifest root element")

    sequence_elements = root_element.findall(
        "./xs:complexType/xs:sequence/xs:element",
        XML_SCHEMA_NAMESPACE,
    )
    ordered_children = [element.attrib.get("name", "") for element in sequence_elements]
    if not ordered_children or any(not name for name in ordered_children):
        raise ValueError("profile manifest XSD has an invalid root element sequence")

    required_root_attributes: dict[str, str] = {}
    for attribute_node in root_element.findall(
        "./xs:complexType/xs:attribute[@use='required']",
        XML_SCHEMA_NAMESPACE,
    ):
        attribute_name = attribute_node.attrib.get("name")
        if not attribute_name:
            raise ValueError("profile manifest XSD contains a required root attribute without a name")
        required_root_attributes[attribute_name] = attribute_node.attrib.get("type", "xs:string")

    required_child_attributes: dict[str, dict[str, str]] = {}
    for element in sequence_elements:
        element_name = element.attrib.get("name")
        if not element_name:
            continue
        attributes: dict[str, str] = {}
        for attribute_node in element.findall(
            "./xs:complexType/xs:attribute[@use='required']",
            XML_SCHEMA_NAMESPACE,
        ):
            attribute_name = attribute_node.attrib.get("name")
            if not attribute_name:
                raise ValueError(f"profile manifest XSD contains unnamed required attribute on {element_name}")
            attributes[attribute_name] = attribute_node.attrib.get("type", "xs:string")
        if attributes:
            required_child_attributes[element_name] = attributes

    return {
        "root_name": "MonadoProfileManifest",
        "profile_ids": profile_ids,
        "ordered_children": ordered_children,
        "required_root_attributes": required_root_attributes,
        "required_child_attributes": required_child_attributes,
    }


def _xsd_value_is_valid(value: str, value_type: str, profile_ids: set[str]) -> bool:
    if value_type == "xs:nonNegativeInteger":
        return _is_non_negative_integer(value)
    if value_type == "xs:boolean":
        return _is_xsd_boolean(value)
    if value_type == "ProfileIdType":
        return value in profile_ids
    return len(value) > 0


def validate_index(index: dict, root: Path) -> list[str]:
    errors: list[str] = []
    _append(errors, index.get("schemaVersion") == "2.0.0", "index.schemaVersion must be 2.0.0")
    _append(errors, index.get("status") == "authoritative", "index.status must be authoritative")
    _append(errors, index.get("executionMode") == "proposal-only", "index.executionMode must be proposal-only")
    _append(errors, index.get("destructiveApplyDefault") is False, "index.destructiveApplyDefault must be false")

    contracts = index.get("contracts", {})
    required_contract_keys = {"storage", "profiles", "experience", "synchronization", "repositoryMap"}
    _append(
        errors,
        isinstance(contracts, dict) and required_contract_keys.issubset(contracts.keys()),
        "index.contracts must include storage/profiles/experience/synchronization/repositoryMap",
    )

    if isinstance(contracts, dict):
        for key in required_contract_keys:
            value = contracts.get(key)
            if not isinstance(value, str):
                errors.append(f"index.contracts.{key} must be a string path")
                continue
            if not _resolve_contract_path(root, value).is_file():
                errors.append(f"index.contracts.{key} points to missing file: {value}")

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

    blocked_paths = index.get("blockedPathsFromDraftPr205")
    _append(
        errors,
        isinstance(blocked_paths, list) and len(blocked_paths) >= 1,
        "index.blockedPathsFromDraftPr205 must declare protected draft PR #205 paths",
    )
    return errors


def validate_profiles_contract(profiles_contract: dict) -> list[str]:
    errors: list[str] = []
    _append(
        errors,
        profiles_contract.get("defaultProfile") == "core",
        "profiles.defaultProfile must be core",
    )

    profiles = profiles_contract.get("profiles", [])
    profile_ids = {p.get("id") for p in profiles if isinstance(p, dict)}
    _append(
        errors,
        profile_ids == EXPECTED_PROFILES,
        f"profiles set mismatch: expected {sorted(EXPECTED_PROFILES)} got {sorted(profile_ids)}",
    )

    admins = [p for p in profiles if isinstance(p, dict) and p.get("administrator") is True]
    _append(errors, len(admins) == 1, "exactly one administrator profile is required")
    if admins:
        sysadmin = admins[0]
        _append(errors, sysadmin.get("id") == "sysadmin", "sysadmin must be the only administrator")
        _append(errors, sysadmin.get("hidden") is True, "sysadmin must be hidden")
        _append(errors, sysadmin.get("enabledByDefault") is False, "sysadmin must be disabled by default")
        activation = sysadmin.get("activation", {})
        if isinstance(activation, dict):
            _append(errors, activation.get("physicalPresenceRequired") is True, "sysadmin requires physical presence")
            _append(errors, activation.get("minimumFactors", 0) >= 2, "sysadmin requires at least two factors")
            _append(errors, activation.get("remoteActivationDenied") is True, "sysadmin must deny remote activation")
            _append(errors, activation.get("cloudActivationDenied") is True, "sysadmin must deny cloud activation")
            _append(errors, activation.get("aiActivationDenied") is True, "sysadmin must deny AI activation")
        else:
            errors.append("sysadmin.activation must be an object")

    states = profiles_contract.get("states", [])
    state_ids = {s.get("id") for s in states if isinstance(s, dict)}
    _append(errors, state_ids == EXPECTED_STATE_IDS, "states must be recovery and quarantine")
    overlays = profiles_contract.get("overlays", [])
    overlay_ids = {o.get("id") for o in overlays if isinstance(o, dict)}
    _append(errors, overlay_ids == EXPECTED_OVERLAY_IDS, "overlays must include only airgap")

    invariants = profiles_contract.get("invariants", {})
    for key in (
        "onlySysAdminIsAdministrator",
        "sysAdminHiddenAndDisabledByDefault",
        "recoveryAndQuarantineAreStatesNotProfiles",
        "airgapIsOverlayNotProfile",
    ):
        _append(errors, isinstance(invariants, dict) and invariants.get(key) is True, f"invariant {key} must be true")

    return errors


def validate_storage_contract(storage_contract: dict) -> list[str]:
    errors: list[str] = []
    policy = storage_contract.get("executionPolicy", {})
    _append(errors, isinstance(policy, dict) and policy.get("mode") == "proposal-only", "storage.mode must be proposal-only")
    _append(errors, isinstance(policy, dict) and policy.get("destructiveApplyDefault") is False, "storage.destructiveApplyDefault must be false")

    exact_size_lock = storage_contract.get("exactSizeLock", {})
    _append(
        errors,
        isinstance(exact_size_lock, dict) and exact_size_lock.get("status") == "unresolved",
        "storage.exactSizeLock.status must remain unresolved",
    )
    _append(
        errors,
        isinstance(exact_size_lock, dict) and exact_size_lock.get("applyBlocked") is True,
        "storage.exactSizeLock.applyBlocked must be true",
    )

    disks = storage_contract.get("disks", [])
    if not isinstance(disks, list):
        return errors + ["storage.disks must be an array"]
    disk_by_id = {disk.get("id"): disk for disk in disks if isinstance(disk, dict)}

    disk0 = disk_by_id.get("disk0", {})
    volumes = disk0.get("volumes", []) if isinstance(disk0, dict) else []
    has_core_cross = any(
        isinstance(volume, dict)
        and volume.get("preferredLetter") == "X"
        and volume.get("label") == "CORE_CROSS"
        for volume in volumes
    )
    _append(errors, has_core_cross, "storage disk0 must include X: CORE_CROSS")

    disk1 = disk_by_id.get("disk1", {})
    _append(
        errors,
        isinstance(disk1, dict) and isinstance(disk1.get("domainsVolume"), dict),
        "storage disk1 must define domainsVolume",
    )
    virtual_disks = disk1.get("virtualDisks", []) if isinstance(disk1, dict) else []
    d_drive = next(
        (v for v in virtual_disks if isinstance(v, dict) and v.get("preferredLetter") == "D"),
        None,
    )
    v_drive = next(
        (v for v in virtual_disks if isinstance(v, dict) and v.get("preferredLetter") == "V"),
        None,
    )
    _append(errors, isinstance(d_drive, dict), "storage disk1 must include dynamic D: VHDX dev drive")
    _append(errors, isinstance(v_drive, dict), "storage disk1 must include V: vault VHDX")
    if isinstance(v_drive, dict):
        _append(errors, v_drive.get("autoMount") is False, "storage V: vault VHDX must never auto-mount")
        _append(errors, v_drive.get("bitLocker") == "required", "storage V: vault VHDX must require BitLocker")
    return errors


def validate_experience_contract(experience_contract: dict) -> list[str]:
    errors: list[str] = []
    common_core = experience_contract.get("commonCoreInstall", {})
    packages = common_core.get("packages", []) if isinstance(common_core, dict) else []
    _append(
        errors,
        isinstance(common_core, dict) and common_core.get("noApplicationDuplication") is True,
        "experience.commonCoreInstall.noApplicationDuplication must be true",
    )
    _append(errors, len(packages) == len(set(packages)), "experience common core packages must be unique")

    alvis = experience_contract.get("alvis", {})
    _append(errors, isinstance(alvis, dict) and alvis.get("assistantId") == "ALVIS", "experience.ALVIS assistant id mismatch")
    _append(errors, isinstance(alvis, dict) and alvis.get("mode") == "reactive-assistant", "experience.ALVIS mode must be reactive-assistant")
    _append(errors, isinstance(alvis, dict) and alvis.get("singleAssistantInstance") is True, "experience.ALVIS must be single instance")
    _append(errors, isinstance(alvis, dict) and alvis.get("administrator") is False, "experience.ALVIS must not be administrator")
    _append(errors, isinstance(alvis, dict) and alvis.get("autonomousApplyDenied") is True, "experience.ALVIS must deny autonomous apply")

    profile_map = experience_contract.get("profiles", {})
    _append(
        errors,
        isinstance(profile_map, dict) and EXPECTED_PROFILES.issubset(profile_map.keys()),
        "experience.profiles must include all permanent profiles",
    )
    return errors


def validate_sync_contract(sync_contract: dict) -> list[str]:
    errors: list[str] = []
    _append(errors, sync_contract.get("executionMode") == "proposal-only", "sync.executionMode must be proposal-only")
    _append(
        errors,
        sync_contract.get("directExternalDeliveryEnabled") is False,
        "sync.directExternalDeliveryEnabled must be false",
    )
    idempotency = sync_contract.get("idempotency", {})
    _append(
        errors,
        isinstance(idempotency, dict) and str(idempotency.get("keyTemplate", "")).startswith(
            "sha256(normalized-length-prefixed:"
        ),
        "sync.idempotency.keyTemplate must be normalized length-prefixed sha256",
    )
    surfaces = sync_contract.get("surfaces", {})
    if isinstance(surfaces, dict):
        azure_devops = surfaces.get("azure-devops", {})
        _append(errors, isinstance(azure_devops, dict) and azure_devops.get("readOnly") is True, "sync.azure-devops must be read-only")
        adobe = surfaces.get("adobe-design", {})
        _append(
            errors,
            isinstance(adobe, dict) and adobe.get("writesEnabled") is False,
            "sync.adobe-design must be evidence-reference-only (writes disabled)",
        )
    else:
        errors.append("sync.surfaces must be an object")
    return errors


def validate_repository_map_contract(repository_map: dict) -> list[str]:
    errors: list[str] = []
    _append(
        errors,
        repository_map.get("canonicalRepository") == "M0nado/helios-platform",
        "repositoryMap canonicalRepository must be M0nado/helios-platform",
    )
    dedicated_targets = repository_map.get("dedicatedTargets", {})
    gui = dedicated_targets.get("gui", {}) if isinstance(dedicated_targets, dict) else {}
    usb = dedicated_targets.get("usbWizard", {}) if isinstance(dedicated_targets, dict) else {}
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
    codeowners_paths = repository_map.get("codeownersReadyPaths", [])
    _append(
        errors,
        isinstance(codeowners_paths, list) and len(codeowners_paths) >= 4,
        "repositoryMap must declare CODEOWNERS-ready path groups",
    )
    return errors


def validate_profile_manifests(manifest_directory: Path, expected_profiles: set[str]) -> list[str]:
    errors: list[str] = []
    _append(errors, manifest_directory.is_dir(), "profile manifest directory is missing")
    _append(errors, XSD_PATH.is_file(), "profile manifest XSD is missing")
    if errors:
        return errors

    try:
        constraints = _load_manifest_schema_constraints(XSD_PATH)
    except ValueError as exc:
        errors.append(str(exc))
        return errors

    schema_profile_ids = constraints["profile_ids"]
    if not isinstance(schema_profile_ids, set):
        errors.append("profile manifest schema profileId constraints are invalid")
        return errors

    _append(
        errors,
        schema_profile_ids == expected_profiles,
        "profile manifest XSD profileId enumeration must match expected profile set",
    )

    ordered_children = constraints["ordered_children"]
    required_root_attributes = constraints["required_root_attributes"]
    required_child_attributes = constraints["required_child_attributes"]
    root_name = constraints["root_name"]

    manifest_files = sorted(manifest_directory.glob("*.xml"))
    _append(errors, len(manifest_files) == len(expected_profiles), "profile manifest count must match expected profile count")

    discovered_profiles: set[str] = set()
    for manifest in manifest_files:
        try:
            root = ET.fromstring(manifest.read_text(encoding="utf-8"))
        except ET.ParseError as exc:
            errors.append(f"{manifest.name}: XML parse error: {exc}")
            continue
        if root.tag != root_name:
            errors.append(f"{manifest.name}: root element must be {root_name}")
            continue

        for attribute_name, attribute_type in required_root_attributes.items():
            value = root.attrib.get(attribute_name)
            if value is None:
                errors.append(f"{manifest.name}: missing required root attribute '{attribute_name}'")
                continue
            if not _xsd_value_is_valid(value, attribute_type, schema_profile_ids):
                errors.append(
                    f"{manifest.name}: root attribute '{attribute_name}' violates XSD type {attribute_type}"
                )

        profile_id = root.attrib.get("profileId")
        if profile_id:
            discovered_profiles.add(profile_id)

        children = [child.tag for child in root]
        _append(
            errors,
            children == ordered_children,
            f"{manifest.name}: manifest elements must follow XSD sequence {ordered_children}",
        )

        for child in root:
            attributes = required_child_attributes.get(child.tag, {})
            for attribute_name, attribute_type in attributes.items():
                value = child.attrib.get(attribute_name)
                if value is None:
                    errors.append(
                        f"{manifest.name}: missing required attribute '{attribute_name}' on element '{child.tag}'"
                    )
                    continue
                if not _xsd_value_is_valid(value, attribute_type, schema_profile_ids):
                    errors.append(
                        f"{manifest.name}: element '{child.tag}' attribute '{attribute_name}' violates XSD type {attribute_type}"
                    )

    _append(errors, discovered_profiles == expected_profiles, "profile manifest IDs must match expected profile set")
    return errors


def load_contract_bundle(root: Path) -> dict[str, dict]:
    index = _read_json(root / "config" / "monado-enterprise" / "v2" / "index.json")
    contracts = index["contracts"]
    return {
        "index": index,
        "storage": _read_json(_resolve_contract_path(root, contracts["storage"])),
        "profiles": _read_json(_resolve_contract_path(root, contracts["profiles"])),
        "experience": _read_json(_resolve_contract_path(root, contracts["experience"])),
        "synchronization": _read_json(_resolve_contract_path(root, contracts["synchronization"])),
        "repositoryMap": _read_json(_resolve_contract_path(root, contracts["repositoryMap"])),
    }


def validate_contract_bundle(root: Path) -> list[str]:
    errors: list[str] = []
    bundle = load_contract_bundle(root)
    index = bundle["index"]
    errors.extend(validate_index(index, root))
    errors.extend(validate_storage_contract(bundle["storage"]))
    errors.extend(validate_profiles_contract(bundle["profiles"]))
    errors.extend(validate_experience_contract(bundle["experience"]))
    errors.extend(validate_sync_contract(bundle["synchronization"]))
    errors.extend(validate_repository_map_contract(bundle["repositoryMap"]))
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
        print("Monado enterprise v2 contract validation failed:")
        for error in errors:
            print(f"- {error}")
        return 1
    print("Monado enterprise v2 contract validation passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
