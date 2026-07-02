# HELIOS Command Center

This is the single entry point for local, GitHub-hosted, Azure, AI, Codex, branch intelligence, build graph, and dashboard operations.

## Fast path

```bash
./helios.sh setup
./helios.sh dashboard
./helios.sh status
./helios.sh all
```

## Core commands

| Command | Purpose |
| --- | --- |
| `./helios.sh setup` | Bootstrap local tools and regenerate base reports. |
| `./helios.sh dashboard` | Serve the local dashboard. |
| `./helios.sh status` | Generate GitHub/Azure/AI/Codex control summary. |
| `./helios.sh github` | Generate GitHub inventory. |
| `./helios.sh azure` | Generate Azure inventory. |
| `./helios.sh build` | Generate or run the build graph. |
| `./helios.sh codex` | Generate Codex-ready task packets. |
| `./helios.sh recommendations` | Generate branch merge/prune recommendations. |
| `./helios.sh all` | Run the safe reporting pipeline. |

## Safety

All commands are read-only/reporting by default unless a script explicitly documents an opt-in apply mode. Secrets are mapped in `config/secrets-map.example.json` and secret values are never printed.
