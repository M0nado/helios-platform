#!/usr/bin/env bash
set -euo pipefail

# Bootstraps local developer/CI tools without requiring root access.
# Installs into HELIOS_TOOLS_DIR (default: .tools) and prints PATH export lines.

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
TOOLS_DIR="$(realpath -m "${HELIOS_TOOLS_DIR:-${REPO_ROOT}/.tools}")"
if [[ "${TOOLS_DIR}" == "/" || "${TOOLS_DIR}" == "/tmp" || "${TOOLS_DIR}" == "${REPO_ROOT}" ]]; then
  echo "Refusing unsafe HELIOS_TOOLS_DIR: ${TOOLS_DIR}" >&2
  exit 2
fi

DOTNET_DIR="$TOOLS_DIR/dotnet"
GH_DIR="$TOOLS_DIR/gh"
AZ_DIR="$TOOLS_DIR/azcli-venv"
GH_VERSION="${GH_VERSION:-2.96.0}"
DOTNET_VERSION="${DOTNET_VERSION:-8.0.423}"
AZURE_CLI_VERSION="${AZURE_CLI_VERSION:-2.88.0}"
DOTNET_INSTALL_COMMIT="a6dd53ae9c18d045e8f5dc7db84485a5bef04d43"
DOTNET_INSTALL_SHA256="082f7685e156738a1b2e2ed8381a621870d4ce8e8c59278034556f05c186eb2e"

case "$(uname -m)" in
  x86_64|amd64)
    GH_ARCH="amd64"
    GH_SHA256="${GH_SHA256:-83d5c2ccad5498f58bf6368acb1ab32588cf43ab3a4b1c301bf36328b1c8bd60}"
    ;;
  aarch64|arm64)
    GH_ARCH="arm64"
    GH_SHA256="${GH_SHA256:-06f86ec7103d41993b76cd78072f43595c34aaa56506d971d9860e67140bf909}"
    ;;
  *)
    echo "Unsupported architecture: $(uname -m)" >&2
    exit 2
    ;;
esac

mkdir -p "$TOOLS_DIR"

if [ ! -x "$DOTNET_DIR/dotnet" ]; then
  echo "Installing .NET SDK $DOTNET_VERSION into $DOTNET_DIR"
  curl -fsSL \
    "https://raw.githubusercontent.com/dotnet/install-scripts/${DOTNET_INSTALL_COMMIT}/src/dotnet-install.sh" \
    -o "$TOOLS_DIR/dotnet-install.sh"
  echo "${DOTNET_INSTALL_SHA256}  ${TOOLS_DIR}/dotnet-install.sh" |
    sha256sum --check --status
  bash "$TOOLS_DIR/dotnet-install.sh" --version "$DOTNET_VERSION" --install-dir "$DOTNET_DIR" --no-path
else
  echo ".NET already installed at $DOTNET_DIR"
fi

if [ ! -x "$GH_DIR/bin/gh" ]; then
  echo "Installing GitHub CLI $GH_VERSION into $GH_DIR"
  tmp="$TOOLS_DIR/gh_${GH_VERSION}_linux_${GH_ARCH}.tar.gz"
  curl -fsSL "https://github.com/cli/cli/releases/download/v${GH_VERSION}/gh_${GH_VERSION}_linux_${GH_ARCH}.tar.gz" -o "$tmp"
  echo "${GH_SHA256}  ${tmp}" | sha256sum --check --status
  rm -rf "$TOOLS_DIR/gh_${GH_VERSION}_linux_${GH_ARCH}" "$GH_DIR"
  tar -xzf "$tmp" -C "$TOOLS_DIR"
  mv "$TOOLS_DIR/gh_${GH_VERSION}_linux_${GH_ARCH}" "$GH_DIR"
else
  echo "GitHub CLI already installed at $GH_DIR"
fi

if [ ! -x "$AZ_DIR/bin/az" ]; then
  echo "Installing Azure CLI into $AZ_DIR"
  python3 -m venv "$AZ_DIR"
  "$AZ_DIR/bin/pip" install --disable-pip-version-check "azure-cli==$AZURE_CLI_VERSION"
else
  echo "Azure CLI already installed at $AZ_DIR"
fi

cat <<PATHINFO

Add these tools to your shell:
export PATH="$DOTNET_DIR:$GH_DIR/bin:$AZ_DIR/bin:\$PATH"

Authenticate as needed:
gh auth login
az login
PATHINFO
