#!/usr/bin/env python3
"""Validate the complete Monadoblade profile/storage/GUI contract bundle.

This validator keeps the profile workflow maintainable by moving contract and
secret-pattern logic out of an inline YAML heredoc. It deliberately performs no
external writes and treats every governed boundary as fail-closed.
"""

from __future__ import annotations

import argparse
import json
import re
from pathlib import Path
from typing import Any, Iterable


REPOSITORY_ROOT = Path(__file__).resolve().parents[2]

REQUIRED_JSON_PATHS = (
    "config/profiles/monadoblade-profiles.v1.json",
    "config/profiles/monadoblade-profiles.v2.json",
    "config/profiles/monadoblade-profiles.migration.v1-to-v2.json",
    "config/storage/monadoblade-partitions.2x2tb.v1.json",
    "config/storage/monadoblade-folder-hierarchy.v1.json",
    "config/software/monadoblade-profile-software.v1.json",
    "config/gui/monado-profile-shell.v1.json",
    "config/gui/monado-profile-shell.v2.json",
    "config/aihub/profile-optimization-policies.v1.json",
    "config/aihub/alvis-capabilities.v1.json",
    "config/automation/monadoblade-14-script-manifest.v1.json",
    "config/runtime/helios-fabric-services.v1.json",
    "config/runtime/helios-fabric-services.v2.json",
    "config/integrations/monadoblade-collaboration-projection.v1.json",
    "config/usb/monadoblade-usb-wizard.v1.json",
    "config/monado-enterprise/v2/index.json",
    "config/monado-enterprise/v2/storage.contract.v2.json",
    "config/monado-enterprise/v2/profiles.contract.v2.json",
    "config/monado-enterprise/v2/experience.contract.v2.json",
    "config/monado-enterprise/v2/synchronization.contract.v2.json",
    "config/monado-enterprise/v2/repository-map.contract.v2.json",
    "config/monado-enterprise/v3/index.json",
    "config/monado-enterprise/v3/profiles.contract.v3.json",
    "config/monado-enterprise/v3/experience.contract.v3.json",
    "config/monado-enterprise/v3/storage.contract.v3.json",
    "config/monado-enterprise/v3/integration-projection.contract.v3.json",
    "config/monado-enterprise/v3/repository-map.contract.v3.json",
    "config/monado-enterprise/v3/libraries.contract.v3.json",
    "config/monado-enterprise/v3/migration-map.contract.v3.json",
)

SCHEMA_DOCUMENT_PAIRS = (
    ("schemas/monado-enterprise/v2/index.schema.json", "config/monado-enterprise/v2/index.json"),
    ("schemas/monado-enterprise/v2/storage.contract.v2.schema.json", "config/monado-enterprise/v2/storage.contract.v2.json"),
    ("schemas/monado-enterprise/v2/profiles.contract.v2.schema.json", "config/monado-enterprise/v2/profiles.contract.v2.json"),
    ("schemas/monado-enterprise/v2/experience.contract.v2.schema.json", "config/monado-enterprise/v2/experience.contract.v2.json"),
    ("schemas/monado-enterprise/v2/synchronization.contract.v2.schema.json", "config/monado-enterprise/v2/synchronization.contract.v2.json"),
    ("schemas/monado-enterprise/v2/repository-map.contract.v2.schema.json", "config/monado-enterprise/v2/repository-map.contract.v2.json"),
    ("schemas/monado-enterprise/v3/index.schema.json", "config/monado-enterprise/v3/index.json"),
    ("schemas/monado-enterprise/v3/profiles.contract.v3.schema.json", "config/monado-enterprise/v3/profiles.contract.v3.json"),
    ("schemas/monado-enterprise/v3/experience.contract.v3.schema.json", "config/monado-enterprise/v3/experience.contract.v3.json"),
    ("schemas/monado-enterprise/v3/storage.contract.v3.schema.json", "config/monado-enterprise/v3/storage.contract.v3.json"),
    ("schemas/monado-enterprise/v3/integration-projection.contract.v3.schema.json", "config/monado-enterprise/v3/integration-projection.contract.v3.json"),
    ("schemas/monado-enterprise/v3/repository-map.contract.v3.schema.json", "config/monado-enterprise/v3/repository-map.contract.v3.json"),
    ("schemas/monado-enterprise/v3/libraries.contract.v3.schema.json", "config/monado-enterprise/v3/libraries.contract.v3.json"),
    ("schemas/monado-enterprise/v3/migration-map.contract.v3.schema.json", "config/monado-enterprise/v3/migration-map.contract.v3.json"),
)

