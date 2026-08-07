#!/usr/bin/env python3
"""Validate the canonical repository registry, .gitmodules declarations, and gitlinks."""

from __future__ import annotations

import configparser
import json
import subprocess
import sys
from pathlib import Path
from urllib.parse import urlparse


ROOT = Path(__file__).resolve().parents[2]
SUPPORTED_SCHEMA_VERSION = "1.0"
SUPPORTED_INTEGRATION_MODES = {"canonical", "pinned-submodule", "contract-only"}


def github_name(url: str) -> str:
    parsed = urlparse(url)
    path = parsed.path.strip("/")
    if path.endswith(".git"):
        path = path[:-4]
    if parsed.hostname != "github.com" or path.count("/") != 1:
        raise ValueError(f"unsupported canonical GitHub URL: {url}")
    return path.casefold()


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
        if "\t" not in line:
            continue
        metadata, path = line.split("\t", 1)
        mode, sha, _stage = metadata.split()
        if mode == "160000":
            links[path] = sha
    return links


def validate(root: Path = ROOT) -> list[str]:
    errors: list[str] = []
    repository_map = root / "config/integrations/repositories.json"
    gitmodules = root / ".gitmodules"

    try:
        registry = json.loads(repository_map.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        return [f"{repository_map}: cannot read valid JSON: {exc}"]

    schema_version = registry.get("schemaVersion")
    if schema_version != SUPPORTED_SCHEMA_VERSION:
        errors.append(
            f"{repository_map}: unsupported schemaVersion {schema_version!r}; expected "
            f"{SUPPORTED_SCHEMA_VERSION!r}"
        )

    entries = registry.get("repositories", [])
    if not isinstance(entries, list) or not entries:
        errors.append(f"{repository_map}: 'repositories' must be a non-empty array")
        entries = []

    names: list[str] = []
    canonical_entries: list[str] = []
    expected: set[str] = set()

    for index, entry in enumerate(entries):
        location = f"{repository_map}: repositories[{index}]"
        if not isinstance(entry, dict):
            errors.append(f"{location} must be an object")
            continue

        name = entry.get("name")
        integration_mode = entry.get("integrationMode")

        if not isinstance(name, str) or name.count("/") != 1:
            errors.append(f"{location}.name must use the 'owner/repository' format")
            continue

        normalized_name = name.casefold()
        names.append(normalized_name)

        if integration_mode not in SUPPORTED_INTEGRATION_MODES:
            errors.append(f"{location}.integrationMode unsupported: {integration_mode!r}")
            continue
        if integration_mode == "canonical":
            canonical_entries.append(normalized_name)
        elif integration_mode == "pinned-submodule":
            expected.add(normalized_name)

    if len(names) != len(set(names)):
        errors.append("repository registry contains duplicate names")

    canonical_platform = registry.get("canonicalPlatform", "")
    if not isinstance(canonical_platform, str):
        errors.append(f"{repository_map}: 'canonicalPlatform' must be a string")
    canonical_platform = canonical_platform.casefold()
    if len(canonical_entries) != 1:
        errors.append("repository registry must contain exactly one canonical integrationMode")
    elif canonical_entries[0] != canonical_platform:
        errors.append("canonical integrationMode entry does not match canonicalPlatform")

    parser = configparser.ConfigParser(interpolation=None)
    try:
        parser.read(gitmodules, encoding="utf-8")
    except (OSError, configparser.Error) as exc:
        return [*errors, f"{gitmodules}: cannot read valid Git configuration: {exc}"]

    if parser.defaults():
        errors.append(".gitmodules DEFAULT section is not allowed")
    if not parser.sections():
        errors.append(".gitmodules must declare at least one submodule section")

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

    try:
        links = gitlinks(root)
    except (OSError, subprocess.CalledProcessError) as exc:
        return [*errors, f"{gitmodules}: cannot inspect tracked gitlinks: {exc}"]

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
