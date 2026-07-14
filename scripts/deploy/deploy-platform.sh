#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
TEMPLATE_FILE="$ROOT_DIR/deployment/main.bicep"

PHASE="all"
PLATFORM_TARGET="both"
WHAT_IF="false"
LOCATION="${LOCATION:-eastus}"
ENVIRONMENT_NAME="${ENVIRONMENT_NAME:-prod}"
BASE_NAME="${BASE_NAME:-helios}"
RESOURCE_GROUP="${RESOURCE_GROUP:-}"
DEPLOY_OBSERVABILITY="${DEPLOY_OBSERVABILITY:-true}"
PARAMETERS_FILE="${PARAMETERS_FILE:-}"
DEPLOYMENT_OUTPUT_DIR="${DEPLOYMENT_OUTPUT_DIR:-$ROOT_DIR/deployment-artifacts}"
RUN_ID="${RUN_ID:-$(date -u +%Y%m%dT%H%M%SZ)}"
RUN_STARTED_AT="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
RUN_STATUS="success"
FAILED_COMMAND=""
FAILED_LINE=""
CURRENT_PHASE=""
RUN_DIRECTORY=""
MAIN_LOG_FILE=""
SUMMARY_FILE=""
STATUS_FILE=""
LATEST_SUMMARY_FILE=""
LATEST_STATUS_FILE=""
LATEST_RUN_DIR_FILE=""
DEPLOYMENT_NAMES=()

usage() {
  cat <<'EOF'
Usage: deploy-platform.sh --resource-group <name> [options]

Options:
  --phase <preflight|infrastructure|container-apps|aks|integrations|verification|all>
  --target <aca|aks|both>
  --location <azure-region>
  --environment <name>
  --base-name <name>
  --what-if <true|false>

Optional environment variables:
  DEPLOY_OBSERVABILITY=true|false
  PARAMETERS_FILE=deployment/parameters/<file>.json
  DEPLOYMENT_OUTPUT_DIR=<artifact-directory>
  SLACK_WEBHOOK_URL=<incoming-webhook>
  DEPLOYMENT_STATUS_WEBHOOK=<generic-webhook>
  HUBSPOT_TOKEN=<private-app-token>
EOF
}

timestamp() {
  date -u +%Y-%m-%dT%H:%M:%SZ
}

log() {
  local message="$1"
  local line="[$(timestamp)] $message"
  if [[ -n "$MAIN_LOG_FILE" ]]; then
    printf '%s\n' "$line" | tee -a "$MAIN_LOG_FILE"
  else
    printf '%s\n' "$line"
  fi
}

append_summary() {
  [[ -n "$SUMMARY_FILE" ]] || return 0
  printf '%s\n' "$1" >> "$SUMMARY_FILE"
}

normalize_bool() {
  case "${1,,}" in
    true|1|yes|y) echo "true" ;;
    *) echo "false" ;;
  esac
}

init_output_dir() {
  RUN_DIRECTORY="$DEPLOYMENT_OUTPUT_DIR/$ENVIRONMENT_NAME/$RUN_ID"
  mkdir -p "$RUN_DIRECTORY"

  MAIN_LOG_FILE="$RUN_DIRECTORY/deploy.log"
  SUMMARY_FILE="$RUN_DIRECTORY/deployment-summary.md"
  STATUS_FILE="$RUN_DIRECTORY/deployment-status.json"
  LATEST_SUMMARY_FILE="$DEPLOYMENT_OUTPUT_DIR/latest-summary.md"
  LATEST_STATUS_FILE="$DEPLOYMENT_OUTPUT_DIR/latest-status.json"
  LATEST_RUN_DIR_FILE="$DEPLOYMENT_OUTPUT_DIR/latest-run-dir.txt"

  mkdir -p "$DEPLOYMENT_OUTPUT_DIR"
  : > "$MAIN_LOG_FILE"
}