SECRET_SCAN_ROOTS = (
    "config",
    "docs/architecture",
    "schemas/monado-enterprise/v2",
    "schemas/monado-enterprise/v3",
)

# The left boundary is important: without it, the old expression matched the
# ordinary contract identifier "disk-or-vhdx-apply-from-runtime" at "sk-".
SECRET_PATTERNS: tuple[tuple[str, re.Pattern[str]], ...] = (
    (
        "openai-api-key",
        re.compile(r"(?<![A-Za-z0-9])sk-(?:proj-|svcacct-)?[A-Za-z0-9_-]{20,}"),
    ),
    (
        "github-personal-access-token",
        re.compile(r"(?<![A-Za-z0-9_])github_pat_[A-Za-z0-9_]{20,}"),
    ),
    (
        "assigned-secret-value",
        re.compile(
            r"(?i)(client_secret|api_key|access_token)\s*[:=]\s*[\"'][^\"']{12,}[\"']"
        ),
    ),
)


class ContractValidationError(RuntimeError):
    """Raised when a governed contract violates a required invariant."""


def _load_json(relative_path: str) -> Any:
    path = REPOSITORY_ROOT / relative_path
    if not path.is_file():
        raise ContractValidationError(f"Missing required contract: {relative_path}")
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except json.JSONDecodeError as exc:
        raise ContractValidationError(f"Invalid JSON in {relative_path}: {exc}") from exc


def _require(condition: bool, message: str) -> None:
    if not condition:
        raise ContractValidationError(message)


def _validate_required_json() -> None:
    for relative_path in REQUIRED_JSON_PATHS:
        _load_json(relative_path)
        print(f"valid json: {relative_path}")


def _validate_v1_profiles() -> None:
    data = _load_json("config/profiles/monadoblade-profiles.v1.json")
    profiles = data["profiles"]
    ids = [profile["id"] for profile in profiles]
    expected = {
        "developer",
        "sysadmin",
        "sysops",
        "gamer",
        "studio",
        "personal",
        "server-background",
    }
    _require(set(ids) == expected, f"Profile set mismatch: {set(ids)}")
    _require(len(ids) == len(set(ids)), "Duplicate profile IDs")

    admins = [profile for profile in profiles if profile.get("admin")]
    _require(
        [profile["id"] for profile in admins] == ["sysadmin"],
        "SysAdmin must be the only administrator profile",
    )
    activation = admins[0].get("activation", {})
    required = (
        "physicalPresenceRequired",
        "remoteActivationDenied",
        "cloudActivationDenied",
        "aiActivationDenied",
    )
    _require(
        all(activation.get(key) is True for key in required),
        "SysAdmin physical/local activation boundary is incomplete",
    )
    _require(
        isinstance(activation.get("minimumFactors"), int)
        and not isinstance(activation.get("minimumFactors"), bool)
        and activation["minimumFactors"] >= 2,
        "SysAdmin requires at least two local factors",
    )
    print("v1 profile invariants passed")


