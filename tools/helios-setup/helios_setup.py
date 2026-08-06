from __future__ import annotations

import argparse
import json
import os
import shutil
import subprocess
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable


ROOT = Path(__file__).resolve().parents[2]
DEFAULT_MANIFEST = ROOT / "config" / "bootstrap" / "enterprise-automatic-setup.v1.json"


@dataclass(frozen=True)
class StepResult:
    command: str
    status: str
    exit_code: int | None = None
    message: str | None = None


ENV_REPLACEMENTS = {
    "<approved-subscription-id>": "HELIOS_AZURE_SUBSCRIPTION_ID",
    "<approved-org>": "HELIOS_AZURE_DEVOPS_ORG",
    "<approved-project>": "HELIOS_AZURE_DEVOPS_PROJECT",
    "<reviewed-config-directory>": "HELIOS_AGENT365_CONFIG_DIR",
}

PHASE_CONFIRMATIONS = {
    "tools": "INSTALL HELIOS TOOLS",
    "developer-auth": "AUTHENTICATE HELIOS",
    "context": "SELECT HELIOS TARGETS",
    "agent365-developer": "PREPARE HELIOS AGENT 365",
    "agent365-admin-handoff": "GRANT HELIOS AGENT CONSENT",
    "azure-plan": "RUN HELIOS DEV WHATIF",
    "connections": "VALIDATE HELIOS CONNECTIONS",
    "development-apply": "DEPLOY HELIOS DEV",
}


class SetupError(RuntimeError):
    pass


def load_manifest(path: Path) -> dict:
    payload = json.loads(path.read_text(encoding="utf-8"))
    if payload.get("schemaVersion") != "1.0":
        raise SetupError("Unsupported setup manifest schema.")
    if payload.get("productionEnabled") is not False:
        raise SetupError("The setup manifest must keep production disabled.")
    phases = payload.get("phases")
    if not isinstance(phases, list) or not phases:
        raise SetupError("The setup manifest does not define phases.")
    return payload


def phase_map(manifest: dict) -> dict[str, dict]:
    return {str(item["id"]): item for item in manifest["phases"]}


def resolve_command(command: str) -> str:
    resolved = command
    for marker, env_name in ENV_REPLACEMENTS.items():
        value = os.getenv(env_name)
        if marker in resolved:
            if not value:
                raise SetupError(
                    f"Command requires {env_name}. Set it in the current shell; do not commit it."
                )
            resolved = resolved.replace(marker, value)
    return resolved


def executable_name(command: str) -> str:
    first = command.strip().split(maxsplit=1)[0]
    return first.strip('"')


def check_tools(commands: Iterable[str]) -> list[StepResult]:
    seen: set[str] = set()
    results: list[StepResult] = []
    for command in commands:
        exe = executable_name(command)
        if exe in seen or exe.startswith("<"):
            continue
        seen.add(exe)
        location = shutil.which(exe)
        results.append(
            StepResult(
                command=exe,
                status="available" if location else "missing",
                message=location,
            )
        )
    return results


def run_command(command: str) -> StepResult:
    print(f"\n> {command}", flush=True)
    completed = subprocess.run(command, shell=True, check=False)
    return StepResult(
        command=command,
        status="success" if completed.returncode == 0 else "failed",
        exit_code=completed.returncode,
    )


def print_plan(manifest: dict, selected_phase: str | None = None) -> None:
    phases = phase_map(manifest)
    selected = [phases[selected_phase]] if selected_phase else manifest["phases"]
    print(f"Setup: {manifest['setupId']}")
    print(f"Mode: {manifest['defaultMode']}")
    print(f"Production enabled: {manifest['productionEnabled']}")
    print(f"Production blocker: #{manifest['productionBlockerIssue']}")
    for phase in selected:
        print(f"\n[{phase['id']}] {phase['description']}")
        print(f"Approval: {phase['approval']}")
        for command in phase.get("commands", []):
            print(f"  - {command}")


def execute_phase(manifest: dict, phase_id: str, confirmation: str) -> int:
    phases = phase_map(manifest)
    if phase_id not in phases:
        raise SetupError(f"Unknown phase: {phase_id}")
    if phase_id == "production":
        raise SetupError("Production is disabled while GitHub Issue #162 remains the hard gate.")

    expected = PHASE_CONFIRMATIONS.get(phase_id)
    if not expected or confirmation != expected:
        raise SetupError(f"Exact confirmation required: {expected}")

    phase = phases[phase_id]
    commands = [resolve_command(command) for command in phase.get("commands", [])]
    for command in commands:
        result = run_command(command)
        if result.status != "success":
            print(f"FAILED ({result.exit_code}): {command}", file=sys.stderr)
            return 1
    return 0


def validate_manifest(manifest: dict) -> None:
    ids = [item["id"] for item in manifest["phases"]]
    if len(ids) != len(set(ids)):
        raise SetupError("Duplicate phase IDs detected.")
    required = {
        "tools",
        "developer-auth",
        "context",
        "agent365-developer",
        "agent365-admin-handoff",
        "azure-plan",
        "connections",
        "development-apply",
        "production",
    }
    missing = sorted(required - set(ids))
    if missing:
        raise SetupError(f"Missing required phases: {missing}")
    if phase_map(manifest)["production"].get("commands"):
        raise SetupError("Production phase must not expose commands.")


def main() -> int:
    parser = argparse.ArgumentParser(description="HELIOS enterprise setup controller")
    parser.add_argument("action", choices=["plan", "check-tools", "validate", "execute"])
    parser.add_argument("--manifest", type=Path, default=DEFAULT_MANIFEST)
    parser.add_argument("--phase")
    parser.add_argument("--confirm", default="")
    args = parser.parse_args()

    try:
        manifest = load_manifest(args.manifest)
        validate_manifest(manifest)

        if args.action == "plan":
            print_plan(manifest, args.phase)
            return 0
        if args.action == "validate":
            print("Manifest validation passed.")
            return 0
        if args.action == "check-tools":
            commands = [
                command
                for phase in manifest["phases"]
                for command in phase.get("commands", [])
            ]
            results = check_tools(commands)
            print(json.dumps([result.__dict__ for result in results], indent=2))
            return 0 if all(result.status == "available" for result in results) else 2
        if not args.phase:
            raise SetupError("--phase is required for execute.")
        return execute_phase(manifest, args.phase, args.confirm)
    except (OSError, json.JSONDecodeError, SetupError) as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
