#!/usr/bin/env bash
set -euo pipefail

mode="status"
repo="${HELIOS_GITHUB_REPOSITORY:-M0nado/helios-platform}"
subscription="${HELIOS_AZURE_SUBSCRIPTION:-}"

usage() {
  cat <<'USAGE'
Usage: configure-cloud-auth.sh [--status|--interactive|--ci-check] [--repo OWNER/REPO] [--subscription ID]

  --status       Report local GitHub and Azure authentication readiness (default).
  --interactive  Start GitHub browser login and Azure device-code login.
  --ci-check     Validate that GitHub Actions OIDC inputs are present; never logs in.

Credentials are handled only by gh/az credential stores. Tokens are never printed or written to the repository.
USAGE
}

while (($#)); do
  case "$1" in
    --status|--interactive|--ci-check) mode="${1#--}"; shift ;;
    --repo) repo="${2:?--repo requires OWNER/REPO}"; shift 2 ;;
    --subscription) subscription="${2:?--subscription requires an ID}"; shift 2 ;;
    -h|--help) usage; exit 0 ;;
    *) echo "Unknown option: $1" >&2; usage >&2; exit 2 ;;
  esac
done

[[ "$repo" =~ ^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$ ]] || { echo "Invalid repository: $repo" >&2; exit 2; }

status() {
  local failures=0
  if command -v gh >/dev/null 2>&1 && gh auth status --hostname github.com >/dev/null 2>&1; then
    echo "READY github account=$(gh api user --jq .login) repository=$repo"
  else
    echo "ACTION github run: gh auth login --hostname github.com --web --git-protocol https" >&2
    failures=1
  fi
  if command -v az >/dev/null 2>&1 && az account show --only-show-errors >/dev/null 2>&1; then
    echo "READY azure subscription=$(az account show --query id --output tsv --only-show-errors)"
  else
    echo "ACTION azure run: az login --use-device-code" >&2
    failures=1
  fi
  return "$failures"
}

case "$mode" in
  status) status ;;
  interactive)
    command -v gh >/dev/null 2>&1 || { echo "GitHub CLI is not installed." >&2; exit 127; }
    command -v az >/dev/null 2>&1 || { echo "Azure CLI is not installed." >&2; exit 127; }
    gh auth login --hostname github.com --web --git-protocol https --scopes repo,read:org,workflow
    gh repo set-default "$repo"
    az login --use-device-code --output none
    if [[ -n "$subscription" ]]; then az account set --subscription "$subscription"; fi
    status
    ;;
  ci-check)
    [[ "${GITHUB_ACTIONS:-}" == "true" ]] || { echo "--ci-check must run in GitHub Actions." >&2; exit 2; }
    missing=0
    for name in AZURE_CLIENT_ID AZURE_TENANT_ID AZURE_SUBSCRIPTION_ID; do
      [[ -n "${!name:-}" ]] || { echo "Missing GitHub environment variable: $name" >&2; missing=1; }
    done
    [[ -n "${ACTIONS_ID_TOKEN_REQUEST_URL:-}" ]] || { echo "OIDC is unavailable; grant id-token: write to this job." >&2; missing=1; }
    ((missing == 0)) || exit 1
    echo "READY GitHub Actions OIDC inputs are present; no interactive credentials used."
    ;;
esac
