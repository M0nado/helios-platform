# Helios One-File Autosetup
# Generates the complete Helios/Hermes autosetup system without running merges.

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Write-Host "=== Helios Hermes Autosetup Generator Starting ==="

$folders = @(
    "scripts",
    ".github/workflows",
    "autosetup",
    "autosetup/modules",
    "autosetup/fleet",
    "autosetup/xcore",
    "autosetup/hermes",
    "autosetup/os"
)

foreach ($folder in $folders) {
    New-Item -ItemType Directory -Force -Path $folder | Out-Null
}

@'
repos:
  - M0nado/helios-platform
  - M0nado/helios-control
  - M0nado/hermes-core
  - M0nado/hermes-fleet-production
  - M0nado/hermes-fleet-public
  - M0nado/hermes-fleet-private
  - M0nado/xcore-agent
  - M0nado/usb-wizard
  - M0nado/os-setup

branches:
  clean: hermes-clean
  merge: hermes-merge

setup:
  baseDirectory: repos
  defaultRemoteBranch: main
  requireAzureLogin: false
  requireGitHubToken: true
  mergeAllRemoteBranches: true
  summaryPath: autosetup/summary.md

focus:
  primary:
    - helios-control
    - hermes-fleet-production
  languages:
    csharp: frontend orchestration, installer, security, Azure integration
    winui3: Windows desktop shell and setup UX
    cpp: performance backend and native optimization hooks
    fsharp: math predictions, analytics, and parallel computation models
    python: AIHub integration and automation adapters
    xcore: Hermes specialist agent setup and fleet coordination

'@ | Set-Content -Path "autosetup/manifest.yaml" -Encoding UTF8

@'
#!/usr/bin/env bash
set -Eeuo pipefail

MANIFEST="${MANIFEST:-autosetup/manifest.yaml}"
BASE="${BASE:-repos}"
SUMMARY="${SUMMARY_PATH:-autosetup/summary.md}"
DEFAULT_REMOTE_BRANCH="${DEFAULT_REMOTE_BRANCH:-main}"
CLEAN_BRANCH="${CLEAN_BRANCH:-hermes-clean}"
MERGE_BRANCH="${MERGE_BRANCH:-hermes-merge}"
REQUIRE_AZURE_LOGIN="${REQUIRE_AZURE_LOGIN:-false}"
MERGE_ALL_REMOTE_BRANCHES="${MERGE_ALL_REMOTE_BRANCHES:-true}"

mkdir -p "$BASE" "$(dirname "$SUMMARY")"
printf '# Helios/Hermes autosetup summary\n\nStarted: %s\n\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)" > "$SUMMARY"

need() { command -v "$1" >/dev/null 2>&1 || { echo "Missing required command: $1" >&2; exit 1; }; }
repo_target() {
  local repo="$1" owner name
  owner="${repo%%/*}"
  name="${repo##*/}"
  printf '%s/%s/%s' "$BASE" "$owner" "$name"
}
log_summary() { printf '%s\n' "$1" | tee -a "$SUMMARY"; }

need git
need gh
need yq

export REQUIRE_AZURE_LOGIN
./scripts/azure-preflight.sh | tee -a "$SUMMARY"

mapfile -t repos < <(yq -r '.repos[]' "$MANIFEST")
if [[ "${#repos[@]}" -eq 0 ]]; then
  echo "No repositories listed in $MANIFEST" >&2
  exit 1
fi

log_summary "\n## Repository synchronization"
for repo in "${repos[@]}"; do
  target="$(repo_target "$repo")"
  mkdir -p "$(dirname "$target")"
  log_summary "\n### $repo"
  if [[ -d "$target/.git" ]]; then
    git -C "$target" remote set-url origin "https://github.com/$repo.git" || true
    git -C "$target" fetch --all --prune
    log_summary "- [x] fetched existing clone at \`$target\`"
  else
    gh repo clone "$repo" "$target" -- --quiet
    git -C "$target" fetch --all --prune
    log_summary "- [x] cloned to \`$target\`"
  fi

  if git -C "$target" show-ref --verify --quiet "refs/remotes/origin/$DEFAULT_REMOTE_BRANCH"; then
    remote_branch="$DEFAULT_REMOTE_BRANCH"
  else
    remote_branch="$(git -C "$target" remote show origin | awk '/HEAD branch/ {print $NF}')"
  fi
  [[ -n "$remote_branch" ]] || { echo "Unable to determine remote branch for $repo" >&2; exit 1; }

  git -C "$target" checkout -B "$CLEAN_BRANCH" "origin/$remote_branch"
  log_summary "- [x] reset \`$CLEAN_BRANCH\` from \`origin/$remote_branch\`"

  git -C "$target" checkout -B "$MERGE_BRANCH"
  git -C "$target" merge --no-edit -X theirs "$CLEAN_BRANCH"

  if [[ "$MERGE_ALL_REMOTE_BRANCHES" == "true" ]]; then
    mapfile -t remote_branches < <(git -C "$target" for-each-ref --format='%(refname:short)' refs/remotes/origin       | sed '/^origin\/HEAD$/d'       | sed "s#^origin/##"       | sort -u)
    for branch in "${remote_branches[@]}"; do
      [[ "$branch" == "$remote_branch" ]] && continue
      log_summary "- [ ] merging remote branch \`origin/$branch\`"
      git -C "$target" merge --no-edit -X theirs "origin/$branch"
      log_summary "- [x] merged \`origin/$branch\`"
    done
  fi

  log_summary "- [x] prepared merge branch \`$MERGE_BRANCH\`"
