#!/usr/bin/env python3
"""Validate GitHub workflow hygiene without requiring network access."""
from __future__ import annotations

import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
WORKFLOWS = ROOT / ".github" / "workflows"
REPORT = ROOT / "artifacts" / "workflow-validation.json"
STALE_ACTIONS = {
    "actions/checkout@v3": "actions/checkout@v4",
    "actions/setup-dotnet@v3": "actions/setup-dotnet@v4",
    "actions/cache@v3": "actions/cache@v4",
    "actions/upload-artifact@v3": "actions/upload-artifact@v4",
    "actions/download-artifact@v3": "actions/download-artifact@v4",
    "actions/setup-powershell@v2": "actions/setup-powershell@v4",
    "github/codeql-action/upload-sarif@v2": "github/codeql-action/upload-sarif@v3",
    "aquasecurity/trivy-action@master": "aquasecurity/trivy-action@0.24.0",
}
IGNORED_REFERENCE_PREFIXES = ("http://", "https://", "${{", "$", "*", "**")
REFERENCE_RE = re.compile(r"['\"]([A-Za-z0-9_./${}*-]+\.(?:ps1|sh|sln|slnx|csproj|json|md|yml|yaml|py))['\"]")


def workflow_files() -> list[Path]:
    return sorted(WORKFLOWS.glob("*.yml")) + sorted(WORKFLOWS.glob("*.yaml"))


def missing_references(text: str) -> list[str]:
    missing: set[str] = set()
    for ref in REFERENCE_RE.findall(text):
        if ref.startswith(IGNORED_REFERENCE_PREFIXES):
            continue
        if Path(ref).name in {"parsed_components.json", "parsed_variants.json"}:
            continue
        if "${{" in ref or "$" in ref or "*" in ref or "{" in ref or "}" in ref:
            continue
        if not (ROOT / ref).exists():
            missing.add(ref)
    return sorted(missing)


def main() -> int:
    failures = []
    details = []
    for path in workflow_files():
        rel = path.relative_to(ROOT).as_posix()
        text = path.read_text(encoding="utf-8")
        stale = [old for old in STALE_ACTIONS if old in text]
        missing = missing_references(text)
        cache_build_outputs = "**/bin/" in text or "**/obj/" in text
        continue_on_error_build = "ContinueOnError=true" in text
        record = {
            "workflow": rel,
            "stale_actions": stale,
            "missing_references": missing,
            "caches_build_outputs": cache_build_outputs,
            "build_continue_on_error": continue_on_error_build,
        }
        details.append(record)
        if stale or missing or cache_build_outputs or continue_on_error_build:
            failures.append(record)

    REPORT.parent.mkdir(parents=True, exist_ok=True)
    REPORT.write_text(json.dumps({"ok": not failures, "workflows": details}, indent=2) + "\n", encoding="utf-8")
    print(REPORT.relative_to(ROOT))
    if failures:
        print(json.dumps(failures, indent=2))
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
