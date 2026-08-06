#!/usr/bin/env bash
# HELIOS/Hermes non-mutating setup validation.
# Checks local tool availability and versions only; it does not install packages,
# authenticate, create Azure resources, or write project state.

set -u

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'
FAILED=0
WARNED=0

info() { echo -e "${BLUE}[INFO]${NC} $*"; }
pass() { echo -e "${GREEN}[PASS]${NC} $*"; }
warn() { echo -e "${YELLOW}[WARN]${NC} $*"; WARNED=1; }
fail() { echo -e "${RED}[FAIL]${NC} $*"; FAILED=1; }

have() { command -v "$1" >/dev/null 2>&1; }
run_version() {
  local label="$1" command_name="$2"; shift 2
  if have "$command_name"; then
    local output
    output=$("$@" 2>&1 | head -n 3 | tr '\n' ' ')
    pass "$label: $output"
  else
    fail "$label not found ($command_name)"
  fi
}

repo_root=$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)
cd "$repo_root"

info "HELIOS/Hermes setup validation (non-mutating)"
info "Repository: $repo_root"
echo ""

run_version ".NET SDK 8.x" dotnet dotnet --version
if have dotnet; then
  if dotnet --list-sdks 2>/dev/null | grep -Eq '^8\.'; then
    pass ".NET 8 SDK is installed"
  else
    fail ".NET 8 SDK is required by HELIOS net8.0 and net8.0-windows projects"
  fi
fi

run_version "PowerShell 7" pwsh pwsh -NoLogo -NoProfile -Command '$PSVersionTable.PSVersion.ToString()'
run_version "Git" git git --version
run_version "GitHub CLI" gh gh --version
run_version "Azure CLI" az az version --output table
if have az; then
  if az extension list --query "[].name" --output tsv 2>/dev/null | grep -Eq '^(account|devops|ml|azure-iot|containerapp)$'; then
    pass "Azure CLI extensions detected"
  else
    warn "Recommended Azure CLI extensions not detected: account, devops, ml, azure-iot, containerapp"
  fi
  if az account show >/dev/null 2>&1; then
    pass "Azure CLI has an active login context"
  else
    warn "Azure CLI is installed but not logged in; run 'az login' before deployment"
  fi
fi

run_version "Docker" docker docker --version
run_version "Python 3.11+" python3 python3 --version
if have python3; then
  python3 - <<'PY'
import sys
if sys.version_info >= (3, 11):
    print("[PASS] Python runtime is 3.11 or newer")
else:
    print("[FAIL] Python 3.11+ is recommended for AI Hub integration")
    raise SystemExit(1)
PY
  [ $? -eq 0 ] || FAILED=1
fi
run_version "Node.js" node node --version
run_version "npm" npm npm --version
run_version "CMake" cmake cmake --version
run_version "Ninja" ninja ninja --version
run_version "MSBuild" msbuild msbuild -version
run_version "Visual Studio locator" vswhere vswhere -latest -property installationVersion

if have dotnet; then
  if dotnet fsi --help >/dev/null 2>&1; then
    pass "F# interactive/tooling available through .NET SDK"
  else
    warn "F# tooling not detected through 'dotnet fsi'"
  fi
fi

if [[ "$OSTYPE" == msys* || "$OSTYPE" == cygwin* || "${OS:-}" == Windows_NT ]]; then
  info "Windows-specific SDK checks are best run with scripts/dev/Validate-Setup.ps1"
else
  warn "WinUI 3 and Windows App SDK builds require Windows 11/Server 2022 with Visual Studio Build Tools"
fi

echo ""
if [ "$FAILED" -ne 0 ]; then
  fail "Validation completed with missing required tools"
  exit 1
fi
if [ "$WARNED" -ne 0 ]; then
  warn "Validation completed with warnings"
  exit 0
fi
pass "Validation completed successfully"
