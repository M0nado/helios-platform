#!/usr/bin/env python3
from __future__ import annotations

import argparse
import fnmatch
import json
import os
import shlex
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT))
from scripts.common.tool_resolver import path_with_repo_tools
CFG = ROOT / "config/build-graph.json"
OUT = ROOT / "reports/build-graph/latest.json"
MD = ROOT / "reports/build-graph/latest.md"
LEGACY_OUT = ROOT / "reports/build-graph/build-graph.json"
LEGACY_MD = ROOT / "reports/build-graph/build-graph.md"
IGNORED_CHANGED_PATTERNS = ["reports/**", "status-site/**", ".build/**", ".tools/**", "**/bin/**", "**/obj/**", "**/__pycache__/**"]


def is_ignored_generated(path: str) -> bool:
    return any(fnmatch.fnmatch(path, pattern) for pattern in IGNORED_CHANGED_PATTERNS)


def git_changed_files(include_generated: bool = False) -> set[str]:
    files: set[str] = set()
    for cmd in (["git", "diff", "--name-only"], ["git", "diff", "--name-only", "--cached"]):
        proc = subprocess.run(cmd, cwd=ROOT, text=True, capture_output=True)
        if proc.returncode == 0:
            files.update(line.strip() for line in proc.stdout.splitlines() if line.strip())
    proc = subprocess.run(["git", "ls-files", "--others", "--exclude-standard"], cwd=ROOT, text=True, capture_output=True)
    if proc.returncode == 0:
        files.update(line.strip() for line in proc.stdout.splitlines() if line.strip())
    if include_generated:
        return files
    return {path for path in files if not is_ignored_generated(path)}


def load_nodes() -> list[dict[str, Any]]:
    return json.loads(CFG.read_text(encoding="utf-8"))["nodes"]


def matches_changed(node: dict[str, Any], changed: set[str]) -> bool:
    patterns = node.get("paths", [])
    if not patterns:
        return False
    for file in changed:
        for pattern in patterns:
            if fnmatch.fnmatch(file, pattern) or file.startswith(pattern.rstrip("/**")):
                return True
    return False


def expand_dependencies(nodes: list[dict[str, Any]], selected: list[dict[str, Any]]) -> list[dict[str, Any]]:
    by_id = {node["id"]: node for node in nodes}
    expanded: list[dict[str, Any]] = []
    visiting: set[str] = set()
    visited: set[str] = set()

    def visit(node_id: str) -> None:
        if node_id in visited:
            return
        if node_id in visiting:
            raise SystemExit(f"Build graph dependency cycle at {node_id}")
        if node_id not in by_id:
            raise SystemExit(f"Build graph dependency not found: {node_id}")
        visiting.add(node_id)
        node = by_id[node_id]
        for dependency in node.get("dependsOn", []):
            visit(dependency)
        visiting.remove(node_id)
        visited.add(node_id)
        expanded.append(node)

    for node in selected:
        visit(node["id"])
    return expanded


def select_nodes(nodes: list[dict[str, Any]], args: argparse.Namespace) -> list[dict[str, Any]]:
    if args.node:
        wanted = set(args.node)
        selected = [node for node in nodes if node["id"] in wanted]
        missing = wanted.difference(node["id"] for node in selected)
        if missing:
            raise SystemExit(f"No matching build graph node(s): {', '.join(sorted(missing))}")
        return expand_dependencies(nodes, selected)
    if args.changed_only:
        changed = git_changed_files(include_generated=args.include_generated)
        selected = [node for node in nodes if matches_changed(node, changed)]
        if args.include_readiness:
            selected.extend(node for node in nodes if node["id"] == "full-stack-readiness" and node not in selected)
        return expand_dependencies(nodes, selected)
    if args.all or args.command in {"run", "graph"}:
        return expand_dependencies(nodes, nodes)
    return []


def run_node(node: dict[str, Any], timeout: int) -> dict[str, Any]:
    started = datetime.now(timezone.utc)
    env = os.environ.copy()
    env["PATH"] = path_with_repo_tools(env.get("PATH"))
    proc = subprocess.run(node["command"], cwd=ROOT, text=True, capture_output=True, shell=True, timeout=timeout, env=env)
    ended = datetime.now(timezone.utc)
    output = (proc.stdout + proc.stderr).splitlines()
    return {
        "id": node["id"],
        "title": node.get("title", node["id"]),
        "command": node["command"],
        "exitCode": proc.returncode,
        "ok": proc.returncode == 0,
        "startedUtc": started.isoformat(),
        "endedUtc": ended.isoformat(),
        "tail": output[-20:],
    }


