using System;
using System.IO;

namespace Helios.Autosetup
{
    public static class HeliosAutosetup
    {
        private const string ManifestPath = "autosetup/manifest.yaml";
        private const string ScriptsDir = "scripts";
        private const string WorkflowsDir = ".github/workflows";

        public static void Main(string[] args)
        {
            Console.WriteLine("=== Helios Hermes Autosetup Orchestrator ===");

            CreateFolders();
            CreateManifest();
            CreateScript("autosetup.sh", AutosetupScript, executable: true);
            CreateScript("azure-preflight.sh", AzurePreflightScript, executable: true);
            CreateScript("validate-consolidation.sh", ValidateConsolidationScript, executable: true);
            CreateWorkflow();

            Console.WriteLine("=== Autosetup files created. Run scripts/autosetup.sh or the GitHub workflow. ===");
        }

        private static void CreateFolders()
        {
            Directory.CreateDirectory("autosetup");
            Directory.CreateDirectory(ScriptsDir);
            Directory.CreateDirectory(WorkflowsDir);
        }

        private static void CreateManifest() => File.WriteAllText(ManifestPath, Manifest);

        private static void CreateScript(string fileName, string content, bool executable)
        {
            var path = Path.Combine(ScriptsDir, fileName);
            File.WriteAllText(path, content);
            if (executable && !OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                    UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                    UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
            }
        }

        private static void CreateWorkflow() => File.WriteAllText(Path.Combine(WorkflowsDir, Path.GetFileName("autosetup.yml")), Workflow);

        private const string Manifest = """
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
  mergeAllRemoteBranches: true
  pushMergeBranches: false
  requireGitHubToken: true
  installAzureCliIfMissing: false
  requireAzureLogin: false
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

repoProfiles:
  helios-platform:
    role: C# platform shell, installer orchestration, Azure/security integration
    priority: high
  helios-control:
    role: C# and WinUI 3 control-plane frontend and operator setup flow
    priority: critical
  hermes-fleet-production:
    role: production Hermes fleet coordination, security hardening, rollout gates
    priority: critical
  hermes-core:
    role: Hermes core services and XCore specialist integration
    priority: high
  xcore-agent:
    role: specialist agent runtime and cross-repo automation hooks
    priority: high
  hermes-fleet-public:
    role: public fleet templates and examples
    priority: normal
  hermes-fleet-private:
    role: private fleet configuration and sensitive deployment adapters
    priority: high
  usb-wizard:
    role: Windows setup, boot media, and installer references
    priority: normal
  os-setup:
    role: host provisioning, drivers, security baseline, and Azure CLI bootstrap
    priority: normal
""";

