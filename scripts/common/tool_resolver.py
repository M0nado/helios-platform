#!/usr/bin/env python3
"""Shared repo-local tool resolution helpers for HELIOS scripts."""
from __future__ import annotations

import os
import shutil
from pathlib import Path
from typing import Iterable

ROOT = Path(__file__).resolve().parents[2]

_TOOL_CANDIDATES = {
    "dotnet": (Path(".tools/dotnet/dotnet"),),
    "az": (Path(".tools/azcli-venv/bin/az"),),
    "gh": (Path(".tools/gh/bin/gh"),),
    "bicep": (Path(".tools/bin/bicep"),),
}

_PATH_ADDITIONS = (
    Path(".tools/dotnet"),
    Path(".tools/azcli-venv/bin"),
    Path(".tools/gh/bin"),
    Path(".tools/bin"),
)


def resolve_tool(name: str, root: Path | None = None) -> Path | None:
    """Resolve a tool using PATH first, then known repo-local locations."""
    found = shutil.which(name)
    if found:
        return Path(found).resolve()

    repo_root = root or ROOT
    for relative in _TOOL_CANDIDATES.get(name, ()):  # known repo-local tools only
        candidate = repo_root / relative
        if candidate.is_file() and os.access(candidate, os.X_OK):
            return candidate.resolve()
    return None


def path_additions(root: Path | None = None, existing_only: bool = True) -> list[Path]:
    """Return repo-local directories that should be prepended to PATH."""
    repo_root = root or ROOT
    additions = [repo_root / relative for relative in _PATH_ADDITIONS]
    if existing_only:
        additions = [path for path in additions if path.is_dir()]
    return [path.resolve() for path in additions]


def path_addition_strings(root: Path | None = None, existing_only: bool = True) -> list[str]:
    return [str(path) for path in path_additions(root=root, existing_only=existing_only)]


def environment_with_tools(root: Path | None = None, env: dict[str, str] | None = None) -> dict[str, str]:
    """Build an environment with repo-local tool directories prepended to PATH."""
    merged = dict(env or os.environ)
    additions = path_addition_strings(root=root, existing_only=True)
    current_path = merged.get("PATH", "")
    merged["PATH"] = os.pathsep.join([*additions, current_path]) if additions else current_path
    return merged


def resolved_tools(names: Iterable[str], root: Path | None = None) -> dict[str, str | None]:
    """Resolve a collection of tool names as strings for JSON reports."""
    return {name: (str(path) if (path := resolve_tool(name, root=root)) else None) for name in names}
