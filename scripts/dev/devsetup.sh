#!/usr/bin/env bash
# HELIOS/Hermes local development bootstrap.
# Idempotent by design: creates only missing local-development artifacts and never
# performs Azure production deployment. Use scripts/dev/validate-setup.sh for a
# read-only prerequisite check.

set -euo pipefail

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

CREATE_VENV=true
INSTALL_DEPS=false
VALIDATE_ONLY=false

while [[ $# -gt 0 ]]; do
  case "$1" in
    --validate-only) VALIDATE_ONLY=true; shift ;;
    --skip-venv) CREATE_VENV=false; shift ;;
    --install-deps) INSTALL_DEPS=true; shift ;;
    -h|--help)
      cat <<'USAGE'
Usage: scripts/dev/devsetup.sh [--validate-only] [--skip-venv] [--install-deps]

Local development only. This script does not create Azure resources, modify
subscriptions, or deploy production infrastructure.

Options:
  --validate-only  Run non-mutating prerequisite checks and exit.
  --skip-venv      Do not create the Python virtual environment.
  --install-deps   Install npm/pip dependencies when manifest files exist.
USAGE
      exit 0 ;;
    *) echo "Unknown option: $1" >&2; exit 2 ;;
  esac
done

log_info() { echo -e "${BLUE}[INFO]${NC} $*"; }
log_success() { echo -e "${GREEN}[✓]${NC} $*"; }
log_warning() { echo -e "${YELLOW}[WARN]${NC} $*"; }
log_error() { echo -e "${RED}[✗]${NC} $*"; }

repo_root=$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)
cd "$repo_root"

printf '\n==========================================\nHELIOS/Hermes Local Dev Setup\n==========================================\n\n'

if [[ "$VALIDATE_ONLY" == true ]]; then
  exec "$repo_root/scripts/dev/validate-setup.sh"
fi

log_info "Running prerequisite validation first (non-mutating)"
if ! "$repo_root/scripts/dev/validate-setup.sh"; then
  log_warning "Validation reported missing tools. Continuing with best-effort local bootstrap."
fi
printf '\n'

if [[ -f "/.dockerenv" ]]; then
  log_info "Running inside a development container"
else
  log_warning "Not running inside a development container"
  log_info "For containerized development, use the repository devcontainer or Codespaces."
fi
printf '\n'

if command -v node >/dev/null 2>&1; then
  log_success "Node.js $(node --version) detected"
  if [[ -f package.json ]]; then
    if [[ "$INSTALL_DEPS" == true ]]; then
      log_info "Installing npm dependencies from package.json"
      npm install --prefer-offline
      log_success "npm dependencies installed"
    else
      log_info "package.json found; run with --install-deps to execute npm install"
    fi
  fi
else
  log_warning "Node.js not found; UI/tooling packages will be unavailable"
fi
printf '\n'

if command -v python3 >/dev/null 2>&1; then
  log_success "$(python3 --version) detected"
  if [[ "$CREATE_VENV" == true ]]; then
    if [[ ! -d .venv ]]; then
      log_info "Creating Python virtual environment at .venv"
      python3 -m venv .venv
      log_success "Python virtual environment created"
    else
      log_success "Python virtual environment already exists"
    fi
    if [[ "$INSTALL_DEPS" == true ]]; then
      # shellcheck disable=SC1091
      source .venv/bin/activate
      python -m pip install --upgrade pip setuptools wheel
      if [[ -f requirements.txt ]]; then
        python -m pip install -r requirements.txt
      else
        log_info "No requirements.txt found; install AI Hub packages from the documented optional set when needed"
      fi
    fi
  fi
else
  log_warning "Python3 not found; AI Hub integration packages require Python 3.11+"
fi
printf '\n'

log_info "Preparing local-only directories"
mkdir -p logs data .config/helios
log_success "Local directories are present"

if [[ ! -f .env ]]; then
  log_info "Creating local .env template without secrets"
  cat > .env <<'ENVEOF'
DOTNET_ENVIRONMENT=Development
ASPNETCORE_ENVIRONMENT=Development
PYTHONUNBUFFERED=1
HELIOS_LOG_LEVEL=Debug
# Do not store production secrets here. Use Azure Key Vault or user secrets.
ENVEOF
  log_success ".env template created"
else
  log_success ".env already exists; leaving it unchanged"
fi

printf '\n==========================================\n'
log_success "Local development setup complete"
printf '==========================================\n\n'
cat <<'NEXT'
Next steps:
  1. Review GETTING_STARTED.md for full stack prerequisites.
  2. Activate Python when needed: source .venv/bin/activate
  3. Restore/build .NET projects: dotnet restore && dotnet build
  4. Authenticate Azure only when deploying: az login && az account set --subscription <id>
NEXT
