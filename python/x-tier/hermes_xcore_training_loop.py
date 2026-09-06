from __future__ import annotations

"""Compatibility launcher for proposal-only Hermes/XCore training plans.

The historical entry point performed a simulated route/execute/score/memory
cycle. That behavior is retained as a deterministic review proposal only; this
module does not contact workers, train models, write reinforcement memory, or
perform external network activity.
"""

from pathlib import Path
import sys

_REPOSITORY_ROOT = Path(__file__).resolve().parents[2]
if str(_REPOSITORY_ROOT) not in sys.path:
    sys.path.insert(0, str(_REPOSITORY_ROOT))

from python.aihub.training_proposal import main


if __name__ == "__main__":
    raise SystemExit(main())