def next_fixes(results: list[dict[str, Any]]) -> list[dict[str, str]]:
    fixes: list[dict[str, str]] = []
    for result in results:
        if result.get("ok"):
            continue
        tail = "\n".join(str(line) for line in result.get("tail", []))
        command = result.get("command", "")
        suggestion = "Inspect the command output and rerun this node."
        if "dotnet" in command or "dotnet" in tail.lower():
            suggestion = "Run scripts/setup/bootstrap-local-tools.sh or install .NET 8 SDK."
        elif "az" in command or "bicep" in command or "azure" in tail.lower():
            suggestion = "Run scripts/setup/bootstrap-local-tools.sh, az login, and configure Azure resource group/secrets."
        elif "cmake" in command or "cmake" in tail.lower():
            suggestion = "Install CMake 3.22+ and rerun native-configure."
        fixes.append({"node": str(result.get("id")), "command": suggestion, "reason": tail[-500:]})
    return fixes


def write(nodes: list[dict[str, Any]], selected: list[dict[str, Any]], results: list[dict[str, Any]], dry_run: bool) -> None:
    OUT.parent.mkdir(parents=True, exist_ok=True)
    payload = {
        "generatedUtc": datetime.now(timezone.utc).isoformat(),
        "config": str(CFG.relative_to(ROOT)),
        "dryRun": dry_run,
        "selected": [node["id"] for node in selected],
        "nodes": nodes,
        "results": results,
        "ok": all(result.get("ok", False) for result in results) if results else True,
        "nextFixes": next_fixes(results),
    }
    text = json.dumps(payload, indent=2, sort_keys=True) + "\n"
    OUT.write_text(text, encoding="utf-8")
    LEGACY_OUT.write_text(text, encoding="utf-8")

    lines = ["# HELIOS Build Graph", "", f"Generated: `{payload['generatedUtc']}`", "", "## Nodes", "", "| Node | Title | Command |", "| --- | --- | --- |"]
    lines.extend(f"| `{node['id']}` | {node.get('title', node['id'])} | `{node['command']}` |" for node in nodes)
    if selected:
        lines.extend(["", "## Selected", "", ", ".join(f"`{node['id']}`" for node in selected)])
    fixes = payload["nextFixes"]
    if fixes:
        lines.extend(["", "## Fix first", "", "| Node | Suggested command | Reason |", "| --- | --- | --- |"])
        for fix in fixes:
            reason = str(fix["reason"]).replace("|", "\\|").replace("\n", "<br>")
            lines.append(f"| `{fix['node']}` | `{fix['command']}` | {reason} |")
    if results:
        lines.extend(["", "## Results", "", "| Node | Exit | Tail |", "| --- | --- | --- |"])
        for result in results:
            tail = "<br>".join(str(line).replace("|", "\\|") for line in result.get("tail", [])[-5:])
            lines.append(f"| `{result['id']}` | {result['exitCode']} | {tail} |")
    MD.write_text("\n".join(lines) + "\n", encoding="utf-8")
    LEGACY_MD.write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description="Run or list HELIOS build graph quality gates.")
    parser.add_argument("command", nargs="?", default="list", choices=["list", "run", "graph"])
    parser.add_argument("--node", action="append", help="Run one node by id; can be provided multiple times.")
    parser.add_argument("--all", action="store_true", help="Run all nodes.")
    parser.add_argument("--changed-only", action="store_true", help="Run nodes whose path patterns match current git changes.")
    parser.add_argument("--include-generated", action="store_true", help="Include generated reports/build outputs in changed-only matching.")
    parser.add_argument("--include-readiness", action="store_true", help="Always include full-stack-readiness in the selected node set.")
    parser.add_argument("--dry-run", action="store_true", help="Write selected nodes without executing commands.")
    parser.add_argument("--timeout", type=int, default=180, help="Timeout per node in seconds.")
    args = parser.parse_args()

    nodes = load_nodes()
    selected = select_nodes(nodes, args)
    results: list[dict[str, Any]] = []
    if args.command in {"run", "graph"} and selected and not args.dry_run:
        for node in selected:
            results.append(run_node(node, args.timeout))
    write(nodes, selected, results, args.dry_run)
    print(f"Wrote {OUT.relative_to(ROOT)}")
    print(f"Wrote {MD.relative_to(ROOT)}")
    if results:
        fixes = next_fixes(results)
        if fixes:
            print("Fix first:")
            for fix in fixes:
                print(f"- {fix['node']}: {fix['command']}")
    if args.command == "list":
        for node in nodes:
            print(f"{node['id']}: {node.get('title', node['id'])}")
    return 1 if any(not result.get("ok", False) for result in results) else 0


if __name__ == "__main__":
    sys.exit(main())
