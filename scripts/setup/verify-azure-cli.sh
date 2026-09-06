#!/usr/bin/env bash
set -euo pipefail

# Read-only verification: never install tools, create credentials, select a
# subscription, or deploy resources.
failures=0

check_command() {
  local name="$1" url="$2"
  if command -v "$name" >/dev/null 2>&1; then
    printf '✅ %s: %s\n' "$name" "$(command -v "$name")"
  else
    printf '❌ %s is not installed. See %s\n' "$name" "$url" >&2
    failures=$((failures + 1))
  fi
}

check_command az "https://learn.microsoft.com/cli/azure/install-azure-cli"
check_command azd "https://learn.microsoft.com/azure/developer/azure-developer-cli/install-azd"

if command -v az >/dev/null 2>&1; then
  version="$(az version --query '"azure-cli"' -o tsv 2>/dev/null || true)"
  if [[ -z "$version" || "${version%%.*}" -lt 2 ]]; then
    printf '❌ Azure CLI version 2 or newer is required (found: %s).\n' "${version:-unknown}" >&2
    failures=$((failures + 1))
  else
    printf '✅ Azure CLI version: %s\n' "$version"
  fi

  if account="$(az account show --only-show-errors -o json 2>/dev/null)"; then
    AZURE_ACCOUNT_JSON="$account" python3 - <<'PY'
import json
import os

account = json.loads(os.environ["AZURE_ACCOUNT_JSON"])
print(f"✅ Signed in tenant: {account.get('tenantId', '<unknown>')}")
print(f"✅ Selected subscription: {account.get('name', '<unknown>')} ({account.get('id', '<unknown>')})")
print(f"ℹ️  Identity: {account.get('user', {}).get('name', '<unknown>')}")
PY
  else
    printf '⚠️  Not signed in. Local users may run az login. CI must use GitHub OIDC.\n'
  fi

  if bicep_version="$(az bicep version 2>/dev/null)"; then
    printf '✅ %s\n' "$bicep_version"
  else
    printf '❌ Azure CLI Bicep support is unavailable.\n' >&2
    failures=$((failures + 1))
  fi
fi

if (( failures > 0 )); then
  printf 'Azure tooling verification failed with %d problem(s). No changes were made.\n' "$failures" >&2
  exit 1
fi

printf 'Azure tooling verification completed. No Azure state was changed.\n'
