#!/usr/bin/env python3
"""Validate the canonical repository registry, .gitmodules, and gitlinks."""

from __future__ import annotations

import configparser
import argparse
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
        mode, sha, stage = metadata.split()
        if stage != "0":
            raise ValueError(f"unmerged index entry at stage {stage}: {path}")
        if mode == "160000":
            if path in links:
                raise ValueError(f"duplicate gitlink index entry: {path}")
            links[path] = sha
    return links


def validate_registry(registry: object) -> tuple[list[dict[str, object]], list[str]]:
    """Return structurally valid repository entries and field-oriented errors."""
    if not isinstance(registry, dict):
        return [], ["$: expected object"]
    repositories = registry.get("repositories")
    if not isinstance(repositories, list):
        return [], ["$.repositories: expected array"]
    entries: list[dict[str, object]] = []
    errors: list[str] = []
    allowed_modes = {"canonical", "contract-only", "pinned-submodule"}
    for index, entry in enumerate(repositories):
        prefix = f"$.repositories[{index}]"
        if not isinstance(entry, dict):
            errors.append(f"{prefix}: expected object")
            continue
        valid = True
        for field in ("name", "role", "integrationMode"):
            if not isinstance(entry.get(field), str) or not entry[field].strip():
                errors.append(f"{prefix}.{field}: expected nonempty string")
                valid = False
        authority = entry.get("authority")
        if not isinstance(authority, list) or not authority or any(
            not isinstance(item, str) or not item.strip() for item in authority
        ):
            errors.append(f"{prefix}.authority: expected nonempty string array")
            valid = False
        if isinstance(entry.get("integrationMode"), str) and entry["integrationMode"] not in allowed_modes:
            errors.append(f"{prefix}.integrationMode: unsupported value")
            valid = False
        if valid:
            entries.append(entry)
    return entries, errors


def validate(root: Path = ROOT, *, require_gitlinks: bool = True) -> list[str]:
    errors: list[str] = []
    registry_path = root / "config/integrations/repositories.json"
    try:
        registry = json.loads(registry_path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        return [f"cannot read repository registry: {exc}"]
    entries, errors = validate_registry(registry)
    names = [str(entry["name"]).casefold() for entry in entries]
    if len(names) != len(set(names)):
        errors.append("repository registry contains duplicate names")

    expected = {
        str(entry["name"]).casefold()
        for entry in entries
        if entry.get("integrationMode") == "pinned-submodule"
    }
    parser = configparser.ConfigParser(interpolation=None, strict=True)
    try:
        with (root / ".gitmodules").open(encoding="utf-8") as stream:
            parser.read_file(stream)
    except (OSError, configparser.Error) as exc:
        return errors + [f"cannot read .gitmodules: {exc}"]
    declared: dict[str, str] = {}
    for section in parser.sections():
        path = parser[section].get("path", "")
        url = parser[section].get("url", "")
        try:
            name = github_name(url)
        except ValueError as exc:
            errors.append(str(exc))
            continue
        candidate = Path(path)
        if (
            not path
            or candidate.is_absolute()
            or ".." in candidate.parts
            or candidate.as_posix() != path
            or len(candidate.parts) < 2
            or candidate.parts[0] != "modules"
        ):
            errors.append(f"unsafe submodule path: {path!r}")
            continue
        if path in declared:
            errors.append(f"duplicate submodule path: {path}")
        declared[path] = name

    declared_names = set(declared.values())
    for name in sorted(expected - declared_names):
        errors.append(f"pinned-submodule missing from .gitmodules: {name}")
    for name in sorted(declared_names - expected):
        errors.append(f"unapproved .gitmodules repository: {name}")
    for name in sorted(name for name in declared_names if list(declared.values()).count(name) > 1):
        errors.append(f"duplicate submodule repository: {name}")

    if require_gitlinks:
        try:
            links = gitlinks(root)
        except (OSError, subprocess.CalledProcessError, ValueError) as exc:
            errors.append(f"cannot inspect gitlinks: {exc}")
            return errors
        for path in sorted(set(declared) - set(links)):
            errors.append(f"declared submodule has no 160000 gitlink: {path}")
        for path in sorted(set(links) - set(declared)):
            errors.append(f"orphan 160000 gitlink: {path}")
    return errors


def validate_bootstrap(root: Path = ROOT) -> list[str]:
    """Validate declarations for an empty bootstrap, or all pins once any exist."""
    declaration_errors = validate(root, require_gitlinks=False)
    if declaration_errors:
        return declaration_errors
    try:
        links = gitlinks(root)
    except (OSError, subprocess.CalledProcessError, ValueError) as exc:
        return [f"cannot inspect gitlinks: {exc}"]
    return validate(root) if links else []


if __name__ == "__main__":
    argument_parser = argparse.ArgumentParser(description=__doc__)
    argument_parser.add_argument(
        "--bootstrap",
        action="store_true",
        help="validate registry and .gitmodules while approved gitlinks are being bootstrapped",
    )
    arguments = argument_parser.parse_args()
    failures = validate_bootstrap() if arguments.bootstrap else validate()
    if failures:
        print("Repository integrity validation failed:", file=sys.stderr)
        for failure in failures:
            print(f"- {failure}", file=sys.stderr)
        raise SystemExit(1)
    scope = "empty-bootstrap" if arguments.bootstrap and not gitlinks(ROOT) else "full-gitlink"
    print(f"Repository registry and submodule {scope} are consistent.")
