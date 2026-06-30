#!/usr/bin/env bash
set -Eeuo pipefail

REQUIRE_AZURE_LOGIN="${REQUIRE_AZURE_LOGIN:-false}"
AZURE_SUBSCRIPTION_ID="${AZURE_SUBSCRIPTION_ID:-}"
AZURE_TENANT_ID="${AZURE_TENANT_ID:-}"
AZURE_RESOURCE_GROUP="${AZURE_RESOURCE_GROUP:-}"

section() { printf '\n== %s ==\n' "$1"; }
warn() { printf 'WARN: %s\n' "$1" >&2; }
fail() { printf 'ERROR: %s\n' "$1" >&2; exit 1; }

section "Azure CLI preflight"
if ! command -v az >/dev/null 2>&1; then
  fail "Azure CLI is not installed. Install it from https://learn.microsoft.com/cli/azure/install-azure-cli and rerun autosetup."
fi

az --version | sed -n '1,8p'

if ! az account show >/tmp/helios-az-account.json 2>/tmp/helios-az-account.err; then
  if [[ "$REQUIRE_AZURE_LOGIN" == "true" ]]; then
    cat /tmp/helios-az-account.err >&2 || true
    fail "Azure CLI is installed but not logged in. Run 'az login' locally or configure azure/login OIDC in GitHub Actions."
  fi
  warn "Azure CLI is not logged in; continuing because REQUIRE_AZURE_LOGIN is not true."
  exit 0
fi

ACCOUNT_NAME=$(az account show --query name -o tsv)
ACCOUNT_ID=$(az account show --query id -o tsv)
TENANT_ID=$(az account show --query tenantId -o tsv)
printf 'Active subscription: %s (%s)\n' "$ACCOUNT_NAME" "$ACCOUNT_ID"
printf 'Tenant: %s\n' "$TENANT_ID"

if [[ -n "$AZURE_SUBSCRIPTION_ID" && "$ACCOUNT_ID" != "$AZURE_SUBSCRIPTION_ID" ]]; then
  fail "Active subscription '$ACCOUNT_ID' does not match AZURE_SUBSCRIPTION_ID '$AZURE_SUBSCRIPTION_ID'."
fi

if [[ -n "$AZURE_TENANT_ID" && "$TENANT_ID" != "$AZURE_TENANT_ID" ]]; then
  fail "Active tenant '$TENANT_ID' does not match AZURE_TENANT_ID '$AZURE_TENANT_ID'."
fi

if [[ -n "$AZURE_RESOURCE_GROUP" ]]; then
  az group show --name "$AZURE_RESOURCE_GROUP" >/dev/null \
    || fail "Azure resource group '$AZURE_RESOURCE_GROUP' was not found in the active subscription."
  printf 'Resource group available: %s\n' "$AZURE_RESOURCE_GROUP"
fi

printf 'Azure CLI preflight completed.\n'
