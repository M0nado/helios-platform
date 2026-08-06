#!/usr/bin/env bash
# HELIOS Full-Access Azure CLI Session Bootstrap (POSIX shell)
# Prepares an operator shell for full read/write HELIOS repository work and Azure CLI automation.

set -euo pipefail

WORKSPACE_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
SUBSCRIPTION_ID="${HELIOS_AZURE_SUBSCRIPTION_ID:-}"
TENANT_ID="${HELIOS_AZURE_TENANT_ID:-}"
LOCATION="${HELIOS_LOCATION:-eastus2}"
ENVIRONMENT="${HELIOS_ENVIRONMENT:-production}"
SKIP_LOGIN=0
USE_DEVICE_CODE=0
PERSIST_ENV=0

usage() {
  cat <<USAGE
Usage: $0 [options]

Options:
  --workspace-root PATH    Workspace root to scan for Git repositories.
  --subscription-id ID     Azure subscription ID to select.
  --tenant-id ID           Azure tenant ID to use during login.
  --location NAME          HELIOS Azure location (default: eastus2).
  --environment NAME       development, staging, or production (default: production).
  --use-device-code        Use Azure CLI device code login.
  --skip-login             Do not run az login; use current Azure CLI context.
  --persist-env            Append HELIOS exports to ~/.helios/env.
  -h, --help               Show this help.
USAGE
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --workspace-root) WORKSPACE_ROOT="$2"; shift 2 ;;
    --subscription-id) SUBSCRIPTION_ID="$2"; shift 2 ;;
    --tenant-id) TENANT_ID="$2"; shift 2 ;;
    --location) LOCATION="$2"; shift 2 ;;
    --environment) ENVIRONMENT="$2"; shift 2 ;;
    --use-device-code) USE_DEVICE_CODE=1; shift ;;
    --skip-login) SKIP_LOGIN=1; shift ;;
    --persist-env) PERSIST_ENV=1; shift ;;
    -h|--help) usage; exit 0 ;;
    *) echo "Unknown option: $1" >&2; usage; exit 2 ;;
  esac
done

case "$ENVIRONMENT" in
  development|staging|production) ;;
  *) echo "Invalid --environment '$ENVIRONMENT'" >&2; exit 2 ;;
esac

require_command() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "Required command '$1' was not found. $2" >&2
    exit 1
  fi
}

export_and_optionally_persist() {
  local name="$1"
  local value="$2"
  export "$name=$value"
  if [[ "$PERSIST_ENV" -eq 1 ]]; then
    mkdir -p "$HOME/.helios"
    grep -v "^export ${name}=" "$HOME/.helios/env" 2>/dev/null > "$HOME/.helios/env.tmp" || true
    printf 'export %s=%q\n' "$name" "$value" >> "$HOME/.helios/env.tmp"
    mv "$HOME/.helios/env.tmp" "$HOME/.helios/env"
  fi
}

echo "[HELIOS] Validating tooling"
require_command git "Install Git, then rerun this script."
require_command az "Install Azure CLI, then rerun this script."

export_and_optionally_persist HELIOS_SESSION_ACCESS_MODE full-read-write
export_and_optionally_persist HELIOS_REPO_WRITE_ENABLED true
export_and_optionally_persist HELIOS_BRANCH_INTEGRATION_MODE merge-with-review
export_and_optionally_persist HELIOS_ENVIRONMENT "$ENVIRONMENT"
export_and_optionally_persist HELIOS_LOCATION "$LOCATION"

if [[ "$SKIP_LOGIN" -eq 0 ]]; then
  echo "[HELIOS] Authenticating Azure CLI"
  login_args=(login)
  [[ "$USE_DEVICE_CODE" -eq 1 ]] && login_args+=(--use-device-code)
  [[ -n "$TENANT_ID" ]] && login_args+=(--tenant "$TENANT_ID")
  az "${login_args[@]}" >/dev/null
fi

if [[ -n "$SUBSCRIPTION_ID" ]]; then
  echo "[HELIOS] Selecting Azure subscription $SUBSCRIPTION_ID"
  az account set --subscription "$SUBSCRIPTION_ID"
fi

account_json="$(az account show --output json)"
export_and_optionally_persist HELIOS_AZURE_SUBSCRIPTION_ID "$(python3 -c 'import json,sys; print(json.load(sys.stdin)["id"])' <<<"$account_json")"
export_and_optionally_persist HELIOS_AZURE_TENANT_ID "$(python3 -c 'import json,sys; print(json.load(sys.stdin)["tenantId"])' <<<"$account_json")"

echo "[HELIOS] Discovering Git repositories under $WORKSPACE_ROOT"
python3 "$(dirname "${BASH_SOURCE[0]}")/helios_repo_integrator.py" --workspace-root "$WORKSPACE_ROOT" --mode inventory

echo "[HELIOS] Full read/write Azure CLI session is ready. Source ~/.helios/env in future shells if --persist-env was used."
