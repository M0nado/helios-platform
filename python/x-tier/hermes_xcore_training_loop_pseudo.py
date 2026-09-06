from __future__ import annotations

"""Deprecated compatibility alias for the HELIOS training proposal generator.

This path remains only for older documentation and scripts. It produces a
proposal and has no worker, network, model-training, shell, or privileged
execution authority.
"""

from pathlib import Path
import sys

_REPOSITORY_ROOT = Path(__file__).resolve().parents[2]
if str(_REPOSITORY_ROOT) not in sys.path:
    sys.path.insert(0, str(_REPOSITORY_ROOT))

from python.aihub.training_proposal import main


if __name__ == "__main__":
    raise SystemExit(main())
