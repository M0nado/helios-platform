#!/usr/bin/env python3
"""Dependency-free smoke tests for the normalized integration event envelope."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
schema = json.loads((ROOT / "config/integrations/event-contract.schema.json").read_text())
required = set(schema["required"])
expected = {"schemaVersion", "eventId", "correlationId", "source", "eventType", "repository", "environment", "occurredAt", "dataClassification", "links", "payload"}
missing = expected - required
if missing:
    raise SystemExit(f"event envelope no longer requires: {sorted(missing)}")
properties = schema["properties"]
for identifier in ("eventId", "correlationId"):
    if properties[identifier].get("minLength", 0) < 4:
        raise SystemExit(f"{identifier} must remain a non-trivial correlation identifier")
if properties["payload"].get("type") != "object":
    raise SystemExit("payload must remain a bounded object")
print("Normalized integration event contract smoke tests passed.")
