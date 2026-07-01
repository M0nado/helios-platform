# Hermes/XCore Reinforcement Learning Upgrade Plan

The first integrated RL layer is intentionally lightweight and auditable. It provides a deterministic epsilon-greedy policy surface that can be used by Hermes/XCore experiments before any production policy is enabled.

## Initial implementation

Path: `src/python/hermes_xcore/src/hermes_xcore/reinforcement_learning.py`

Capabilities:

- `RewardEvent` records action/reward/context metadata.
- `EpsilonGreedyPolicy` selects actions with bounded exploration.
- `record_reward` updates action-value estimates incrementally.
- `snapshot` exports auditable policy state for persistence or review.

## Upgrade path

1. Add offline replay from Hermes fleet logs.
2. Add safety gates for max exploration rate and denied actions.
3. Add vector/context features from `feature_pipeline.py`.
4. Add routing policy bridge to `routing_policy.py`.
5. Promote stable contract types into C# `HELIOS.Platform.Contracts`.
6. Add CI tests under `tests/python/` and integration tests under `tests/integration/`.