        private const string AutosetupScript = """
#!/usr/bin/env bash
set -Eeuo pipefail

MANIFEST="${MANIFEST:-autosetup/manifest.yaml}"
BASE="${BASE:-repos}"
SUMMARY="${SUMMARY_PATH:-autosetup/summary.md}"
DEFAULT_REMOTE_BRANCH="${DEFAULT_REMOTE_BRANCH:-main}"
CLEAN_BRANCH="${CLEAN_BRANCH:-hermes-clean}"
MERGE_BRANCH="${MERGE_BRANCH:-hermes-merge}"
REQUIRE_AZURE_LOGIN="${REQUIRE_AZURE_LOGIN:-false}"
INSTALL_AZURE_CLI="${INSTALL_AZURE_CLI:-false}"
MERGE_ALL_REMOTE_BRANCHES="${MERGE_ALL_REMOTE_BRANCHES:-true}"
PUSH_MERGE_BRANCHES="${PUSH_MERGE_BRANCHES:-false}"

mkdir -p "$BASE" "$(dirname "$SUMMARY")"
printf '# Helios/Hermes autosetup summary\n\nStarted: %s\n\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)" > "$SUMMARY"

need() { command -v "$1" >/dev/null 2>&1 || { echo "Missing required command: $1" >&2; exit 1; }; }
log_summary() { printf '%s\n' "$1" | tee -a "$SUMMARY"; }
repo_target() {
  local repo="$1" owner name
  owner="${repo%%/*}"
  name="${repo##*/}"
  printf '%s/%s/%s' "$BASE" "$owner" "$name"
}
read_manifest_repos() {
  awk '
    /^repos:[[:space:]]*$/ { in_repos=1; next }
    /^[^[:space:]-][^:]*:[[:space:]]*$/ { if (in_repos) exit }
    in_repos && /^[[:space:]]*-[[:space:]]+/ {
      sub(/^[[:space:]]*-[[:space:]]*/, "")
      gsub(/["'"'"']/, "")
      print
    }
  ' "$MANIFEST"
}
remote_default_branch() {
  local target="$1" branch
  if git -C "$target" show-ref --verify --quiet "refs/remotes/origin/$DEFAULT_REMOTE_BRANCH"; then
    printf '%s' "$DEFAULT_REMOTE_BRANCH"
    return 0
  fi
  branch="$(git -C "$target" remote show origin | awk '/HEAD branch/ {print $NF}')"
  [[ -n "$branch" && "$branch" != "(unknown)" ]] || return 1
  printf '%s' "$branch"
}
clone_or_fetch_repo() {
  local repo="$1" target="$2"
  mkdir -p "$(dirname "$target")"
  if [[ -d "$target/.git" ]]; then
    git -C "$target" remote set-url origin "https://github.com/$repo.git" || true
    git -C "$target" fetch --all --prune --tags
    log_summary "- [x] fetched existing clone at \`$target\`"
  else
    if command -v gh >/dev/null 2>&1; then
      gh repo clone "$repo" "$target" -- --quiet
    else
      git clone --quiet "https://github.com/$repo.git" "$target"
    fi
    git -C "$target" fetch --all --prune --tags
    log_summary "- [x] cloned to \`$target\`"
  fi
}
merge_remote_branches() {
  local target="$1" remote_branch="$2" branch
  [[ "$MERGE_ALL_REMOTE_BRANCHES" == "true" ]] || return 0
  while IFS= read -r branch; do
    [[ -n "$branch" ]] || continue
    [[ "$branch" == "$remote_branch" ]] && continue
    log_summary "- [ ] merging remote branch \`origin/$branch\`"
    git -C "$target" merge --no-edit -X theirs "origin/$branch"
    log_summary "- [x] merged \`origin/$branch\`"
  done < <(git -C "$target" for-each-ref --format='%(refname:short)' refs/remotes/origin \
    | sed '/^origin\/HEAD$/d' \
    | sed 's#^origin/##' \
    | sort -u)
}

need git
[[ -f "$MANIFEST" ]] || { echo "Manifest not found: $MANIFEST" >&2; exit 1; }

export REQUIRE_AZURE_LOGIN INSTALL_AZURE_CLI
./scripts/azure-preflight.sh | tee -a "$SUMMARY"

mapfile -t repos < <(read_manifest_repos)
if [[ "${#repos[@]}" -eq 0 ]]; then
  echo "No repositories listed in $MANIFEST" >&2
  exit 1
fi

log_summary "\n## Repository synchronization"
for repo in "${repos[@]}"; do
  target="$(repo_target "$repo")"
  log_summary "\n### $repo"
  clone_or_fetch_repo "$repo" "$target"

  remote_branch="$(remote_default_branch "$target")" || {
    echo "Unable to determine remote branch for $repo" >&2
    exit 1
  }

  git -C "$target" checkout -B "$CLEAN_BRANCH" "origin/$remote_branch"
  log_summary "- [x] reset \`$CLEAN_BRANCH\` from \`origin/$remote_branch\`"

  git -C "$target" checkout -B "$MERGE_BRANCH"
  git -C "$target" merge --no-edit -X theirs "$CLEAN_BRANCH"
  merge_remote_branches "$target" "$remote_branch"
  log_summary "- [x] prepared merge branch \`$MERGE_BRANCH\`"

  if [[ "$PUSH_MERGE_BRANCHES" == "true" ]]; then
    git -C "$target" push --set-upstream origin "$MERGE_BRANCH"
    log_summary "- [x] pushed \`$MERGE_BRANCH\`"
  else
    log_summary "- [ ] push skipped; set PUSH_MERGE_BRANCHES=true to publish \`$MERGE_BRANCH\`"
  fi
done

BASE="$BASE" SUMMARY_PATH="$SUMMARY" ./scripts/validate-consolidation.sh "$BASE"

log_summary "\n## Next integration gates"
log_summary "- Review helios-control and hermes-fleet-production first; they are critical focus repos in autosetup/manifest.yaml."
log_summary "- Wire C# frontend/install/security, WinUI 3 shell, C++ performance backend, F# analytics, Python AIHub, and Hermes XCore specialists by repo-specific PRs."
log_summary "- Run build/test/security validation in each cloned repository before setting PUSH_MERGE_BRANCHES=true."
log_summary "\nCompleted: $(date -u +%Y-%m-%dT%H:%M:%SZ)"
""";

