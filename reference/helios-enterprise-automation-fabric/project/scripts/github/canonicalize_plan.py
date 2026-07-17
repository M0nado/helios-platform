#!/usr/bin/env python3
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
from typing import Any

VOLATILE_KEYS = {"timestamp", "duration", "durationMs", "correlationId"}


def normalize(value: Any) -> Any:
    if isinstance(value, dict):
        return {key: normalize(item) for key, item in sorted(value.items()) if key not in VOLATILE_KEYS}
    if isinstance(value, list):
        normalized = [normalize(item) for item in value]
        if all(isinstance(item, dict) and "resourceId" in item for item in normalized):
            normalized.sort(key=lambda item: str(item["resourceId"]))
        return normalized
    return value


def main() -> int:
    parser = argparse.ArgumentParser(description="Canonicalize Azure what-if JSON and emit immutable metadata.")
    parser.add_argument("--input", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--metadata-output", type=Path)
    args = parser.parse_args()
    payload = normalize(json.loads(args.input.read_text(encoding="utf-8")))
    encoded = (json.dumps(payload, indent=2, sort_keys=True) + "\n").encode("utf-8")
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_bytes(encoded)
    metadata = {
        "format": "helios-azure-what-if-v1",
        "sha256": hashlib.sha256(encoded).hexdigest(),
        "bytes": len(encoded),
        "input": str(args.input),
        "canonical": str(args.output),
    }
    if args.metadata_output:
        args.metadata_output.parent.mkdir(parents=True, exist_ok=True)
        args.metadata_output.write_text(json.dumps(metadata, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(json.dumps(metadata, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
