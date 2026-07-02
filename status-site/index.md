# HELIOS Control Plane Dashboard

This site is the lightweight web control surface for Branch Intelligence, integration readiness, Azure readiness, and agent work queues.

## Fast start

```bash
scripts/setup/helios-dev.sh --serve
```

Open <http://127.0.0.1:8787/>. Regenerate reports while the local server is running:

```bash
curl -X POST http://127.0.0.1:8787/rebuild
```

## Reports

- [Branch Intelligence](reports/dashboard.md)
- [Branch Graphs](reports/graphs.md)
- [Idea Impact](reports/idea-impact-summary.md)
- [Agent Work Queue](reports/agent-work-queue.md)
- [Module Ranking](reports/branch-ranking.md)
- [Wiki Export](wiki-export/Branch-Intelligence.md)

## Cloud hooks

- GitHub Actions: `HELIOS Control Plane`
- Azure templates: `infra/azure/main.bicep`
- Key Vault sync: `scripts/azure/sync-keyvault-secrets.sh --vault <vault-name> --dry-run`
- Cloud Shell: `scripts/cloudshell/helios-cloudshell.sh`
