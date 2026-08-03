# Cloud Shell, GitHub CLI, Azure CLI, and API Integration

Use this guide for non-local HELIOS operations in Azure Cloud Shell, GitHub Codespaces, or a hosted runner.

## Secrets and keys

Preferred storage order:

1. GitHub Actions secrets for workflow-only values.
2. Azure Key Vault for shared cloud/runtime values.
3. Local environment variables only for development.

Never commit real API keys or webhook URLs.

## Quick start

```bash
gh auth login
az login
scripts/cloudshell/helios-cloudshell.sh
```

## Key Vault secret names

Use the names from `config/integrations.example.json`:

- `OPENAI-API-KEY`
- `AZURE-OPENAI-ENDPOINT`
- `AZURE-OPENAI-API-KEY`
- `SLACK-WEBHOOK-URL`
- `M365-TENANT-ID`

## GitHub Actions hooks

- `HELIOS_PLATFORM_REMOTE_URL`
- `HELIOS_CONTROL_REMOTE_URL`
- `HERMES_FLEET_REMOTE_URL`
- `XCORE_REMOTE_URL`
- `OPENAI_API_KEY`
- `AZURE_OPENAI_ENDPOINT`
- `AZURE_OPENAI_API_KEY`

## One-command local dashboard

For the simplest local setup, run:

```bash
scripts/setup/helios-dev.sh --serve
```

This bootstraps local CLIs, regenerates Branch Intelligence reports, updates graph/wiki exports, writes safe AI-enrichment readiness metadata, and serves `status-site/` at <http://127.0.0.1:8787/>.

Refresh the reports without restarting the server:

```bash
curl -X POST http://127.0.0.1:8787/rebuild
```

## Azure Key Vault secret sync

After `az login` and deploying `infra/azure/main.bicep`, sync local environment variables into Key Vault without printing secret values:

```bash
scripts/azure/sync-keyvault-secrets.sh --vault <vault-name> --dry-run
scripts/azure/sync-keyvault-secrets.sh --vault <vault-name>
```

Supported variables are `OPENAI_API_KEY`, `AZURE_OPENAI_ENDPOINT`, `AZURE_OPENAI_API_KEY`, `SLACK_WEBHOOK_URL`, `M365_TENANT_ID`, and `M365_CLIENT_ID`.

## GitHub-hosted control plane

Use the `HELIOS Control Plane` workflow when you want GitHub-hosted runners to build analytics, validate native/XCore CMake, regenerate branch/idea reports, and publish the Pages dashboard artifact. Keep real remote URLs and provider credentials in GitHub Secrets or Azure Key Vault; do not commit them to the manifest.
