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
EXPECTED_CONTRACT_FILE_NAMES = {
    "storage": "storage.contract.v2.json",
    "profileCatalog": "profile-catalog.v2.json",
    "profileExperience": "profile-experience.contract.v2.json",
    "chromaWyvern": "chroma-wyvern.contract.v2.json",
    "alvisBudget": "alvis-tool-budgets.v2.json",
    "repositoryOwnership": "repository-ownership.contract.v2.json",
    "synchronization": "synchronization.contract.v2.json",
    "openAiProposalSchema": "openai-proposal.schema.v2.json",
    "profileXmlSchema": "profile-manifest.v2.xsd",
}
EXPECTED_REFERENCED_CONTRACT_IDS = {
    "storage": "monado-storage-v2",
    "profileCatalog": "monado-profile-catalog-v2",
    "profileExperience": "monado-profile-experience-v2",
    "chromaWyvern": "monado-chroma-wyvern-v2",
    "alvisBudget": "alvis-tool-budgets-v2",
    "repositoryOwnership": "monado-repository-ownership-v2",
    "synchronization": "monado-sync-v2",
}
EXPECTED_AZURE_DEVOPS_MODE = "read-only-mirror-until-separate-approved-identity"
EXPECTED_INSTANCE_SCHEMA_REFERENCE = "../../../schemas/monadoblade/experience-fabric/v2/contract-instance.schema.json"
EXPECTED_ENVELOPE_FIELDS = {
    "schemaVersion",
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
EXPECTED_IDEMPOTENCY_INPUT_FIELDS = ["routeId", "source.eventId", "target.system", "operation"]
EXPECTED_IDEMPOTENCY_NORMALIZATION = "nfc+trim+length-prefixed-utf8"
EXPECTED_IDEMPOTENCY_KEY_TEMPLATE = "sha256(normalized-length-prefixed:{routeId}:{source.eventId}:{target.system}:{operation})"
EXPECTED_IDEMPOTENCY_DUPLICATE_OUTCOME = "return-recorded-result-without-repeating-side-effect"
EXPECTED_OVERLAY_CONTRACT_ID = "monadoblade-experience-fabric-v2-overlay"
EXPECTED_CANONICAL_CONTRACT_ID = "monado-enterprise-experience-fabric-v2"
EXPECTED_OPENAI_SCHEMA_ID = "https://helios-platform.dev/schemas/monado-openai-proposal-v2.json"
EXPECTED_PRIVILEGED_ACTION_TYPES = {"privileged-proposal", "deployment-proposal", "rollback-proposal"}
EXPECTED_SYNC_PROHIBITIONS = {
    "direct-to-main-write",
    "implicit-external-message-side-effect",
    "azure-apply-from-runtime",
    "tenant-consent-from-runtime",
    "direct-production-deployment",
    "external-write-without-approval",
}
EXPECTED_DENIED_OPERATIONS = {
    "direct-disk-mutation",
    "direct-tenant-consent",
    "direct-rbac-write",
    "direct-production-deployment",
    "physical-usb-write",
    "raw-secret-readback",
}
EXPECTED_OWNERSHIP = {
    "helios-platform": "canonical-contracts-event-routing-policy-docs-pages-wiki-source-validation",
    "helios-monado-blade": "dedicated-gui-engine-and-themed-interaction",
    "PHASE-0-USB-Creator": "dedicated-removable-media-builder-and-privileged-device-operations",
    "helios-ai-hub": "provider-runtime-adapters-and-governed-learning",
}
EXPECTED_NON_OWNERSHIP_RULES = {
    "do-not-duplicate-gui-implementation-in-canonical-platform",
    "do-not-duplicate-usb-wizard-implementation-in-canonical-platform",
}
EXPECTED_CODEOWNERS_PATHS = {
    "/config/monadoblade/experience-fabric/** @M0nado/platform-architecture",
    "/docs/architecture/MONADO_ENTERPRISE_EXPERIENCE_FABRIC_V2.md @M0nado/platform-architecture",
    "/scripts/control/validate_monado_enterprise_experience_v2.py @M0nado/security-engineering",
    "/scripts/control/tests/test_validate_monado_enterprise_experience_v2.py @M0nado/security-engineering",
    "/.github/workflows/validate-monadoblade-profile-contracts.yml @M0nado/devops",
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


def _is_under_base(path: Path, base: Path) -> bool:
    try:
        path.resolve().relative_to(base.resolve())
        return True
    except ValueError:
        return False


def validate_contracts(base: Path = BASE) -> list[str]:
    errors: list[str] = []
    for name in sorted(EXPECTED_JSON):
        path = base / name
        if not path.exists():
            errors.append(f"missing contract file: {path}")

    if errors:
        return errors

    root = _load_json(base / "monado-enterprise-experience-fabric.v2.json")
    _require(
        root.get("$schema") == EXPECTED_INSTANCE_SCHEMA_REFERENCE,
        "root contract must reference the monadoblade v2 instance schema",
        errors,
    )
    _require(root.get("contractId") == EXPECTED_OVERLAY_CONTRACT_ID, "root contractId must be monadoblade-experience-fabric-v2-overlay", errors)
    _require(
        root.get("extendsCanonicalContractId") == EXPECTED_CANONICAL_CONTRACT_ID,
        "root must explicitly reference the canonical monado-enterprise v2 contract ID",
        errors,
    )
    _require(root.get("status") == "authoritative-proposal-only", "root status must remain authoritative-proposal-only", errors)
    execution = root.get("execution", {})
    _require(isinstance(execution, Mapping), "root execution must be an object", errors)
    _require(execution.get("destructiveApplyDefault") is False, "destructive apply must be disabled by default", errors)
    _require(execution.get("runtimeSideEffectsAllowed") is False, "runtime side effects must be disabled", errors)
    contracts = root.get("contracts", {})
    _require(isinstance(contracts, Mapping), "root contracts must be an object", errors)
    resolved_contract_docs: dict[str, dict] = {}
    if isinstance(contracts, Mapping):
        _require(EXPECTED_CONTRACT_KEYS.issubset(contracts.keys()), "root contracts mapping is incomplete", errors)
        for key in EXPECTED_CONTRACT_KEYS:
            path = contracts.get(key)
            _require(isinstance(path, str) and path.strip() != "", f"root contracts.{key} must be a non-empty path", errors)
            if isinstance(path, str) and path.strip() != "":
                resolved = _resolve_contract_path(base, path)
                _require(_is_under_base(resolved, base), f"root contracts.{key} must stay under config/monadoblade/experience-fabric", errors)
                _require(resolved.is_file(), f"root contracts.{key} points to a missing file", errors)
                expected_file_name = EXPECTED_CONTRACT_FILE_NAMES[key]
                _require(
                    resolved.name == expected_file_name,
                    f"root contracts.{key} must target {expected_file_name}",
                    errors,
                )
                if not resolved.is_file():
                    continue
                if key in EXPECTED_REFERENCED_CONTRACT_IDS:
                    try:
                        referenced = _load_json(resolved)
                    except (OSError, json.JSONDecodeError, ValueError) as exc:
                        errors.append(f"root contracts.{key} must resolve to a JSON object contract: {exc}")
                        continue
                    resolved_contract_docs[key] = referenced
                    expected_contract_id = EXPECTED_REFERENCED_CONTRACT_IDS[key]
                    _require(
                        referenced.get("contractId") == expected_contract_id,
                        f"root contracts.{key} must reference contractId {expected_contract_id}",
                        errors,
                    )
                    _require(
                        referenced.get("$schema") == EXPECTED_INSTANCE_SCHEMA_REFERENCE,
                        f"root contracts.{key} must reference the monadoblade v2 instance schema",
                        errors,
                    )
                elif key == "openAiProposalSchema":
                    try:
                        referenced = _load_json(resolved)
                    except (OSError, json.JSONDecodeError, ValueError) as exc:
                        errors.append(f"root contracts.{key} must resolve to a JSON object schema: {exc}")
                        continue
                    resolved_contract_docs[key] = referenced
                    _require(
                        referenced.get("$id") == EXPECTED_OPENAI_SCHEMA_ID,
                        "root contracts.openAiProposalSchema must reference the canonical OpenAI proposal schema",
                        errors,
                    )
                    _require(
                        referenced.get("$schema") == "https://json-schema.org/draft/2020-12/schema",
                        "OpenAI proposal schema must declare draft/2020-12 meta-schema",
                        errors,
                    )

    storage = resolved_contract_docs.get("storage", _load_json(base / "storage.contract.v2.json"))
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
    vhdx_ids = [item.get("id") for item in vhdx_items if isinstance(item.get("id"), str)]
    _require(len(vhdx_ids) == len(set(vhdx_ids)), "storage vhdx entries must use unique id values", errors)
    vhdx_letters = [item.get("targetLetter") for item in vhdx_items if isinstance(item.get("targetLetter"), str)]
    _require(
        len(vhdx_letters) == len(set(vhdx_letters)),
        "storage vhdx entries must use unique target letters",
        errors,
    )
    devdrive = next((item for item in vhdx_items if item.get("id") == "devdrive"), None)
    vault = next((item for item in vhdx_items if item.get("id") == "vault"), None)
    _require(devdrive is not None and devdrive.get("targetLetter") == "D", "devdrive vhdx must target D", errors)
    _require(vault is not None and vault.get("targetLetter") == "V", "vault vhdx must target V", errors)
    _require(vault is not None and vault.get("autoMount") is False, "vault vhdx must never auto-mount", errors)
    _require(
        vault is not None and vault.get("kind") == "dynamic-bitlocker-encrypted",
        "vault vhdx must remain dynamic-bitlocker-encrypted",
        errors,
    )

    catalog = resolved_contract_docs.get("profileCatalog", _load_json(base / "profile-catalog.v2.json"))
    profiles = catalog.get("profiles", [])
    _require(len(profiles) == len(EXPECTED_PROFILES), "profile catalog must contain exactly eight entries", errors)
    ids = {entry.get("id") for entry in profiles if isinstance(entry, Mapping)}
    _require(
        len(ids) == len([entry for entry in profiles if isinstance(entry, Mapping)]),
        "profile catalog entries must not duplicate profile IDs",
        errors,
    )
    _require(ids == EXPECTED_PROFILES, f"profile set mismatch: {sorted(ids)}", errors)
    default_profile = catalog.get("defaultProfile")
    _require(default_profile == "personal", "profile catalog defaultProfile must remain personal", errors)
    default_entry = (
        next((entry for entry in profiles if isinstance(entry, Mapping) and entry.get("id") == default_profile), None)
        if isinstance(default_profile, str)
        else None
    )
    _require(default_entry is not None, "profile catalog defaultProfile must reference a defined profile", errors)
    if isinstance(default_entry, Mapping):
        _require(default_entry.get("administrator") is False, "default profile must not be an administrator profile", errors)
        _require(default_entry.get("enabledByDefault", True) is True, "default profile must be enabled by default", errors)
        _require(default_entry.get("hidden", False) is False, "default profile must not be hidden", errors)
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

    experience = resolved_contract_docs.get("profileExperience", _load_json(base / "profile-experience.contract.v2.json"))
    profile_experience = experience.get("profiles", {})
    _require(isinstance(profile_experience, Mapping), "profile experience profiles must be an object", errors)
    _require(set(profile_experience.keys()) == EXPECTED_PROFILES, "profile experience must define every expected profile", errors)
    common_core = experience.get("commonCoreInstall", {})
    _require(common_core.get("singleInstallAuthority") is True, "common core must be single install authority", errors)
    _require(common_core.get("profileLinksOnly") is True, "common core must use profile links only", errors)

    chroma = resolved_contract_docs.get("chromaWyvern", _load_json(base / "chroma-wyvern.contract.v2.json"))
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

    alvis = resolved_contract_docs.get("alvisBudget", _load_json(base / "alvis-tool-budgets.v2.json"))
    _require(alvis.get("administratorDenied") is True, "ALVIS administratorDenied must be true", errors)
    _require(alvis.get("externalWriteRequiresApproval") is True, "ALVIS externalWriteRequiresApproval must be true", errors)
    denied_operations = alvis.get("deniedOperations", [])
    _require(isinstance(denied_operations, list), "ALVIS deniedOperations must be a list", errors)
    if isinstance(denied_operations, list):
        denied_operation_set = {item for item in denied_operations if isinstance(item, str)}
        _require(
            EXPECTED_DENIED_OPERATIONS.issubset(denied_operation_set),
            "ALVIS deniedOperations must include the full protected operation set",
            errors,
        )
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

    sync = resolved_contract_docs.get("synchronization", _load_json(base / "synchronization.contract.v2.json"))
    _require(sync.get("defaultMode") == "propose-and-validate-only", "synchronization defaultMode must remain propose-and-validate-only", errors)
    idempotency = sync.get("idempotency", {})
    _require(isinstance(idempotency, Mapping), "synchronization idempotency must be an object", errors)
    if isinstance(idempotency, Mapping):
        _require(idempotency.get("algorithm") == "sha256", "synchronization idempotency algorithm must be sha256", errors)
        input_fields = idempotency.get("inputFields", [])
        _require(
            isinstance(input_fields, list) and input_fields == EXPECTED_IDEMPOTENCY_INPUT_FIELDS,
            "synchronization idempotency inputFields must match canonical field order",
            errors,
        )
        _require(
            idempotency.get("normalization") == EXPECTED_IDEMPOTENCY_NORMALIZATION,
            "synchronization idempotency normalization must remain canonical",
            errors,
        )
        _require(
            idempotency.get("keyTemplate") == EXPECTED_IDEMPOTENCY_KEY_TEMPLATE,
            "synchronization idempotency keyTemplate must remain canonical",
            errors,
        )
        _require(
            idempotency.get("duplicateOutcome") == EXPECTED_IDEMPOTENCY_DUPLICATE_OUTCOME,
            "synchronization idempotency duplicateOutcome must remain canonical",
            errors,
        )
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
    canonical_event_schema = _load_json(REPOSITORY_ROOT / "config" / "integrations" / "event-contract.schema.json")
    canonical_required_fields = set(canonical_event_schema.get("required", []))
    _require(
        canonical_required_fields.issubset(envelope_fields),
        "synchronization envelope requiredFields must include canonical integration-event required fields",
        errors,
    )
    sync_prohibitions = sync.get("prohibitions", [])
    _require(isinstance(sync_prohibitions, list), "synchronization prohibitions must be a list", errors)
    if isinstance(sync_prohibitions, list):
        sync_prohibition_set = {item for item in sync_prohibitions if isinstance(item, str)}
        _require(
            EXPECTED_SYNC_PROHIBITIONS.issubset(sync_prohibition_set),
            "synchronization prohibitions must include the full protected operation set",
            errors,
        )

    ownership = resolved_contract_docs.get("repositoryOwnership", _load_json(base / "repository-ownership.contract.v2.json"))
    _require(ownership.get("canonicalPlatform") == "M0nado/helios-platform", "repository ownership canonicalPlatform mismatch", errors)
    ownership_map = ownership.get("ownership", {})
    _require(isinstance(ownership_map, Mapping), "repository ownership.ownership must be an object", errors)
    if isinstance(ownership_map, Mapping):
        _require(
            set(ownership_map.keys()) == set(EXPECTED_OWNERSHIP.keys()),
            "repository ownership must define the canonical repository ownership boundary set",
            errors,
        )
        for repository, expected_role in EXPECTED_OWNERSHIP.items():
            _require(
                ownership_map.get(repository) == expected_role,
                f"repository ownership for {repository} must remain '{expected_role}'",
                errors,
            )
    codeowners_paths = ownership.get("codeownersReadyPaths", [])
    _require(isinstance(codeowners_paths, list) and len(codeowners_paths) >= 3, "repository ownership must define CODEOWNERS-ready paths", errors)
    if isinstance(codeowners_paths, list):
        codeowners_path_set = {entry for entry in codeowners_paths if isinstance(entry, str)}
        _require(
            EXPECTED_CODEOWNERS_PATHS.issubset(codeowners_path_set),
            "repository ownership must include required CODEOWNERS-ready path boundaries",
            errors,
        )
    non_ownership_rules = ownership.get("nonOwnershipRules", [])
    _require(isinstance(non_ownership_rules, list), "repository ownership nonOwnershipRules must be a list", errors)
    if isinstance(non_ownership_rules, list):
        non_ownership_rule_set = {rule for rule in non_ownership_rules if isinstance(rule, str)}
        _require(
            EXPECTED_NON_OWNERSHIP_RULES.issubset(non_ownership_rule_set),
            "repository ownership nonOwnershipRules must include canonical anti-duplication boundaries",
            errors,
        )

    openai_schema = resolved_contract_docs.get("openAiProposalSchema", _load_json(base / "openai-proposal.schema.v2.json"))
    _require(openai_schema.get("type") == "object", "OpenAI proposal schema must define object root", errors)
    _require(openai_schema.get("additionalProperties") is False, "OpenAI proposal schema must fail closed on additional properties", errors)
    required = set(openai_schema.get("required", []))
    _require({"proposalId", "correlationId", "approval", "rollbackPlan", "expiresAtUtc"}.issubset(required), "OpenAI proposal schema must require proposal/approval/rollback fields", errors)
    schema_guards = openai_schema.get("allOf", [])
    _require(isinstance(schema_guards, list) and len(schema_guards) > 0, "OpenAI proposal schema must define conditional safety guards", errors)
    privileged_guard_present = False
    if isinstance(schema_guards, list):
        for guard in schema_guards:
            if not isinstance(guard, Mapping):
                continue
            action_type_enum = (
                guard.get("if", {})
                .get("properties", {})
                .get("actionType", {})
                .get("enum", [])
            )
            guard_action_types = set(action_type_enum) if isinstance(action_type_enum, list) else set()
            required_const = (
                guard.get("then", {})
                .get("properties", {})
                .get("approval", {})
                .get("properties", {})
                .get("required", {})
                .get("const")
            )
            if guard_action_types == EXPECTED_PRIVILEGED_ACTION_TYPES and required_const is True:
                privileged_guard_present = True
                break
    _require(
        privileged_guard_present,
        "OpenAI proposal schema must enforce approval.required=true for privileged/deployment/rollback proposals",
        errors,
    )

    return errors


def validate_profile_xml(base: Path = BASE) -> list[str]:
    errors: list[str] = []
    alvis = _load_json(base / "alvis-tool-budgets.v2.json")
    alvis_profiles = alvis.get("profiles", {})
    _require(isinstance(alvis_profiles, Mapping), "ALVIS profiles must be an object for XML budget cross-checks", errors)
    experience = _load_json(base / "profile-experience.contract.v2.json")
    experience_profiles = experience.get("profiles", {})
    _require(isinstance(experience_profiles, Mapping), "profile-experience profiles must be an object for XML cross-checks", errors)
    xml_root = base / "xml"
    xsd_path = xml_root / "profile-manifest.v2.xsd"
    if not xsd_path.exists():
        return [f"missing XSD: {xsd_path}"]

    xsd_text = xsd_path.read_text(encoding="utf-8")
    if "<xs:element name=\"ProfileManifest\"" not in xsd_text:
        errors.append("XSD must define ProfileManifest root element")
    if "<xs:attribute name=\"schemaVersion\"" not in xsd_text or "fixed=\"2.0.0\"" not in xsd_text:
        errors.append("XSD must constrain schemaVersion to fixed value 2.0.0")
    if "<xs:attribute name=\"profileId\"" not in xsd_text:
        errors.append("XSD must define profileId attribute")
    for profile_id in sorted(EXPECTED_PROFILES):
        if f"<xs:enumeration value=\"{profile_id}\"/>" not in xsd_text:
            errors.append(f"XSD must enumerate profileId value '{profile_id}'")

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
        expected_id = path.name.removesuffix(".profile.v2.xml")
        _require(
            profile_id == expected_id,
            f"{path}: profileId must match filename-derived id '{expected_id}'",
            errors,
        )

        xml_values: dict[str, str] = {}
        for element in ("SemanticUi", "ServiceMode", "NetworkMode", "TelemetryClass", "AlvisMaxToolCallsPerPlan"):
            node = root.find(f"m:{element}", XML_NS)
            value = (node.text or "").strip() if node is not None else ""
            xml_values[element] = value
            _require(value != "", f"{path}: missing {element}", errors)

        if profile_id in EXPECTED_PROFILES and isinstance(experience_profiles, Mapping):
            expected_profile = experience_profiles.get(profile_id)
            _require(
                isinstance(expected_profile, Mapping),
                f"{path}: profile-experience must define profile {profile_id}",
                errors,
            )
            if isinstance(expected_profile, Mapping):
                for xml_field, json_field in (
                    ("SemanticUi", "uiSemantic"),
                    ("ServiceMode", "serviceMode"),
                    ("NetworkMode", "networkMode"),
                    ("TelemetryClass", "telemetryClass"),
                ):
                    expected_value = expected_profile.get(json_field)
                    _require(
                        isinstance(expected_value, str) and expected_value.strip() != "",
                        f"{path}: profile-experience {profile_id}.{json_field} must be a non-empty string",
                        errors,
                    )
                    if isinstance(expected_value, str) and expected_value.strip() != "":
                        _require(
                            xml_values[xml_field] == expected_value,
                            f"{path}: {xml_field} must match profile-experience value '{expected_value}' for {profile_id}",
                            errors,
                        )

        max_calls_text = xml_values["AlvisMaxToolCallsPerPlan"]
        _require(
            max_calls_text.isdigit() and int(max_calls_text) > 0,
            f"{path}: AlvisMaxToolCallsPerPlan must be a positive integer",
            errors,
        )
        if max_calls_text.isdigit() and profile_id in EXPECTED_PROFILES and isinstance(alvis_profiles, Mapping):
            policy = alvis_profiles.get(profile_id)
            expected_budget = policy.get("maxToolCallsPerPlan") if isinstance(policy, Mapping) else None
            _require(
                isinstance(expected_budget, int) and expected_budget > 0,
                f"{path}: ALVIS profile {profile_id} must define a positive maxToolCallsPerPlan",
                errors,
            )
            if isinstance(expected_budget, int) and expected_budget > 0:
                _require(
                    int(max_calls_text) == expected_budget,
                    f"{path}: AlvisMaxToolCallsPerPlan must match ALVIS profile budget ({expected_budget}) for {profile_id}",
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
