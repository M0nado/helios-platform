from __future__ import annotations

"""Compatibility layer for canonical HELIOS security optimization plans."""

from pathlib import Path
import sys

_REPOSITORY_ROOT = Path(__file__).resolve().parents[2]
if str(_REPOSITORY_ROOT) not in sys.path:
    sys.path.insert(0, str(_REPOSITORY_ROOT))

from python.aihub.canonical_catalog import (  # noqa: E402
    SecurityOptimizationPlan,
    build_security_plan,
)


def build_plan(profile: str = "balanced") -> SecurityOptimizationPlan:
    return build_security_plan(profile)


__all__ = ["SecurityOptimizationPlan", "build_plan"]
