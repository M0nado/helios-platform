from __future__ import annotations

"""Compatibility launcher for deterministic canonical AIHub artifacts."""

from pathlib import Path
import sys

_REPOSITORY_ROOT = Path(__file__).resolve().parents[2]
if str(_REPOSITORY_ROOT) not in sys.path:
    sys.path.insert(0, str(_REPOSITORY_ROOT))

from python.aihub.build_canonical_artifacts import main


if __name__ == "__main__":
    raise SystemExit(main())
