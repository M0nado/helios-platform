#!/usr/bin/env python3
"""Dry-run and optionally prepare HELIOS external repository consolidation."""
from __future__ import annotations

import argparse
import json
import os
import subprocess
import sys
from dataclasses import dataclass, asdict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
MANIFEST = ROOT / "MERGE_SOURCE_MANIFEST.yaml"

@dataclass
class Source:
    id: str
    kind: str = ""
    git_remote_name: str = ""
    git_remote_url_or_local_path: str = ""
    source_branch: str = "main"
    target_branch: str = "main"
    submodule_path: str = ""
    consolidation_mode: str = ""
    merge_order: int = 999


def parse_manifest() -> list[Source]:
    sources: list[Source] = []
    current: dict[str, str] | None = None
    in_sources = False

    for raw in MANIFEST.read_text(encoding="utf-8").splitlines():
        line = raw.rstrip()
        if line.startswith("merge_sources:"):
            in_sources = True
            continue
        if not in_sources:
            continue
        stripped = line.strip()
        if stripped.startswith("- id:"):
            if current:
                sources.append(Source(**{k: v for k, v in current.items() if k in Source.__dataclass_fields__}))
            current = {"id": stripped.split(":", 1)[1].strip().strip('"\'')}
            continue
        if current is None or not stripped or stripped.startswith("#") or ":" not in stripped:
            continue
        key, value = stripped.split(":", 1)
        key = key.strip()
        if key in Source.__dataclass_fields__:
            value = value.strip().strip('"\'')
            if key == "merge_order":
                try:
                    current[key] = int(value)  # type: ignore[assignment]
                except ValueError:
                    current[key] = 999  # type: ignore[assignment]
            else:
                current[key] = value
    if current:
        sources.append(Source(**{k: v for k, v in current.items() if k in Source.__dataclass_fields__}))
    return sorted(sources, key=lambda source: source.merge_order)


def git(args: list[str], check: bool = False) -> subprocess.CompletedProcess[str]:
    env = os.environ.copy()
    env["GIT_TERMINAL_PROMPT"] = "0"
    return subprocess.run(["git", *args], cwd=ROOT, env=env, text=True, capture_output=True, check=check)


def remote_head(source: Source) -> tuple[bool, str, str]:
    if source.kind == "current_repository":
        result = git(["rev-parse", "HEAD"])
        return result.returncode == 0, result.stdout.strip(), result.stderr.strip()
    if not source.git_remote_url_or_local_path:
        return False, "", "missing remote url/path"
    result = git(["ls-remote", "--heads", source.git_remote_url_or_local_path, source.source_branch])
    if result.returncode != 0:
        return False, "", result.stderr.strip()
    parts = result.stdout.strip().split()
    if not parts:
        return False, "", f"branch {source.source_branch!r} not found"
    return True, parts[0], ""


def configure_remote(source: Source) -> None:
    if source.kind == "current_repository" or not source.git_remote_name:
        return
    existing = git(["remote", "get-url", source.git_remote_name])
    if existing.returncode == 0:
        if existing.stdout.strip() != source.git_remote_url_or_local_path:
            git(["remote", "set-url", source.git_remote_name, source.git_remote_url_or_local_path], check=True)
    else:
        git(["remote", "add", source.git_remote_name, source.git_remote_url_or_local_path], check=True)
    git(["fetch", "--prune", source.git_remote_name, source.source_branch], check=True)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--dry-run", action="store_true", help="validate only; do not change git remotes")
    parser.add_argument("--apply", action="store_true", help="add/fetch configured remotes after reachability passes")
    parser.add_argument("--allow-unreachable", action="store_true", help="write the report but return success when private/unreachable sources fail")
    args = parser.parse_args()
    if args.apply and args.dry_run:
        parser.error("--apply and --dry-run are mutually exclusive")

    sources = parse_manifest()
    report = []
    failures = []
    for source in sources:
        reachable, sha, error = remote_head(source)
        item = asdict(source)
        item.update({"reachable": reachable, "head_sha": sha, "error": error})
        report.append(item)
        status = "OK" if reachable else "FAIL"
        print(f"[{status}] {source.id} {source.git_remote_url_or_local_path or '(current)'} {source.source_branch} {sha or error}")
        if not reachable:
            failures.append(source.id)

    out_dir = ROOT / "artifacts" / "consolidation"
    out_dir.mkdir(parents=True, exist_ok=True)
    (out_dir / "consolidation-report.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")

    if failures:
        print("Consolidation pre-merge gate failed for: " + ", ".join(failures))
        if args.allow_unreachable:
            print("Continuing because --allow-unreachable was supplied.")
        else:
            return 2

    if args.apply:
        for source in sources:
            configure_remote(source)
        print("Configured and fetched all reachable external remotes.")
    else:
        print("Dry-run complete; no remotes, submodules, or merges were modified.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
