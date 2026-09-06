from __future__ import annotations

"""Compatibility launcher for the hardened HELIOS AIHub runtime.

The former prototype HTTP implementation is intentionally retired. This file
contains no server, route, task-execution, shell, or public-bind logic; it only
forwards to the guarded loopback/catalog runtime.
"""

from pathlib import Path
import sys

_REPOSITORY_ROOT = Path(__file__).resolve().parents[2]
if str(_REPOSITORY_ROOT) not in sys.path:
    sys.path.insert(0, str(_REPOSITORY_ROOT))

from python.aihub.secure_runtime.catalog_server import run_catalog_server


def main() -> int:
    run_catalog_server()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
