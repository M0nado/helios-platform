#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
TOOLS_DIR="${HELIOS_TOOLS_DIR:-$ROOT/.tools/bin}"
ACTIONLINT_VERSION="${ACTIONLINT_VERSION:-v1.7.7}"
mkdir -p "$TOOLS_DIR"

if [[ ! -x "$TOOLS_DIR/actionlint" ]]; then
  if command -v actionlint >/dev/null 2>&1; then
    cp "$(command -v actionlint)" "$TOOLS_DIR/actionlint"
  else
    GOBIN="$TOOLS_DIR" go install "github.com/rhysd/actionlint/cmd/actionlint@$ACTIONLINT_VERSION"
  fi
fi

"$TOOLS_DIR/actionlint" "$@"
