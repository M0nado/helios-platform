#!/usr/bin/env python3
"""Validate the governed capability registry without requiring third-party packages."""

from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[2]
DEFAULT_REGISTRY = ROOT / "config/integrations/capability-profiles.json"
ENVIRONMENTS = {"local", "development", "test", "staging", "production"}
APPROVAL_MODES = {"none", "review", "protected-environment", "tenant-admin"}
DURATION = re.compile(r"^PT(?:[1-9]|[1-9][0-9]|1[01][0-9]|120)M$")
REQUIRED_FIELDS = {
    "id", "provider", "capability", "effect", "scopes", "resources",
    "environments", "maximumDuration", "approval", "idempotency", "cleanup",
}


def validate(document: dict[str, Any]) -> list[str]:
    errors: list[str] = []
    if document.get("schemaVersion") != "1.0":
        errors.append("$.schemaVersion must be 1.0")
    if document.get("defaultDecision") != "deny":
        errors.append("$.defaultDecision must be deny (unknown profiles are fail-closed)")

    profiles = document.get("profiles")
    if not isinstance(profiles, list) or not profiles:
        return errors + ["$.profiles must be a non-empty array"]

    seen: set[str] = set()
    for index, profile in enumerate(profiles):
        path = f"$.profiles[{index}]"
        if not isinstance(profile, dict):
            errors.append(f"{path} must be an object")
            continue
        missing = REQUIRED_FIELDS - profile.keys()
        if missing:
            errors.append(f"{path} missing fields: {', '.join(sorted(missing))}")
            continue
        profile_id = profile["id"]
        if not isinstance(profile_id, str) or not re.fullmatch(r"[a-z0-9]+(?:[.-][a-z0-9]+)*", profile_id):
            errors.append(f"{path}.id is invalid")
        elif profile_id in seen:
            errors.append(f"{path}.id duplicates {profile_id}")
        seen.add(profile_id)
        if profile["effect"] != "allow":
            errors.append(f"{path}.effect must be allow; denials belong to the default decision")
        for field in ("scopes", "resources", "environments"):
            value = profile[field]
            if not isinstance(value, list) or not value or any(not isinstance(item, str) or not item for item in value):
                errors.append(f"{path}.{field} must be a non-empty string array")
            elif len(value) != len(set(value)):
                errors.append(f"{path}.{field} must not contain duplicates")
        if isinstance(profile["environments"], list) and not set(profile["environments"]).issubset(ENVIRONMENTS):
            errors.append(f"{path}.environments contains an unknown environment")
        if not isinstance(profile["maximumDuration"], str) or not DURATION.fullmatch(profile["maximumDuration"]):
            errors.append(f"{path}.maximumDuration must be PT1M through PT120M")
        _validate_approval(profile["approval"], path, errors)
        _validate_idempotency(profile["idempotency"], path, errors)
        _validate_cleanup(profile["cleanup"], path, errors)
    return errors


def _validate_approval(value: Any, path: str, errors: list[str]) -> None:
    if not isinstance(value, dict) or set(value) != {"mode", "approvers"}:
        errors.append(f"{path}.approval must contain exactly mode and approvers")
        return
    mode, approvers = value["mode"], value["approvers"]
    if mode not in APPROVAL_MODES:
        errors.append(f"{path}.approval.mode is invalid")
    if not isinstance(approvers, list) or any(not isinstance(x, str) or not x for x in approvers):
        errors.append(f"{path}.approval.approvers must be a string array")
    elif (mode == "none" and approvers) or (mode != "none" and not approvers):
        errors.append(f"{path}.approval.approvers does not match mode {mode}")


def _validate_idempotency(value: Any, path: str, errors: list[str]) -> None:
    if not isinstance(value, dict) or set(value) != {"required", "key", "replayWindow"}:
        errors.append(f"{path}.idempotency has invalid fields")
        return
    required = value["required"]
    if not isinstance(required, bool):
        errors.append(f"{path}.idempotency.required must be boolean")
    if required:
        if not isinstance(value["key"], str) or not value["key"]:
            errors.append(f"{path}.idempotency.key is required")
        if not isinstance(value["replayWindow"], str) or not DURATION.fullmatch(value["replayWindow"]):
            errors.append(f"{path}.idempotency.replayWindow is invalid")
    elif value["key"] is not None or value["replayWindow"] is not None:
        errors.append(f"{path}.idempotency key and replayWindow must be null when not required")


def _validate_cleanup(value: Any, path: str, errors: list[str]) -> None:
    if not isinstance(value, dict) or set(value) != {"required", "actions"}:
        errors.append(f"{path}.cleanup has invalid fields")
        return
    required, actions = value["required"], value["actions"]
    if not isinstance(required, bool) or not isinstance(actions, list) or any(not isinstance(x, str) or not x for x in actions):
        errors.append(f"{path}.cleanup has invalid required/actions values")
    elif required != bool(actions):
        errors.append(f"{path}.cleanup.actions does not match required")


def resolve_profile(document: dict[str, Any], profile_id: str) -> str:
    """Return the profile effect or the fail-closed default for an unknown ID."""
    return next((p["effect"] for p in document.get("profiles", []) if p.get("id") == profile_id), document.get("defaultDecision", "deny"))


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("registry", nargs="?", type=Path, default=DEFAULT_REGISTRY)
    parser.add_argument("--resolve", metavar="PROFILE_ID")
    args = parser.parse_args()
    document = json.loads(args.registry.read_text(encoding="utf-8"))
    errors = validate(document)
    if errors:
        print("\n".join(f"ERROR: {error}" for error in errors), file=sys.stderr)
        return 1
    if args.resolve:
        print(resolve_profile(document, args.resolve))
    else:
        print(f"Validated {len(document['profiles'])} capability profiles; unknown profile decision=deny")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
