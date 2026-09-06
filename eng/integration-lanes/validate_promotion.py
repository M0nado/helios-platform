#!/usr/bin/env python3
"""Require architecture evidence when stable dependency inputs change."""
from __future__ import annotations

import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
DEPENDENCY_INPUTS = ("global.json", "Directory.Packages.props", "stable-lane.json", "package-lock.json", "packages.lock.json", "pyproject.toml", "uv.lock")
ADR_MARKERS = ("**Status:** Accepted", "## Context", "## Decision", "## Promotion gate")

def git(*args: str) -> str:
    return subprocess.check_output(["git", *args], cwd=ROOT, text=True)

def main() -> int:
    if len(sys.argv) != 2:
        raise SystemExit("usage: validate_promotion.py <base-ref>")
    base = sys.argv[1]
    changed = [line for line in git("diff", "--name-only", f"{base}...HEAD").splitlines() if line]
    dependency_changes = [path for path in changed if path.endswith(DEPENDENCY_INPUTS)]
    if not dependency_changes:
        print("No stable dependency promotion detected.")
        return 0
    adrs = [path for path in changed if path.startswith("docs/architecture/decisions/ADR-") and path.endswith(".md")]
    accepted = []
    for path in adrs:
        content = (ROOT / path).read_text()
        if all(marker in content for marker in ADR_MARKERS):
            accepted.append(path)
    if not accepted:
        print(f"ERROR: stable dependency inputs changed without a new accepted promotion ADR: {dependency_changes}", file=sys.stderr)
        return 1
    print(f"Promotion evidence accepted from: {', '.join(accepted)}")
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
