#!/usr/bin/env python3
"""Blocking HELIOS final gate.

Runs the merge-readiness order requested for HELIOS/Hermes XCore:
workspace hygiene, JSON/Python validation, Azure Bicep build, C# builds,
F# analytics tests, native C++ build, security tests, and mass integration
readiness. Use --report-only for local environments that do not have the full
Windows/.NET/Azure toolchain installed.
"""
from __future__ import annotations

import argparse
import datetime as dt
import json
import shutil
import subprocess
from pathlib import Path
from typing import Any

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "reports" / "final-gate"

COMMANDS: list[dict[str, Any]] = [
    {"id": "workspace-whitespace", "requiredTool": "git", "command": "git diff --check"},
    {"id": "json-configs", "requiredTool": "python3", "command": "python3 -c \"import json; files=['config/helios-agents.json','config/helios-capabilities.json','config/helios-auto-upgrade.json','config/helios-github-setup.json','config/helios-llm-router.json','config/helios-mass-integration.json','config/helios-specializations.json','config/helios-super-automation-backlog.json','config/helios-policy.json']; [json.load(open(path, encoding='utf-8')) for path in files]\""},
    {"id": "python-entrypoints", "requiredTool": "python3", "command": "python3 -m py_compile scripts/github/mass_integration.py scripts/github/setup_repository.py scripts/integrations/helios_capability_setup.py scripts/automation/helios_auto_upgrade.py scripts/automation/finish_helios_setup.py scripts/automation/llm_router_plan.py scripts/automation/specialization_matrix.py scripts/automation/super_automation_backlog.py scripts/automation/final_gate.py scripts/security/policy_gate.py scripts/github/conflict_forecast.py scripts/automation/autofix_loop.py scripts/automation/fix_csharp_compile.py"},
    {"id": "azure-bicep-build", "requiredTool": "az", "command": "az bicep build --file infra/azure/main.bicep"},
    {"id": "csharp-contracts-build", "requiredTool": "dotnet", "command": "dotnet build src/core/HELIOS.Platform.Contracts/HELIOS.Platform.Contracts.csproj --configuration Release"},
    {"id": "csharp-core-build", "requiredTool": "dotnet", "command": "dotnet build HELIOS.Platform.csproj --configuration Release"},
    {"id": "fsharp-analytics-tests", "requiredTool": "dotnet", "command": "dotnet test tests/analytics/HELIOS.Analytics.FSharp.Tests/HELIOS.Analytics.FSharp.Tests.fsproj --configuration Release"},
    {"id": "native-cpp-build", "requiredTool": "cmake", "command": "cmake -S src/native/HELIOS.Native.Performance -B build/native-performance && cmake --build build/native-performance --config Release"},
    {"id": "security-tests", "requiredTool": "dotnet", "command": "dotnet test tests/SecurityValidationTests.csproj --configuration Release"},
    {"id": "main-tests", "requiredTool": "dotnet", "command": "dotnet test src/tests/HELIOS.Platform.Tests.csproj --configuration Release"},
    {"id": "mass-integration-score", "requiredTool": "python3", "command": "python3 scripts/github/mass_integration.py score --no-fetch"},
    {"id": "conflict-forecast", "requiredTool": "python3", "command": "python3 scripts/github/conflict_forecast.py"},
    {"id": "policy-gate", "requiredTool": "python3", "command": "python3 scripts/security/policy_gate.py"},
    {"id": "autofix-plan", "requiredTool": "python3", "command": "python3 scripts/automation/autofix_loop.py plan"}
]


def run_step(step: dict[str, Any], report_only: bool) -> dict[str, Any]:
    tool = step["requiredTool"]
    if shutil.which(tool) is None:
        return {**step, "status": "warning" if report_only else "failed", "exitCode": None, "stdout": "", "stderr": f"Required tool '{tool}' is not available"}
    proc = subprocess.run(step["command"], cwd=ROOT, shell=True, text=True, capture_output=True)
    status = "passed" if proc.returncode == 0 else ("warning" if report_only else "failed")
    return {**step, "status": status, "exitCode": proc.returncode, "stdout": proc.stdout[-4000:], "stderr": proc.stderr[-4000:]}


def write_reports(payload: dict[str, Any]) -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / "final-gate.json").write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    lines = ["# HELIOS Final Gate", "", f"Generated: {payload['generatedUtc']}", f"Report only: {payload['reportOnly']}", "", "## First Blocker", ""]
    if payload.get("firstBlocker"):
        blocker = payload["firstBlocker"]
        lines.extend([f"- Step: `{blocker['id']}`", f"- Rerun: `{blocker['rerun']}`", f"- Fixer: `{blocker['recommendedFixer']}`", ""])
    else:
        lines.extend(["- None", ""])
    lines.extend(["| Step | Status | Command |", "| --- | --- | --- |"])
    for result in payload["results"]:
        lines.append(f"| `{result['id']}` | {result['status']} | `{result['command']}` |")
    (OUT / "final-gate.md").write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--report-only", action="store_true", help="record warnings instead of failing on missing tools or failed commands")
    args = parser.parse_args()
    results = [run_step(step, report_only=args.report_only) for step in COMMANDS]
    failed = [result for result in results if result["status"] == "failed"]
    first_failed = failed[0] if failed else None
    first_blocker = None
    if first_failed:
        first_blocker = {
            "id": first_failed["id"],
            "command": first_failed["command"],
            "category": first_failed["id"].split("-")[0],
            "recommendedFixer": "scripts/automation/autofix_loop.py plan",
            "rerun": first_failed["command"],
            "blocksMassIntegration": True,
        }
    payload = {
        "generatedUtc": dt.datetime.now(dt.timezone.utc).isoformat(),
        "reportOnly": args.report_only,
        "results": results,
        "failed": failed,
        "firstBlocker": first_blocker,
    }
    write_reports(payload)
    print(f"Wrote {(OUT / 'final-gate.md').relative_to(ROOT)}")
    return 1 if failed else 0


if __name__ == "__main__":
    raise SystemExit(main())
