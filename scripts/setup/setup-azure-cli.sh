#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'USAGE'
Usage: setup-azure-cli.sh [--login] [--service-principal] [--tenant <id>] [--subscription <id>] [--install-bicep]

Installs or validates Azure CLI for HELIOS deployment workflows on Linux/macOS/WSL.
The script is safe to run repeatedly: it detects an existing az installation,
prints the active account when available, and optionally selects a subscription.

Options:
  --login              Run az login when no active account is detected.
  --service-principal  Login with HELIOS_AZURE_CLIENT_ID/SECRET and tenant.
  --tenant <id>        Tenant for service-principal login; defaults to HELIOS_AZURE_TENANT_ID.
  --subscription <id>  Set the active Azure subscription after validation/login.
  --install-bicep      Install or upgrade the Azure CLI Bicep extension/runtime.
  -h, --help           Show this help.
USAGE
}

run_login=false
service_principal=false
install_bicep=false
tenant_id="${HELIOS_AZURE_TENANT_ID:-}"
subscription_id="${HELIOS_AZURE_SUBSCRIPTION_ID:-}"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --login)
      run_login=true
      shift
      ;;
    --service-principal)
      service_principal=true
      shift
      ;;
    --tenant)
      tenant_id="${2:-}"
      if [[ -z "$tenant_id" ]]; then
        echo "error: --tenant requires a value" >&2
        exit 2
      fi
      shift 2
      ;;
    --subscription)
      subscription_id="${2:-}"
      if [[ -z "$subscription_id" ]]; then
        echo "error: --subscription requires a value" >&2
        exit 2
      fi
      shift 2
      ;;
    --install-bicep)
      install_bicep=true
      shift
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "error: unknown argument: $1" >&2
      usage >&2
      exit 2
      ;;
  esac
done

install_azure_cli() {
  if command -v az >/dev/null 2>&1; then
    return
  fi

  case "$(uname -s)" in
    Linux)
      if command -v apt-get >/dev/null 2>&1; then
        curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
      elif command -v dnf >/dev/null 2>&1; then
        sudo rpm --import https://packages.microsoft.com/keys/microsoft.asc
        sudo dnf install -y https://packages.microsoft.com/config/rhel/9.0/packages-microsoft-prod.rpm
        sudo dnf install -y azure-cli
      else
        echo "error: unsupported Linux package manager. Install Azure CLI manually: https://learn.microsoft.com/cli/azure/install-azure-cli" >&2
        exit 1
      fi
      ;;
    Darwin)
      if ! command -v brew >/dev/null 2>&1; then
        echo "error: Homebrew is required to install Azure CLI on macOS." >&2
        exit 1
      fi
      brew update
      brew install azure-cli
      ;;
    *)
      echo "error: unsupported OS. Install Azure CLI manually: https://learn.microsoft.com/cli/azure/install-azure-cli" >&2
      exit 1
      ;;
  esac
}

install_azure_cli

echo "Azure CLI: $(az version --query '\"azure-cli\"' -o tsv)"

if ! az account show --only-show-errors >/dev/null 2>&1; then
  if [[ "$service_principal" == true ]]; then
    if [[ -z "${HELIOS_AZURE_CLIENT_ID:-}" || -z "${HELIOS_AZURE_CLIENT_SECRET:-}" || -z "$tenant_id" ]]; then
      echo "error: service-principal login requires HELIOS_AZURE_CLIENT_ID, HELIOS_AZURE_CLIENT_SECRET, and --tenant or HELIOS_AZURE_TENANT_ID" >&2
      exit 2
    fi
    az login --service-principal \
      --username "$HELIOS_AZURE_CLIENT_ID" \
      --password "$HELIOS_AZURE_CLIENT_SECRET" \
      --tenant "$tenant_id" >/dev/null
  elif [[ "$run_login" == true ]]; then
    az login --use-device-code
  else
    echo "No Azure account is active. Re-run with --login, use --service-principal, or authenticate with az login." >&2
  fi
fi

if [[ -n "$subscription_id" ]]; then
  az account set --subscription "$subscription_id"
fi

if [[ "$install_bicep" == true ]]; then
  az bicep install >/dev/null
  az bicep upgrade >/dev/null
  echo "Bicep: $(az bicep version)"
fi

if az account show --only-show-errors >/dev/null 2>&1; then
  az account show --query '{name:name, tenantId:tenantId, subscription:id, user:user.name}' -o table
fi
