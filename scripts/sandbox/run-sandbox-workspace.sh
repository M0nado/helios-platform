#!/usr/bin/env bash
set -euo pipefail
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
STAMP="$(date -u +'%Y%m%dT%H%M%SZ')"
SANDBOX_DIR="$ROOT_DIR/.sandbox/workspaces/$STAMP"
RESULT_DIR="$ROOT_DIR/.sandbox/results/$STAMP"
mkdir -p "$SANDBOX_DIR" "$RESULT_DIR"
rsync -a --exclude .git --exclude bin --exclude obj --exclude .sandbox "$ROOT_DIR/" "$SANDBOX_DIR/"
(
  cd "$SANDBOX_DIR"
  python3 scripts/analysis/branch_intelligence.py --out "$RESULT_DIR/branch-intelligence"
)
echo "Sandbox workspace: $SANDBOX_DIR"
echo "Sandbox results: $RESULT_DIR"
