#!/usr/bin/env python3
from __future__ import annotations

import argparse
import configparser
import json
import re
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Any

SUBMODULE_SECTION_PREFIX = 'submodule "'
SUBMODULE_SECTION_SUFFIX = '"'
HTTPS_REPO_PATTERN = re.compile(r"^https://github\.com/(?P<owner>[^/]+)/(?P<repo>[^/.]+)(?:\.git)?/?$")
SSH_REPO_PATTERN = re.compile(r"^git@github\.com:(?P<owner>[^/]+)/(?P<repo>[^/.]+)(?:\.git)?$")


@dataclass(frozen=True)
class SubmoduleEntry:
    name: str
    path: str
    url: str
    branch: str | None


def parse_repository_name(url: str) -> str:
    https_match = HTTPS_REPO_PATTERN.match(url)
    if https_match:
        return f"{https_match.group('owner')}/{https_match.group('repo')}"
    ssh_match = SSH_REPO_PATTERN.match(url)
    if ssh_match:
        return f"{ssh_match.group('owner')}/{ssh_match.group('repo')}"
    raise ValueError(f"Unsupported submodule URL format: {url}")


def load_repository_names(repositories_path: Path) -> set[str]:
    payload = json.loads(repositories_path.read_text(encoding="utf-8"))
    repositories = payload.get("repositories")
    if not isinstance(repositories, list):
        raise ValueError("repositories.json must contain a 'repositories' array")

    names: set[str] = set()
    for item in repositories:
        if not isinstance(item, dict) or "name" not in item:
            raise ValueError("Each repository entry must contain a 'name' field")
        repository_name = item["name"]
        if not isinstance(repository_name, str) or not repository_name.strip():
            raise ValueError("Repository names must be non-empty strings")
        names.add(repository_name.strip())
    return names


def load_submodule_entries(gitmodules_path: Path) -> list[SubmoduleEntry]:
    parser = configparser.ConfigParser()
    parser.optionxform = str
    parser.read_string(gitmodules_path.read_text(encoding="utf-8"))

    entries: list[SubmoduleEntry] = []
    for section_name in parser.sections():
        if not (
            section_name.startswith(SUBMODULE_SECTION_PREFIX)
            and section_name.endswith(SUBMODULE_SECTION_SUFFIX)
        ):
            continue

        submodule_name = section_name[len(SUBMODULE_SECTION_PREFIX) : -len(SUBMODULE_SECTION_SUFFIX)]
        path = parser.get(section_name, "path", fallback="").strip()
        url = parser.get(section_name, "url", fallback="").strip()
        branch = parser.get(section_name, "branch", fallback="").strip() or None
        entries.append(SubmoduleEntry(name=submodule_name, path=path, url=url, branch=branch))

    return entries


def validate_submodule_map(
    submodules: list[SubmoduleEntry],
    known_repositories: set[str],
) -> list[str]:
    errors: list[str] = []
    seen_paths: set[str] = set()
    seen_urls: set[str] = set()

    if not submodules:
        errors.append("No submodule entries were found in .gitmodules")
        return errors

    for entry in submodules:
        if not entry.path:
            errors.append(f"Submodule '{entry.name}' is missing path")
        if not entry.url:
            errors.append(f"Submodule '{entry.name}' is missing url")
            continue
        if entry.path in seen_paths:
            errors.append(f"Duplicate submodule path detected: {entry.path}")
        else:
            seen_paths.add(entry.path)
        if entry.url in seen_urls:
            errors.append(f"Duplicate submodule url detected: {entry.url}")
        else:
            seen_urls.add(entry.url)
        if not entry.path.startswith("modules/"):
            errors.append(
                f"Submodule '{entry.name}' path must be under modules/: {entry.path}"
            )
        if entry.branch is None:
            errors.append(f"Submodule '{entry.name}' is missing explicit branch")

        try:
            repository_name = parse_repository_name(entry.url)
        except ValueError as exc:
            errors.append(str(exc))
            continue

        if repository_name not in known_repositories:
            errors.append(
                f"Submodule '{entry.name}' references unknown repository '{repository_name}'"
            )

    return errors


def build_summary(
    gitmodules_path: Path,
    repositories_path: Path,
    submodules: list[SubmoduleEntry],
    errors: list[str],
) -> dict[str, Any]:
    return {
        "gitmodulesPath": str(gitmodules_path),
        "repositoriesPath": str(repositories_path),
        "submoduleCount": len(submodules),
        "errors": errors,
        "ok": len(errors) == 0,
        "submodules": [
            {
                "name": item.name,
                "path": item.path,
                "url": item.url,
                "branch": item.branch,
            }
            for item in submodules
        ],
    }


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Validate HELIOS submodule integrity mapping")
    parser.add_argument(
        "--gitmodules",
        type=Path,
        default=Path(".gitmodules"),
        help="Path to .gitmodules file",
    )
    parser.add_argument(
        "--repositories",
        type=Path,
        default=Path("config/integrations/repositories.json"),
        help="Path to repositories.json file",
    )
    parser.add_argument(
        "--json-out",
        type=Path,
        default=None,
        help="Optional output path for JSON summary",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()

    if not args.gitmodules.exists():
        raise FileNotFoundError(f".gitmodules file not found: {args.gitmodules}")
    if not args.repositories.exists():
        raise FileNotFoundError(f"repositories.json file not found: {args.repositories}")

    known_repositories = load_repository_names(args.repositories)
    submodules = load_submodule_entries(args.gitmodules)
    errors = validate_submodule_map(submodules, known_repositories)
    summary = build_summary(args.gitmodules, args.repositories, submodules, errors)

    if args.json_out is not None:
        args.json_out.parent.mkdir(parents=True, exist_ok=True)
        args.json_out.write_text(json.dumps(summary, indent=2) + "\n", encoding="utf-8")

    if errors:
        for error in errors:
            print(f"ERROR: {error}", file=sys.stderr)
        return 1

    print(
        f"Validated {len(submodules)} submodules against {len(known_repositories)} repository entries."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
