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
