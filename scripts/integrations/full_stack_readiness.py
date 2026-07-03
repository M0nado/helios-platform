#!/usr/bin/env python3
from __future__ import annotations

import json
import shutil
import subprocess
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT_DIR = ROOT / "reports" / "readiness"

TOOLS = {
    "python3": ["python3", "--version"],
    "dotnet": ["dotnet", "--version"],
    "cmake": ["cmake", "--version"],
    "az": ["az", "--version"],
    "az-bicep": ["az", "bicep", "version"],
    "git": ["git", "--version"],
    "gh": ["gh", "--version"],
}

PATHS = [
    "docs/integration/remote-manifest.json",
    "scripts/azure/bicep_report.py",
    "src/tools/HELIOS.RepositoryAnalytics/HELIOS.RepositoryAnalytics.csproj",
    "src/native/HELIOS.Native.Performance/CMakeLists.txt",
    "samples/native-interop/HELIOS.NativeInterop.Sample.csproj",
    "scripts/native/native_smoke.py",
]

HINTS = {
    "dotnet": "Run scripts/setup/bootstrap-local-tools.sh or install .NET 8 SDK.",
    "az": "Install Azure CLI, then run az login for online validation.",
    "az-bicep": "Run az bicep version or configure scripts/setup/bootstrap-local-tools.sh Bicep path.",
    "cmake": "Install CMake 3.22+ for Native/XCore builds.",
    "gh": "Optional: install GitHub CLI for workflow and remote branch status checks.",
}


def run(command: list[str]) -> dict[str, object]:
    if shutil.which(command[0]) is None:
        return {"available": False, "ok": False, "detail": f"{command[0]} not found"}
    proc = subprocess.run(command, cwd=ROOT, text=True, capture_output=True, timeout=30)
    detail = (proc.stdout or proc.stderr).splitlines()[:3]
    return {"available": True, "ok": proc.returncode == 0, "exitCode": proc.returncode, "detail": detail}


def main() -> int:
    tools = {name: {**run(command), "hint": HINTS.get(name, "")} for name, command in TOOLS.items()}
    paths = {path: (ROOT / path).exists() for path in PATHS}
    payload = {
        "generatedUtc": datetime.now(timezone.utc).isoformat(),
        "tools": tools,
        "paths": paths,
        "nextCommands": [
            "scripts/setup/bootstrap-local-tools.sh",
            "python3 scripts/analysis/branch_intelligence.py --remote-inventory-only",
            "python3 scripts/azure/bicep_report.py build",
            "cmake -S src/native/HELIOS.Native.Performance -B .build/native && cmake --build .build/native",
        ],
    }
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    (OUT_DIR / "full-stack-readiness.json").write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n")
    lines = ["# Full-stack Readiness", "", f"Generated: `{payload['generatedUtc']}`", "", "## Tools", "", "| Tool | Available | OK | Hint |", "| --- | --- | --- | --- |"]
    for name, result in tools.items():
        lines.append(f"| `{name}` | {result['available']} | {result['ok']} | {result['hint']} |")
    lines.extend(["", "## Paths", "", "| Path | Exists |", "| --- | --- |"])
    for path, exists in paths.items():
        lines.append(f"| `{path}` | {exists} |")
    lines.extend(["", "## Next commands", ""])
    lines.extend(f"- `{command}`" for command in payload["nextCommands"])
    (OUT_DIR / "full-stack-readiness.md").write_text("\n".join(lines) + "\n")
    print(f"Wrote {(OUT_DIR / 'full-stack-readiness.json').relative_to(ROOT)}")
    print(f"Wrote {(OUT_DIR / 'full-stack-readiness.md').relative_to(ROOT)}")
    return 0 if all(paths.values()) else 1


if __name__ == "__main__":
    raise SystemExit(main())
