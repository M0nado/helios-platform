#!/usr/bin/env bash
set -euo pipefail
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
find "$ROOT_DIR/.sandbox/workspaces" -mindepth 1 -maxdepth 1 -type d -mtime +7 -print -exec rm -rf {} + 2>/dev/null || true
find "$ROOT_DIR/.sandbox/results" -mindepth 1 -maxdepth 1 -type d -mtime +30 -print -exec rm -rf {} + 2>/dev/null || true
