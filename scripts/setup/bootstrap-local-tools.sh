#!/usr/bin/env bash
set -euo pipefail

# Bootstraps local developer/CI tools without requiring root access.
# Installs into HELIOS_TOOLS_DIR (default: .tools) and prints PATH export lines.

TOOLS_DIR="${HELIOS_TOOLS_DIR:-$(pwd)/.tools}"
DOTNET_DIR="$TOOLS_DIR/dotnet"
GH_DIR="$TOOLS_DIR/gh"
AZ_DIR="$TOOLS_DIR/azcli-venv"
GH_VERSION="${GH_VERSION:-2.76.2}"
DOTNET_CHANNEL="${DOTNET_CHANNEL:-8.0}"

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

Authenticate as needed:
gh auth login
az login

Optional OpenAI/Azure OpenAI:
export OPENAI_API_KEY="..."
export AZURE_OPENAI_ENDPOINT="..."
export AZURE_OPENAI_API_KEY="..."
PATHINFO