write_summary_header() {
  cat > "$SUMMARY_FILE" <<EOF
## HELIOS deployment run

- Run ID: $RUN_ID
- Started (UTC): $RUN_STARTED_AT
- Phase: $PHASE
- Target: $PLATFORM_TARGET
- Environment: $ENVIRONMENT_NAME
- Resource group: $RESOURCE_GROUP
- Location: $LOCATION
- What-if: $WHAT_IF
- Base name: $BASE_NAME
- Deploy observability: $DEPLOY_OBSERVABILITY
- Parameters file: ${PARAMETERS_FILE:-inline only}

## Progress
EOF
}

build_notification_payload() {
  local status_label="$1"
  local finished_at="$2"
  python3 - "$status_label" "$finished_at" <<'PY'
import json
import os
import sys

status_label, finished_at = sys.argv[1:3]
run_url = os.environ.get("RUN_URL", "")
text = (
    f"HELIOS deployment {status_label}: phase={os.environ.get('PHASE', 'all')} "
    f"target={os.environ.get('PLATFORM_TARGET', 'both')} env={os.environ.get('ENVIRONMENT_NAME', 'prod')} "
    f"rg={os.environ.get('RESOURCE_GROUP', '')} what-if={os.environ.get('WHAT_IF', 'false')}"
)
if run_url:
    text = f"{text} run={run_url}"

payload = {
    "text": text,
    "status": status_label,
    "phase": os.environ.get("PHASE", "all"),
    "target": os.environ.get("PLATFORM_TARGET", "both"),
    "environment": os.environ.get("ENVIRONMENT_NAME", "prod"),
    "resourceGroup": os.environ.get("RESOURCE_GROUP", ""),
    "location": os.environ.get("LOCATION", "eastus"),
    "whatIf": os.environ.get("WHAT_IF", "false"),
    "runId": os.environ.get("RUN_ID", ""),
    "runUrl": run_url,
    "sourceRef": os.environ.get("SOURCE_REF", ""),
    "commitSha": os.environ.get("COMMIT_SHA", ""),
    "startedAt": os.environ.get("RUN_STARTED_AT", ""),
    "finishedAt": finished_at,
    "currentPhase": os.environ.get("CURRENT_PHASE", "")
}
print(json.dumps(payload))
PY
}

post_notification() {
  local webhook_url="$1"
  local webhook_name="$2"
  local status_label="$3"
  local finished_at="$4"
  local payload

  [[ -n "$webhook_url" ]] || return 0
  payload="$(build_notification_payload "$status_label" "$finished_at")"

  if curl -fsS -X POST -H 'Content-Type: application/json' --data "$payload" "$webhook_url" >/dev/null; then
    log "Posted $status_label notification to $webhook_name"
  else
    log "Warning: failed to post $status_label notification to $webhook_name"
  fi
}

notify_start() {
  local now
  now="$(timestamp)"
  post_notification "${SLACK_WEBHOOK_URL:-}" "slack" "started" "$now"
  post_notification "${DEPLOYMENT_STATUS_WEBHOOK:-}" "deployment-status-webhook" "started" "$now"
}

write_status_json() {
  local exit_code="$1"
  local finished_at="$2"
  python3 - "$STATUS_FILE" "$RUN_STATUS" "$exit_code" "$finished_at" "${DEPLOYMENT_NAMES[*]:-}" <<'PY'
import json
import os
import sys

path, run_status, exit_code, finished_at, deployments = sys.argv[1:6]
status = {
    "status": run_status,
    "exitCode": int(exit_code),
    "phase": os.environ.get("PHASE", "all"),
    "target": os.environ.get("PLATFORM_TARGET", "both"),
    "environment": os.environ.get("ENVIRONMENT_NAME", "prod"),
    "resourceGroup": os.environ.get("RESOURCE_GROUP", ""),
    "location": os.environ.get("LOCATION", "eastus"),
    "whatIf": os.environ.get("WHAT_IF", "false"),
    "baseName": os.environ.get("BASE_NAME", "helios"),
    "deployObservability": os.environ.get("DEPLOY_OBSERVABILITY", "true"),
    "parametersFile": os.environ.get("PARAMETERS_FILE", ""),
    "runId": os.environ.get("RUN_ID", ""),
    "runUrl": os.environ.get("RUN_URL", ""),
    "sourceRef": os.environ.get("SOURCE_REF", ""),
    "commitSha": os.environ.get("COMMIT_SHA", ""),
    "startedAt": os.environ.get("RUN_STARTED_AT", ""),
    "finishedAt": finished_at,
    "currentPhase": os.environ.get("CURRENT_PHASE", ""),
    "deployments": [name for name in deployments.split() if name],
}
with open(path, "w", encoding="utf-8") as handle:
    json.dump(status, handle, indent=2)
PY
}

