#!/usr/bin/env python3
"""Validate the canonical repository registry, .gitmodules, and gitlinks."""

from __future__ import annotations

import configparser
import json
import subprocess
import sys
from pathlib import Path
from urllib.parse import urlparse


ROOT = Path(__file__).resolve().parents[2]


def github_name(url: str) -> str:
    path = urlparse(url).path.strip("/")
    if path.endswith(".git"):
        path = path[:-4]
    if urlparse(url).hostname != "github.com" or path.count("/") != 1:
        raise ValueError(f"unsupported canonical GitHub URL: {url}")
    return path.casefold()


def gitlinks(root: Path) -> dict[str, str]:
    result = subprocess.run(
        ["git", "ls-files", "--stage", "modules/"],
        cwd=root,
        check=True,
        capture_output=True,
        text=True,
    )
    links: dict[str, str] = {}
    for line in result.stdout.splitlines():
        metadata, path = line.split("\t", 1)
        mode, sha, _stage = metadata.split()
        if mode == "160000":
            links[path] = sha
    return links


def validate(root: Path = ROOT) -> list[str]:
    errors: list[str] = []
    registry = json.loads((root / "config/integrations/repositories.json").read_text())
    entries = registry.get("repositories", [])
    names = [entry.get("name", "").casefold() for entry in entries]
    if len(names) != len(set(names)):
        errors.append("repository registry contains duplicate names")

    allowed_modes = {"canonical", "contract-only", "pinned-submodule"}
    for entry in entries:
        if entry.get("integrationMode") not in allowed_modes:
            errors.append(f"{entry.get('name')}: invalid or missing integrationMode")

    expected = {
        entry["name"].casefold()
        for entry in entries
        if entry.get("integrationMode") == "pinned-submodule"
    }
    parser = configparser.ConfigParser()
    parser.read(root / ".gitmodules")
    declared: dict[str, str] = {}
    for section in parser.sections():
        path = parser[section].get("path", "")
        url = parser[section].get("url", "")
        try:
            name = github_name(url)
        except ValueError as exc:
            errors.append(str(exc))
            continue
        if path in declared:
            errors.append(f"duplicate submodule path: {path}")
        declared[path] = name

    declared_names = set(declared.values())
    for name in sorted(expected - declared_names):
        errors.append(f"pinned-submodule missing from .gitmodules: {name}")
    for name in sorted(declared_names - expected):
        errors.append(f"unapproved .gitmodules repository: {name}")

    links = gitlinks(root)
    for path in sorted(set(declared) - set(links)):
        errors.append(f"declared submodule has no 160000 gitlink: {path}")
    for path in sorted(set(links) - set(declared)):
        errors.append(f"orphan 160000 gitlink: {path}")
    return errors


if __name__ == "__main__":
    failures = validate()
    if failures:
        print("Repository integrity validation failed:", file=sys.stderr)
        for failure in failures:
            print(f"- {failure}", file=sys.stderr)
        raise SystemExit(1)
    print("Repository registry, submodule declarations, and gitlinks are consistent.")
