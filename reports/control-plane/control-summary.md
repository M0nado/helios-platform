# HELIOS Control Plane Summary

Generated: `2026-07-02T19:06:42.534028+00:00`

## Local quick start

```bash
scripts/setup/helios-dev.sh --serve
```

## Github Control

- ❌ **auth**: gh not found
- ❌ **repo**: gh not found
- ❌ **workflows**: gh not found
- ❌ **secrets**: gh not found
### Safe commands

- `gh auth login`
- `gh workflow run helios-control-plane.yml -f publish_pages=false -f update_wiki=true`
- `gh run list --limit 10`


## Azure Control

- ❌ **account**: az not found
- ❌ **bicep**: az not found
- ❌ **groups**: az not found
### Safe commands

- `az login`
- `az group create --name <resource-group> --location <region>`
- `az deployment group what-if --resource-group <resource-group> --template-file infra/azure/main.bicep --parameters @infra/azure/parameters/dev.json`
- `scripts/azure/sync-keyvault-secrets.sh --vault <vault-name> --dry-run`


## Ai Control

- ℹ️ **openai**: 0/1 configured
- ℹ️ **azureOpenAI**: 0/2 configured
- ℹ️ **claude**: 0/1 configured
- ℹ️ **codex**: 2/2 configured
- ℹ️ **microsoft365**: 0/2 configured
- ℹ️ **slack**: 0/1 configured
### Safe commands

- `python3 scripts/ai/enrich-ideas.py`
- `python3 scripts/integrations/check-connections.py`
- `python3 scripts/control/helios-control.py ai`
- `scripts/azure/sync-keyvault-secrets.sh --vault <vault-name> --dry-run`
