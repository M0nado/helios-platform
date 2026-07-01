# Hermes/XCore Reinforcement Learning Upgrade Plan

The first integrated RL layer is intentionally lightweight and auditable. It provides a deterministic epsilon-greedy policy surface that can be used by Hermes/XCore experiments before any production policy is enabled.

## Initial implementation

Path: `src/python/hermes_xcore/src/hermes_xcore/reinforcement_learning.py`

Capabilities:

- `RewardEvent` records action/reward/context metadata.
- `PolicyDecision` records action, score, context, and reason for audit trails.
- `OfflineReplayBuffer` replays Hermes fleet reward logs and CSV exports.
- `FeatureContextAdapter` converts selected feature values into stable numeric RL context.
- `EpsilonGreedyPolicy` selects actions with bounded exploration.
- `record_reward` updates action-value estimates incrementally.
- `snapshot` exports auditable policy state for persistence or review.

## Upgrade path

1. Extend offline replay with persisted Hermes fleet logs and production telemetry exports.
2. Add safety gates for max exploration rate and denied actions.
3. Feed richer vector/context features from `feature_pipeline.py`.
4. Add a direct routing policy bridge to `routing_policy.py`.
5. Promote stable contract types into C# `HELIOS.Platform.Contracts`.
6. Expand CI tests under `tests/python/` and integration tests under `tests/integration/`.
