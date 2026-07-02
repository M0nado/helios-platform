# HELIOS Control Plane Permissions

HELIOS control-plane automation is safe-by-default. Read-only inventory and dry-run validation are allowed by default; mutation requires explicit future `--apply` support and a scoped permission review.

## Tiers

| Tier | Name | Allowed by default | Examples |
| --- | --- | --- | --- |
| 0 | Offline local reports | Yes | Parse JSON, generate dashboard, compile scripts |
| 1 | Authenticated read inventory | Yes | `gh repo view`, `gh workflow list`, `az account show` |
| 2 | Dry-run / what-if | Yes | Azure what-if, Key Vault sync dry-run |
| 3 | Repo-level apply | No | Change branch protection, set repo variables |
| 4 | Org/subscription apply | No | Org secrets, Azure resource deployment |
| 5 | Enterprise/global admin | No | Enterprise policy changes |

## Rules

- Never print secret values.
- Never mutate GitHub, Azure, OpenAI, Slack, Microsoft 365, or Codex settings by default.
- Prefer manifests in `config/` plus generated drift reports under `reports/control-plane/`.
- Require explicit `--apply` and a concrete scope before future mutation commands are added.