done

BASE="$BASE" SUMMARY_PATH="$SUMMARY" ./scripts/validate-consolidation.sh "$BASE"

log_summary "\n## Next manual integration gates"
log_summary "- Review helios-control and hermes-fleet-production first."
log_summary "- Wire C# frontend/install/security, WinUI 3 shell, C++ performance backend, F# analytics, Python AIHub, and Hermes XCore specialists by repo-specific PRs."
log_summary "- Run build/test/security validation in each cloned repository before pushing merge branches."
log_summary "\nCompleted: $(date -u +%Y-%m-%dT%H:%M:%SZ)"

'@ | Set-Content -Path "scripts/autosetup.sh" -Encoding UTF8

@'
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

'@ | Set-Content -Path "scripts/azure-preflight.sh" -Encoding UTF8

@'
#!/usr/bin/env bash
set -Eeuo pipefail

BASE="${1:-${BASE:-repos}}"
SUMMARY="${SUMMARY_PATH:-autosetup/summary.md}"
mkdir -p "$(dirname "$SUMMARY")"

section() { printf '\n## %s\n' "$1" | tee -a "$SUMMARY"; }
check_glob() {
  local label="$1" pattern="$2"
  if find "$BASE" -path "$pattern" -print -quit 2>/dev/null | grep -q .; then
    printf -- '- [x] %s (%s)\n' "$label" "$pattern" | tee -a "$SUMMARY"
  else
    printf -- '- [ ] %s missing (%s)\n' "$label" "$pattern" | tee -a "$SUMMARY"
  fi
}

section "Consolidation validation"
printf 'Base directory: `%s`\n' "$BASE" | tee -a "$SUMMARY"

check_glob 'C# core/platform projects' '*/**/*.csproj'
check_glob 'WinUI/WPF desktop surface' '*/**/*.xaml'
check_glob 'C++ performance backend candidates' '*/**/*.[ch]pp'
check_glob 'F# analytics/math candidates' '*/**/*.fsproj'
check_glob 'Python AIHub/automation candidates' '*/**/*.py'
check_glob 'Azure IaC/config candidates' '*/**/*azure*'
check_glob 'Hermes fleet assets' '*/**/*hermes*'
check_glob 'XCore agent assets' '*/**/*xcore*'

printf '\nValidation complete. Missing optional language buckets should become follow-up tasks, not silent autosetup success.\n' | tee -a "$SUMMARY"

'@ | Set-Content -Path "scripts/validate-consolidation.sh" -Encoding UTF8

@'
name: Helios Hermes Autosetup

on:
  workflow_dispatch:
    inputs:
      require_azure_login:
        description: Require Azure CLI account validation after azure/login
        required: true
        default: 'false'
        type: choice
        options: ['false', 'true']
      default_remote_branch:
        description: Default remote branch to normalize from
        required: true
        default: main

permissions:
  contents: read
  id-token: write

jobs:
  autosetup:
    runs-on: ubuntu-latest
    env:
      GH_TOKEN: ${{ secrets.HELIOS_AUTOMATION_PAT || secrets.GITHUB_TOKEN }}
      REQUIRE_AZURE_LOGIN: ${{ inputs.require_azure_login }}
      DEFAULT_REMOTE_BRANCH: ${{ inputs.default_remote_branch }}
      AZURE_SUBSCRIPTION_ID: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
      AZURE_TENANT_ID: ${{ secrets.AZURE_TENANT_ID }}
    steps:
      - uses: actions/checkout@v4

      - name: Install yq
        run: |
          sudo apt-get update
          sudo apt-get install -y yq
          gh --version
          yq --version

      - name: Azure Login (optional OIDC)
        if: ${{ inputs.require_azure_login == 'true' }}
        uses: azure/login@v2
        with:
          client-id: ${{ secrets.AZURE_CLIENT_ID }}
          tenant-id: ${{ secrets.AZURE_TENANT_ID }}
          subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}

      - name: Run autosetup
        run: ./scripts/autosetup.sh

      - name: Upload autosetup summary
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: autosetup-summary
          path: autosetup/summary.md

'@ | Set-Content -Path ".github/workflows/autosetup.yml" -Encoding UTF8

Write-Host "=== Helios Hermes Autosetup Generator Complete ==="
Write-Host "Next: run scripts/autosetup.sh, or dispatch .github/workflows/autosetup.yml."
