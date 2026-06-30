#!/usr/bin/env bash
set -Eeuo pipefail

MANIFEST="${MANIFEST:-autosetup/manifest.yaml}"
BASE="${BASE:-repos}"
SUMMARY="${SUMMARY_PATH:-autosetup/summary.md}"
DEFAULT_REMOTE_BRANCH="${DEFAULT_REMOTE_BRANCH:-main}"
CLEAN_BRANCH="${CLEAN_BRANCH:-hermes-clean}"
MERGE_BRANCH="${MERGE_BRANCH:-hermes-merge}"
REQUIRE_AZURE_LOGIN="${REQUIRE_AZURE_LOGIN:-false}"
MERGE_ALL_REMOTE_BRANCHES="${MERGE_ALL_REMOTE_BRANCHES:-true}"

mkdir -p "$BASE" "$(dirname "$SUMMARY")"
printf '# Helios/Hermes autosetup summary\n\nStarted: %s\n\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)" > "$SUMMARY"

need() { command -v "$1" >/dev/null 2>&1 || { echo "Missing required command: $1" >&2; exit 1; }; }
repo_target() {
  local repo="$1" owner name
  owner="${repo%%/*}"
  name="${repo##*/}"
  printf '%s/%s/%s' "$BASE" "$owner" "$name"
}
log_summary() { printf '%s\n' "$1" | tee -a "$SUMMARY"; }

need git
need gh
need yq

export REQUIRE_AZURE_LOGIN
./scripts/azure-preflight.sh | tee -a "$SUMMARY"

mapfile -t repos < <(yq -r '.repos[]' "$MANIFEST")
if [[ "${#repos[@]}" -eq 0 ]]; then
  echo "No repositories listed in $MANIFEST" >&2
  exit 1
fi

log_summary "\n## Repository synchronization"
for repo in "${repos[@]}"; do
  target="$(repo_target "$repo")"
  mkdir -p "$(dirname "$target")"
  log_summary "\n### $repo"
  if [[ -d "$target/.git" ]]; then
    git -C "$target" remote set-url origin "https://github.com/$repo.git" || true
    git -C "$target" fetch --all --prune
    log_summary "- [x] fetched existing clone at \`$target\`"
  else
    gh repo clone "$repo" "$target" -- --quiet
    git -C "$target" fetch --all --prune
    log_summary "- [x] cloned to \`$target\`"
  fi

  if git -C "$target" show-ref --verify --quiet "refs/remotes/origin/$DEFAULT_REMOTE_BRANCH"; then
    remote_branch="$DEFAULT_REMOTE_BRANCH"
  else
    remote_branch="$(git -C "$target" remote show origin | awk '/HEAD branch/ {print $NF}')"
  fi
  [[ -n "$remote_branch" ]] || { echo "Unable to determine remote branch for $repo" >&2; exit 1; }

  git -C "$target" checkout -B "$CLEAN_BRANCH" "origin/$remote_branch"
  log_summary "- [x] reset \`$CLEAN_BRANCH\` from \`origin/$remote_branch\`"

  git -C "$target" checkout -B "$MERGE_BRANCH"
  git -C "$target" merge --no-edit -X theirs "$CLEAN_BRANCH"

  if [[ "$MERGE_ALL_REMOTE_BRANCHES" == "true" ]]; then
    mapfile -t remote_branches < <(git -C "$target" for-each-ref --format='%(refname:short)' refs/remotes/origin       | sed '/^origin\/HEAD$/d'       | sed "s#^origin/##"       | sort -u)
    for branch in "${remote_branches[@]}"; do
      [[ "$branch" == "$remote_branch" ]] && continue
      log_summary "- [ ] merging remote branch \`origin/$branch\`"
      git -C "$target" merge --no-edit -X theirs "origin/$branch"
      log_summary "- [x] merged \`origin/$branch\`"
    done
  fi

  log_summary "- [x] prepared merge branch \`$MERGE_BRANCH\`"
done

BASE="$BASE" SUMMARY_PATH="$SUMMARY" ./scripts/validate-consolidation.sh "$BASE"

log_summary "\n## Next manual integration gates"
log_summary "- Review helios-control and hermes-fleet-production first."
log_summary "- Wire C# frontend/install/security, WinUI 3 shell, C++ performance backend, F# analytics, Python AIHub, and Hermes XCore specialists by repo-specific PRs."
log_summary "- Run build/test/security validation in each cloned repository before pushing merge branches."
log_summary "\nCompleted: $(date -u +%Y-%m-%dT%H:%M:%SZ)"
