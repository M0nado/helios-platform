#!/usr/bin/env python3
from __future__ import annotations

import argparse
import fnmatch
import json
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

ROOT = Path(__file__).resolve().parents[2]
CFG = ROOT / "config/build-graph.json"
OUT = ROOT / "reports/build-graph/latest.json"
MD = ROOT / "reports/build-graph/latest.md"
LEGACY_OUT = ROOT / "reports/build-graph/build-graph.json"
LEGACY_MD = ROOT / "reports/build-graph/build-graph.md"


def git_changed_files() -> set[str]:
    files: set[str] = set()
    for cmd in (["git", "diff", "--name-only"], ["git", "diff", "--name-only", "--cached"]):
        proc = subprocess.run(cmd, cwd=ROOT, text=True, capture_output=True)
        if proc.returncode == 0:
            files.update(line.strip() for line in proc.stdout.splitlines() if line.strip())
    proc = subprocess.run(["git", "ls-files", "--others", "--exclude-standard"], cwd=ROOT, text=True, capture_output=True)
    if proc.returncode == 0:
        files.update(line.strip() for line in proc.stdout.splitlines() if line.strip())
    return files


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


def select_nodes(nodes: list[dict[str, Any]], args: argparse.Namespace) -> list[dict[str, Any]]:
    if args.node:
        wanted = set(args.node)
        selected = [node for node in nodes if node["id"] in wanted]
        missing = wanted.difference(node["id"] for node in selected)
        if missing:
            raise SystemExit(f"No matching build graph node(s): {', '.join(sorted(missing))}")
        return selected
    if args.changed_only:
        changed = git_changed_files()
        return [node for node in nodes if matches_changed(node, changed)]
    if args.all or args.command in {"run", "graph"}:
        return nodes
    return []


def run_node(node: dict[str, Any], timeout: int) -> dict[str, Any]:
    started = datetime.now(timezone.utc)
    proc = subprocess.run(node["command"], cwd=ROOT, text=True, capture_output=True, shell=True, timeout=timeout)
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
    }
    text = json.dumps(payload, indent=2, sort_keys=True) + "\n"
    OUT.write_text(text, encoding="utf-8")
    LEGACY_OUT.write_text(text, encoding="utf-8")

    lines = ["# HELIOS Build Graph", "", f"Generated: `{payload['generatedUtc']}`", "", "## Nodes", "", "| Node | Title | Command |", "| --- | --- | --- |"]
    lines.extend(f"| `{node['id']}` | {node.get('title', node['id'])} | `{node['command']}` |" for node in nodes)
    if selected:
        lines.extend(["", "## Selected", "", ", ".join(f"`{node['id']}`" for node in selected)])
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
    if args.command == "list":
        for node in nodes:
            print(f"{node['id']}: {node.get('title', node['id'])}")
    return 1 if any(not result.get("ok", False) for result in results) else 0


if __name__ == "__main__":
    sys.exit(main())
