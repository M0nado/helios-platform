#!/usr/bin/env python3
"""HELIOS/HERMES platform orchestration manifest validator.

This tool gives the repository a single, testable source of truth for the
helios-control and hermes-fleet-production integration plan while the language
specific components are being filled in.
"""

from __future__ import annotations

import argparse
import json
import shutil
import subprocess
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Any

REPO_ROOT = Path(__file__).resolve().parents[1]
DEFAULT_MANIFEST = REPO_ROOT / "platform" / "helios_fleet_manifest.json"
REQUIRED_SYSTEMS = {
    "helios-control",
    "hermes-fleet-production",
    "hermes-xcore",
    "hermes-analytics",
    "aihub-integration",
}


@dataclass(frozen=True)
class ValidationResult:
    """Structured validation result suitable for CLI and tests."""

    ok: bool
    messages: tuple[str, ...]


def load_manifest(path: Path = DEFAULT_MANIFEST) -> dict[str, Any]:
    """Load a HELIOS fleet manifest from disk."""

    with path.open("r", encoding="utf-8") as handle:
        data = json.load(handle)
    if not isinstance(data, dict):
        raise ValueError("manifest root must be a JSON object")
    return data


def validate_manifest(manifest: dict[str, Any]) -> ValidationResult:
    """Validate cross-stack coverage for HELIOS/HERMES integration."""

    messages: list[str] = []
    systems = manifest.get("systems", [])
    if not isinstance(systems, list):
        return ValidationResult(False, ("systems must be a list",))

    names = {system.get("name") for system in systems if isinstance(system, dict)}
    missing_systems = sorted(REQUIRED_SYSTEMS - names)
    if missing_systems:
        messages.append(f"missing systems: {', '.join(missing_systems)}")

    for system in systems:
        if not isinstance(system, dict):
            messages.append("system entries must be objects")
            continue
        name = system.get("name", "<unnamed>")
        if not system.get("primary_stack"):
            messages.append(f"{name}: primary_stack is required")
        if not system.get("responsibilities"):
            messages.append(f"{name}: responsibilities are required")

    azure_cli = manifest.get("azure_cli", {})
    if not isinstance(azure_cli, dict):
        messages.append("azure_cli must be an object")
    else:
        for script in azure_cli.get("bootstrap_scripts", []):
            script_path = REPO_ROOT / script
            if not script_path.exists():
                messages.append(f"missing Azure bootstrap script: {script}")

    return ValidationResult(not messages, tuple(messages) or ("manifest is valid",))


def check_azure_cli() -> ValidationResult:
    """Check local Azure CLI availability without requiring login or network."""

    az = shutil.which("az")
    if not az:
        return ValidationResult(False, ("Azure CLI executable 'az' was not found",))

    probe = subprocess.run(
        [az, "version", "--output", "json"],
        capture_output=True,
        text=True,
        timeout=30,
        check=False,
    )
    if probe.returncode != 0:
        return ValidationResult(False, (probe.stderr.strip() or "az version failed",))
    return ValidationResult(True, ("Azure CLI is installed",))


def summarize(manifest: dict[str, Any]) -> str:
    """Return a concise human-readable stack summary."""

    lines = [f"Platform: {manifest.get('platform', 'unknown')}", "Systems:"]
    for system in manifest.get("systems", []):
        stacks = ", ".join(system.get("primary_stack", []))
        lines.append(f"- {system.get('name')}: {system.get('role')} [{stacks}]")
    priorities = manifest.get("optimization_priorities", [])
    if priorities:
        lines.append("Optimization priorities:")
        lines.extend(f"- {priority}" for priority in priorities)
    return "\n".join(lines)


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Validate HELIOS/HERMES orchestration manifest")
    parser.add_argument("--manifest", type=Path, default=DEFAULT_MANIFEST)
    parser.add_argument("--check-azure-cli", action="store_true", help="also probe local Azure CLI installation")
    parser.add_argument("--summary", action="store_true", help="print platform summary")
    args = parser.parse_args(argv)

    manifest = load_manifest(args.manifest)
    validation = validate_manifest(manifest)
    for message in validation.messages:
        print(message)
    if args.summary:
        print()
        print(summarize(manifest))
    if args.check_azure_cli:
        azure = check_azure_cli()
        for message in azure.messages:
            print(message)
        validation = ValidationResult(validation.ok and azure.ok, validation.messages + azure.messages)
    return 0 if validation.ok else 1


if __name__ == "__main__":
    sys.exit(main())
