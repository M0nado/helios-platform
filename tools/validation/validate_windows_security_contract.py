#!/usr/bin/env python3
"""Validate HELIOS Windows environment and boot-security contracts.

This validator is read-only. It proves that source defaults remain audit-first,
offline scanning is never automatic, recovery evidence cannot contain a recovery
password, and active PowerShell adapters do not include prohibited disk or
security-disable operations.
"""

from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path
from typing import Any

GUID = re.compile(
    r"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$",
    re.IGNORECASE,
)
SHA256 = re.compile(r"^[0-9a-f]{64}$", re.IGNORECASE)
FORBIDDEN_SOURCE_TERMS = (
    "Clear-Disk",
    "Initialize-Disk",
    "Format-Volume",
    "Remove-Partition",
    "DisableRealtimeMonitoring $true",
    "DisableAntiSpyware",
)


class ContractError(RuntimeError):
    """Raised when a Windows security contract violates an invariant."""


def require(condition: bool, message: str) -> None:
    if not condition:
        raise ContractError(message)


def load_object(path: Path) -> dict[str, Any]:
    try:
        value = json.loads(path.read_text(encoding="utf-8"))
    except FileNotFoundError as exc:
        raise ContractError(f"Missing required file: {path}") from exc
    except json.JSONDecodeError as exc:
        raise ContractError(f"Invalid JSON in {path}: {exc}") from exc
    require(isinstance(value, dict), f"JSON root must be an object: {path}")
    return value


def validate_environment_policy(policy: dict[str, Any]) -> None:
    require(policy.get("schemaVersion") == 2, "Environment schemaVersion must be 2.")
    require(policy.get("mode") == "preserve-and-append", "Environment mode must preserve and append.")

    suffixes = policy.get("systemPathSuffixes")
    require(isinstance(suffixes, list), "systemPathSuffixes must be a list.")
    expected = {
        "System32",
        "",
        "System32\\Wbem",
        "System32\\WindowsPowerShell\\v1.0",
        "System32\\OpenSSH",
    }
    require(expected.issubset(set(suffixes)), "Required Windows PATH suffixes are missing.")

    tools = policy.get("criticalTools")
    require(isinstance(tools, list), "criticalTools must be a list.")
    paths = {
        str(item.get("name")): str(item.get("relativePath"))
        for item in tools
        if isinstance(item, dict)
    }
    require(paths.get("sfc.exe") == "System32\\sfc.exe", "sfc.exe path is incorrect.")
    require(paths.get("DISM.exe") == "System32\\DISM.exe", "DISM.exe path is incorrect.")
    require(
        paths.get("winmgmt.exe") == "System32\\Wbem\\winmgmt.exe",
        "winmgmt.exe must resolve through System32\\Wbem.",
    )
    require(paths.get("netsh.exe") == "System32\\netsh.exe", "netsh.exe path is incorrect.")
    require(paths.get("pnputil.exe") == "System32\\pnputil.exe", "pnputil.exe path is incorrect.")

    machine = policy.get("machineWrite", {})
    require(machine.get("requiresAdministrator") is True, "Machine writes must require Administrator.")
    require(
        machine.get("confirmationPhrase") == "REPAIR HELIOS MACHINE ENVIRONMENT",
        "Machine repair confirmation changed.",
    )
    safety = policy.get("safety", {})
    require(all(value is False for value in safety.values()), "Environment safety flags must all remain false.")


def validate_boot_policy(policy: dict[str, Any]) -> None:
    require(policy.get("schemaVersion") == 2, "Boot-security schemaVersion must be 2.")
    require(policy.get("defaultMode") == "audit", "Boot-security default mode must remain audit.")

    startup = policy.get("startup", {})
    scheduled = policy.get("scheduledScans", {})
    require(startup.get("postureAudit") is True, "Startup posture audit must remain enabled.")
    require(startup.get("automaticOfflineScan") is False, "Automatic offline scanning is prohibited.")
    require(scheduled.get("dailyQuickScan") is True, "Daily quick scan must remain the scheduled default.")
    require(scheduled.get("weeklyFullScanDefault") is False, "Weekly full scans must remain opt-in.")
    require(scheduled.get("offlineScanScheduled") is False, "Offline scanning must never be scheduled.")

    rules = policy.get("attackSurfaceReduction", {}).get("rules")
    require(isinstance(rules, list) and rules, "ASR rules must be a non-empty list.")
    ids = [str(rule.get("id", "")) for rule in rules if isinstance(rule, dict)]
    require(len(ids) == len(rules), "Each ASR rule must be an object with an ID.")
    require(all(GUID.fullmatch(rule_id) for rule_id in ids), "Every ASR rule ID must be a GUID.")
    require(len(set(rule_id.lower() for rule_id in ids)) == len(ids), "ASR rule IDs must be unique.")

    offline = policy.get("offlineRecovery", {})
    require(offline.get("provider") == "Microsoft Defender Offline", "Unexpected offline recovery provider.")
    require(offline.get("requiresWinRE") is True, "Offline recovery must require WinRE.")
    require(offline.get("automatic") is False, "Offline recovery cannot be automatic.")
    require(offline.get("bitLockerSuspendReboots") == 1, "BitLocker suspension must be limited to one reboot.")
    require(
        offline.get("requiresRecoveryEvidenceWhenProtected") is True,
        "Protected BitLocker volumes require secret-free recovery evidence.",
    )
    require(
        offline.get("confirmationPhrase") == "RUN HELIOS OFFLINE ROOTKIT SCAN",
        "Offline scan confirmation phrase changed.",
    )

    safety = policy.get("safety", {})
    require(safety and all(value is False for value in safety.values()), "All boot-security safety flags must remain false.")


