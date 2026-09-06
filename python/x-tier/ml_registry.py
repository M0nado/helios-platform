from __future__ import annotations

"""Compatibility registry backed by the canonical HELIOS model catalog."""

from dataclasses import asdict
import hashlib
import json
import os
from pathlib import Path
import sys
import tempfile

_REPOSITORY_ROOT = Path(__file__).resolve().parents[2]
if str(_REPOSITORY_ROOT) not in sys.path:
    sys.path.insert(0, str(_REPOSITORY_ROOT))

from python.aihub.canonical_catalog import ModelProfile, build_model_registry  # noqa: E402


class MLRegistry:
    """Persists the disabled-by-default canonical registry atomically."""

    def __init__(self, path: str = "artifacts/aihub/canonical/model-registry.json"):
        self.path = Path(path)
        self.profiles: list[ModelProfile] = []

    def seed_default(self) -> None:
        self.profiles = build_model_registry()

    def save(self) -> str:
        self.path.parent.mkdir(parents=True, exist_ok=True)
        encoded = (
            json.dumps([asdict(profile) for profile in self.profiles], indent=2, sort_keys=True)
            + "\n"
        ).encode("utf-8")
        descriptor, temp_name = tempfile.mkstemp(
            prefix=f".{self.path.name}.",
            suffix=".tmp",
            dir=self.path.parent,
        )
        temp_path = Path(temp_name)
        try:
            with os.fdopen(descriptor, "wb") as handle:
                handle.write(encoded)
                handle.flush()
                os.fsync(handle.fileno())
            os.replace(temp_path, self.path)
        finally:
            temp_path.unlink(missing_ok=True)
        return hashlib.sha256(encoded).hexdigest()


__all__ = ["MLRegistry", "ModelProfile"]
