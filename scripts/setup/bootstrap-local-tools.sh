#!/usr/bin/env bash
set -euo pipefail

# Bootstraps local developer/CI tools without requiring root access.
# Installs outside the checkout by default and never persists authentication secrets.

TOOLS_DIR="${HELIOS_TOOLS_DIR:-${XDG_DATA_HOME:-$HOME/.local/share}/helios/tools}"
DOTNET_DIR="$TOOLS_DIR/dotnet"
GH_DIR="$TOOLS_DIR/gh"
AZ_DIR="$TOOLS_DIR/azcli-venv"
GH_VERSION="${GH_VERSION:-2.76.2}"
DOTNET_CHANNEL="${DOTNET_CHANNEL:-8.0}"
MODE="all"
INTERACTIVE_AUTH=false

usage() {
  cat <<'EOF'
Usage: bootstrap-local-tools.sh [--install-only|--verify] [--interactive-auth]

Authentication is never stored by this script. In automation, inject GH_TOKEN
through a protected secret channel and use Azure workload identity. Interactive
GitHub/Azure login is available only when --interactive-auth is explicit.
EOF
}

while [ "$#" -gt 0 ]; do
  case "$1" in
    --install-only) MODE="install" ;;
    --verify) MODE="verify" ;;
    --interactive-auth) INTERACTIVE_AUTH=true ;;
    -h|--help) usage; exit 0 ;;
    *) echo "Unknown option: $1" >&2; usage >&2; exit 2 ;;
  esac
  shift
done

verify_auth() {
  local failed=0
  if command -v gh >/dev/null 2>&1 && gh auth status >/dev/null 2>&1; then
    echo "GitHub CLI authentication: ready"
  else
    echo "GitHub CLI authentication: unavailable" >&2
    failed=1
  fi

  if command -v az >/dev/null 2>&1 && az account show --output none >/dev/null 2>&1; then
    echo "Azure CLI authentication: ready"
  else
    echo "Azure CLI authentication: unavailable" >&2
    failed=1
  fi
  return "$failed"
}

if [ "$MODE" = "verify" ]; then
  verify_auth
  exit $?
fi

mkdir -p "$TOOLS_DIR"

if [ ! -x "$DOTNET_DIR/dotnet" ]; then
  echo "Installing .NET SDK channel $DOTNET_CHANNEL into $DOTNET_DIR"
  curl -fsSL https://dot.net/v1/dotnet-install.sh -o "$TOOLS_DIR/dotnet-install.sh"
  bash "$TOOLS_DIR/dotnet-install.sh" --channel "$DOTNET_CHANNEL" --install-dir "$DOTNET_DIR" --no-path
else
  echo ".NET already installed at $DOTNET_DIR"
fi

if [ ! -x "$GH_DIR/bin/gh" ]; then
  echo "Installing GitHub CLI $GH_VERSION into $GH_DIR"
  tmp="$TOOLS_DIR/gh_${GH_VERSION}_linux_amd64.tar.gz"
  curl -fsSL "https://github.com/cli/cli/releases/download/v${GH_VERSION}/gh_${GH_VERSION}_linux_amd64.tar.gz" -o "$tmp"
  rm -rf "$TOOLS_DIR/gh_${GH_VERSION}_linux_amd64" "$GH_DIR"
  tar -xzf "$tmp" -C "$TOOLS_DIR"
  mv "$TOOLS_DIR/gh_${GH_VERSION}_linux_amd64" "$GH_DIR"
else
  echo "GitHub CLI already installed at $GH_DIR"
fi

if [ ! -x "$AZ_DIR/bin/az" ]; then
  echo "Installing Azure CLI into $AZ_DIR"
  python3 -m venv "$AZ_DIR"
  "$AZ_DIR/bin/pip" install --disable-pip-version-check --upgrade pip
  "$AZ_DIR/bin/pip" install --disable-pip-version-check azure-cli
else
  echo "Azure CLI already installed at $AZ_DIR"
fi

cat <<PATHINFO

Add these tools to your shell:
export PATH="$DOTNET_DIR:$GH_DIR/bin:$AZ_DIR/bin:\$PATH"

Verify authentication without displaying credentials:
scripts/setup/bootstrap-local-tools.sh --verify

Optional OpenAI/Azure OpenAI:
export OPENAI_API_KEY="..."
export AZURE_OPENAI_ENDPOINT="..."
export AZURE_OPENAI_API_KEY="..."
PATHINFO

export PATH="$DOTNET_DIR:$GH_DIR/bin:$AZ_DIR/bin:$PATH"

if [ "$INTERACTIVE_AUTH" = true ]; then
  gh auth status >/dev/null 2>&1 || gh auth login
  az account show --output none >/dev/null 2>&1 || az login
fi

if [ "$MODE" = "all" ]; then
  verify_auth || {
    echo "Tools are installed. Supply approved GitHub/Azure identity, then rerun with --verify." >&2
    exit 3
  }
fi
