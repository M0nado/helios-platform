#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'EOF'
Usage: scripts/azure/sync-keyvault-secrets.sh --vault <name> [--dry-run]

Syncs supported local environment variables into Azure Key Vault secret names used by HELIOS.
Secret values are never printed. Requires: az login and Key Vault RBAC permissions.
EOF
}

VAULT="${HELIOS_KEYVAULT_NAME:-}"
DRY_RUN="false"
while [[ $# -gt 0 ]]; do
  case "$1" in
    --vault) VAULT="${2:-}"; shift 2 ;;
    --dry-run) DRY_RUN="true"; shift ;;
    -h|--help) usage; exit 0 ;;
    *) echo "Unknown argument: $1" >&2; usage; exit 2 ;;
  esac
done

if [[ -z "$VAULT" ]]; then
  echo "Set --vault or HELIOS_KEYVAULT_NAME." >&2
  exit 2
fi
if ! command -v az >/dev/null 2>&1; then
  echo "az not found. Run scripts/setup/helios-dev.sh first or use Azure Cloud Shell." >&2
  exit 127
fi
az account show >/dev/null

sync_secret() {
  local env_name="$1"
  local secret_name="$2"
  if [[ -z "${!env_name:-}" ]]; then
    echo "skip $secret_name ($env_name unset)"
    return 0
  fi
  if [[ "$DRY_RUN" == "true" ]]; then
    echo "would sync $secret_name from $env_name"
  else
    az keyvault secret set --vault-name "$VAULT" --name "$secret_name" --value "${!env_name}" --only-show-errors >/dev/null
    echo "synced $secret_name from $env_name"
  fi
}

if command -v python3 >/dev/null 2>&1 && [[ -f config/secrets-map.example.json ]]; then
  while IFS=$'\t' read -r env_name secret_name; do
    sync_secret "$env_name" "$secret_name"
  done < <(python3 - <<'PY'
import json
from pathlib import Path
data=json.loads(Path('config/secrets-map.example.json').read_text())
for item in data.get('secrets',[]):
    print(f"{item['localEnv']}\t{item['keyVaultSecret']}")
PY
)
else
  sync_secret OPENAI_API_KEY OPENAI-API-KEY
  sync_secret AZURE_OPENAI_ENDPOINT AZURE-OPENAI-ENDPOINT
  sync_secret AZURE_OPENAI_API_KEY AZURE-OPENAI-API-KEY
  sync_secret SLACK_WEBHOOK_URL SLACK-WEBHOOK-URL
  sync_secret M365_TENANT_ID M365-TENANT-ID
  sync_secret M365_CLIENT_ID M365-CLIENT-ID
fi
