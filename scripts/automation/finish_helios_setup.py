#!/usr/bin/env python3
"""Final HELIOS setup finisher.

Runs the complete non-mutating verification stack by default and can run the
mutating setup/apply chain when explicitly called with apply mode.
"""
from __future__ import annotations

import argparse
import datetime as dt
import json
import subprocess
from pathlib import Path
from typing import Any

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "reports" / "finish-setup"

VERIFY_COMMANDS = [
    "python3 scripts/github/setup_repository.py verify",
    "python3 scripts/integrations/helios_capability_setup.py verify",
    "python3 scripts/github/mass_integration.py score --no-fetch",
    "python3 scripts/automation/helios_auto_upgrade.py gui",
    "python3 -m json.tool config/helios-agents.json",
    "python3 -m json.tool config/helios-capabilities.json",
    "python3 -m json.tool config/helios-github-setup.json",
    "python3 -m json.tool config/helios-mass-integration.json",
    "python3 -m json.tool config/helios-auto-upgrade.json",
    "python3 -m py_compile scripts/github/mass_integration.py scripts/github/setup_repository.py scripts/integrations/helios_capability_setup.py scripts/automation/helios_auto_upgrade.py scripts/automation/finish_helios_setup.py",
    "git diff --check"
]

APPLY_COMMANDS = [
    "python3 scripts/github/setup_repository.py setup --apply",
    "python3 scripts/integrations/helios_capability_setup.py setup --apply",
    "python3 scripts/github/mass_integration.py all --apply",
    "python3 scripts/automation/helios_auto_upgrade.py apply"
]


def run(command: str, execute: bool) -> dict[str, Any]:
    if not execute:
        return {"command": command, "dryRun": True, "exitCode": None, "stdout": "", "stderr": ""}
    proc = subprocess.run(command, cwd=ROOT, shell=True, text=True, capture_output=True)
    return {
        "command": command,
        "dryRun": False,
        "exitCode": proc.returncode,
        "stdout": proc.stdout[-3000:],
        "stderr": proc.stderr[-3000:]
    }


def write_reports(payload: dict[str, Any]) -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / "finish-setup.json").write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    lines = ["# HELIOS Finish Setup", "", f"Generated: {payload['generatedUtc']}", f"Mode: {payload['mode']}", "", "## Results", ""]
    for result in payload["results"]:
        if result["dryRun"]:
            status = "DRY-RUN"
        elif result["exitCode"] == 0:
            status = "PASS"
        else:
            status = "FAIL"
        lines.append(f"- **{status}** `{result['command']}`")
    (OUT / "finish-setup.md").write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("mode", choices=["plan", "verify", "apply"], nargs="?", default="verify")
    args = parser.parse_args()
    commands = VERIFY_COMMANDS if args.mode in {"plan", "verify"} else VERIFY_COMMANDS + APPLY_COMMANDS
    execute = args.mode in {"verify", "apply"}
    results = [run(command, execute=execute) for command in commands]
    payload = {
        "generatedUtc": dt.datetime.now(dt.timezone.utc).isoformat(),
        "mode": args.mode,
        "results": results,
        "failed": [r for r in results if (not r["dryRun"] and r["exitCode"] not in (0, None))]
    }
    write_reports(payload)
    print(f"Wrote {(OUT / 'finish-setup.md').relative_to(ROOT)}")
    return 1 if payload["failed"] else 0


if __name__ == "__main__":
    raise SystemExit(main())
