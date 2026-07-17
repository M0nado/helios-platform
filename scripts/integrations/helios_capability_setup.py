#!/usr/bin/env python3
"""HELIOS deep capability setup/readiness reporter.

This script centralizes setup ordering for Azure CLI, Microsoft 365/Copilot,
GitHub/Cloud Shell, MCP, OpenAI/Codex, Hermes XCore, and agent skills. It is a
readiness/report generator by default and only executes declared setup commands
when --apply is supplied.
"""
from __future__ import annotations

import argparse
import datetime as dt
import json
import os
import shutil
import subprocess
from pathlib import Path
from typing import Any

ROOT = Path(__file__).resolve().parents[2]
DEFAULT_CONFIG = ROOT / "config" / "helios-capabilities.json"
DEFAULT_OUT = ROOT / "reports" / "capabilities"


def run(command: str) -> dict[str, Any]:
    proc = subprocess.run(command, cwd=ROOT, shell=True, text=True, capture_output=True)
    return {
        "command": command,
        "exitCode": proc.returncode,
        "stdout": proc.stdout[-2000:],
        "stderr": proc.stderr[-2000:],
    }


def load_config(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8"))


def check_command(name: str) -> bool:
    return shutil.which(name) is not None


def check_capability(capability: dict[str, Any], apply: bool) -> dict[str, Any]:
    command_status = {name: check_command(name) for name in capability.get("requiredCommands", [])}
    env_status = {name: bool(os.environ.get(name)) for name in capability.get("requiredEnvironment", [])}
    optional_env_status = {name: bool(os.environ.get(name)) for name in capability.get("optionalEnvironment", [])}
    path_status = {path: (ROOT / path).exists() for path in capability.get("ownedPaths", [])}
    missing_commands = [name for name, present in command_status.items() if not present]
    missing_environment = [name for name, present in env_status.items() if not present]
    missing_paths = [path for path, present in path_status.items() if not present]
    readiness_score = 100
    readiness_score -= len(missing_commands) * 25
    readiness_score -= len(missing_environment) * 15
    readiness_score -= len(missing_paths) * 10
    readiness_score = max(0, readiness_score)
    status = "ready" if readiness_score >= 80 and not missing_commands and not missing_paths else "needs-setup"
    setup_results = []
    if apply:
        for command in capability.get("setupCommands", []):
            setup_results.append(run(command))
    return {
        "id": capability["id"],
        "title": capability.get("title", capability["id"]),
        "status": status,
        "readinessScore": readiness_score,
        "automationRole": capability.get("automationRole", ""),
        "commands": command_status,
        "requiredEnvironment": env_status,
        "optionalEnvironment": optional_env_status,
        "paths": path_status,
        "missingCommands": missing_commands,
        "missingEnvironment": missing_environment,
        "missingPaths": missing_paths,
        "setupCommands": capability.get("setupCommands", []),
        "setupResults": setup_results,
    }


def write_reports(payload: dict[str, Any], out_dir: Path) -> None:
    out_dir.mkdir(parents=True, exist_ok=True)
    (out_dir / "capability-readiness.json").write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    lines = [
        "# HELIOS Capability Readiness",
        "",
        f"Generated: {payload['generatedUtc']}",
        f"Apply mode: {payload['apply']}",
        "",
        "## Setup order",
        "",
    ]
    for idx, item in enumerate(payload["setupOrder"], 1):
        lines.append(f"{idx}. `{item}`")
    lines.extend(["", "## Capability status", "", "| Capability | Status | Score | Missing commands | Missing env |", "| --- | --- | ---: | --- | --- |"])
    for result in payload["capabilities"]:
        lines.append(
            f"| `{result['id']}` | {result['status']} | {result['readinessScore']} | "
            f"{', '.join(result['missingCommands']) or '-'} | {', '.join(result['missingEnvironment']) or '-'} |"
        )
    lines.append("")
    lines.append("## Automation roles")
    lines.append("")
    for result in payload["capabilities"]:
        lines.append(f"- `{result['id']}`: {result['automationRole']}")
    (out_dir / "capability-readiness.md").write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("mode", choices=["verify", "setup"], nargs="?", default="verify")
    parser.add_argument("--config", default=str(DEFAULT_CONFIG))
    parser.add_argument("--out", default=str(DEFAULT_OUT))
    parser.add_argument("--apply", action="store_true", help="execute declared setup commands")
    args = parser.parse_args()
    apply = args.apply or args.mode == "setup"
    config = load_config(Path(args.config))
    ordered = []
    by_id = {item["id"]: item for item in config.get("capabilities", [])}
    for capability_id in config.get("setupOrder", []):
        if capability_id in by_id:
            ordered.append(by_id[capability_id])
    for capability in config.get("capabilities", []):
        if capability not in ordered:
            ordered.append(capability)
    payload = {
        "generatedUtc": dt.datetime.now(dt.timezone.utc).isoformat(),
        "apply": apply,
        "setupOrder": [item["id"] for item in ordered],
        "capabilities": [check_capability(item, apply=apply) for item in ordered],
    }
    write_reports(payload, Path(args.out))
    print(f"Wrote {(Path(args.out) / 'capability-readiness.md').relative_to(ROOT)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
