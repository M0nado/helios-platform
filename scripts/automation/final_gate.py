#!/usr/bin/env python3
"""Blocking HELIOS final gate.

Runs the merge-readiness order requested for HELIOS/Hermes XCore and writes a
stable first-blocker report used by mass integration, autofix, dashboard, and
policy gates. Use --report-only for local environments that do not have the full
Windows/.NET/Azure toolchain installed.
"""
from __future__ import annotations

import argparse
import datetime as dt
import json
import platform
import shutil
import subprocess
from pathlib import Path
from typing import Any

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "reports" / "final-gate"

COMMANDS: list[dict[str, Any]] = [
    {"id": "workspace-whitespace", "domain": "repo", "language": "git", "ownerAgent": "github-admiral", "requiredTool": "git", "command": "git diff --check", "fixer": "git diff --check"},
    {"id": "json-configs", "domain": "config", "language": "json", "ownerAgent": "github-admiral", "requiredTool": "python3", "command": "python3 -c \"import json; files=['config/helios-agents.json','config/helios-agent-runtime.json','config/helios-agent-shop.json','config/helios-language-ownership.json','config/helios-language-optimization-matrix.json','config/helios-unitary-ai-system.json','config/helios-ml-models.json','config/helios-party-formations.json','config/helios-capabilities.json','config/helios-auto-upgrade.json','config/helios-github-setup.json','config/helios-llm-router.json','config/helios-mass-integration.json','config/helios-model-store.json','config/helios-policy.json','config/helios-specializations.json','config/helios-super-automation-backlog.json']; [json.load(open(path, encoding='utf-8')) for path in files]\"", "fixer": "python3 -m json.tool <file>"},
    {"id": "python-entrypoints", "domain": "automation", "language": "python", "ownerAgent": "python-aihub", "requiredTool": "python3", "command": "python3 -m py_compile scripts/github/mass_integration.py scripts/github/setup_repository.py scripts/integrations/helios_capability_setup.py scripts/automation/helios_auto_upgrade.py scripts/automation/finish_helios_setup.py scripts/automation/llm_router_plan.py scripts/automation/specialization_matrix.py scripts/automation/super_automation_backlog.py scripts/automation/final_gate.py scripts/security/policy_gate.py scripts/github/conflict_forecast.py scripts/automation/autofix_loop.py scripts/automation/fix_csharp_compile.py scripts/automation/code_fix_center.py scripts/automation/provider_health.py scripts/automation/model_cost_speed_optimizer.py scripts/automation/gui_runner_bridge.py scripts/learning/fleet_deploy.py scripts/learning/core_ai_learning.py scripts/learning/summarize_learning.py scripts/learning/record_event.py scripts/security/automation_audit.py scripts/automation/language_ownership_report.py scripts/automation/language_optimization_matrix.py scripts/automation/unitary_ai_system.py scripts/learning/ml_model_registry.py scripts/learning/party_formations.py scripts/github/language_aware_score.py scripts/github/merge_decision_pipeline.py scripts/analytics/fsharp_category_report.py scripts/automation/validate_report_contracts.py scripts/automation/language_required_checks.py scripts/browser/edge_mode_readiness.py", "fixer": "python3 scripts/automation/code_fix_center.py"},
    {"id": "azure-bicep-build", "domain": "azure", "language": "bicep", "ownerAgent": "azure-warden", "requiredTool": "az", "command": "az bicep build --file infra/azure/main.bicep", "fixer": "python3 scripts/azure/bicep_report.py build"},
    {"id": "csharp-contracts-build", "domain": "core", "language": "csharp", "ownerAgent": "csharp-center", "requiredTool": "dotnet", "command": "dotnet build src/core/HELIOS.Platform.Contracts/HELIOS.Platform.Contracts.csproj --configuration Release", "fixer": "python3 scripts/automation/fix_csharp_compile.py"},
    {"id": "csharp-core-build", "domain": "core", "language": "csharp", "ownerAgent": "csharp-center", "requiredTool": "dotnet", "command": "dotnet build HELIOS.Platform.csproj --configuration Release", "fixer": "python3 scripts/automation/fix_csharp_compile.py"},
    {"id": "fsharp-analytics-tests", "domain": "analytics", "language": "fsharp", "ownerAgent": "fsharp-oracle", "requiredTool": "dotnet", "command": "dotnet test tests/analytics/HELIOS.Analytics.FSharp.Tests/HELIOS.Analytics.FSharp.Tests.fsproj --configuration Release", "fixer": "python3 scripts/analytics/fsharp_test_report.py"},
    {"id": "native-benchmark-baseline", "domain": "native", "language": "cpp", "ownerAgent": "cpp-accelerator", "requiredTool": "python3", "command": "python3 scripts/native/benchmark_native.py", "fixer": "python3 scripts/native/benchmark_native.py"},
    {"id": "native-cpp-build", "domain": "native", "language": "cpp", "ownerAgent": "cpp-accelerator", "requiredTool": "cmake", "command": "cmake -S src/native/HELIOS.Native.Performance -B build/native-performance && cmake --build build/native-performance --config Release", "fixer": "python3 scripts/native/benchmark_native.py"},
    {"id": "azure-validate", "domain": "azure", "language": "bicep", "ownerAgent": "azure-warden", "requiredTool": "python3", "command": "python3 scripts/azure/bicep_report.py validate", "fixer": "python3 scripts/azure/bicep_report.py validate"},
    {"id": "azure-what-if", "domain": "azure", "language": "bicep", "ownerAgent": "azure-warden", "requiredTool": "python3", "command": "python3 scripts/azure/bicep_report.py what-if", "fixer": "python3 scripts/azure/bicep_report.py what-if"},
    {"id": "security-tests", "domain": "security", "language": "csharp", "ownerAgent": "security-warden", "requiredTool": "dotnet", "command": "dotnet test tests/SecurityValidationTests.csproj --configuration Release", "fixer": "python3 scripts/security/policy_gate.py"},
    {"id": "main-tests", "domain": "tests", "language": "csharp", "ownerAgent": "csharp-center", "requiredTool": "dotnet", "command": "dotnet test src/tests/HELIOS.Platform.Tests.csproj --configuration Release", "fixer": "python3 scripts/automation/fix_csharp_compile.py"},
    {"id": "mass-integration-score", "domain": "github", "language": "git", "ownerAgent": "github-admiral", "requiredTool": "python3", "command": "python3 scripts/github/mass_integration.py score --no-fetch", "fixer": "python3 scripts/github/mass_integration.py score --no-fetch"},
    {"id": "conflict-forecast", "domain": "github", "language": "git", "ownerAgent": "github-admiral", "requiredTool": "python3", "command": "python3 scripts/github/conflict_forecast.py", "fixer": "python3 scripts/github/conflict_forecast.py"},
    {"id": "python-aihub-smoke", "domain": "aihub", "language": "python", "ownerAgent": "python-aihub", "requiredTool": "python3", "command": "python3 tools/aihub/smoke-test.py", "fixer": "python3 tools/aihub/smoke-test.py"},
    {"id": "vault-readiness", "domain": "security", "language": "policy", "ownerAgent": "azure-warden", "requiredTool": "python3", "command": "python3 scripts/security/vault_readiness.py verify", "fixer": "python3 scripts/security/vault_readiness.py verify"},
    {"id": "policy-gate", "domain": "security", "language": "policy", "ownerAgent": "security-warden", "requiredTool": "python3", "command": "python3 scripts/security/policy_gate.py", "fixer": "python3 scripts/security/policy_gate.py"},
    {"id": "language-ownership", "domain": "architecture", "language": "mixed", "ownerAgent": "csharp-center", "requiredTool": "python3", "command": "python3 scripts/automation/language_ownership_report.py", "fixer": "python3 scripts/automation/language_ownership_report.py"},
    {"id": "unitary-ai-system", "domain": "architecture", "language": "mixed", "ownerAgent": "csharp-center", "requiredTool": "python3", "command": "python3 scripts/automation/unitary_ai_system.py", "fixer": "python3 scripts/automation/unitary_ai_system.py"},
    {"id": "language-optimization-matrix", "domain": "architecture", "language": "mixed", "ownerAgent": "csharp-center", "requiredTool": "python3", "command": "python3 scripts/automation/language_optimization_matrix.py", "fixer": "python3 scripts/automation/language_optimization_matrix.py"},
    {"id": "ml-model-registry", "domain": "learning", "language": "fsharp", "ownerAgent": "fsharp-oracle", "requiredTool": "python3", "command": "python3 scripts/learning/ml_model_registry.py", "fixer": "python3 scripts/learning/ml_model_registry.py"},
    {"id": "party-formations", "domain": "agents", "language": "mixed", "ownerAgent": "github-admiral", "requiredTool": "python3", "command": "python3 scripts/learning/party_formations.py", "fixer": "python3 scripts/learning/party_formations.py"},
    {"id": "language-aware-merge-score", "domain": "github", "language": "mixed", "ownerAgent": "fsharp-oracle", "requiredTool": "python3", "command": "python3 scripts/github/language_aware_score.py", "fixer": "python3 scripts/github/language_aware_score.py"},
    {"id": "merge-decision-pipeline", "domain": "github", "language": "mixed", "ownerAgent": "github-admiral", "requiredTool": "python3", "command": "python3 scripts/github/merge_decision_pipeline.py", "fixer": "python3 scripts/github/merge_decision_pipeline.py"},
    {"id": "fsharp-category-report", "domain": "analytics", "language": "fsharp", "ownerAgent": "fsharp-oracle", "requiredTool": "python3", "command": "python3 scripts/analytics/fsharp_category_report.py scripts/automation/validate_report_contracts.py scripts/automation/language_required_checks.py scripts/browser/edge_mode_readiness.py", "fixer": "python3 scripts/analytics/fsharp_category_report.py"},
    {"id": "report-contract-validation", "domain": "contracts", "language": "csharp", "ownerAgent": "csharp-center", "requiredTool": "python3", "command": "python3 scripts/automation/validate_report_contracts.py", "fixer": "python3 scripts/automation/validate_report_contracts.py"},
    {"id": "language-required-checks", "domain": "architecture", "language": "mixed", "ownerAgent": "csharp-center", "requiredTool": "python3", "command": "python3 scripts/automation/language_required_checks.py", "fixer": "python3 scripts/automation/language_required_checks.py"},
    {"id": "edge-mode-readiness", "domain": "browser", "language": "csharp", "ownerAgent": "csharp-center", "requiredTool": "python3", "command": "python3 scripts/browser/edge_mode_readiness.py", "fixer": "python3 scripts/browser/edge_mode_readiness.py"},
    {"id": "autofix-plan", "domain": "autofix", "language": "python", "ownerAgent": "python-aihub", "requiredTool": "python3", "command": "python3 scripts/automation/autofix_loop.py plan", "fixer": "python3 scripts/automation/autofix_loop.py plan"},
]


