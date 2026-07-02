#!/usr/bin/env bash
# HELIOS repository optimization and merge readiness audit.
# Scans available branches and multi-language project surfaces without changing branches.

set -euo pipefail

ROOT_DIR="$(git rev-parse --show-toplevel 2>/dev/null || pwd)"
cd "$ROOT_DIR"

log() { printf '[helios-optimize] %s\n' "$*"; }
section() { printf '\n== %s ==\n' "$*"; }

section "Repository"
log "root: $ROOT_DIR"
log "branch: $(git branch --show-current 2>/dev/null || echo unknown)"
log "head: $(git rev-parse --short HEAD 2>/dev/null || echo unknown)"

section "Merge surfaces"
mapfile -t branches < <(git for-each-ref --format='%(refname:short)' refs/heads refs/remotes 2>/dev/null | sed '/\/HEAD$/d' | sort -u)
if ((${#branches[@]} == 0)); then
  log "no local or remote branches found"
else
  printf '%s\n' "${branches[@]}" | sed 's/^/- /'
fi

section "Conflict markers"
if rg -n '^(<<<<<<< .+|=======$|>>>>>>> .+)' --glob '!dotnet-install.sh' .; then
  log "conflict markers found; resolve before merging"
  exit 2
else
  log "no conflict markers detected"
fi

section "Project inventory"
find . -type f \( \
  -name '*.sln' -o -name '*.csproj' -o -name '*.fsproj' -o -name '*.vcxproj' -o \
  -name 'package.json' -o -name 'pyproject.toml' -o -name 'requirements*.txt' \
\) -not -path './.git/*' | sort | sed 's#^./#- #'

section "Language counts"
printf 'C#: %s\n' "$(find . -type f -name '*.cs' -not -path './.git/*' | wc -l | tr -d ' ')"
printf 'C++: %s\n' "$(find . -type f \( -name '*.cpp' -o -name '*.cc' -o -name '*.cxx' -o -name '*.hpp' -o -name '*.h' \) -not -path './.git/*' | wc -l | tr -d ' ')"
printf 'F#: %s\n' "$(find . -type f -name '*.fs' -not -path './.git/*' | wc -l | tr -d ' ')"
printf 'Python: %s\n' "$(find . -type f -name '*.py' -not -path './.git/*' | wc -l | tr -d ' ')"

section "Recommended next checks"
cat <<'CHECKS'
- dotnet restore src/core/HELIOS.Platform/HELIOS.Platform.csproj
- dotnet build src/core/HELIOS.Platform/HELIOS.Platform.csproj -c Release --no-restore
- bash scripts/dev/devsetup.sh --check-only
CHECKS