publish_latest_artifacts() {
  [[ -f "$SUMMARY_FILE" ]] && cp "$SUMMARY_FILE" "$LATEST_SUMMARY_FILE"
  [[ -f "$STATUS_FILE" ]] && cp "$STATUS_FILE" "$LATEST_STATUS_FILE"
  printf '%s\n' "$RUN_DIRECTORY" > "$LATEST_RUN_DIR_FILE"
}

finalize() {
  local exit_code="$1"
  local finished_at

  trap - ERR EXIT

  [[ -n "$RUN_DIRECTORY" ]] || return 0

  if [[ "$exit_code" -ne 0 ]]; then
    RUN_STATUS="failed"
  fi

  finished_at="$(timestamp)"
  append_summary ""
  append_summary "## Outcome"
  append_summary "- Status: **$RUN_STATUS**"
  append_summary "- Finished (UTC): $finished_at"
  append_summary "- Artifact directory: $RUN_DIRECTORY"
  append_summary "- Main log: $MAIN_LOG_FILE"
  append_summary "- Status JSON: $STATUS_FILE"

  if [[ "$exit_code" -ne 0 ]]; then
    append_summary "- Failed command: ${FAILED_COMMAND:-unknown}"
    append_summary "- Failed line: ${FAILED_LINE:-unknown}"
  fi

  write_status_json "$exit_code" "$finished_at"
  publish_latest_artifacts

  if [[ -n "${GITHUB_STEP_SUMMARY:-}" && -f "$SUMMARY_FILE" ]]; then
    cat "$SUMMARY_FILE" >> "$GITHUB_STEP_SUMMARY" || true
  fi

  post_notification "${SLACK_WEBHOOK_URL:-}" "slack" "$RUN_STATUS" "$finished_at"
  post_notification "${DEPLOYMENT_STATUS_WEBHOOK:-}" "deployment-status-webhook" "$RUN_STATUS" "$finished_at"
  log "Deployment artifacts available under $RUN_DIRECTORY"
}

validate_inputs() {
  case "$PHASE" in
    preflight|infrastructure|container-apps|aks|integrations|verification|all) ;;
    *)
      echo "Unsupported phase: $PHASE" >&2
      exit 1
      ;;
  esac

  case "$PLATFORM_TARGET" in
    aca|aks|both) ;;
    *)
      echo "Unsupported target: $PLATFORM_TARGET" >&2
      exit 1
      ;;
  esac

  if [[ -z "$RESOURCE_GROUP" ]]; then
    echo "RESOURCE_GROUP is required." >&2
    usage
    exit 1
  fi

  WHAT_IF="$(normalize_bool "$WHAT_IF")"
  DEPLOY_OBSERVABILITY="$(normalize_bool "$DEPLOY_OBSERVABILITY")"

  if [[ -n "$PARAMETERS_FILE" && ! -f "$PARAMETERS_FILE" ]]; then
    echo "PARAMETERS_FILE does not exist: $PARAMETERS_FILE" >&2
    exit 1
  fi
}

ensure_resource_group() {
  if az group show --name "$RESOURCE_GROUP" >/dev/null 2>&1; then
    log "Using existing resource group $RESOURCE_GROUP"
    return
  fi

  if [[ "$WHAT_IF" == "true" ]]; then
    log "Resource group $RESOURCE_GROUP does not exist and what-if mode cannot create it."
    exit 1
  fi

  log "Creating resource group $RESOURCE_GROUP in $LOCATION"
  az group create --name "$RESOURCE_GROUP" --location "$LOCATION" >/dev/null
}

