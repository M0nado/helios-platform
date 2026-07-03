#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
INSTALL_DIR="${DOTNET_INSTALL_DIR:-$ROOT/.dotnet-local}"
CHANNEL="${DOTNET_CHANNEL:-8.0}"
mkdir -p "$INSTALL_DIR"
if [ ! -x "$INSTALL_DIR/dotnet" ]; then
  curl -fsSL https://dot.net/v1/dotnet-install.sh -o "$INSTALL_DIR/dotnet-install.sh"
  bash "$INSTALL_DIR/dotnet-install.sh" --channel "$CHANNEL" --install-dir "$INSTALL_DIR" --no-path
fi
cat <<MSG
DOTNET_ROOT=$INSTALL_DIR
PATH=$INSTALL_DIR:\$PATH
Run this in your shell to use the local SDK:
  export DOTNET_ROOT="$INSTALL_DIR" PATH="$INSTALL_DIR:\$PATH" DOTNET_CLI_TELEMETRY_OPTOUT=1
MSG
"$INSTALL_DIR/dotnet" --info
