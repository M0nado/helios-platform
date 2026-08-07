#!/usr/bin/env python3
"""Fail-closed validation for the reviewed submodule approval manifest."""

from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
from pathlib import Path
from urllib.parse import urlparse

ROOT = Path(__file__).resolve().parents[2]
MANIFEST = ROOT / "config/integrations/approved-submodules.json"
MANIFEST_SCHEMA = ROOT / "config/integrations/approved-submodules.schema.json"
EXPECTED_SCHEMA_ID = "https://helios-platform.dev/schemas/approved-submodules-v1.schema.json"
SHA = re.compile(r"^[0-9a-f]{40}$")
GITHUB_REPO_URL = re.compile(r"^https://github\.com/[^\s]+\.git$")
SUBMODULE_FIELDS = (
    "path",
    "url",
    "commit",
    "evidenceUrl",
    "ownershipDecision",
    "licenseDecision",
    "dependencySecurityReview",
    "contractEvidence",
    "buildEvidence",
    "testEvidence",
)


class GitCommandError(RuntimeError):
    """Raised when a git command fails."""


def git(*args: str) -> str:
    result = subprocess.run(
        ["git", *args], cwd=ROOT, check=False, text=True, capture_output=True
    )
    if result.returncode != 0:
        detail = (result.stderr or result.stdout).strip() or f"exit code {result.returncode}"
        raise GitCommandError(f"git {' '.join(args)} failed: {detail}")
    return result.stdout.strip()


def _load_json(path: Path) -> tuple[object | None, str | None]:
    try:
        return json.loads(path.read_text(encoding="utf-8")), None
    except (OSError, UnicodeError, json.JSONDecodeError) as exc:
        return None, f"{path.relative_to(ROOT)}: cannot load JSON ({exc})"


def _non_empty_text(value: object) -> bool:
    return isinstance(value, str) and value.strip() != ""


def _is_http_uri(value: str) -> bool:
    parsed = urlparse(value)
    return parsed.scheme in {"http", "https"} and bool(parsed.netloc)


def _validate_manifest_schema() -> list[str]:
    data, error = _load_json(MANIFEST_SCHEMA)
    if error:
        return [error]
    if not isinstance(data, dict):
        return [f"{MANIFEST_SCHEMA.relative_to(ROOT)}: schema root must be a JSON object"]

    errors: list[str] = []
    if data.get("$schema") != "https://json-schema.org/draft/2020-12/schema":
        errors.append(f"{MANIFEST_SCHEMA.relative_to(ROOT)}: must declare JSON Schema draft 2020-12")
    if data.get("$id") != EXPECTED_SCHEMA_ID:
        errors.append(f"{MANIFEST_SCHEMA.relative_to(ROOT)}: unexpected schema identifier")
    if set(data.get("required", [])) != {"approved", "submodules"}:
        errors.append(
            f"{MANIFEST_SCHEMA.relative_to(ROOT)}: root required fields must include approved and submodules"
        )
    declared_fields = (
        data.get("properties", {})
        .get("submodules", {})
        .get("items", {})
        .get("required", [])
    )
    if tuple(declared_fields) != SUBMODULE_FIELDS:
        errors.append(
            f"{MANIFEST_SCHEMA.relative_to(ROOT)}: submodule required fields are out of sync with validator"
        )
    return errors


def _normalize_manifest(data: object) -> tuple[list[dict[str, str]], list[str]]:
    if not isinstance(data, dict):
        return [], ["manifest root must be a JSON object"]

    errors: list[str] = []
    allowed_root_fields = {"approved", "submodules"}
    extra_root_fields = sorted(set(data) - allowed_root_fields)
    if extra_root_fields:
        errors.append(f"unsupported top-level fields: {', '.join(extra_root_fields)}")
    if data.get("approved") is not True:
        errors.append("approved must be boolean true")

    entries = data.get("submodules")
    if not isinstance(entries, list) or not entries:
        errors.append("submodules must be a non-empty array")
        return [], errors

    normalized: list[dict[str, str]] = []
    seen_paths: set[str] = set()
    for index, entry in enumerate(entries):
        prefix = f"submodules[{index}]"
        if not isinstance(entry, dict):
            errors.append(f"{prefix}: entry must be an object")
            continue

        unsupported = sorted(set(entry) - set(SUBMODULE_FIELDS))
        if unsupported:
            errors.append(f"{prefix}: unsupported fields: {', '.join(unsupported)}")
            continue

        missing = [field for field in SUBMODULE_FIELDS if not _non_empty_text(entry.get(field))]
        if missing:
            errors.append(f"{prefix}: missing required fields: {', '.join(missing)}")
            continue

        normalized_entry = {field: entry[field].strip() for field in SUBMODULE_FIELDS}
        path = normalized_entry["path"]
        if path in seen_paths:
            errors.append(f"{prefix}: duplicate path '{path}'")
            continue

        if not GITHUB_REPO_URL.fullmatch(normalized_entry["url"]):
            errors.append(f"{prefix}: url must match https://github.com/<owner>/<repo>.git")
            continue
        if not _is_http_uri(normalized_entry["evidenceUrl"]):
            errors.append(f"{prefix}: evidenceUrl must be a valid HTTP(S) URI")
            continue

        seen_paths.add(path)
        normalized.append(normalized_entry)
    return normalized, errors


