#!/usr/bin/env python3
"""Validate the consolidated HELIOS capability backlog."""

import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
PATH = ROOT / "config" / "capabilities" / "major-capabilities.v1.json"
OWNERS = {"csharp", "cpp", "fsharp", "python", "platform"}
GATES = {"standard", "approval", "what-if-and-approval"}


def main() -> int:
    data = json.loads(PATH.read_text(encoding="utf-8"))
    items = data["capabilities"]
    errors: list[str] = []
    ids = [item.get("id") for item in items]
    if len(items) != 50:
        errors.append(f"expected 50 capabilities, found {len(items)}")
    if len(set(ids)) != len(ids):
        errors.append("capability IDs must be unique")
    for item in items:
        if item.get("owner") not in OWNERS:
            errors.append(f"{item.get('id')}: invalid owner")
        if item.get("gate") not in GATES:
            errors.append(f"{item.get('id')}: invalid gate")
        if not item.get("outcome") or not item.get("checks"):
            errors.append(f"{item.get('id')}: outcome and checks are required")
    if errors:
        print("\n".join(errors))
        return 1
    print(f"validated {len(items)} governed capabilities across {len(OWNERS)} owners")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
