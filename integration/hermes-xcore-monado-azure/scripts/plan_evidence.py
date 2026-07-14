from __future__ import annotations

import argparse
import hashlib
import json
import re
from pathlib import Path
from typing import Any

SENSITIVE_KEYS = {
    "authorization",
    "authorizationheader",
}
SENSITIVE_KEY_SUFFIXES = (
    "connectionstring",
    "credential",
    "credentials",
    "instrumentationkey",
    "password",
    "secret",
    "sharedkey",
    "token",
)


def fail(message: str) -> None:
    raise SystemExit(message)


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def sha256_bytes(value: bytes) -> str:
    return hashlib.sha256(value).hexdigest()


def canonical_json(value: Any) -> bytes:
    return (json.dumps(value, indent=2, sort_keys=True) + "\n").encode("utf-8")


def redact(value: Any) -> Any:
    if isinstance(value, dict):
        result: dict[str, Any] = {}
        for key, item in value.items():
            normalized = re.sub(r"[^a-z0-9]", "", key.lower())
            if normalized in SENSITIVE_KEYS or normalized.endswith(SENSITIVE_KEY_SUFFIXES):
                result[key] = "***REDACTED***"
            else:
                result[key] = redact(item)
        return result
    if isinstance(value, list):
        return [redact(item) for item in value]
    return value


def parse_pairs(values: list[str]) -> dict[str, str]:
    result: dict[str, str] = {}
    for value in values:
        if "=" not in value:
            fail(f"Expected key=value, received: {value}")
        key, item = value.split("=", 1)
        if not key or key in result:
            fail(f"Invalid or duplicate key: {key}")
        result[key] = item
    return result


def require_source_sha(value: str) -> str:
    if not re.fullmatch(r"[0-9a-f]{40}", value):
        fail("source SHA must be a full lowercase Git commit SHA")
    return value


def create(args: argparse.Namespace) -> None:
    require_source_sha(args.source_sha)
    raw = Path(args.raw)
    plan = Path(args.plan)
    manifest_path = Path(args.manifest)
    template = Path(args.template)

    with raw.open(encoding="utf-8") as handle:
        raw_value = json.load(handle)
    sanitized = redact(raw_value)
    plan.parent.mkdir(parents=True, exist_ok=True)
    plan.write_bytes(canonical_json(sanitized))
    raw.unlink()

    manifest = {
        "schemaVersion": 1,
        "phase": args.phase,
        "sourceSha": args.source_sha,
        "template": {
            "path": template.as_posix(),
            "sha256": sha256(template),
        },
        "plan": {
            "path": plan.name,
            "sha256": sha256(plan),
            "unredactedSha256": sha256_bytes(canonical_json(raw_value)),
        },
        "parameters": parse_pairs(args.parameter),
        "bindings": parse_pairs(args.binding),
    }
    manifest_path.write_bytes(canonical_json(manifest))


def verify(args: argparse.Namespace) -> None:
    require_source_sha(args.source_sha)
    manifest_path = Path(args.manifest)
    plan = Path(args.plan)
    template = Path(args.template)
    with manifest_path.open(encoding="utf-8") as handle:
        manifest = json.load(handle)

    unredacted_sha = manifest.get("plan", {}).get("unredactedSha256", "")
    if not re.fullmatch(r"[0-9a-f]{64}", unredacted_sha):
        fail("reviewed plan manifest has no valid unredacted evidence hash")
    expected = {
        "schemaVersion": 1,
        "phase": args.phase,
        "sourceSha": args.source_sha,
        "template": {
            "path": template.as_posix(),
            "sha256": sha256(template),
        },
        "plan": {
            "path": plan.name,
            "sha256": sha256(plan),
            "unredactedSha256": unredacted_sha,
        },
        "parameters": parse_pairs(args.parameter),
        "bindings": parse_pairs(args.binding),
    }
    if manifest != expected:
        fail("reviewed plan manifest does not match this source, template, or parameter set")


def compare(args: argparse.Namespace) -> None:
    raw = Path(args.raw)
    reviewed = Path(args.reviewed)
    current = Path(args.current)
    manifest_path = Path(args.manifest)
    with raw.open(encoding="utf-8") as handle:
        raw_value = json.load(handle)
    with manifest_path.open(encoding="utf-8") as handle:
        manifest = json.load(handle)
    expected_unredacted_sha = manifest.get("plan", {}).get("unredactedSha256")
    current_unredacted_sha = sha256_bytes(canonical_json(raw_value))
    if current_unredacted_sha != expected_unredacted_sha:
        raw.unlink()
        fail("Azure state drifted after plan approval; unredacted evidence hash changed")
    sanitized = redact(raw_value)
    current.parent.mkdir(parents=True, exist_ok=True)
    current.write_bytes(canonical_json(sanitized))
    raw.unlink()

    with reviewed.open(encoding="utf-8") as handle:
        reviewed_value = json.load(handle)
    with current.open(encoding="utf-8") as handle:
        current_value = json.load(handle)
    if current_value != reviewed_value:
        fail(
            "Azure state drifted after plan approval; "
            f"reviewed={sha256(reviewed)} current={sha256(current)}"
        )


def add_common_manifest_arguments(parser: argparse.ArgumentParser) -> None:
    parser.add_argument("--phase", required=True, choices=("foundation", "release"))
    parser.add_argument("--source-sha", required=True)
    parser.add_argument("--template", required=True)
    parser.add_argument("--parameter", action="append", default=[])
    parser.add_argument("--binding", action="append", default=[])


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser()
    subparsers = parser.add_subparsers(dest="command", required=True)

    create_parser = subparsers.add_parser("create")
    add_common_manifest_arguments(create_parser)
    create_parser.add_argument("--raw", required=True)
    create_parser.add_argument("--plan", required=True)
    create_parser.add_argument("--manifest", required=True)
    create_parser.set_defaults(handler=create)

    verify_parser = subparsers.add_parser("verify")
    add_common_manifest_arguments(verify_parser)
    verify_parser.add_argument("--plan", required=True)
    verify_parser.add_argument("--manifest", required=True)
    verify_parser.set_defaults(handler=verify)

    compare_parser = subparsers.add_parser("compare")
    compare_parser.add_argument("--raw", required=True)
    compare_parser.add_argument("--reviewed", required=True)
    compare_parser.add_argument("--manifest", required=True)
    compare_parser.add_argument("--current", required=True)
    compare_parser.set_defaults(handler=compare)

    return parser


if __name__ == "__main__":
    arguments = build_parser().parse_args()
    arguments.handler(arguments)