def run_step(step: dict[str, Any], report_only: bool) -> dict[str, Any]:
    if shutil.which(step["requiredTool"]) is None:
        status = "warning" if report_only else "failed"
        return {**step, "status": status, "exitCode": None, "stdout": "", "stderr": f"Required tool '{step['requiredTool']}' is not available"}
    proc = subprocess.run(step["command"], cwd=ROOT, shell=True, text=True, capture_output=True)
    status = "passed" if proc.returncode == 0 else ("warning" if report_only else "failed")
    return {**step, "status": status, "exitCode": proc.returncode, "stdout": proc.stdout[-4000:], "stderr": proc.stderr[-4000:]}


def blocker_from(result: dict[str, Any] | None) -> dict[str, Any] | None:
    if not result:
        return None
    return {
        "id": result["id"],
        "domain": result.get("domain", "unknown"),
        "language": result.get("language", "unknown"),
        "ownerAgent": result.get("ownerAgent", "unknown"),
        "command": result["command"],
        "rerun": result["command"],
        "recommendedFixer": result.get("fixer", "python3 scripts/automation/autofix_loop.py plan"),
        "requiredArtifacts": ["reports/final-gate/final-gate.json"],
        "blocksMassIntegration": True,
        "stderrTail": result.get("stderr", "")[-1200:],
    }


