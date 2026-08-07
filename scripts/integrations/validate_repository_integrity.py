#!/usr/bin/env python3
"""Validate the canonical repository map and its declared Git submodules."""

from __future__ import annotations

import argparse
import configparser
import json
import re
import subprocess
import sys
from pathlib import Path
from typing import Any


REPOSITORY_NAME = re.compile(r"^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$")
GITHUB_URL = re.compile(
    r"^(?:https://github\.com/|git@github\.com:)(?P<name>[^/\s]+/[^/\s]+?)(?:\.git)?$",
    re.IGNORECASE,
)


def _load_json(path: Path, errors: list[str]) -> Any:
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        errors.append(f"{path}: cannot read valid JSON: {exc}")
        return None


def _repository_from_url(url: str) -> str | None:
    match = GITHUB_URL.fullmatch(url)
    return match.group("name") if match else None


def _load_tracked_gitlinks(gitmodules: Path, errors: list[str]) -> set[str]:
    """Return mode-160000 paths from the index containing ``gitmodules``."""
    try:
        result = subprocess.run(
            ["git", "-C", str(gitmodules.parent), "ls-files", "--stage", "-z"],
            check=True,
            capture_output=True,
            text=True,
        )
    except (OSError, subprocess.CalledProcessError) as exc:
        errors.append(f"{gitmodules}: cannot inspect tracked gitlinks: {exc}")
        return set()

    gitlinks: set[str] = set()
    for entry in result.stdout.split("\0"):
        if not entry:
            continue
        metadata, separator, path = entry.partition("\t")
        if separator and metadata.split(maxsplit=1)[0] == "160000":
            gitlinks.add(path)
    return gitlinks


def validate(
    repository_map: Path,
    gitmodules: Path,
    tracked_gitlinks: set[str] | None = None,
    require_gitlinks: bool = False,
) -> list[str]:
    """Return human-readable integrity errors for the supplied files."""
    errors: list[str] = []
    document = _load_json(repository_map, errors)
    if not isinstance(document, dict):
        if document is not None:
            errors.append(f"{repository_map}: top-level value must be an object")
        return errors

    for key in ("schemaVersion", "controlPlane", "canonicalPlatform", "repositories"):
        if key not in document:
            errors.append(f"{repository_map}: missing required property {key!r}")
    schema_version = document.get("schemaVersion")
    if not isinstance(schema_version, str):
        errors.append(f"{repository_map}: 'schemaVersion' must be a string")
    elif schema_version != "1.0":
        errors.append(
            f"{repository_map}: unsupported schemaVersion {schema_version!r}; expected '1.0'"
        )

    repositories = document.get("repositories")
    if not isinstance(repositories, list) or not repositories:
        errors.append(f"{repository_map}: 'repositories' must be a non-empty array")
        repositories = []

    names: dict[str, str] = {}
    roles: set[str] = set()
    for index, entry in enumerate(repositories):
        location = f"{repository_map}: repositories[{index}]"
        if not isinstance(entry, dict):
            errors.append(f"{location} must be an object")
            continue
        name, role, authority = (entry.get(key) for key in ("name", "role", "authority"))
        if not isinstance(name, str) or not REPOSITORY_NAME.fullmatch(name):
            errors.append(f"{location}.name must use the 'owner/repository' format")
        elif name.casefold() in names:
            errors.append(f"{location}.name duplicates {name!r}")
        else:
            names[name.casefold()] = role if isinstance(role, str) else ""
        if not isinstance(role, str) or not role.strip():
            errors.append(f"{location}.role must be a non-empty string")
        elif role in roles:
            errors.append(f"{location}.role duplicates {role!r}")
        else:
            roles.add(role)
        if (
            not isinstance(authority, list)
            or not authority
            or any(not isinstance(item, str) or not item.strip() for item in authority)
            or len(authority) != len(set(authority))
        ):
            errors.append(f"{location}.authority must contain unique, non-empty strings")

    expected_roles = {"controlPlane": "control-plane", "canonicalPlatform": "canonical-platform"}
    for key, expected_role in expected_roles.items():
        name = document.get(key)
        if not isinstance(name, str) or not REPOSITORY_NAME.fullmatch(name):
            errors.append(f"{repository_map}: {key!r} must use the 'owner/repository' format")
        elif names.get(name.casefold()) != expected_role:
            errors.append(
                f"{repository_map}: {key!r} must reference a repository with role {expected_role!r}"
            )

    parser = configparser.ConfigParser(interpolation=None)
    try:
        with gitmodules.open(encoding="utf-8") as stream:
            parser.read_file(stream)
    except (OSError, configparser.Error) as exc:
        errors.append(f"{gitmodules}: cannot read valid Git configuration: {exc}")
        return errors

    paths: set[str] = set()
    submodule_repositories: set[str] = set()
    for section in parser.sections():
        if not section.startswith('submodule "') or not section.endswith('"'):
            errors.append(f"{gitmodules}: unexpected section {section!r}")
            continue
        path = parser.get(section, "path", fallback="").strip()
        url = parser.get(section, "url", fallback="").strip()
        if not path or path in paths or Path(path).is_absolute() or ".." in Path(path).parts:
            errors.append(f"{gitmodules}: {section} has a missing, duplicate, or unsafe path")
        paths.add(path)
        repository = _repository_from_url(url)
        if repository is None:
            errors.append(f"{gitmodules}: {section} must use a GitHub HTTPS or SSH repository URL")
        elif repository.casefold() in submodule_repositories:
            errors.append(f"{gitmodules}: repository {repository!r} is declared more than once")
        else:
            submodule_repositories.add(repository.casefold())
            if repository.casefold() not in names:
                errors.append(
                    f"{gitmodules}: submodule repository {repository!r} is absent from {repository_map}"
                )

    if tracked_gitlinks is None:
        tracked_gitlinks = _load_tracked_gitlinks(gitmodules, errors)
    if require_gitlinks or tracked_gitlinks:
        for path in sorted(paths - tracked_gitlinks):
            errors.append(f"{gitmodules}: declared submodule path {path!r} is not a tracked gitlink")
    for path in sorted(tracked_gitlinks - paths):
        errors.append(f"{gitmodules}: tracked gitlink {path!r} is not declared as a submodule")

    return errors


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--repository-map",
        type=Path,
        default=Path("config/integrations/repositories.json"),
    )
    parser.add_argument("--gitmodules", type=Path, default=Path(".gitmodules"))
    parser.add_argument(
        "--require-gitlinks",
        action="store_true",
        help="Require every declared submodule path to be tracked as a mode-160000 gitlink.",
    )
    args = parser.parse_args(argv)

    errors = validate(
        args.repository_map,
        args.gitmodules,
        require_gitlinks=args.require_gitlinks,
    )
    if errors:
        print("Repository integrity validation failed:", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1
    print("Repository integrity validation passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
