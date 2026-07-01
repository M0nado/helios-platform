# Upstream Upgrade Plan

HELIOS should treat `helios-control` and `hermes-fleet-production` as upstream sources that can evolve naturally in their own GitHub repositories. Imported code must keep provenance and be refreshed through repeatable sync steps rather than one-off copy/paste.

## Upstream sources

| Source | Remote | Current status | Upgrade strategy |
|---|---|---|---|
| `helios-control` | `https://github.com/M0nado/helios-control.git` | Configured, fetch blocked by credentials in this environment | Re-fetch when credentials or local path are available, then import WinUI/control changes through `src/frontend/HELIOS.Control.WinUI/` and contracts/orchestration boundaries. |
| `hermes-fleet-production` | `https://github.com/Yolkster64/hermes-fleet-platforms.git` | Fetched successfully | Track remote branches, import targeted Hermes/XCore/RL/Azure assets, and avoid bulk-merging generated history. |
| `hermes-core` | not provided separately | Covered by Hermes fleet until a core repo is provided | Use `src/integrations/Hermes/` and `src/core/HELIOS.Platform.Contracts/` as stable integration boundaries. |
| `xcore-agent` | not provided separately | Covered by Hermes/XCore assets until an agent repo is provided | Use `src/python/hermes_xcore/`, `src/integrations/XCore/`, and `src/native/HELIOS.Performance/` as staging boundaries. |

## Recurring upgrade loop

1. Fetch all configured upstreams.
2. Regenerate branch inventory.
3. Diff upstream branch tips against last imported SHAs.
4. Classify changes by subsystem: WinUI, C# contracts/orchestration, Hermes, XCore, RL, C++, Python, Azure, security, docs.
5. Import changes into canonical target paths only.
6. Preserve reference/prose/pseudocode as docs when it is not executable.
7. Run language-specific validation.
8. Update `docs/status/verification-log.md` and source inventory docs.

## RL must stay in scope

Reinforcement learning belongs in the Hermes/XCore Python adapter first, then graduates into C# contracts/orchestration once the runtime API stabilizes.

Canonical RL paths:

- `src/python/hermes_xcore/src/hermes_xcore/reinforcement_learning.py`
- `docs/integration/hermes-xcore/RL_UPGRADE_PLAN.md`
- `docs/testing/test-matrix.yaml`

RL upgrade priorities:

1. Policy abstraction for action selection.
2. Reward event schema.
3. Offline replay/evaluation loop.
4. Safe exploration controls.
5. Audit trail for policy updates.
6. Integration contract for C# orchestration.