def _validate_six_profile_delivery_fabric() -> None:
    six = _load_json("config/profiles/monadoblade-profiles.v2.json")
    profiles = six["profiles"]
    ids = [profile["id"] for profile in profiles]
    expected = ["core", "developer", "studio", "gamer", "ai-server", "sysadmin-offline"]
    _require(ids == expected, f"v2 profile sequence mismatch: {ids}")
    _require(six.get("defaultProfile") == "core", "v2 default profile must be core")

    by_id = {profile["id"]: profile for profile in profiles}
    expected_kanji = {
        "core": "核",
        "developer": "創",
        "studio": "響",
        "gamer": "迅",
        "ai-server": "智",
        "sysadmin-offline": "統",
    }
    for profile_id, kanji in expected_kanji.items():
        _require(
            by_id[profile_id].get("kanji") == kanji,
            f"v2 kanji mismatch for {profile_id}",
        )

    activation = by_id["sysadmin-offline"].get("activation", {})
    required = (
        "physicalPresenceRequired",
        "remoteActivationDenied",
        "cloudActivationDenied",
        "aiActivationDenied",
        "offlineOnly",
    )
    _require(
        all(activation.get(key) is True for key in required),
        "sysadmin-offline activation boundary incomplete",
    )
    minimum_factors = activation.get("minimumFactors")
    _require(
        type(minimum_factors) is int and minimum_factors >= 2,
        "sysadmin-offline requires at least two local factors",
    )

    migration = _load_json("config/profiles/monadoblade-profiles.migration.v1-to-v2.json")
    expected_mappings = {
        "developer": "developer",
        "sysadmin": "sysadmin-offline",
        "sysops": "ai-server",
        "gamer": "gamer",
        "studio": "studio",
        "personal": "core",
        "server-background": "ai-server",
    }
    _require(
        migration["mapV1ToV2"] == expected_mappings,
        f"Unexpected v1->v2 mapping: {migration['mapV1ToV2']}",
    )

    shell = _load_json("config/gui/monado-profile-shell.v2.json")
    expected_chain = [
        "safe-boot",
        "identity-verified",
        "wheel-ready",
        "profile-selected",
        "shell-ready",
    ]
    _require(
        shell.get("postAuthStateMachine") == expected_chain,
        f"Unexpected shell post-auth chain: {shell.get('postAuthStateMachine')}",
    )
    _require(
        shell.get("securityBoundary", {}).get("runsAfterWindowsAuthentication") is True,
        "Shell v2 must be post-auth only",
    )

    usb = _load_json("config/usb/monadoblade-usb-wizard.v1.json")
    _require(usb.get("defaultMode") == "dry-run", "USB wizard must default to dry-run")
    _require(usb.get("routes", {}).get("inventory") == "read-only", "USB wizard inventory route must be read-only")
    _require(
        usb.get("boundaries", {}).get("recoveryWorkflowProfile") == "sysadmin-offline",
        "Recovery workflow must remain sysadmin-offline",
    )
    _require(
        usb.get("boundaries", {}).get("quarantineWorkflowProfile") == "sysadmin-offline",
        "Quarantine workflow must remain sysadmin-offline",
    )

    alvis = _load_json("config/aihub/alvis-capabilities.v1.json")
    capabilities = alvis.get("capabilities", {})
    _require(capabilities.get("search") == ["search_*", "fetch_*"], "ALVIS search/fetch capabilities must remain read-only verbs")
    _require(capabilities.get("plan") == ["plan_*"], "ALVIS plan capability must remain plan-only")
    _require(capabilities.get("request") == ["request_*"], "ALVIS request capability must remain approval-pending")
    _require(alvis.get("directExecutorToolsDenied") is True, "ALVIS direct executor tools must be denied")

    projection = _load_json("config/integrations/monadoblade-collaboration-projection.v1.json")
    _require(projection.get("sourceOfTruth") == "github", "Integration projection source of truth must be GitHub")
    _require(
        projection.get("allowExecutionFromProjectedSystems") is False,
        "Projected systems cannot trigger execution",
    )
    print("six-profile delivery-fabric invariants passed")


def _validate_v1_storage() -> None:
    data = _load_json("config/storage/monadoblade-partitions.2x2tb.v1.json")
    _require(data.get("destructiveApplyDefault") is False, "Destructive apply must default to false")
    _require(data.get("requireWhatIf") is True, "Storage layout must require what-if")

    guardrails = data.get("guardrails", {})
    required = (
        "neverClearDiskWithoutExactIdentity",
        "neverFormatSystemOrBootDisk",
        "neverDeleteOemRecoveryAutomatically",
        "requireBackupEvidenceBeforeApply",
        "requireBitLockerRecoveryEvidence",
    )
    _require(all(guardrails.get(key) is True for key in required), "Storage guardrails are incomplete")

    workload = next(disk for disk in data["disks"] if disk["diskRole"] == "workload")
    labels = {partition["label"] for partition in workload["partitions"]}
    expected = {
        "MONADO_DEV",
        "MONADO_AI_MODELS",
        "MONADO_SECURE",
        "MONADO_QUARANTINE",
        "MONADO_BACKUP",
    }
    _require(labels == expected, f"Workload volume mismatch: {labels}")
    print("v1 storage invariants passed")


