from __future__ import annotations

"""Compatibility layer for the proposal-only HELIOS VM topology."""

from pathlib import Path
import sys

_REPOSITORY_ROOT = Path(__file__).resolve().parents[2]
if str(_REPOSITORY_ROOT) not in sys.path:
    sys.path.insert(0, str(_REPOSITORY_ROOT))

from python.aihub.canonical_catalog import VMTarget, build_vm_topology  # noqa: E402


class VMOrchestrator:
    """Returns reviewed VM targets without creating or changing any VM."""

    def build_default_topology(self) -> list[VMTarget]:
        return build_vm_topology()


__all__ = ["VMOrchestrator", "VMTarget"]
