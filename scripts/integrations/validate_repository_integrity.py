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
SUPPORTED_INTEGRATION_MODES = {"canonical", "pinned-submodule", "contract-only"}


def github_name(url: str) -> str:
    path = urlparse(url).path.strip("/")
    if path.endswith(".git"):
        path = path[:-4]
    if urlparse(url).hostname != "github.com" or path.count("/") != 1:
        raise ValueError(f"unsupported canonical GitHub URL: {url}")
    return path.casefold()


def expected_submodule_path(name: str) -> str:
    _owner, repository = name.split("/", 1)
    return f"modules/{repository}"


def gitlinks(root: Path) -> dict[str, str]:
    result = subprocess.run(
        ["git", "ls-files", "--stage"],
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

    integration_modes = [entry.get("integrationMode", "") for entry in entries]
    for mode in sorted(set(integration_modes)):
        if mode not in SUPPORTED_INTEGRATION_MODES:
            errors.append(f"unsupported integrationMode: {mode}")

    canonical_platform = registry.get("canonicalPlatform", "").casefold()
    canonical_entries = [
        entry["name"].casefold()
        for entry in entries
        if entry.get("integrationMode") == "canonical"
    ]
    if len(canonical_entries) != 1:
        errors.append("repository registry must contain exactly one canonical integrationMode")
    elif canonical_entries[0] != canonical_platform:
        errors.append("canonical integrationMode entry does not match canonicalPlatform")

    expected = {
        entry["name"].casefold()
        for entry in entries
        if entry.get("integrationMode") == "pinned-submodule"
    }
    parser = configparser.ConfigParser()
    parser.read(root / ".gitmodules")
    declared: dict[str, str] = {}
    for section in parser.sections():
        if not section.startswith('submodule "') or not section.endswith('"'):
            errors.append(f"invalid .gitmodules section: {section}")
            continue
        path = parser.get(section, "path", fallback="").strip()
        url = parser.get(section, "url", fallback="").strip()
        if not path or not url:
            errors.append(f"incomplete submodule declaration: {section}")
            continue
        try:
            name = github_name(url)
        except ValueError as exc:
            errors.append(str(exc))
            continue
        if path in declared:
            errors.append(f"duplicate submodule path: {path}")
        declared[path] = name

        normalized_path = path.replace("\\", "/").strip("/").casefold()
        expected_path = expected_submodule_path(name).casefold()
        if normalized_path != expected_path:
            errors.append(
                f"submodule path for {name} must be {expected_submodule_path(name)} (found {path})"
            )

    declared_names_list = list(declared.values())
    duplicate_declared_names = sorted(
        {
            name
            for name in declared_names_list
            if declared_names_list.count(name) > 1
        }
    )
    for name in duplicate_declared_names:
        errors.append(f"duplicate .gitmodules repository declaration: {name}")

    declared_name_set = set(declared_names_list)
    for name in sorted(expected - declared_name_set):
        errors.append(f"pinned-submodule missing from .gitmodules: {name}")
    for name in sorted(declared_name_set - expected):
        errors.append(f"unapproved .gitmodules repository: {name}")

    links = gitlinks(root)
    for path, name in sorted(declared.items()):
        if name in expected and path not in links:
            errors.append(f"declared submodule has no 160000 gitlink: {path}")
    for path in sorted(links):
        if path not in declared:
            errors.append(f"orphan gitlink not declared in .gitmodules: {path}")
    for path in sorted(set(declared) & set(links)):
        sha = links[path]
        if len(sha) != 40 or any(char not in "0123456789abcdef" for char in sha):
            errors.append(f"gitlink has invalid commit SHA at {path}: {sha}")

    return errors


if __name__ == "__main__":
    failures = validate()
    if failures:
        print("Repository integrity validation failed:", file=sys.stderr)
        for failure in failures:
            print(f"- {failure}", file=sys.stderr)
        raise SystemExit(1)
    print("Repository registry, submodule declarations, and gitlinks are consistent.")
