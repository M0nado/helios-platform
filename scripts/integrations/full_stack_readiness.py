#!/usr/bin/env python3
from __future__ import annotations

import json
import shutil
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "reports" / "integrations" / "full-stack-readiness.json"
MD = ROOT / "reports" / "integrations" / "full-stack-readiness.md"

REQUIRED_TOOLS = {
    "python3": ["python3", "--version"],
    "dotnet": ["dotnet", "--version"],
    "cmake": ["cmake", "--version"],
    "az": ["az", "--version"],
    "az-bicep": ["az", "bicep", "version"],
    "git": ["git", "--version"],
}
OPTIONAL_TOOLS = {
    "gh": ["gh", "--version"],
}
REQUIRED_PATHS = [
    "helios.sh",
    "config/execution-order.json",
    "config/secrets-map.example.json",
    "scripts/integrations/readiness_score.py",
    "scripts/integrations/check-connections.py",
    "infra/azure/main.bicep",
]


def run(cmd: list[str]) -> tuple[int, str]:
    try:
        proc = subprocess.run(cmd, cwd=ROOT, text=True, capture_output=True, timeout=30)
    except subprocess.TimeoutExpired:
        return 124, "Timed out after 30 seconds"
    except OSError as exc:
        return 127, str(exc)
    detail = (proc.stdout or proc.stderr).strip()
    return proc.returncode, detail


def check_tool(name: str, cmd: list[str], required: bool) -> dict[str, object]:
    executable = cmd[0]
    path = shutil.which(executable)
    if path is None:
        return {
            "name": name,
            "required": required,
            "command": cmd,
            "path": None,
            "available": False,
            "healthy": False,
            "status": "missing",
            "detail": f"{executable} not found on PATH",
        }

    code, detail = run(cmd)
    first_line = detail.splitlines()[0] if detail else "Command completed without output"
    return {
        "name": name,
        "required": required,
        "command": cmd,
        "path": path,
        "available": True,
        "healthy": code == 0,
        "status": "ok" if code == 0 else "unhealthy",
        "exitCode": code,
        "detail": first_line,
    }


def status_icon(item: dict[str, object]) -> str:
    if item["healthy"]:
        return "✅"
    return "❌" if item["required"] else "⚠️"


def main() -> int:
    required_tools = [check_tool(name, cmd, True) for name, cmd in REQUIRED_TOOLS.items()]
    optional_tools = [check_tool(name, cmd, False) for name, cmd in OPTIONAL_TOOLS.items()]
    paths = [
        {"path": path, "exists": (ROOT / path).exists()}
        for path in REQUIRED_PATHS
    ]

    required_tools_ok = all(item["healthy"] for item in required_tools)
    optional_tools_ok = all(item["healthy"] for item in optional_tools)
    paths_ok = all(item["exists"] for item in paths)
    overall_ok = required_tools_ok and paths_ok

    payload = {
        "generatedUtc": datetime.now(timezone.utc).isoformat(),
        "summary": {
            "requiredToolsOk": required_tools_ok,
            "optionalToolsOk": optional_tools_ok,
            "pathsOk": paths_ok,
            "overallOk": overall_ok,
        },
        "requiredTools": required_tools,
        "optionalTools": optional_tools,
        "paths": paths,
        "notes": "Optional tools are reported as warnings only and do not affect overallOk or the process exit code.",
    }

    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n")

    lines = [
        "# HELIOS Full Stack Readiness",
        "",
        f"Generated: `{payload['generatedUtc']}`",
        "",
        "## Summary",
        "",
        f"- Required tools: {'✅ OK' if required_tools_ok else '❌ Missing or unhealthy'}",
        f"- Optional tools: {'✅ OK' if optional_tools_ok else '⚠️ Warnings only'}",
        f"- Required paths: {'✅ OK' if paths_ok else '❌ Missing'}",
        f"- Overall: {'✅ OK' if overall_ok else '❌ Not ready'}",
        "",
        "## Required tools",
        "",
        "| Tool | Status | Command | Detail |",
        "| --- | --- | --- | --- |",
    ]
    for item in required_tools:
        lines.append(f"| `{item['name']}` | {status_icon(item)} {item['status']} | `{' '.join(item['command'])}` | {item['detail']} |")
    lines += ["", "## Optional tools", "", "| Tool | Status | Command | Detail |", "| --- | --- | --- | --- |"]
    for item in optional_tools:
        lines.append(f"| `{item['name']}` | {status_icon(item)} {item['status']} | `{' '.join(item['command'])}` | {item['detail']} |")
    lines += ["", "## Required paths", "", "| Path | Status |", "| --- | --- |"]
    for item in paths:
        lines.append(f"| `{item['path']}` | {'✅ exists' if item['exists'] else '❌ missing'} |")
    MD.write_text("\n".join(lines) + "\n")

    print(f"Wrote {OUT.relative_to(ROOT)}")
    print(f"Wrote {MD.relative_to(ROOT)}")
    return 0 if overall_ok else 1


if __name__ == "__main__":
    sys.exit(main())
