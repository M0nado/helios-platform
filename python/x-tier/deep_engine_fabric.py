from __future__ import annotations

"""Compatibility exports for the canonical HELIOS AIHub engine catalog.

This module preserves the historical import path while delegating all behavior
to the typed, proposal-only canonical catalog. It contains no training,
deployment, shell, network, or privileged-operation authority.
"""

from pathlib import Path
import sys

_REPOSITORY_ROOT = Path(__file__).resolve().parents[2]
if str(_REPOSITORY_ROOT) not in sys.path:
    sys.path.insert(0, str(_REPOSITORY_ROOT))

from python.aihub.canonical_catalog import (  # noqa: E402
    EngineSpec,
    build_engine_catalog,
    recommend_engine_mix,
)

__all__ = ["EngineSpec", "build_engine_catalog", "recommend_engine_mix"]
