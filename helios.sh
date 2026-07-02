#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'USAGE'
Usage: ./helios.sh <command>

Commands:
  prune-generated   Remove local generated artifacts that should not be committed.
USAGE
}

prune_generated() {
  local paths=(
    "reports"
    "status-site/index.html"
    "status-site/actions.md"
    "status-site/wiki-export"
    ".github/PULL_REQUEST_BODY.md"
  )

  for path in "${paths[@]}"; do
    if [[ -e "$path" || -L "$path" ]]; then
      rm -rf -- "$path"
      printf 'removed %s\n' "$path"
    fi
  done
}

case "${1:-}" in
  prune-generated)
    prune_generated
    ;;
  -h|--help|help)
    usage
    ;;
  "")
    usage >&2
    exit 2
    ;;
  *)
    printf 'Unknown command: %s\n\n' "$1" >&2
    usage >&2
    exit 2
    ;;
esac
