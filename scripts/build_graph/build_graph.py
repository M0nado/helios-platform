#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import shlex
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SCRIPTS = ROOT / "scripts"
if str(SCRIPTS) not in sys.path:
    sys.path.insert(0, str(SCRIPTS))

from common.tool_resolver import environment_with_tools, path_addition_strings, resolve_tool, resolved_tools

CFG = ROOT / "config/build-graph.json"
OUT = ROOT / "reports/build-graph/build-graph.json"
MD = ROOT / "reports/build-graph/build-graph.md"
KNOWN_TOOLS = ("dotnet", "az", "gh", "bicep", "python3", "cmake", "git")


def load():
    return json.loads(CFG.read_text())["nodes"]


def command_tool(command: str) -> str | None:
    try:
        parts = shlex.split(command)
    except ValueError:
        return None
    return parts[0] if parts else None


def command_resolution(command: str) -> dict[str, str | None]:
    tool = command_tool(command)
    if not tool:
        return {"tool": None, "resolvedPath": None}
    path = resolve_tool(tool, root=ROOT)
    return {"tool": tool, "resolvedPath": str(path) if path else None}


def report_metadata() -> dict[str, object]:
    return {
        "effectivePathAdditions": path_addition_strings(root=ROOT, existing_only=True),
        "resolvedTools": resolved_tools(KNOWN_TOOLS, root=ROOT),
    }


def write(nodes, results=None):
    OUT.parent.mkdir(parents=True, exist_ok=True)
    metadata = report_metadata()
    payload = {
        "generatedUtc": datetime.now(timezone.utc).isoformat(),
        **metadata,
        "nodes": [{**n, "commandResolution": command_resolution(n.get("command", ""))} for n in nodes],
        "results": results or [],
    }
    OUT.write_text(json.dumps(payload, indent=2) + "\n")

    lines = [
        "# HELIOS Build Graph",
        "",
        "## Tool path additions",
        "",
        *[f"- `{path}`" for path in metadata["effectivePathAdditions"]],
        "",
        "| Node | Title | Command | Resolved tool |",
        "| --- | --- | --- | --- |",
    ]
    for n in payload["nodes"]:
        resolved = n["commandResolution"].get("resolvedPath") or "unresolved"
        lines.append(f"| `{n['id']}` | {n['title']} | `{n['command']}` | `{resolved}` |")
    if results:
        lines += ["", "## Results", "", "| Node | Exit | Dry run | Tail |", "| --- | --- | --- | --- |"]
        for r in results:
            lines.append(f"| `{r['id']}` | {r['exitCode']} | {r.get('dryRun', False)} | `{str(r.get('tail', []))[:160]}` |")
    MD.write_text("\n".join(lines) + "\n")


def run_node(n, dry_run: bool = False):
    resolution = command_resolution(n["command"])
    if dry_run:
        return {
            "id": n["id"],
            "exitCode": 0,
            "dryRun": True,
            "command": n["command"],
            "commandResolution": resolution,
            "effectivePathAdditions": path_addition_strings(root=ROOT, existing_only=True),
            "tail": [f"DRY RUN: would execute {n['command']}"],
        }
    p = subprocess.run(
        n["command"],
        cwd=ROOT,
        text=True,
        capture_output=True,
        shell=True,
        timeout=180,
        env=environment_with_tools(root=ROOT),
    )
    return {
        "id": n["id"],
        "exitCode": p.returncode,
        "dryRun": False,
        "command": n["command"],
        "commandResolution": resolution,
        "effectivePathAdditions": path_addition_strings(root=ROOT, existing_only=True),
        "tail": (p.stdout + p.stderr).splitlines()[-10:],
    }


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("command", nargs="?", default="list", choices=["list", "run", "graph"])
    ap.add_argument("--node")
    ap.add_argument("--all", action="store_true")
    ap.add_argument("--dry-run", action="store_true")
    a = ap.parse_args()
    nodes = load()
    results = []
    if a.command == "run":
        selected = nodes if a.all else [n for n in nodes if n["id"] == a.node]
        if not selected:
            raise SystemExit("No matching node; use --node <id> or --all")
        for n in selected:
            results.append(run_node(n, dry_run=a.dry_run))
    write(nodes, results)
    print(f"Wrote {OUT.relative_to(ROOT)}")
    print(f"Wrote {MD.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