def write_reports(payload: dict[str, Any]) -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / "final-gate.json").write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    lines = ["# HELIOS Final Gate", "", f"Generated: {payload['generatedUtc']}", f"Status: **{payload['status']}**", f"Report only: {payload['reportOnly']}", "", "## First Blocker", ""]
    blocker = payload.get("firstBlocker")
    if blocker:
        lines.extend([
            f"- Step: `{blocker['id']}`",
            f"- Domain: `{blocker['domain']}`",
            f"- Language: `{blocker['language']}`",
            f"- Owner agent: `{blocker['ownerAgent']}`",
            f"- Rerun: `{blocker['rerun']}`",
            f"- Fixer: `{blocker['recommendedFixer']}`",
            "",
        ])
    else:
        lines.extend(["- None", ""])
    lines.extend(["## Results", "", "| Step | Domain | Language | Agent | Status | Command |", "| --- | --- | --- | --- | --- | --- |"])
    for result in payload["results"]:
        lines.append(f"| `{result['id']}` | {result.get('domain','')} | {result.get('language','')} | {result.get('ownerAgent','')} | {result['status']} | `{result['command']}` |")
    (OUT / "final-gate.md").write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--report-only", action="store_true", help="record warnings instead of failing on missing tools or failed commands")
    args = parser.parse_args()
    results = [run_step(step, report_only=args.report_only) for step in COMMANDS]
    failed = [result for result in results if result["status"] == "failed"]
    warnings = [result for result in results if result["status"] == "warning"]
    payload = {
        "schemaVersion": 2,
        "generatedUtc": dt.datetime.now(dt.timezone.utc).isoformat(),
        "reportOnly": args.report_only,
        "status": "failed" if failed else ("warning" if warnings else "passed"),
        "runner": {"python": platform.python_version(), "platform": platform.platform(), "cwd": str(ROOT)},
        "summary": {"passed": sum(1 for r in results if r["status"] == "passed"), "warning": len(warnings), "failed": len(failed), "total": len(results)},
        "results": results,
        "failed": failed,
        "warnings": warnings,
        "firstBlocker": blocker_from(failed[0] if failed else None),
        "mergeReady": not failed,
    }
    write_reports(payload)
    print(f"Wrote {(OUT / 'final-gate.md').relative_to(ROOT)}")
    return 1 if failed else 0


if __name__ == "__main__":
    raise SystemExit(main())
