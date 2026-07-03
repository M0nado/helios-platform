from __future__ import annotations

import os
import shutil
from pathlib import Path
from typing import Iterable

ROOT = Path(__file__).resolve().parents[2]
TOOL_PATHS = {
    "dotnet": [ROOT / ".tools" / "dotnet" / "dotnet"],
    "az": [ROOT / ".tools" / "azcli-venv" / "bin" / "az"],
    "gh": [ROOT / ".tools" / "gh" / "bin" / "gh"],
    "bicep": [ROOT / ".tools" / "bin" / "bicep"],
}
PATH_DIRS = [
    ROOT / ".tools" / "dotnet",
    ROOT / ".tools" / "azcli-venv" / "bin",
    ROOT / ".tools" / "gh" / "bin",
    ROOT / ".tools" / "bin",
]


def resolve_tool(name: str) -> str | None:
    found = shutil.which(name)
    if found:
        return found
    for candidate in TOOL_PATHS.get(name, []):
        if candidate.exists() and os.access(candidate, os.X_OK):
            return str(candidate)
    return None


def path_with_repo_tools(existing: str | None = None) -> str:
    current = existing if existing is not None else os.environ.get("PATH", "")
    prefixes = [str(path) for path in PATH_DIRS if path.exists()]
    return os.pathsep.join(prefixes + ([current] if current else []))


def command_with_resolved_tool(command: list[str]) -> list[str]:
    if not command:
        return command
    resolved = resolve_tool(command[0])
    return [resolved or command[0], *command[1:]]


def missing_required(tools: Iterable[str]) -> list[str]:
    return [tool for tool in tools if resolve_tool(tool) is None]
