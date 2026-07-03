# HELIOS Command Center

This is the single entry point for local, GitHub-hosted, Azure, AI, Claude, Codex, branch intelligence, build graph, dashboard, MAUI/Visual Studio, and hybrid architecture operations.

## Fast path

```bash
./helios.sh setup
./helios.sh dashboard
./helios.sh status
./helios.sh all
```

## Required execution order

The `./helios.sh all` command follows `config/execution-order.json`:

1. HELIOS Command Center single entry point.
2. Unified secrets map.
3. GitHub inventory.
4. Azure inventory.
5. Control-plane permissions model.
6. Build graph.
7. Branch merge/prune recommendations.
8. Dashboard actions page.
9. Codex task packet generation.
10. Opt-in AI enrichment.
11. GitHub desired-state manifest.
12. Azure desired-state manifest.
13. Hosted GitHub workflow expansion.
14. Cloud Shell full setup.
15. Long-term org/enterprise apply-mode controls.

## Core commands

| Command | Purpose |
| --- | --- |
| `./helios.sh setup` | Bootstrap local tools and regenerate base reports. |
| `./helios.sh dashboard` | Serve the local dashboard. |
| `./helios.sh status` | Generate GitHub/Azure/AI/Claude/Codex control summary. |
| `./helios.sh github` | Generate GitHub inventory. |
| `./helios.sh azure` | Generate Azure inventory. |
| `./helios.sh build` | Generate or run the build graph. |
| `./helios.sh codex` | Generate Codex-ready task packets. |
| `./helios.sh recommendations` | Generate branch merge/prune recommendations. |
| `./helios.sh all` | Run the safe reporting pipeline in the required order. |

## Safety

All commands are read-only/reporting by default unless a script explicitly documents an opt-in apply mode. Secrets are mapped in `config/secrets-map.example.json` and secret values are never printed.
