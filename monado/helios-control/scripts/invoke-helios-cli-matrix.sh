#!/usr/bin/env bash
set -Eeuo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
CONFIG="${ROOT_DIR}/config/cli-matrix.json"
INCLUDE_NETWORK=false
[[ "${1:-}" == '--include-network-tools' ]] && INCLUDE_NETWORK=true
command -v jq >/dev/null || { echo 'jq is required to read config/cli-matrix.json' >&2; exit 2; }

RESULT_DIR="$(mktemp -d)"
trap 'rm -rf "$RESULT_DIR"' EXIT

run_check() {
  local item="$1" id command_name network_required required pinned_version output status status_label detected_version
  id="$(jq -r '.id' <<<"$item")"
  command_name="$(jq -r '.command' <<<"$item")"
  network_required="$(jq -r '.networkRequired' <<<"$item")"
  required="$(jq -r '.required' <<<"$item")"
  pinned_version="$(jq -r '.pinnedVersion // ""' <<<"$item")"
  if [[ "$network_required" == true && "$INCLUDE_NETWORK" != true ]]; then
    jq -nc --arg id "$id" --arg expectedVersion "$pinned_version" --argjson required "$required" \
      '{id:$id,found:false,status:"network-check-skipped",version:null,expectedVersion:(if $expectedVersion=="" then null else $expectedVersion end),required:$required}' > "${RESULT_DIR}/${id}.json"
    return
  fi
  if ! command -v "$command_name" >/dev/null; then
    jq -nc --arg id "$id" --arg expectedVersion "$pinned_version" --argjson required "$required" \
      '{id:$id,found:false,status:"missing",version:null,expectedVersion:(if $expectedVersion=="" then null else $expectedVersion end),required:$required}' > "${RESULT_DIR}/${id}.json"
    return
  fi
  mapfile -t arguments < <(jq -r '.arguments[]' <<<"$item")
  set +e
  output="$($command_name "${arguments[@]}" 2>&1)"
  status=$?
  set -e
  output="$(sed -n '/./{s/[[:space:]]\+/ /g;p;q;}' <<<"$output" | cut -c1-180)"
  status_label="error"
  if [[ $status -eq 0 ]]; then
    status_label="ready"
  fi
  if [[ "$status_label" == "ready" && -n "$pinned_version" ]]; then
    detected_version="$(grep -Eo '[0-9]+\.[0-9]+\.[0-9]+([-+][0-9A-Za-z.-]+)?' <<<"$output" | head -n1 || true)"
    if [[ -z "$detected_version" || "$detected_version" != "$pinned_version" ]]; then
      status_label="version-mismatch"
    fi
  fi
  jq -nc --arg id "$id" --arg version "$output" --arg status "$status_label" --arg expectedVersion "$pinned_version" --argjson required "$required" \
    '{id:$id,found:true,status:$status,version:$version,expectedVersion:(if $expectedVersion=="" then null else $expectedVersion end),required:$required}' > "${RESULT_DIR}/${id}.json"
}

while IFS= read -r item; do run_check "$item" & done < <(jq -c '.tools[]' "$CONFIG")
wait
missing_required="$(jq -s '[.[] | select(.required and .status != "ready")] | length' "${RESULT_DIR}"/*.json)"
jq -s --argjson missingRequired "$missing_required" \
  '{schemaVersion:1,ready:($missingRequired == 0),missingRequired:$missingRequired,tools:sort_by(.id)}' \
  "${RESULT_DIR}"/*.json
[[ "$missing_required" -eq 0 ]]
