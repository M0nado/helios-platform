#!/usr/bin/env bash
set -Eeuo pipefail

REQUIRE_AZURE_LOGIN="${REQUIRE_AZURE_LOGIN:-false}"
INSTALL_AZURE_CLI="${INSTALL_AZURE_CLI:-false}"
AZURE_SUBSCRIPTION_ID="${AZURE_SUBSCRIPTION_ID:-}"
AZURE_TENANT_ID="${AZURE_TENANT_ID:-}"
AZURE_RESOURCE_GROUP="${AZURE_RESOURCE_GROUP:-}"

section() { printf '\n== %s ==\n' "$1"; }
warn() { printf 'WARN: %s\n' "$1" >&2; }
fail() { printf 'ERROR: %s\n' "$1" >&2; exit 1; }
install_azure_cli_linux() {
  if command -v apt-get >/dev/null 2>&1; then
    local codename
    codename="$(. /etc/os-release && printf '%s' "${VERSION_CODENAME:-${UBUNTU_CODENAME:-}}")"
    [[ -n "$codename" ]] || fail "Unable to determine Debian/Ubuntu codename for Azure CLI apt repository."
    sudo apt-get update
    sudo apt-get install -y ca-certificates curl apt-transport-https lsb-release gnupg
    sudo mkdir -p /etc/apt/keyrings
    curl -fsSL https://packages.microsoft.com/keys/microsoft.asc | sudo gpg --dearmor -o /etc/apt/keyrings/microsoft.gpg
    sudo chmod go+r /etc/apt/keyrings/microsoft.gpg
    echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/microsoft.gpg] https://packages.microsoft.com/repos/azure-cli/ $codename main"       | sudo tee /etc/apt/sources.list.d/azure-cli.list >/dev/null
    sudo apt-get update
    sudo apt-get install -y azure-cli
  elif command -v rpm >/dev/null 2>&1; then
    sudo rpm --import https://packages.microsoft.com/keys/microsoft.asc
    sudo dnf install -y https://packages.microsoft.com/config/rhel/9.0/packages-microsoft-prod.rpm || sudo yum install -y https://packages.microsoft.com/config/rhel/9.0/packages-microsoft-prod.rpm
    sudo dnf install -y azure-cli || sudo yum install -y azure-cli
  else
    fail "Automatic Azure CLI install supports apt, dnf, or yum hosts only. Install Azure CLI manually."
  fi
}

section "Azure CLI preflight"
if ! command -v az >/dev/null 2>&1; then
  if [[ "$INSTALL_AZURE_CLI" == "true" ]]; then
    install_azure_cli_linux
  else
    fail "Azure CLI is not installed. Set INSTALL_AZURE_CLI=true on Linux hosts, or install from https://learn.microsoft.com/cli/azure/install-azure-cli."
  fi
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
