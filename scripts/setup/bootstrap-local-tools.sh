#!/usr/bin/env bash
set -euo pipefail

# Bootstraps local developer/CI tools without requiring root access.
# Installs into HELIOS_TOOLS_DIR (default: .tools) and prints PATH export lines.

TOOLS_DIR="${HELIOS_TOOLS_DIR:-$(pwd)/.tools}"
DOTNET_DIR="$TOOLS_DIR/dotnet"
GH_DIR="$TOOLS_DIR/gh"
AZ_DIR="$TOOLS_DIR/azcli-venv"
RG_DIR="$TOOLS_DIR/rg"
GH_VERSION="${GH_VERSION:-2.76.2}"
DOTNET_VERSION="${DOTNET_VERSION:-$(sed -n 's/.*"version"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' global.json | head -1)}"
RG_VERSION="${RG_VERSION:-14.1.1}"

case "$(uname -s)" in
  MINGW*|MSYS*|CYGWIN*)
    exec powershell.exe -NoProfile -ExecutionPolicy Bypass \
      -File "$(cygpath -w "$(pwd)/scripts/setup/bootstrap-local-tools.ps1")" \
      -ToolsDirectory "$(cygpath -w "$TOOLS_DIR")" \
      -DotnetVersion "$DOTNET_VERSION" -GhVersion "$GH_VERSION" -RgVersion "$RG_VERSION"
    ;;
  Linux*)
    PLATFORM="linux"
    EXE_SUFFIX=""
    VENV_BIN="bin"
    GH_ARCHIVE="gh_${GH_VERSION}_linux_amd64.tar.gz"
    GH_EXTRACTED="gh_${GH_VERSION}_linux_amd64"
    RG_ARCHIVE="ripgrep-${RG_VERSION}-x86_64-unknown-linux-musl.tar.gz"
    RG_EXTRACTED="ripgrep-${RG_VERSION}-x86_64-unknown-linux-musl"
    ;;
  *)
    echo "Unsupported bootstrap platform: $(uname -s). Use Linux or Git Bash on Windows." >&2
    exit 2
    ;;
esac

extract_archive() {
  local archive="$1"
  if [[ "$archive" == *.zip ]]; then
    # Git for Windows ships bsdtar; unzip is also supported when available.
    if command -v unzip >/dev/null 2>&1; then
      unzip -q "$archive" -d "$TOOLS_DIR"
    else
      tar -xf "$archive" -C "$TOOLS_DIR"
    fi
  else
    tar -xzf "$archive" -C "$TOOLS_DIR"
  fi
}

mkdir -p "$TOOLS_DIR"

if [ ! -x "$DOTNET_DIR/dotnet$EXE_SUFFIX" ] || [ "$("$DOTNET_DIR/dotnet$EXE_SUFFIX" --version 2>/dev/null || true)" != "$DOTNET_VERSION" ]; then
  echo "Installing .NET SDK $DOTNET_VERSION into $DOTNET_DIR"
  curl -fsSL https://dot.net/v1/dotnet-install.sh -o "$TOOLS_DIR/dotnet-install.sh"
  bash "$TOOLS_DIR/dotnet-install.sh" --version "$DOTNET_VERSION" --install-dir "$DOTNET_DIR" --no-path --os "$PLATFORM"
else
  echo ".NET already installed at $DOTNET_DIR"
fi
INSTALLED_DOTNET_VERSION="$("$DOTNET_DIR/dotnet$EXE_SUFFIX" --version)"
if [ "$INSTALLED_DOTNET_VERSION" != "$DOTNET_VERSION" ]; then
  echo "Expected .NET SDK $DOTNET_VERSION, but local dotnet selected $INSTALLED_DOTNET_VERSION." >&2
  exit 1
fi

if [ ! -x "$GH_DIR/bin/gh$EXE_SUFFIX" ]; then
  echo "Installing GitHub CLI $GH_VERSION into $GH_DIR"
  tmp="$TOOLS_DIR/$GH_ARCHIVE"
  curl -fsSL "https://github.com/cli/cli/releases/download/v${GH_VERSION}/$GH_ARCHIVE" -o "$tmp"
  rm -rf "$TOOLS_DIR/$GH_EXTRACTED" "$GH_DIR"
  extract_archive "$tmp"
  mv "$TOOLS_DIR/$GH_EXTRACTED" "$GH_DIR"
else
  echo "GitHub CLI already installed at $GH_DIR"
fi

if [ ! -e "$AZ_DIR/$VENV_BIN/az$EXE_SUFFIX" ] && [ ! -e "$AZ_DIR/$VENV_BIN/az.cmd" ]; then
  echo "Installing Azure CLI into $AZ_DIR"
  python3 -m venv "$AZ_DIR"
  "$AZ_DIR/$VENV_BIN/python$EXE_SUFFIX" -m pip install --disable-pip-version-check --upgrade pip
  "$AZ_DIR/$VENV_BIN/python$EXE_SUFFIX" -m pip install --disable-pip-version-check azure-cli
else
  echo "Azure CLI already installed at $AZ_DIR"
fi

if [ ! -x "$RG_DIR/rg$EXE_SUFFIX" ]; then
  echo "Installing ripgrep $RG_VERSION into $RG_DIR"
  tmp="$TOOLS_DIR/$RG_ARCHIVE"
  curl -fsSL "https://github.com/BurntSushi/ripgrep/releases/download/${RG_VERSION}/$RG_ARCHIVE" -o "$tmp"
  rm -rf "$TOOLS_DIR/$RG_EXTRACTED" "$RG_DIR"
  extract_archive "$tmp"
  mv "$TOOLS_DIR/$RG_EXTRACTED" "$RG_DIR"
else
  echo "ripgrep already installed at $RG_DIR"
fi

cat <<PATHINFO

Add these tools to your shell:
export PATH="$DOTNET_DIR:$GH_DIR/bin:$AZ_DIR/$VENV_BIN:$RG_DIR:\$PATH"

Authenticate as needed:
gh auth login
az login

Optional OpenAI/Azure OpenAI:
export OPENAI_API_KEY="..."
export AZURE_OPENAI_ENDPOINT="..."
export AZURE_OPENAI_API_KEY="..."
PATHINFO
