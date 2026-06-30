#!/usr/bin/env python3
"""Inventory and optionally combine HELIOS/Hermes integration branches.

The default mode is a safe dry run: it discovers local/remote branches, sibling
repositories, and stack-specific source lanes, then writes a Markdown report.
Use --execute to perform ordered --no-ff merges for discovered branch refs.
"""

from __future__ import annotations

import argparse
import json
import subprocess
from dataclasses import dataclass, asdict
from pathlib import Path
from typing import Iterable

DEFAULT_BRANCH_PATTERNS = (
    "helios-control",
    "hermes-fleet-production",
    "helios",
    "hermes",
    "he",
)

LANE_EXTENSIONS = {
    "csharp_winui_frontend": (".cs", ".csproj", ".xaml"),
    "cpp_performance_security_backend": (".cpp", ".cc", ".cxx", ".c", ".h", ".hpp", ".vcxproj"),
    "fsharp_math_predictions_analytics": (".fs", ".fsi", ".fsx", ".fsproj"),
    "python_aihub_hermes_xcore_automation": (".py",),
    "azure_deployment": (".bicep", ".yml", ".yaml", ".ps1", ".sh"),
}

SKIP_DIRS = {".git", "bin", "obj", "node_modules", ".venv", "packages"}


@dataclass(frozen=True)
class BranchRef:
    name: str
    sha: str
    source: str
    priority: int


@dataclass(frozen=True)
class SiblingRepo:
    path: str
    name: str
    head: str | None


def run(cmd: list[str], cwd: Path, check: bool = True) -> str:
    completed = subprocess.run(cmd, cwd=cwd, text=True, capture_output=True, check=False)
    if check and completed.returncode != 0:
        raise RuntimeError(f"command failed ({completed.returncode}): {' '.join(cmd)}\n{completed.stderr}")
    return completed.stdout.strip()


def ensure_remote(repo: Path, remote_url: str | None, fetch: bool) -> None:
    if remote_url:
        existing = run(["git", "remote", "get-url", "origin"], repo, check=False)
        if existing:
            run(["git", "remote", "set-url", "origin", remote_url], repo)
        else:
            run(["git", "remote", "add", "origin", remote_url], repo)
    if fetch:
        run(["git", "fetch", "--all", "--tags", "--prune"], repo)


def git_refs(repo: Path, patterns: tuple[str, ...]) -> list[BranchRef]:
    output = run([
        "git",
        "for-each-ref",
        "--format=%(refname:short)|%(objectname:short)",
        "refs/heads",
        "refs/remotes",
    ], repo)
    refs: list[BranchRef] = []
    for line in output.splitlines():
        if not line or "|" not in line:
            continue
        name, sha = line.split("|", 1)
        lowered = name.lower()
        if lowered.endswith("/head"):
            continue
        priority = branch_priority(lowered, patterns)
        if priority is None:
            continue
        source = "remote" if name.startswith("origin/") or "/" in name else "local"
        refs.append(BranchRef(name=name, sha=sha, source=source, priority=priority))
    return sorted(refs, key=lambda ref: (ref.priority, ref.name))


def branch_priority(name: str, patterns: tuple[str, ...] = DEFAULT_BRANCH_PATTERNS) -> int | None:
    for index, pattern in enumerate(patterns):
        lowered = pattern.lower()
        if lowered in {"he", "he-*", "he/*"}:
            short_name = name.rsplit("/", 1)[-1]
            if short_name.startswith("he"):
                return 50 + index
            continue
        if lowered in name:
            return (index + 1) * 10
    return None


def find_sibling_repos(search_roots: Iterable[Path], current_repo: Path) -> list[SiblingRepo]:
    siblings: list[SiblingRepo] = []
    wanted = ("helios-control", "hermes-fleet-production", "helios", "hermes")
    current_git = (current_repo / ".git").resolve()
    for root in search_roots:
        if not root.exists():
            continue
        for git_dir in root.rglob(".git"):
            repo = git_dir.parent
            relative_parts = repo.relative_to(root).parts if repo.is_relative_to(root) else repo.parts
            if any(part in SKIP_DIRS for part in relative_parts):
                continue
            if git_dir.resolve() == current_git:
                continue
            name = repo.name.lower()
            if not any(token in name for token in wanted):
                continue
            head = run(["git", "rev-parse", "--short", "HEAD"], repo, check=False) or None
            siblings.append(SiblingRepo(path=str(repo), name=repo.name, head=head))
    return sorted(siblings, key=lambda item: item.path)


def lane_inventory(repo: Path) -> dict[str, dict[str, object]]:
    inventory: dict[str, dict[str, object]] = {
        lane: {"count": 0, "examples": []} for lane in LANE_EXTENSIONS
    }
    for path in repo.rglob("*"):
        if not path.is_file():
            continue
        relative_parts = path.relative_to(repo).parts
        if any(part in SKIP_DIRS for part in relative_parts):
            continue
        suffix = path.suffix.lower()
        for lane, extensions in LANE_EXTENSIONS.items():
            if suffix in extensions:
                entry = inventory[lane]
                entry["count"] = int(entry["count"]) + 1
                examples = entry["examples"]
                if isinstance(examples, list) and len(examples) < 12:
                    examples.append(str(path.relative_to(repo)))
    return inventory


