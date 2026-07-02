#!/usr/bin/env python3
"""Start HELIOS automation in the shortest safe order.

This runner encodes the ASAP sequence: final gate, Azure Bicep validation,
branch scoring, GitHub setup, mass integration branch apply, PR creation, and
auto-merge request. It defaults to plan mode; verify runs non-mutating checks;
apply executes mutating steps only after the blocking final gate succeeds.
"""
from __future__ import annotations

import argparse
import datetime as dt
import json
import subprocess
from pathlib import Path
from typing import Any

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "reports" / "start-asap"

PLAN = [
    {"id": "final-gate", "command": "python3 scripts/automation/final_gate.py", "mode": "verify"},
    {"id": "azure-bicep-build", "command": "./tools/helios.ps1 azure bicep-build", "mode": "verify"},
    {"id": "azure-validate", "command": "./tools/helios.ps1 azure validate", "mode": "verify"},
    {"id": "azure-what-if", "command": "./tools/helios.ps1 azure what-if", "mode": "verify"},
    {"id": "mass-score", "command": "python3 scripts/github/mass_integration.py score", "mode": "verify"},
    {"id": "conflict-forecast", "command": "python3 scripts/github/conflict_forecast.py", "mode": "verify"},
    {"id": "policy-gate", "command": "python3 scripts/security/policy_gate.py", "mode": "verify"},
    {"id": "github-repo-setup", "command": "python3 scripts/github/setup_repository.py setup --apply", "mode": "apply"},
    {"id": "mass-branch-apply", "command": "python3 scripts/github/mass_integration.py branch --apply", "mode": "apply"},
    {"id": "mass-pr-apply", "command": "python3 scripts/github/mass_integration.py pr --apply", "mode": "apply"},
    {"id": "mass-merge-apply", "command": "python3 scripts/github/mass_integration.py merge --apply", "mode": "apply"}
]


def run(command: str, execute: bool) -> dict[str, Any]:
    if not execute:
        return {"command": command, "dryRun": True, "exitCode": None, "stdout": "", "stderr": ""}
    proc = subprocess.run(command, cwd=ROOT, shell=True, text=True, capture_output=True)
    return {"command": command, "dryRun": False, "exitCode": proc.returncode, "stdout": proc.stdout[-4000:], "stderr": proc.stderr[-4000:]}


def write_reports(payload: dict[str, Any]) -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / "start-asap.json").write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    lines = ["# HELIOS Start ASAP", "", f"Generated: {payload['generatedUtc']}", f"Mode: {payload['mode']}", "", "| Order | Step | Status | Command |", "| ---: | --- | --- | --- |"]
    for idx, result in enumerate(payload["results"], 1):
        status = "DRY-RUN" if result["dryRun"] else "PASS" if result["exitCode"] == 0 else "FAIL"
        lines.append(f"| {idx} | `{result['id']}` | {status} | `{result['command']}` |")
    (OUT / "start-asap.md").write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("mode", choices=["plan", "verify", "apply"], nargs="?", default="plan")
    args = parser.parse_args()
    results = []
    for step in PLAN:
        should_execute = args.mode == "apply" or (args.mode == "verify" and step["mode"] == "verify")
        result = {"id": step["id"], **run(step["command"], execute=should_execute)}
        results.append(result)
        if args.mode == "apply" and result["exitCode"] not in (0, None):
            break
    payload = {"generatedUtc": dt.datetime.now(dt.timezone.utc).isoformat(), "mode": args.mode, "results": results}
    write_reports(payload)
    print(f"Wrote {(OUT / 'start-asap.md').relative_to(ROOT)}")
    failed = [r for r in results if not r["dryRun"] and r["exitCode"] not in (0, None)]
    return 1 if failed else 0


if __name__ == "__main__":
    raise SystemExit(main())
