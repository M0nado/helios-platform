#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PR_BODY_FILE="$ROOT_DIR/.github/PULL_REQUEST_BODY.md"

usage() {
  cat <<'USAGE'
HELIOS repository helper

USAGE:
  ./helios.sh pr-update [--dry-run]
  ./helios.sh help

COMMANDS:
  pr-update   Generate .github/PULL_REQUEST_BODY.md from the current git state.
              The generated file is intentionally ignored by git.
USAGE
}

current_branch() {
  git -C "$ROOT_DIR" rev-parse --abbrev-ref HEAD 2>/dev/null || printf 'unknown'
}

base_ref() {
  local ref
  for ref in origin/main origin/master main master; do
    if git -C "$ROOT_DIR" rev-parse --verify --quiet "$ref" >/dev/null; then
      printf '%s' "$ref"
      return 0
    fi
  done
  printf 'HEAD~1'
}

git_section() {
  local title="$1"
  shift
  printf '## %s\n\n' "$title"
  if ! "$@"; then
    printf '_Not available in this checkout._\n'
  fi
  printf '\n'
}

emit_pr_body() {
  local branch base range
  branch="$(current_branch)"
  base="$(base_ref)"
  if git -C "$ROOT_DIR" rev-parse --verify --quiet "$base" >/dev/null; then
    range="$base..HEAD"
  else
    range="HEAD~1..HEAD"
  fi

  cat <<BODY
# Pull Request Summary

## Overview

This PR updates the HELIOS platform repository from branch \`$branch\`.

## Generated Review Notes

- Focus areas requested by the operator: HELIOS control surfaces, Hermes fleet production readiness, C# / WinUI 3 frontend work, C++ performance backend considerations, F# analytics/prediction workflows, Python AI Hub integration, security hardening, parallel optimization, Azure CLI/setup automation, and Hermes XCore specialist setup.
- This file is generated for PR review only and is intentionally ignored by git.
- Review and edit this body before publishing the pull request.

BODY

  git_section "Recent Commits" git -C "$ROOT_DIR" log --oneline --decorate -n 12

  printf '## Changed Files\n\n'
  if git -C "$ROOT_DIR" diff --name-status "$range" -- . ':!.github/PULL_REQUEST_BODY.md'; then
    :
  else
    printf '_No changed files detected for range %s._\n' "$range"
  fi
  printf '\n'

  cat <<'BODY'
## Validation Checklist

- [ ] Run repository build/test commands relevant to touched projects.
- [ ] Validate C# / WinUI 3 application paths where affected.
- [ ] Validate C++ performance-sensitive paths where affected.
- [ ] Validate F# analytics/prediction paths where affected.
- [ ] Validate Python AI Hub integration paths where affected.
- [ ] Confirm Azure CLI/setup automation does not expose secrets.
- [ ] Confirm generated artifacts are not committed.

## Testing

- [ ] Add exact commands and outcomes before opening the PR.
BODY
}

pr_update() {
  local dry_run=false
  while [ "$#" -gt 0 ]; do
    case "$1" in
      --dry-run) dry_run=true ;;
      -h|--help) usage; return 0 ;;
      *) printf 'Unknown pr-update option: %s\n' "$1" >&2; return 2 ;;
    esac
    shift
  done

  mkdir -p "$(dirname "$PR_BODY_FILE")"
  emit_pr_body > "$PR_BODY_FILE"
  if [ "$dry_run" = true ]; then
    printf 'Dry run complete. Generated %s\n' "${PR_BODY_FILE#$ROOT_DIR/}"
  else
    printf 'Generated %s\n' "${PR_BODY_FILE#$ROOT_DIR/}"
  fi
}

case "${1:-help}" in
  help|-h|--help) usage ;;
  pr-update) shift; pr_update "$@" ;;
  *) printf 'Unknown command: %s\n\n' "$1" >&2; usage >&2; exit 2 ;;
esac
