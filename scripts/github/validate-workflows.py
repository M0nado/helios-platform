#!/usr/bin/env python3
"""Validate HELIOS GitHub workflow references without external dependencies."""
from __future__ import annotations

import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
WORKFLOW_DIR = ROOT / ".github" / "workflows"

STALE_ACTIONS = {
    "actions/checkout@v3": "actions/checkout@v4",
    "actions/setup-dotnet@v3": "actions/setup-dotnet@v4",
    "actions/cache@v3": "actions/cache@v4",
    "actions/upload-artifact@v3": "actions/upload-artifact@v4",
    "actions/download-artifact@v3": "actions/download-artifact@v4",
    "github/codeql-action/upload-sarif@v2": "github/codeql-action/upload-sarif@v3",
    "peter-evans/create-pull-request@v5": "peter-evans/create-pull-request@v6",
}
PATH_RE = re.compile(r"['\"]([A-Za-z0-9_./-]+\.(?:ps1|sh|sln|csproj|fsproj|vcxproj|json|md|yml|yaml|py))['\"]")
USES_RE = re.compile(r"uses:\s*([^\s#]+)")


def is_generated_runtime_file(path: str) -> bool:
    return path in {"parsed_components.json", "parsed_variants.json", "wiki.db", "status-report.json"}


def main() -> int:
    failures: list[str] = []
    warnings: list[str] = []
    workflows = sorted(WORKFLOW_DIR.glob("*.yml")) + sorted(WORKFLOW_DIR.glob("*.yaml"))

    for workflow in workflows:
        text = workflow.read_text(encoding="utf-8", errors="ignore")
        rel_workflow = workflow.relative_to(ROOT).as_posix()

        for match in USES_RE.finditer(text):
            action = match.group(1).strip()
            if action in STALE_ACTIONS:
                failures.append(f"{rel_workflow}: stale action {action}; use {STALE_ACTIONS[action]}")
            if action.endswith("@master"):
                failures.append(f"{rel_workflow}: unpinned master action {action}")

        for ref in sorted(set(PATH_RE.findall(text))):
            if is_generated_runtime_file(ref) or ref.startswith("http"):
                continue
            candidate = ROOT / ref
            if not candidate.exists() and not ref.startswith("."):
                failures.append(f"{rel_workflow}: referenced path does not exist: {ref}")

    report = {
        "workflow_count": len(workflows),
        "failures": failures,
        "warnings": warnings,
    }
    out_dir = ROOT / "artifacts" / "github"
    out_dir.mkdir(parents=True, exist_ok=True)
    (out_dir / "workflow-validation-report.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")

    if warnings:
        print("Warnings:")
        for warning in warnings:
            print(f"  - {warning}")

    if failures:
        print("Workflow validation failed:")
        for failure in failures:
            print(f"  - {failure}")
        return 1

    print(f"Validated {len(workflows)} workflow files successfully.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