def validate_recovery_template(template: dict[str, Any]) -> None:
    require(template.get("schemaVersion") == 1, "Recovery evidence schemaVersion must be 1.")
    require(template.get("recoveryKeyEscrowVerified") is False, "Template cannot pre-approve recovery escrow.")
    require(template.get("containsRecoveryPassword") is False, "Template must explicitly exclude the recovery password.")
    serialized = json.dumps(template, sort_keys=True).lower()
    require("48-digit" not in serialized, "Recovery template contains a password placeholder.")


def validate_scripts(repository_root: Path) -> dict[str, int]:
    paths = [
        repository_root / "scripts/windows/environment/Repair-HeliosEnvironment.ps1",
        repository_root / "scripts/windows/security/v2/Get-HeliosBootSecurityPosture.ps1",
        repository_root / "scripts/windows/security/v2/Set-HeliosWindowsSecurityBaseline.ps1",
        repository_root / "scripts/windows/security/v2/Invoke-HeliosDefenderRecovery.ps1",
        repository_root / "scripts/windows/security/v2/Install-HeliosSecurityAuditTasks.ps1",
    ]
    scanned = 0
    for path in paths:
        try:
            content = path.read_text(encoding="utf-8")
        except FileNotFoundError as exc:
            raise ContractError(f"Missing active Windows adapter: {path}") from exc
        scanned += 1
        for term in FORBIDDEN_SOURCE_TERMS:
            require(term not in content, f"Forbidden source term {term!r} in {path}")

    offline = paths[3].read_text(encoding="utf-8")
    require("RUN HELIOS OFFLINE ROOTKIT SCAN" in offline, "Offline confirmation phrase is missing from the adapter.")
    require("Start-MpWDOScan" in offline, "Defender Offline implementation is missing.")
    require("RecoveryEvidencePath" in offline, "Offline recovery evidence gate is missing.")
    require("RebootCount 1" in offline, "BitLocker suspension is not limited to one reboot.")

    tasks = paths[4].read_text(encoding="utf-8")
    require("offlineScanScheduled = $false" in tasks, "Task evidence must prove offline scanning is not scheduled.")
    require("InstallWeeklyFullScan" in tasks, "Weekly full scan opt-in is missing.")
    return {"scriptsScanned": scanned}


def validate_repository(repository_root: Path) -> dict[str, Any]:
    environment = load_object(repository_root / "config/windows/environment-baseline.v2.json")
    boot = load_object(repository_root / "config/windows/boot-security.v2.json")
    recovery = load_object(repository_root / "config/windows/bitlocker-recovery-evidence.template.json")

    validate_environment_policy(environment)
    validate_boot_policy(boot)
    validate_recovery_template(recovery)
    script_result = validate_scripts(repository_root)

    return {
        "status": "passed",
        "environmentSchema": environment["schemaVersion"],
        "bootSecuritySchema": boot["schemaVersion"],
        "asrRules": len(boot["attackSurfaceReduction"]["rules"]),
        "offlineScanAutomatic": boot["offlineRecovery"]["automatic"],
        "weeklyFullScanDefault": boot["scheduledScans"]["weeklyFullScanDefault"],
        "productionEnabled": False,
        **script_result,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repository-root", type=Path, default=Path("."))
    args = parser.parse_args()

    try:
        result = validate_repository(args.repository_root.resolve())
    except ContractError as exc:
        print(json.dumps({"status": "failed", "error": str(exc)}, indent=2), file=sys.stderr)
        return 1

    print(json.dumps(result, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
