#!/usr/bin/env python3
from __future__ import annotations

import json
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SCRIPTS = ROOT / "scripts"
if str(SCRIPTS) not in sys.path:
    sys.path.insert(0, str(SCRIPTS))

from common.tool_resolver import environment_with_tools, path_addition_strings, resolve_tool, resolved_tools

OUT = ROOT / "reports/integrations/full-stack-readiness.json"
MD = ROOT / "reports/integrations/full-stack-readiness.md"
TOOLS = ("python3", "git", "dotnet", "az", "gh", "bicep", "cmake")


def probe_tool(name: str, args: list[str]) -> dict[str, object]:
    resolved = resolve_tool(name, root=ROOT)
    result: dict[str, object] = {
        "name": name,
        "available": resolved is not None,
        "resolvedPath": str(resolved) if resolved else None,
        "command": [name, *args],
    }
    if not resolved:
        result["exitCode"] = None
        result["detail"] = "not found"
        return result
    proc = subprocess.run([str(resolved), *args], cwd=ROOT, text=True, capture_output=True, timeout=30, env=environment_with_tools(root=ROOT))
    result["exitCode"] = proc.returncode
    result["detail"] = (proc.stdout or proc.stderr).splitlines()[:3]
    return result


def main() -> None:
    checks = [
        probe_tool("python3", ["--version"]),
        probe_tool("git", ["--version"]),
        probe_tool("dotnet", ["--version"]),
        probe_tool("az", ["--version"]),
        probe_tool("gh", ["--version"]),
        probe_tool("bicep", ["--version"]),
        probe_tool("cmake", ["--version"]),
    ]
    payload = {
        "generatedUtc": datetime.now(timezone.utc).isoformat(),
        "effectivePathAdditions": path_addition_strings(root=ROOT, existing_only=True),
        "resolvedTools": resolved_tools(TOOLS, root=ROOT),
        "checks": checks,
    }
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n")

    lines = [
        "# HELIOS Full Stack Readiness",
        "",
        f"Generated: `{payload['generatedUtc']}`",
        "",
        "## Effective PATH additions",
        "",
        *[f"- `{path}`" for path in payload["effectivePathAdditions"]],
        "",
        "| Tool | Available | Resolved path | Detail |",
        "| --- | --- | --- | --- |",
    ]
    for check in checks:
        detail = "; ".join(check.get("detail", [])) if isinstance(check.get("detail"), list) else check.get("detail", "")
        lines.append(f"| `{check['name']}` | {'✅' if check['available'] else '⚠️'} | `{check.get('resolvedPath') or ''}` | `{detail}` |")
    MD.write_text("\n".join(lines) + "\n")
    print(f"Wrote {OUT.relative_to(ROOT)}")
    print(f"Wrote {MD.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
