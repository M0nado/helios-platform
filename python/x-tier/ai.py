from __future__ import annotations

"""Compatibility launcher for the guarded HELIOS AIHub CLI.

The historical CLI accepted arbitrary remote base URLs and command-line API
keys. This wrapper delegates to the loopback-only CLI, which reads its token
from `AIHUB_API_KEY` or `AIHUB_API_KEY_FILE` and refuses embedded credentials.
"""

from pathlib import Path
import sys

_REPOSITORY_ROOT = Path(__file__).resolve().parents[2]
if str(_REPOSITORY_ROOT) not in sys.path:
    sys.path.insert(0, str(_REPOSITORY_ROOT))

from python.aihub.secure_cli import main


if __name__ == "__main__":
    raise SystemExit(main())