        private const string AzurePreflightScript = """
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
""";

        private const string ValidateConsolidationScript = """
#!/usr/bin/env bash
set -Eeuo pipefail

BASE="${1:-${BASE:-repos}}"
SUMMARY="${SUMMARY_PATH:-autosetup/summary.md}"
mkdir -p "$(dirname "$SUMMARY")"

section() { printf '\n## %s\n' "$1" | tee -a "$SUMMARY"; }
check_find() {
  local label="$1"; shift
  local result
  result="$(find "$BASE" "$@" -print -quit 2>/dev/null || true)"
  if [[ -n "$result" ]]; then
    printf -- '- [x] %s (`%s`)\n' "$label" "$result" | tee -a "$SUMMARY"
  else
    printf -- '- [ ] %s missing\n' "$label" | tee -a "$SUMMARY"
  fi
}

section "Consolidation validation"
printf 'Base directory: `%s`\n' "$BASE" | tee -a "$SUMMARY"

check_find 'C# core/platform projects' -type f -name '*.csproj'
check_find 'WinUI/WPF desktop surface' -type f \( -name '*.xaml' -o -name '*.xaml.cs' \)
check_find 'C++ performance backend candidates' -type f \( -name '*.cpp' -o -name '*.hpp' -o -name '*.cc' -o -name '*.cxx' -o -name '*.h' \)
check_find 'F# analytics/math candidates' -type f \( -name '*.fsproj' -o -name '*.fs' -o -name '*.fsi' \)
check_find 'Python AIHub/automation candidates' -type f -name '*.py'
check_find 'Azure IaC/config candidates' -type f \( -iname '*azure*' -o -name '*.bicep' -o -name 'main.tf' \)
check_find 'Hermes fleet assets' \( -ipath '*hermes*' -o -ipath '*fleet*' \)
check_find 'XCore agent assets' -ipath '*xcore*'

printf '\nValidation complete. Missing optional language buckets should become follow-up tasks, not silent autosetup success.\n' | tee -a "$SUMMARY"
""";

        private const string Workflow = """
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
      install_azure_cli:
        description: Attempt Azure CLI install if missing on Linux runners
        required: true
        default: 'false'
        type: choice
        options: ['false', 'true']
      default_remote_branch:
        description: Default remote branch to normalize from
        required: true
        default: main
      push_merge_branches:
        description: Push generated hermes-merge branches back to each repo
        required: true
        default: 'false'
        type: choice
        options: ['false', 'true']

permissions:
  contents: read
  id-token: write

env:
  REQUIRE_AZURE_LOGIN: ${{ inputs.require_azure_login }}
  INSTALL_AZURE_CLI: ${{ inputs.install_azure_cli }}
  DEFAULT_REMOTE_BRANCH: ${{ inputs.default_remote_branch }}
  PUSH_MERGE_BRANCHES: ${{ inputs.push_merge_branches }}
  AZURE_SUBSCRIPTION_ID: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
  AZURE_TENANT_ID: ${{ secrets.AZURE_TENANT_ID }}

jobs:
  autosetup:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Validate host tools
        env:
          GH_TOKEN: ${{ secrets.HELIOS_AUTOMATION_PAT || github.token }}
        run: |
          git --version
          gh --version
          gh auth status --hostname github.com || true

      - name: Azure Login (optional OIDC)
        if: ${{ inputs.require_azure_login == 'true' }}
        uses: azure/login@v2
        with:
          client-id: ${{ secrets.AZURE_CLIENT_ID }}
          tenant-id: ${{ secrets.AZURE_TENANT_ID }}
          subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}

      - name: Run autosetup
        env:
          GH_TOKEN: ${{ secrets.HELIOS_AUTOMATION_PAT || github.token }}
        run: ./scripts/autosetup.sh

      - name: Upload autosetup summary
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: autosetup-summary
          path: autosetup/summary.md
""";
    }
}
