#!/usr/bin/env bash
set -Eeuo pipefail

BASE="${1:-${BASE:-repos}}"
SUMMARY="${SUMMARY_PATH:-autosetup/summary.md}"
mkdir -p "$(dirname "$SUMMARY")"

section() { printf '\n## %s\n' "$1" | tee -a "$SUMMARY"; }
check_glob() {
  local label="$1" pattern="$2"
  if find "$BASE" -path "$pattern" -print -quit 2>/dev/null | grep -q .; then
    printf -- '- [x] %s (%s)\n' "$label" "$pattern" | tee -a "$SUMMARY"
  else
    printf -- '- [ ] %s missing (%s)\n' "$label" "$pattern" | tee -a "$SUMMARY"
  fi
}

section "Consolidation validation"
printf 'Base directory: `%s`\n' "$BASE" | tee -a "$SUMMARY"

check_glob 'C# core/platform projects' '*/**/*.csproj'
check_glob 'WinUI/WPF desktop surface' '*/**/*.xaml'
check_glob 'C++ performance backend candidates' '*/**/*.[ch]pp'
check_glob 'F# analytics/math candidates' '*/**/*.fsproj'
check_glob 'Python AIHub/automation candidates' '*/**/*.py'
check_glob 'Azure IaC/config candidates' '*/**/*azure*'
check_glob 'Hermes fleet assets' '*/**/*hermes*'
check_glob 'XCore agent assets' '*/**/*xcore*'

printf '\nValidation complete. Missing optional language buckets should become follow-up tasks, not silent autosetup success.\n' | tee -a "$SUMMARY"