run_deployment() {
  local deployment_name="$1"
  local deploy_container_apps="$2"
  local deploy_aks="$3"
  local deploy_integrations="$4"
  local output_file="$RUN_DIRECTORY/${deployment_name}.json"
  local log_file="$RUN_DIRECTORY/${deployment_name}.log"
  local -a params=()

  CURRENT_PHASE="$deployment_name"
  export CURRENT_PHASE
  DEPLOYMENT_NAMES+=("$deployment_name")

  if [[ -n "$PARAMETERS_FILE" ]]; then
    params+=("@$PARAMETERS_FILE")
  fi

  params+=(
    "location=$LOCATION"
    "environmentName=$ENVIRONMENT_NAME"
    "baseName=$BASE_NAME"
    "deployContainerApps=$deploy_container_apps"
    "deployAks=$deploy_aks"
    "deployIntegrations=$deploy_integrations"
    "deployObservability=$DEPLOY_OBSERVABILITY"
  )

  if [[ -n "${CONTROL_PLANE_IMAGE:-}" ]]; then
    params+=("controlPlaneImage=$CONTROL_PLANE_IMAGE")
  fi

  if [[ -n "${HUBSPOT_SYNC_IMAGE:-}" ]]; then
    params+=("hubspotSyncImage=$HUBSPOT_SYNC_IMAGE")
  fi

  if [[ -n "${AKS_IMAGE:-}" ]]; then
    params+=("aksImage=$AKS_IMAGE")
  fi

  if [[ -n "${SLACK_WEBHOOK_URL:-}" ]]; then
    params+=("slackWebhookUrl=$SLACK_WEBHOOK_URL")
  fi

  if [[ -n "${SLACK_CHANNEL:-}" ]]; then
    params+=("slackChannel=$SLACK_CHANNEL")
  fi

  if [[ -n "${HUBSPOT_BASE_URL:-}" ]]; then
    params+=("hubspotBaseUrl=$HUBSPOT_BASE_URL")
  fi

  if [[ -n "${HUBSPOT_TOKEN:-}" ]]; then
    params+=("hubspotToken=$HUBSPOT_TOKEN")
  fi

  log "Running deployment '$deployment_name' against '$RESOURCE_GROUP'"

  if [[ "$WHAT_IF" == "true" ]]; then
    az deployment group what-if \
      --name "$deployment_name" \
      --resource-group "$RESOURCE_GROUP" \
      --template-file "$TEMPLATE_FILE" \
      --parameters "${params[@]}" | tee "$log_file"

    append_summary "- $deployment_name: what-if completed"
    append_summary "  - Preview log: $log_file"
  else
    az deployment group create \
      --name "$deployment_name" \
      --resource-group "$RESOURCE_GROUP" \
      --template-file "$TEMPLATE_FILE" \
      --parameters "${params[@]}" \
      --output json > "$output_file"

    cp "$output_file" "$log_file"

    local state
    local outputs
    state="$(python3 - "$output_file" <<'PY'
import json
import sys
with open(sys.argv[1], encoding='utf-8') as handle:
    doc = json.load(handle)
print(doc.get('properties', {}).get('provisioningState', 'unknown'))
PY
)"
    outputs="$(python3 - "$output_file" <<'PY'
import json
import sys
with open(sys.argv[1], encoding='utf-8') as handle:
    doc = json.load(handle)
keys = sorted(doc.get('properties', {}).get('outputs', {}).keys())
print(', '.join(keys) if keys else 'none')
PY
)"

    append_summary "- $deployment_name: deployment completed"
    append_summary "  - Provisioning state: $state"
    append_summary "  - Output keys: $outputs"
    append_summary "  - Result file: $output_file"
  fi
}

