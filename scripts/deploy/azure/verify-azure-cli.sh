#!/usr/bin/env bash
set -euo pipefail

: "${AZURE_CLIENT_ID:?AZURE_CLIENT_ID is required for OIDC login context}"
: "${AZURE_TENANT_ID:?AZURE_TENANT_ID is required}"
: "${AZURE_SUBSCRIPTION_ID:?AZURE_SUBSCRIPTION_ID is required}"

az account show --output json >/tmp/helios-azure-account.json
az account set --subscription "$AZURE_SUBSCRIPTION_ID"
az group list --query '[].{name:name, location:location}' --output json > artifacts/deploy/azure-resource-groups.json
cat artifacts/deploy/azure-resource-groups.json