def _configured_submodules() -> dict[str, str]:
    configured: dict[str, str] = {}
    raw = git("config", "-f", ".gitmodules", "--get-regexp", r"^submodule\..*\.path$")
    for line in raw.splitlines():
        parts = line.split(maxsplit=1)
        if len(parts) != 2:
            raise GitCommandError(f"cannot parse .gitmodules entry: {line}")
        key, path = parts
        name = key[len("submodule.") : -len(".path")]
        configured[path] = git("config", "-f", ".gitmodules", f"submodule.{name}.url")
    return configured


def _staged_paths() -> dict[str, tuple[str, str]]:
    staged: dict[str, tuple[str, str]] = {}
    for line in git("ls-files", "--stage").splitlines():
        parts = line.split(maxsplit=3)
        if len(parts) != 4:
            continue
        mode, indexed, _stage, path = parts
        staged[path] = (mode, indexed)
    return staged


def _validate_integrity(
    entries: list[dict[str, str]],
    configured: dict[str, str],
    staged: dict[str, tuple[str, str]],
    *,
    metadata_only: bool,
) -> list[str]:
    errors: list[str] = []

    approved_paths = {entry["path"] for entry in entries}
    if approved_paths != set(configured):
        errors.append("approved paths do not exactly match .gitmodules")
    indexed_gitlinks = {path for path, (mode, _indexed) in staged.items() if mode == "160000"}
    if indexed_gitlinks != approved_paths:
        missing = sorted(approved_paths - indexed_gitlinks)
        extra = sorted(indexed_gitlinks - approved_paths)
        errors.append(
            "indexed gitlink paths do not exactly match approved manifest"
            f" (missing: {missing or 'none'}, extra: {extra or 'none'})"
        )

    for entry in entries:
        path = entry["path"]
        url = entry["url"]
        commit = entry["commit"]

        if not SHA.fullmatch(commit):
            errors.append(f"{path}: commit is not a full lowercase SHA")
            continue
        if configured.get(path) != url:
            errors.append(f"{path}: URL differs from .gitmodules")

        mode, indexed = staged.get(path, (None, None))
        if mode != "160000" or indexed != commit:
            errors.append(f"{path}: index is not mode 160000 at approved commit")

        if metadata_only:
            continue

        module = ROOT / path
        if not module.is_dir() or not (module / ".git").exists():
            errors.append(f"{path}: submodule worktree is not initialized")
            continue

        try:
            actual = git("-C", str(module), "rev-parse", "HEAD")
        except GitCommandError as exc:
            errors.append(f"{path}: cannot resolve checked-out commit ({exc})")
            continue
        if actual != commit:
            errors.append(f"{path}: checked-out commit differs from approval")
        try:
            dirty = git("-C", str(module), "status", "--porcelain", "--untracked-files=all")
        except GitCommandError as exc:
            errors.append(f"{path}: cannot determine submodule worktree status ({exc})")
            continue
        if dirty:
            errors.append(f"{path}: submodule worktree is dirty")
        try:
            nested_dirty = git(
                "-C",
                str(module),
                "submodule",
                "foreach",
                "--quiet",
                "--recursive",
                "git status --porcelain --untracked-files=all",
            )
        except GitCommandError as exc:
            errors.append(f"{path}: cannot determine nested submodule worktree status ({exc})")
            continue
        if nested_dirty:
            errors.append(f"{path}: nested submodule worktree is dirty")

    return errors


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--metadata-only",
        action="store_true",
        help="validate approved gitlinks against repository metadata only",
    )
    args = parser.parse_args(argv)

    if not MANIFEST.exists():
        print(f"BLOCKED: missing reviewed approval manifest: {MANIFEST.relative_to(ROOT)}")
        return 2

    schema_errors = _validate_manifest_schema()
    if schema_errors:
        print("BLOCKED: approval manifest schema validation failed")
        print("\n".join(f"- {error}" for error in schema_errors))
        return 2

    data, load_error = _load_json(MANIFEST)
    if load_error:
        print(f"BLOCKED: {load_error}")
        return 2

    entries, manifest_errors = _normalize_manifest(data)
    if manifest_errors:
        print("BLOCKED: manifest is not approved")
        print("\n".join(f"- {error}" for error in manifest_errors))
        return 2

    try:
        configured = _configured_submodules()
        staged = _staged_paths()
    except GitCommandError as exc:
        print("FAIL: pinned-submodule integrity gate")
        print(f"- {exc}")
        return 1

    errors = _validate_integrity(
        entries, configured, staged, metadata_only=args.metadata_only
    )

    if errors:
        print("FAIL: pinned-submodule integrity gate")
        print("\n".join(f"- {error}" for error in errors))
        return 1

    if args.metadata_only:
        print(f"PASS: {len(entries)} approved gitlinks validated against manifest and index metadata")
        return 0

    try:
        git("submodule", "foreach", "--recursive", "git fsck --full")
    except GitCommandError as exc:
        print("FAIL: pinned-submodule integrity gate")
        print(f"- recursive object validation failed: {exc}")
        return 1

    print(f"PASS: {len(entries)} approved gitlinks and recursive object stores validated")
    return 0


if __name__ == "__main__":
    sys.exit(main(None))