def working_tree_clean(repo: Path) -> bool:
    return run(["git", "status", "--porcelain"], repo, check=False) == ""


def merge_refs(repo: Path, refs: list[BranchRef], execute: bool) -> list[str]:
    commands: list[str] = []
    current = run(["git", "branch", "--show-current"], repo, check=False) or "HEAD"
    if execute and not working_tree_clean(repo):
        raise RuntimeError("refusing to merge with a dirty working tree")
    for ref in refs:
        if ref.name == current:
            continue
        cmd = ["git", "merge", "--no-ff", "--no-edit", ref.name]
        commands.append(" ".join(cmd))
        if execute:
            run(cmd, repo)
    return commands


def write_report(path: Path, refs: list[BranchRef], siblings: list[SiblingRepo], lanes: dict[str, dict[str, object]], merge_commands: list[str], executed: bool) -> None:
    lines = [
        "# HELIOS/Hermes Integration Inventory",
        "",
        f"Merge mode: {'executed' if executed else 'dry run'}",
        "",
        "## Discovered branch refs",
    ]
    if refs:
        lines.extend(["", "| Priority | Source | Ref | SHA |", "|---:|---|---|---|"])
        lines.extend(f"| {ref.priority} | {ref.source} | `{ref.name}` | `{ref.sha}` |" for ref in refs)
    else:
        lines.append("\nNo local or remote HELIOS/Hermes branch refs were available in this checkout.")

    lines.extend(["", "## Sibling repositories"])
    if siblings:
        lines.extend(["", "| Repository | HEAD | Path |", "|---|---|---|"])
        lines.extend(f"| {repo.name} | `{repo.head or 'unknown'}` | `{repo.path}` |" for repo in siblings)
    else:
        lines.append("\nNo `helios-control` or `hermes-fleet-production` sibling repositories were found under the scanned roots.")

    lines.extend(["", "## Stack lane inventory", "", "| Lane | Files | Examples |", "|---|---:|---|"])
    for lane, data in lanes.items():
        examples = data.get("examples", [])
        example_text = ", ".join(f"`{example}`" for example in examples) if isinstance(examples, list) else ""
        lines.append(f"| {lane} | {data.get('count', 0)} | {example_text} |")

    lines.extend(["", "## Planned merge commands"])
    if merge_commands:
        lines.extend(["", "```bash", *merge_commands, "```"])
    else:
        lines.append("\nNo merge commands were generated because no matching branch refs were discovered.")

    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description="Inventory and optionally merge HELIOS/Hermes branches.")
    parser.add_argument("--repo", type=Path, default=Path.cwd(), help="Repository root to inspect.")
    parser.add_argument("--search-root", action="append", type=Path, default=[], help="Root to scan for sibling repos; may be repeated.")
    parser.add_argument("--remote-url", default=None, help="Optional origin URL to configure before fetching, for example https://github.com/ORG/REPO.git.")
    parser.add_argument("--fetch", action="store_true", help="Fetch all remotes/tags before inventorying refs.")
    parser.add_argument("--branch-pattern", action="append", default=[], help="Branch substring/prefix to include; repeat for multiple patterns. Defaults to HELIOS/Hermes/HE patterns.")
    parser.add_argument("--execute", action="store_true", help="Execute merge commands instead of writing a dry-run plan.")
    parser.add_argument("--report", type=Path, default=Path("build/integration/helios-hermes-inventory.md"), help="Markdown report path.")
    parser.add_argument("--json", type=Path, default=None, help="Optional JSON report path.")
    args = parser.parse_args()

    repo = args.repo.resolve()
    patterns = tuple(args.branch_pattern) if args.branch_pattern else DEFAULT_BRANCH_PATTERNS
    ensure_remote(repo, args.remote_url, args.fetch)
    search_roots = args.search_root or [repo.parent, repo.parent.parent]
    refs = git_refs(repo, patterns)
    siblings = find_sibling_repos([root.resolve() for root in search_roots], repo)
    lanes = lane_inventory(repo)
    merge_commands = merge_refs(repo, refs, args.execute)
    write_report(args.report, refs, siblings, lanes, merge_commands, args.execute)

    if args.json:
        args.json.parent.mkdir(parents=True, exist_ok=True)
        args.json.write_text(json.dumps({
            "branches": [asdict(ref) for ref in refs],
            "siblings": [asdict(repo) for repo in siblings],
            "lanes": lanes,
            "mergeCommands": merge_commands,
            "executed": args.execute,
        }, indent=2), encoding="utf-8")

    print(f"wrote {args.report}")
    if args.json:
        print(f"wrote {args.json}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
