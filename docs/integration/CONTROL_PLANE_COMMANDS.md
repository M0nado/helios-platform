# HELIOS Control Plane Commands

Use this page as the safe command map for GitHub, Azure, OpenAI/Azure OpenAI, Codex, Slack, and Microsoft 365 readiness.

## One command for your machine

```bash
scripts/setup/helios-dev.sh --serve
```

This bootstraps local tools, regenerates reports, writes a control summary, and serves the dashboard.

## Generate a command/control summary

```bash
python3 scripts/control/helios-control.py
python3 scripts/control/helios-control.py github
python3 scripts/control/helios-control.py azure
python3 scripts/control/helios-control.py ai
```

Outputs:

- `reports/control-plane/control-summary.json`
- `reports/control-plane/control-summary.md`

## GitHub control

```bash
gh auth login
gh workflow run helios-control-plane.yml -f publish_pages=false -f update_wiki=true
gh run list --limit 10
```

## Azure control

```bash
az login
az group create --name <resource-group> --location <region>
az deployment group what-if --resource-group <resource-group> --template-file infra/azure/main.bicep --parameters @infra/azure/parameters/dev.json
scripts/azure/sync-keyvault-secrets.sh --vault <vault-name> --dry-run
```

## AI/Codex control

```bash
python3 scripts/ai/enrich-ideas.py
python3 scripts/integrations/check-connections.py
python3 scripts/control/helios-control.py ai
```

The current AI/Codex path is safe-by-default: it checks local CLI/env readiness and writes report metadata, but it does not send branch ideas, code, prompts, or secrets to external providers.
