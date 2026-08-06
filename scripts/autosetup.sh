#!/usr/bin/env bash
set -Eeuo pipefail

MANIFEST="${MANIFEST:-autosetup/manifest.yaml}"
BASE="${BASE:-repos}"
SUMMARY="${SUMMARY_PATH:-autosetup/summary.md}"
DEFAULT_REMOTE_BRANCH="${DEFAULT_REMOTE_BRANCH:-main}"
CLEAN_BRANCH="${CLEAN_BRANCH:-hermes-clean}"
MERGE_BRANCH="${MERGE_BRANCH:-hermes-merge}"
REQUIRE_AZURE_LOGIN="${REQUIRE_AZURE_LOGIN:-false}"
INSTALL_AZURE_CLI="${INSTALL_AZURE_CLI:-false}"
MERGE_ALL_REMOTE_BRANCHES="${MERGE_ALL_REMOTE_BRANCHES:-true}"
PUSH_MERGE_BRANCHES="${PUSH_MERGE_BRANCHES:-false}"

mkdir -p "$BASE" "$(dirname "$SUMMARY")"
printf '# Helios/Hermes autosetup summary\n\nStarted: %s\n\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)" > "$SUMMARY"

need() { command -v "$1" >/dev/null 2>&1 || { echo "Missing required command: $1" >&2; exit 1; }; }
log_summary() { printf '%s\n' "$1" | tee -a "$SUMMARY"; }
repo_target() {
  local repo="$1" owner name
  owner="${repo%%/*}"
  name="${repo##*/}"
  printf '%s/%s/%s' "$BASE" "$owner" "$name"
}
read_manifest_repos() {
  awk '
    /^repos:[[:space:]]*$/ { in_repos=1; next }
    /^[^[:space:]-][^:]*:[[:space:]]*$/ { if (in_repos) exit }
    in_repos && /^[[:space:]]*-[[:space:]]+/ {
      sub(/^[[:space:]]*-[[:space:]]*/, "")
      gsub(/["'"'"']/, "")
      print
    }
  ' "$MANIFEST"
}
remote_default_branch() {
  local target="$1" branch
  if git -C "$target" show-ref --verify --quiet "refs/remotes/origin/$DEFAULT_REMOTE_BRANCH"; then
    printf '%s' "$DEFAULT_REMOTE_BRANCH"
    return 0
  fi
  branch="$(git -C "$target" remote show origin | awk '/HEAD branch/ {print $NF}')"
  [[ -n "$branch" && "$branch" != "(unknown)" ]] || return 1
  printf '%s' "$branch"
}
clone_or_fetch_repo() {
  local repo="$1" target="$2"
  mkdir -p "$(dirname "$target")"
  if [[ -d "$target/.git" ]]; then
    git -C "$target" remote set-url origin "https://github.com/$repo.git" || true
    git -C "$target" fetch --all --prune --tags
    log_summary "- [x] fetched existing clone at \`$target\`"
  else
    if command -v gh >/dev/null 2>&1; then
      gh repo clone "$repo" "$target" -- --quiet
    else
      git clone --quiet "https://github.com/$repo.git" "$target"
    fi
    git -C "$target" fetch --all --prune --tags
    log_summary "- [x] cloned to \`$target\`"
  fi
}
merge_remote_branches() {
  local target="$1" remote_branch="$2" branch
  [[ "$MERGE_ALL_REMOTE_BRANCHES" == "true" ]] || return 0
  while IFS= read -r branch; do
    [[ -n "$branch" ]] || continue
    [[ "$branch" == "$remote_branch" ]] && continue
    log_summary "- [ ] merging remote branch \`origin/$branch\`"
    git -C "$target" merge --no-edit -X theirs "origin/$branch"
    log_summary "- [x] merged \`origin/$branch\`"
  done < <(git -C "$target" for-each-ref --format='%(refname:short)' refs/remotes/origin \
    | sed '/^origin\/HEAD$/d' \
    | sed 's#^origin/##' \
    | sort -u)
}

need git
[[ -f "$MANIFEST" ]] || { echo "Manifest not found: $MANIFEST" >&2; exit 1; }

export REQUIRE_AZURE_LOGIN INSTALL_AZURE_CLI
./scripts/azure-preflight.sh | tee -a "$SUMMARY"

mapfile -t repos < <(read_manifest_repos)
if [[ "${#repos[@]}" -eq 0 ]]; then
  echo "No repositories listed in $MANIFEST" >&2
  exit 1
fi

log_summary "\n## Repository synchronization"
for repo in "${repos[@]}"; do
  target="$(repo_target "$repo")"
  log_summary "\n### $repo"
  clone_or_fetch_repo "$repo" "$target"

  remote_branch="$(remote_default_branch "$target")" || {
    echo "Unable to determine remote branch for $repo" >&2
    exit 1
  }

  git -C "$target" checkout -B "$CLEAN_BRANCH" "origin/$remote_branch"
  log_summary "- [x] reset \`$CLEAN_BRANCH\` from \`origin/$remote_branch\`"

  git -C "$target" checkout -B "$MERGE_BRANCH"
  git -C "$target" merge --no-edit -X theirs "$CLEAN_BRANCH"
  merge_remote_branches "$target" "$remote_branch"
  log_summary "- [x] prepared merge branch \`$MERGE_BRANCH\`"

  if [[ "$PUSH_MERGE_BRANCHES" == "true" ]]; then
    git -C "$target" push --set-upstream origin "$MERGE_BRANCH"
    log_summary "- [x] pushed \`$MERGE_BRANCH\`"
  else
    log_summary "- [ ] push skipped; set PUSH_MERGE_BRANCHES=true to publish \`$MERGE_BRANCH\`"
  fi
done

BASE="$BASE" SUMMARY_PATH="$SUMMARY" ./scripts/validate-consolidation.sh "$BASE"

log_summary "\n## Next integration gates"
log_summary "- Review helios-control and hermes-fleet-production first; they are critical focus repos in autosetup/manifest.yaml."
log_summary "- Wire C# frontend/install/security, WinUI 3 shell, C++ performance backend, F# analytics, Python AIHub, and Hermes XCore specialists by repo-specific PRs."
log_summary "- Run build/test/security validation in each cloned repository before setting PUSH_MERGE_BRANCHES=true."
log_summary "\nCompleted: $(date -u +%Y-%m-%dT%H:%M:%SZ)"
