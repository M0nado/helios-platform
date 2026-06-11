#!/usr/bin/env python3
"""Prepare HELIOS multi-repository consolidation safely.

This command intentionally defaults to dry-run behavior. It verifies that the
repositories declared in MERGE_SOURCE_MANIFEST.yaml are reachable, records their
HEAD SHAs, and can optionally add/fetch remotes or stage submodules only after
reachability checks pass.
"""
from __future__ import annotations

import argparse
import json
import os
import re
import subprocess
import sys
from dataclasses import dataclass, asdict
from datetime import datetime, timezone
from pathlib import Path
from typing import Iterable

ROOT = Path(__file__).resolve().parents[2]
MANIFEST = ROOT / "MERGE_SOURCE_MANIFEST.yaml"
DEFAULT_REPORT = ROOT / "artifacts" / "consolidation" / "source-readiness.json"


@dataclass
class Source:
    id: str
    kind: str
    remote: str
    url: str
    branch: str
    order: int
    mode: str
    submodule_path: str | None = None


def run(cmd: list[str], *, check: bool = False) -> subprocess.CompletedProcess[str]:
    return subprocess.run(cmd, cwd=ROOT, text=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=check)


def parse_manifest(path: Path) -> list[Source]:
    if not path.exists():
        raise FileNotFoundError(path)

    sources: list[dict[str, str]] = []
    current: dict[str, str] | None = None
    in_sources = False

    for raw in path.read_text(encoding="utf-8").splitlines():
        line = raw.rstrip()
        if line.startswith("merge_sources:"):
            in_sources = True
            continue
        if not in_sources:
            continue
        if re.match(r"^  - id:", line):
            if current:
                sources.append(current)
            current = {"id": line.split(":", 1)[1].strip().strip('"')}
            continue
        if current is None:
            continue
        match = re.match(r"^    ([A-Za-z0-9_]+):\s*(.*)$", line)
        if match:
            key, value = match.groups()
            current[key] = value.strip().strip('"')
    if current:
        sources.append(current)

    parsed: list[Source] = []
    for item in sources:
        parsed.append(
            Source(
                id=item["id"],
                kind=item.get("kind", "external_repository"),
                remote=item.get("git_remote_name", item["id"]),
                url=item.get("git_remote_url_or_local_path", ""),
                branch=item.get("source_branch", item.get("target_branch", "main")),
                order=int(item.get("merge_order", "999")),
                mode=item.get("consolidation_mode", "submodule"),
                submodule_path=item.get("submodule_path"),
            )
        )
    return sorted(parsed, key=lambda s: s.order)


def remote_sha(source: Source) -> tuple[bool, str | None, str | None]:
    if source.kind == "current_repository":
        proc = run(["git", "rev-parse", source.branch])
        if proc.returncode == 0:
            return True, proc.stdout.strip() or None, None
        head = run(["git", "rev-parse", "HEAD"])
        return head.returncode == 0, head.stdout.strip() or None, proc.stderr.strip() or None
    proc = run(["git", "ls-remote", "--heads", source.url, source.branch])
    if proc.returncode != 0:
        return False, None, proc.stderr.strip()
    first = proc.stdout.strip().splitlines()[0] if proc.stdout.strip() else ""
    if not first:
        return False, None, f"No branch named {source.branch!r} found"
    return True, first.split()[0], None


def configured_remotes() -> dict[str, str]:
    proc = run(["git", "remote", "-v"])
    remotes: dict[str, str] = {}
    for line in proc.stdout.splitlines():
        parts = line.split()
        if len(parts) >= 2 and parts[0] not in remotes:
            remotes[parts[0]] = parts[1]
    return remotes


def add_or_update_remotes(sources: Iterable[Source]) -> list[str]:
    actions: list[str] = []
    remotes = configured_remotes()
    for source in sources:
        if source.kind == "current_repository":
            continue
        if source.remote in remotes:
            if remotes[source.remote] != source.url:
                run(["git", "remote", "set-url", source.remote, source.url], check=True)
                actions.append(f"updated remote {source.remote}")
            else:
                actions.append(f"remote {source.remote} already configured")
        else:
            run(["git", "remote", "add", source.remote, source.url], check=True)
            actions.append(f"added remote {source.remote}")
        run(["git", "fetch", "--prune", source.remote, source.branch], check=True)
        actions.append(f"fetched {source.remote}/{source.branch}")
    return actions


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--manifest", type=Path, default=MANIFEST)
    parser.add_argument("--report", type=Path, default=DEFAULT_REPORT)
    parser.add_argument("--apply-remotes", action="store_true", help="Add/update/fetch git remotes after reachability succeeds.")
    parser.add_argument("--fail-on-unreachable", action="store_true", help="Return non-zero when any source is unreachable.")
    args = parser.parse_args()

    sources = parse_manifest(args.manifest)
    checks = []
    unreachable = []
    for source in sources:
        ok, sha, error = remote_sha(source)
        record = asdict(source) | {"reachable": ok, "sha": sha, "error": error}
        checks.append(record)
        if not ok:
            unreachable.append(source.id)

    actions: list[str] = []
    if args.apply_remotes:
        if unreachable:
            print("Refusing to add/fetch remotes until every source is reachable:", ", ".join(unreachable), file=sys.stderr)
            return 2
        actions = add_or_update_remotes(sources)

    report = {
        "generated_utc": datetime.now(timezone.utc).isoformat(),
        "manifest": str(args.manifest.relative_to(ROOT) if args.manifest.is_relative_to(ROOT) else args.manifest),
        "safe_to_merge": not unreachable,
        "unreachable_sources": unreachable,
        "actions": actions,
        "sources": checks,
    }
    args.report.parent.mkdir(parents=True, exist_ok=True)
    args.report.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")

    print(json.dumps(report, indent=2))
    if unreachable and args.fail_on_unreachable:
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
