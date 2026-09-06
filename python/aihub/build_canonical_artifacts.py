from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path
import tempfile
from typing import Any

from .canonical_catalog import serialize_catalog_bundle


def _atomic_write_json(path: Path, payload: Any) -> str:
    path.parent.mkdir(parents=True, exist_ok=True)
    encoded = (json.dumps(payload, indent=2, sort_keys=True) + "\n").encode("utf-8")
    descriptor, temp_name = tempfile.mkstemp(
        prefix=f".{path.name}.",
        suffix=".tmp",
        dir=path.parent,
    )
    temp_path = Path(temp_name)
    try:
        with os.fdopen(descriptor, "wb") as handle:
            handle.write(encoded)
            handle.flush()
            os.fsync(handle.fileno())
        os.replace(temp_path, path)
    finally:
        temp_path.unlink(missing_ok=True)
    return hashlib.sha256(encoded).hexdigest()


def build_artifacts(output_directory: Path, *, cuda_enabled: bool) -> dict[str, Any]:
    output_directory = Path(output_directory)
    bundle = serialize_catalog_bundle(cuda_enabled=cuda_enabled)

    files: dict[str, Any] = {
        "security-plans.json": bundle["securityPlans"],
        "vm-topology.json": bundle["vmTopology"],
        "model-registry.json": bundle["modelRegistry"],
        "engine-catalog.json": bundle["engineCatalog"],
        "canonical-catalog-bundle.json": bundle,
    }
    manifest: list[dict[str, Any]] = []
    for name, payload in files.items():
        path = output_directory / name
        digest = _atomic_write_json(path, payload)
        manifest.append(
            {
                "path": name,
                "sha256": digest,
                "bytes": path.stat().st_size,
            }
        )

    manifest_payload = {
        "schemaVersion": 1,
        "cudaEnabled": cuda_enabled,
        "productionEnabled": False,
        "files": manifest,
    }
    manifest_digest = _atomic_write_json(
        output_directory / "manifest.json",
        manifest_payload,
    )
    return {
        "outputDirectory": str(output_directory),
        "manifestSha256": manifest_digest,
        **manifest_payload,
    }


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Build deterministic, proposal-only HELIOS AIHub catalog artifacts."
    )
    parser.add_argument(
        "--output",
        type=Path,
        default=Path("artifacts/aihub/canonical"),
    )
    parser.add_argument(
        "--cuda-enabled",
        action=argparse.BooleanOptionalAction,
        default=True,
    )
    args = parser.parse_args()
    result = build_artifacts(args.output, cuda_enabled=args.cuda_enabled)
    print(json.dumps(result, indent=2, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
