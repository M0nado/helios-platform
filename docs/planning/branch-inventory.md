# Branch Inventory

Generated from configured remotes and direct Git inspection.

## Current checkout

| Type | Name | SHA | Notes |
|---|---|---|---|
| Local branch | `work` | `6152988` | Current branch after inventory workflow hardening. |

## Configured remotes

| Remote | URL | Status |
|---|---|---|
| `helios-control` | `https://github.com/M0nado/helios-control.git` | Fetch failed: credentials required. |
| `hermes-fleet-production` | `https://github.com/Yolkster64/hermes-fleet-platforms.git` | Fetched successfully. |

## Remote branches

| Branch | SHA | Notes |
|---|---|---|
| `hermes-fleet-production/main` | `7f42640422bfde5d05fab3aacf20f5616f817636` | Large platform branch. Full merge deferred. |
| `hermes-fleet-production/chore/update-marker` | `0dbe237062b07615d53ed6032e9a320a08c76cfc` | X-tier polyglot branch. Selected assets imported. |
| `hermes-fleet-production/sre/restore-and-azure-scaffold-no-workflow-20260609` | `9e3fa731358060753f00dd4268126d74d070e085` | Azure scaffold branch. Selected assets imported. |

## Source availability

| Source | Status | Inspection output |
|---|---|---|
| `helios-control` | Blocked: credentials/local path required | `docs/planning/helios-control-inventory.md` |
| `hermes-fleet-production` | Available and partially integrated | `docs/planning/hermes-fleet-production-inventory.md` |
| `hermes-core` | Not provided | Pending |
| `xcore-agent` | Not provided | Pending |
