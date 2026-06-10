#!/usr/bin/env bash
# Bootstrap local HELIOS developer tools without committing tool binaries.
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
TOOLS_DIR="${HELIOS_TOOLS_DIR:-${ROOT_DIR}/.tools}"
DOTNET_DIR="${HELIOS_DOTNET_DIR:-${TOOLS_DIR}/dotnet}"
AZURE_CLI_DIR="${HELIOS_AZURE_CLI_DIR:-${TOOLS_DIR}/azure-cli}"
DOTNET_CHANNEL="${HELIOS_DOTNET_CHANNEL:-8.0}"
INSTALL_AZURE_CLI="${HELIOS_INSTALL_AZURE_CLI:-1}"
ENV_FILE="${TOOLS_DIR}/env.sh"

mkdir -p "${TOOLS_DIR}"

log() {
    printf '[bootstrap-local-tools] %s\n' "$*"
}

ensure_dotnet() {
    if [[ -x "${DOTNET_DIR}/dotnet" ]]; then
        log "Using existing .NET SDK at ${DOTNET_DIR}"
        return
    fi

    local installer="${ROOT_DIR}/dotnet-install.sh"
    if [[ ! -f "${installer}" ]]; then
        installer="${TOOLS_DIR}/dotnet-install.sh"
        log "Downloading dotnet-install.sh"
        curl -fsSL https://dot.net/v1/dotnet-install.sh -o "${installer}"
        chmod +x "${installer}"
    fi

    log "Installing .NET SDK channel ${DOTNET_CHANNEL} to ${DOTNET_DIR}"
    mkdir -p "${DOTNET_DIR}"
    bash "${installer}" --channel "${DOTNET_CHANNEL}" --install-dir "${DOTNET_DIR}" --no-path
}

ensure_azure_cli() {
    if [[ "${INSTALL_AZURE_CLI}" == "0" ]]; then
        log "Skipping Azure CLI install because HELIOS_INSTALL_AZURE_CLI=0"
        return
    fi

    if [[ -x "${AZURE_CLI_DIR}/bin/az" ]]; then
        log "Using existing Azure CLI at ${AZURE_CLI_DIR}"
        return
    fi

    log "Installing Azure CLI into Python virtual environment ${AZURE_CLI_DIR}"
    python3 -m venv "${AZURE_CLI_DIR}"
    "${AZURE_CLI_DIR}/bin/pip" install --upgrade pip
    "${AZURE_CLI_DIR}/bin/pip" install azure-cli
}

write_env_file() {
    cat > "${ENV_FILE}" <<EOF
# Source this file to use HELIOS-local toolchain binaries.
export DOTNET_ROOT="${DOTNET_DIR}"
export PATH="${DOTNET_DIR}:${AZURE_CLI_DIR}/bin:\${PATH}"
EOF
    log "Wrote ${ENV_FILE}"
}

ensure_dotnet
ensure_azure_cli
write_env_file

# Print concise verification lines for CI logs.
"${DOTNET_DIR}/dotnet" --info | sed -n '1,8p'
if [[ -x "${AZURE_CLI_DIR}/bin/az" ]]; then
    "${AZURE_CLI_DIR}/bin/az" --version | sed -n '1,8p'
fi

log "Done. Run: source ${ENV_FILE}"