def _validate_v1_aihub_and_automation() -> None:
    aihub = _load_json("config/aihub/profile-optimization-policies.v1.json")
    brain = aihub["brain"]
    required_brain = (
        "denySecretReadback",
        "denyUnboundedShell",
        "denySelfPrivilegeEscalation",
        "requireHumanApprovalForExternalWrites",
    )
    _require(brain.get("autonomousAdministrator") is False, "AIHub cannot be an autonomous administrator")
    _require(all(brain.get(key) is True for key in required_brain), "AIHub safety invariants are incomplete")

    automation = _load_json("config/automation/monadoblade-14-script-manifest.v1.json")
    scripts = automation["scripts"]
    _require(len(scripts) == 14, f"Expected 14 governed scripts, found {len(scripts)}")
    _require(automation.get("rawExecutionDenied") is True, "Raw script execution must be denied")
    _require(automation.get("defaultMode") == "plan", "Automation must default to plan mode")
    _require(
        all("risk" in item and "applyRequires" in item for item in scripts),
        "Every script must declare risk and apply gates",
    )
    print("v1 AIHub and automation invariants passed")


def _validate_schema_pairs() -> None:
    try:
        from jsonschema import Draft202012Validator
    except ImportError as exc:  # pragma: no cover - dependency is installed in CI
        raise ContractValidationError("jsonschema is required for contract validation") from exc

    for schema_path, document_path in SCHEMA_DOCUMENT_PAIRS:
        schema = _load_json(schema_path)
        document = _load_json(document_path)
        if isinstance(document, dict):
            document = {key: value for key, value in document.items() if key != "$schema"}
        Draft202012Validator.check_schema(schema)
        Draft202012Validator(schema).validate(document)
        print(f"schema ok: {document_path}")


def find_secret_patterns(text: str) -> list[str]:
    """Return labels for credential-like values found in *text*."""

    return [label for label, pattern in SECRET_PATTERNS if pattern.search(text)]


def _iter_scan_files(roots: Iterable[str]) -> Iterable[Path]:
    for relative_root in roots:
        root = REPOSITORY_ROOT / relative_root
        if not root.exists():
            continue
        for path in root.rglob("*"):
            if path.is_file():
                yield path


def validate_secret_patterns() -> None:
    findings: list[str] = []
    for path in _iter_scan_files(SECRET_SCAN_ROOTS):
        text = path.read_text(encoding="utf-8", errors="ignore")
        for label in find_secret_patterns(text):
            findings.append(f"{path.relative_to(REPOSITORY_ROOT)}: {label}")

    if findings:
        raise ContractValidationError(
            "Potential credential material detected:\n" + "\n".join(findings)
        )

    # Keep the regression directly beside the scanner so an edit cannot restore
    # the `disk-...` false positive while still reporting a green workflow.
    _require(
        not find_secret_patterns("disk-or-vhdx-apply-from-runtime"),
        "Secret scanner regression: storage action IDs must not match OpenAI key syntax",
    )
    _require(
        "openai-api-key" in find_secret_patterns("value=" + "sk-proj-" + ("A" * 32)),
        "Secret scanner regression: realistic project keys must still be detected",
    )
    print("no secret patterns detected")


def validate_contracts() -> None:
    _validate_required_json()
    _validate_v1_profiles()
    _validate_six_profile_delivery_fabric()
    _validate_v1_storage()
    _validate_v1_aihub_and_automation()
    _validate_schema_pairs()


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--mode",
        choices=("all", "contracts", "secrets"),
        default="all",
        help="Select the validation phase to run.",
    )
    args = parser.parse_args()

    try:
        if args.mode in {"all", "contracts"}:
            validate_contracts()
        if args.mode in {"all", "secrets"}:
            validate_secret_patterns()
    except (ContractValidationError, KeyError, StopIteration, TypeError, ValueError) as exc:
        print(f"ERROR: {exc}")
        return 1

    print(f"Monadoblade profile bundle validation passed ({args.mode}).")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