run_preflight() {
  CURRENT_PHASE="preflight"
  export CURRENT_PHASE
  log "Running preflight checks"
  command -v az >/dev/null
  test -f "$TEMPLATE_FILE"
  az account show --output json > "$RUN_DIRECTORY/account.json"
  az bicep version > "$RUN_DIRECTORY/bicep-version.txt"
  append_summary "- Preflight completed"
  append_summary "  - Account details: $RUN_DIRECTORY/account.json"
  append_summary "  - Bicep version: $RUN_DIRECTORY/bicep-version.txt"
}

run_verification() {
  CURRENT_PHASE="verification"
  export CURRENT_PHASE
  log "Running post-deployment verification"
  az deployment group list \
    --resource-group "$RESOURCE_GROUP" \
    --query "[0:10].{name:name,state:properties.provisioningState}" \
    --output table | tee "$RUN_DIRECTORY/deployment-list.txt"
  az resource list \
    --resource-group "$RESOURCE_GROUP" \
    --query "[].{name:name,type:type,location:location}" \
    --output table | tee "$RUN_DIRECTORY/resource-inventory.txt"
  append_summary "- Verification completed"
  append_summary "  - Deployment list: $RUN_DIRECTORY/deployment-list.txt"
  append_summary "  - Resource inventory: $RUN_DIRECTORY/resource-inventory.txt"
}

main() {
  while [[ $# -gt 0 ]]; do
    case "$1" in
      --phase)
        PHASE="$2"
        shift 2
        ;;
      --target)
        PLATFORM_TARGET="$2"
        shift 2
        ;;
      --location)
        LOCATION="$2"
        shift 2
        ;;
      --environment)
        ENVIRONMENT_NAME="$2"
        shift 2
        ;;
      --base-name)
        BASE_NAME="$2"
        shift 2
        ;;
      --resource-group)
        RESOURCE_GROUP="$2"
        shift 2
        ;;
      --what-if)
        WHAT_IF="$2"
        shift 2
        ;;
      -h|--help)
        usage
        exit 0
        ;;
      *)
        echo "Unknown argument: $1" >&2
        usage
        exit 1
        ;;
    esac
  done

  export PHASE PLATFORM_TARGET WHAT_IF LOCATION ENVIRONMENT_NAME BASE_NAME RESOURCE_GROUP DEPLOY_OBSERVABILITY PARAMETERS_FILE RUN_ID RUN_STARTED_AT

  validate_inputs

  if [[ -n "${AZURE_SUBSCRIPTION_ID:-}" ]]; then
    az account set --subscription "$AZURE_SUBSCRIPTION_ID"
  fi

  init_output_dir
  write_summary_header
  notify_start
  ensure_resource_group

  case "$PHASE" in
    preflight)
      run_preflight
      ;;
    infrastructure)
      run_preflight
      run_deployment "helios-infrastructure-${ENVIRONMENT_NAME}" false false false
      ;;
    container-apps)
      run_preflight
      run_deployment "helios-container-apps-${ENVIRONMENT_NAME}" true false false
      ;;
    aks)
      run_preflight
      run_deployment "helios-aks-${ENVIRONMENT_NAME}" false true false
      ;;
    integrations)
      run_preflight
      run_deployment "helios-integrations-${ENVIRONMENT_NAME}" false false true
      ;;
    verification)
      run_verification
      ;;
    all)
      run_preflight
      run_deployment "helios-infrastructure-${ENVIRONMENT_NAME}" false false false
      run_deployment "helios-integrations-${ENVIRONMENT_NAME}" false false true
      if [[ "$PLATFORM_TARGET" == "aca" || "$PLATFORM_TARGET" == "both" ]]; then
        run_deployment "helios-container-apps-${ENVIRONMENT_NAME}" true false false
      fi
      if [[ "$PLATFORM_TARGET" == "aks" || "$PLATFORM_TARGET" == "both" ]]; then
        run_deployment "helios-aks-${ENVIRONMENT_NAME}" false true false
      fi
      run_verification
      ;;
  esac
}

trap 'RUN_STATUS="failed"; FAILED_COMMAND=$BASH_COMMAND; FAILED_LINE=$LINENO' ERR
trap 'finalize "$?"' EXIT

main "$@"
