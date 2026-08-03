#!/usr/bin/env python3
"""Fail-closed validation for the reviewed submodule approval manifest."""

from __future__ import annotations

import json
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
MANIFEST = ROOT / "config/integrations/approved-submodules.json"
SHA = re.compile(r"^[0-9a-f]{40}$")


def git(*args: str) -> str:
    return subprocess.run(
        ["git", *args], cwd=ROOT, check=True, text=True, capture_output=True
    ).stdout.strip()


def main() -> int:
    if not MANIFEST.exists():
        print(f"BLOCKED: missing reviewed approval manifest: {MANIFEST.relative_to(ROOT)}")
        return 2

    data = json.loads(MANIFEST.read_text(encoding="utf-8"))
    entries = data.get("submodules", [])
    if data.get("approved") is not True or not entries:
        print("BLOCKED: manifest is not approved or contains no submodules")
        return 2

    configured = {}
    raw = git("config", "-f", ".gitmodules", "--get-regexp", r"^submodule\..*\.path$")
    for line in raw.splitlines():
        key, path = line.split(maxsplit=1)
        name = key[len("submodule.") : -len(".path")]
        configured[path] = git("config", "-f", ".gitmodules", f"submodule.{name}.url")

    errors: list[str] = []
    approved_paths = {entry.get("path") for entry in entries}
    if approved_paths != set(configured):
        errors.append("approved paths do not exactly match .gitmodules")

    stages = {line.split()[3]: (line.split()[0], line.split()[1]) for line in git("ls-files", "--stage").splitlines()}
    for entry in entries:
        path, url, commit = entry.get("path"), entry.get("url"), entry.get("commit")
        if not SHA.fullmatch(commit or ""):
            errors.append(f"{path}: commit is not a full lowercase SHA")
            continue
        if configured.get(path) != url:
            errors.append(f"{path}: URL differs from .gitmodules")
        mode, indexed = stages.get(path, (None, None))
        if mode != "160000" or indexed != commit:
            errors.append(f"{path}: index is not mode 160000 at approved commit")
        module = ROOT / path
        if not module.is_dir() or not (module / ".git").exists():
            errors.append(f"{path}: submodule worktree is not initialized")
        else:
            actual = subprocess.run(
                ["git", "-C", str(module), "rev-parse", "HEAD"],
                check=True, text=True, capture_output=True,
            ).stdout.strip()
            if actual != commit:
                errors.append(f"{path}: checked-out commit differs from approval")

    if errors:
        print("FAIL: pinned-submodule integrity gate")
        print("\n".join(f"- {error}" for error in errors))
        return 1
    git("submodule", "foreach", "--recursive", "git fsck --full")
    print(f"PASS: {len(entries)} approved gitlinks and recursive object stores validated")
    return 0


if __name__ == "__main__":
    sys.exit(main())
