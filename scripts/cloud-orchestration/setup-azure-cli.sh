#!/usr/bin/env bash
# HELIOS/Hermes Azure CLI bootstrap helper.
# Installs Azure CLI where possible, verifies the install, and optionally logs in.
set -euo pipefail

usage() {
  cat <<'USAGE'
Usage: setup-azure-cli.sh [--login] [--tenant TENANT_ID] [--subscription SUBSCRIPTION_ID]

Installs or verifies Azure CLI for HELIOS cloud orchestration workflows.
Options:
  --login                         Run az login after installation/verification.
  --tenant TENANT_ID              Tenant to use with az login.
  --subscription SUBSCRIPTION_ID  Subscription to select after login.
  -h, --help                      Show this help.
USAGE
}

LOGIN=false
TENANT=""
SUBSCRIPTION=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    --login) LOGIN=true; shift ;;
    --tenant) TENANT="${2:?--tenant requires a value}"; shift 2 ;;
    --subscription) SUBSCRIPTION="${2:?--subscription requires a value}"; shift 2 ;;
    -h|--help) usage; exit 0 ;;
    *) echo "Unknown argument: $1" >&2; usage; exit 2 ;;
  esac
done

install_az() {
  if command -v az >/dev/null 2>&1; then
    echo "Azure CLI already installed: $(az version --query azure-cli -o tsv 2>/dev/null || az --version | head -1)"
    return 0
  fi

  if [[ "$OSTYPE" == "linux-gnu"* ]]; then
    if command -v apt-get >/dev/null 2>&1; then
      curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
    elif command -v dnf >/dev/null 2>&1; then
      sudo rpm --import https://packages.microsoft.com/keys/microsoft.asc
      sudo dnf install -y https://packages.microsoft.com/config/rhel/9.0/packages-microsoft-prod.rpm
      sudo dnf install -y azure-cli
    elif command -v yum >/dev/null 2>&1; then
      sudo rpm --import https://packages.microsoft.com/keys/microsoft.asc
      sudo yum install -y https://packages.microsoft.com/config/rhel/9.0/packages-microsoft-prod.rpm
      sudo yum install -y azure-cli
    else
      echo "Unsupported Linux package manager. Install Azure CLI manually: https://learn.microsoft.com/cli/azure/install-azure-cli" >&2
      exit 1
    fi
  elif [[ "$OSTYPE" == "darwin"* ]]; then
    command -v brew >/dev/null 2>&1 || { echo "Homebrew is required on macOS." >&2; exit 1; }
    brew update && brew install azure-cli
  else
    echo "Unsupported OS for automatic install. Install Azure CLI manually: https://learn.microsoft.com/cli/azure/install-azure-cli" >&2
    exit 1
  fi
}

install_az
az extension add --name account --upgrade >/dev/null 2>&1 || true
az extension add --name application-insights --upgrade >/dev/null 2>&1 || true
az extension add --name log-analytics --upgrade >/dev/null 2>&1 || true

if [[ "$LOGIN" == true ]]; then
  login_args=()
  [[ -n "$TENANT" ]] && login_args+=(--tenant "$TENANT")
  az login "${login_args[@]}"
fi

if [[ -n "$SUBSCRIPTION" ]]; then
  az account set --subscription "$SUBSCRIPTION"
fi

az account show --query '{tenantId:tenantId, subscription:id, name:name}' -o table 2>/dev/null || \
  echo "Azure CLI is installed. Run with --login and optionally --subscription to bind an account."
