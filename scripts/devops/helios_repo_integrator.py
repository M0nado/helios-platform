#!/usr/bin/env python3
"""Discover HELIOS repositories and create safe branch integration plans.

The tool is intentionally conservative: inventory and plan modes never change a repository.
Merge mode requires explicit branch names and refuses to run when the repository has
uncommitted tracked changes.
"""

from __future__ import annotations

import argparse
import json
import subprocess
from dataclasses import dataclass, asdict
from pathlib import Path
from typing import Iterable


@dataclass(frozen=True)
class RepositoryInventory:
    path: str
    current_branch: str
    remotes: list[str]
    local_branches: list[str]
    remote_branches: list[str]
    tracked_status: list[str]
    untracked_status: list[str]


def run_git(repo: Path, *args: str, check: bool = True) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        ["git", "-C", str(repo), *args],
        check=check,
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
    )


def discover_repositories(root: Path) -> list[Path]:
    repositories: list[Path] = []
    if (root / ".git").exists():
        repositories.append(root)

    for git_dir in root.rglob(".git"):
        repo = git_dir.parent
        if repo not in repositories:
            repositories.append(repo)

    return sorted(repositories)


def split_lines(output: str) -> list[str]:
    return [line.strip() for line in output.splitlines() if line.strip()]


def inventory_repository(repo: Path) -> RepositoryInventory:
    branch_result = run_git(repo, "branch", "--show-current", check=False)
    current_branch = branch_result.stdout.strip() or "DETACHED"

    remotes = split_lines(run_git(repo, "remote").stdout)
    local_branches = [line.removeprefix("* ").strip() for line in split_lines(run_git(repo, "branch", "--no-color").stdout)]
    remote_branches = split_lines(run_git(repo, "branch", "--remotes", "--no-color").stdout)

    tracked_status: list[str] = []
    untracked_status: list[str] = []
    for line in split_lines(run_git(repo, "status", "--short").stdout):
        if line.startswith("??"):
            untracked_status.append(line)
        else:
            tracked_status.append(line)

    return RepositoryInventory(
        path=str(repo),
        current_branch=current_branch,
        remotes=remotes,
        local_branches=local_branches,
        remote_branches=remote_branches,
        tracked_status=tracked_status,
        untracked_status=untracked_status,
    )


def create_plan(inventory: RepositoryInventory) -> dict[str, object]:
    candidate_branches = [
        branch for branch in inventory.remote_branches
        if not branch.endswith("/HEAD")
    ] or inventory.local_branches

    return {
        "repository": inventory.path,
        "base_branch": inventory.current_branch,
        "can_merge_now": bool(candidate_branches) and not inventory.tracked_status,
        "blocked_reasons": (["tracked working tree changes are present"] if inventory.tracked_status else [])
        + (["no local or remote branches discovered"] if not candidate_branches else []),
        "merge_order": candidate_branches,
        "validation_gates": [
            "git status --short",
            "dotnet build (C#/WinUI/F# projects when present)",
            "dotnet test (test projects when present)",
            "native C++ build and benchmark commands when present",
            "python -m pytest (Python tests when present)",
        ],
    }


def ensure_clean_tracked_tree(repo: Path) -> None:
    tracked = [line for line in split_lines(run_git(repo, "status", "--short").stdout) if not line.startswith("??")]
    if tracked:
        raise SystemExit(f"Refusing to merge in {repo}: tracked changes are present: {tracked}")


def merge_branches(repo: Path, branches: Iterable[str]) -> None:
    ensure_clean_tracked_tree(repo)
    for branch in branches:
        print(f"[HELIOS] Merging {branch} into {repo}")
        run_git(repo, "merge", "--no-ff", branch)
        run_git(repo, "status", "--short")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--workspace-root", default=".", help="Workspace root to scan")
    parser.add_argument("--mode", choices=("inventory", "plan", "merge"), default="inventory")
    parser.add_argument("--repo", help="Repository path for merge mode")
    parser.add_argument("--branches", nargs="*", default=[], help="Explicit branches to merge in merge mode")
    parser.add_argument("--output", choices=("json", "text"), default="text")
    args = parser.parse_args()

    root = Path(args.workspace_root).resolve()
    repositories = discover_repositories(root)

    if args.mode == "merge":
        if not args.repo or not args.branches:
            parser.error("merge mode requires --repo and at least one --branches value")
        merge_branches(Path(args.repo).resolve(), args.branches)
        return 0

    inventories = [inventory_repository(repo) for repo in repositories]
    payload: object = inventories if args.mode == "inventory" else [create_plan(item) for item in inventories]

    if args.output == "json":
        print(json.dumps(payload, default=asdict, indent=2))
    else:
        for item in payload:  # type: ignore[union-attr]
            print(json.dumps(asdict(item) if hasattr(item, "__dataclass_fields__") else item, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
