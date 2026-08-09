#!/usr/bin/env bash
set -euo pipefail

# Bootstraps local developer/CI tools without requiring root access.
# Installs into HELIOS_TOOLS_DIR (default: .tools) and prints PATH export lines.

TOOLS_DIR="${HELIOS_TOOLS_DIR:-$(pwd)/.tools}"
DOTNET_DIR="$TOOLS_DIR/dotnet"
GH_DIR="$TOOLS_DIR/gh"
AZ_DIR="$TOOLS_DIR/azcli-venv"
RG_DIR="$TOOLS_DIR/rg"
GH_VERSION="${GH_VERSION:-2.97.0}"
GH_LINUX_AMD64_SHA256="${GH_LINUX_AMD64_SHA256:-a2c9b8497e1f85b1ad0dfcb78b5a622e098801b8e461e459e88e1ee12f018112}"
AZURE_CLI_VERSION="${AZURE_CLI_VERSION:-2.89.0}"
AZURE_DEVOPS_EXTENSION_VERSION="${AZURE_DEVOPS_EXTENSION_VERSION:-1.0.6}"
AZURE_ML_EXTENSION_VERSION="${AZURE_ML_EXTENSION_VERSION:-2.44.1}"
DOTNET_CHANNEL="${DOTNET_CHANNEL:-8.0}"
RG_VERSION="${RG_VERSION:-14.1.1}"

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
  echo "$GH_LINUX_AMD64_SHA256  $tmp" | sha256sum --check --status
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
  "$AZ_DIR/bin/pip" install --disable-pip-version-check "azure-cli==$AZURE_CLI_VERSION"
  "$AZ_DIR/bin/az" extension add --name azure-devops --version "$AZURE_DEVOPS_EXTENSION_VERSION" --yes
  "$AZ_DIR/bin/az" extension add --name ml --version "$AZURE_ML_EXTENSION_VERSION" --yes
else
  echo "Azure CLI already installed at $AZ_DIR"
fi

if [ ! -x "$RG_DIR/bin/rg" ]; then
  echo "Installing ripgrep $RG_VERSION into $RG_DIR"
  tmp="$TOOLS_DIR/ripgrep-${RG_VERSION}-x86_64-unknown-linux-musl.tar.gz"
  curl -fsSL "https://github.com/BurntSushi/ripgrep/releases/download/${RG_VERSION}/ripgrep-${RG_VERSION}-x86_64-unknown-linux-musl.tar.gz" -o "$tmp"
  rm -rf "$TOOLS_DIR/ripgrep-${RG_VERSION}-x86_64-unknown-linux-musl" "$RG_DIR"
  tar -xzf "$tmp" -C "$TOOLS_DIR"
  mv "$TOOLS_DIR/ripgrep-${RG_VERSION}-x86_64-unknown-linux-musl" "$RG_DIR"
else
  echo "ripgrep already installed at $RG_DIR"
fi

cat <<PATHINFO

Add these tools to your shell:
export PATH="$DOTNET_DIR:$GH_DIR/bin:$AZ_DIR/bin:$RG_DIR/bin:\$PATH"

Authenticate and verify without storing repository credentials:
scripts/setup/configure-cloud-auth.sh --interactive
scripts/setup/configure-cloud-auth.sh --status

Optional OpenAI/Azure OpenAI:
export OPENAI_API_KEY="..."
export AZURE_OPENAI_ENDPOINT="..."
export AZURE_OPENAI_API_KEY="..."
PATHINFO
