#!/usr/bin/env bash
set -Eeuo pipefail

BASE="${1:-${BASE:-repos}}"
SUMMARY="${SUMMARY_PATH:-autosetup/summary.md}"
mkdir -p "$(dirname "$SUMMARY")"

section() { printf '\n## %s\n' "$1" | tee -a "$SUMMARY"; }
check_find() {
  local label="$1"; shift
  local result
  result="$(find "$BASE" "$@" -print -quit 2>/dev/null || true)"
  if [[ -n "$result" ]]; then
    printf -- '- [x] %s (`%s`)\n' "$label" "$result" | tee -a "$SUMMARY"
  else
    printf -- '- [ ] %s missing\n' "$label" | tee -a "$SUMMARY"
  fi
}

section "Consolidation validation"
printf 'Base directory: `%s`\n' "$BASE" | tee -a "$SUMMARY"

check_find 'C# core/platform projects' -type f -name '*.csproj'
check_find 'WinUI/WPF desktop surface' -type f \( -name '*.xaml' -o -name '*.xaml.cs' \)
check_find 'C++ performance backend candidates' -type f \( -name '*.cpp' -o -name '*.hpp' -o -name '*.cc' -o -name '*.cxx' -o -name '*.h' \)
check_find 'F# analytics/math candidates' -type f \( -name '*.fsproj' -o -name '*.fs' -o -name '*.fsi' \)
check_find 'Python AIHub/automation candidates' -type f -name '*.py'
check_find 'Azure IaC/config candidates' -type f \( -iname '*azure*' -o -name '*.bicep' -o -name 'main.tf' \)
check_find 'Hermes fleet assets' \( -ipath '*hermes*' -o -ipath '*fleet*' \)
check_find 'XCore agent assets' -ipath '*xcore*'

printf '\nValidation complete. Missing optional language buckets should become follow-up tasks, not silent autosetup success.\n' | tee -a "$SUMMARY"
