#!/usr/bin/env bash
set -euo pipefail

: "${AZURE_SUBSCRIPTION_ID:=}"
if ! command -v az >/dev/null 2>&1; then
  echo "Azure CLI is not installed. Run scripts/setup/setup-azure-cli.sh first." >&2
  exit 1
fi

az version --output table
if [[ -n "$AZURE_SUBSCRIPTION_ID" ]]; then
  az account set --subscription "$AZURE_SUBSCRIPTION_ID"
fi
az account show --output table
