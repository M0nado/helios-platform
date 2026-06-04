#!/usr/bin/env bash
# HELIOS Quick Start USB Installer - Linux/macOS Launcher
# Requires root / sudo privileges

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo ""
echo " ============================================================"
echo "  HELIOS Quick Start USB Installer - Linux/macOS Launcher"
echo " ============================================================"
echo ""

# Check for root privileges
if [[ "$EUID" -ne 0 ]]; then
    echo " [ERROR] This installer must be run as root."
    echo " Usage: sudo bash run_installer.sh"
    echo ""
    exit 1
fi

# Verify Python 3.10+ is available
if ! command -v python3 &>/dev/null; then
    echo " [ERROR] python3 not found. Install Python 3.10+ before continuing."
    exit 1
fi

PY_VER=$(python3 -c "import sys; print(f'{sys.version_info.major}.{sys.version_info.minor}')")
echo " Detected Python ${PY_VER}"

# Install dependencies
echo " Installing Python dependencies ..."
python3 -m pip install --upgrade pip --quiet
python3 -m pip install -r "${SCRIPT_DIR}/requirements.txt" --quiet || \
    echo " [WARNING] Some dependencies failed to install. Continuing anyway."

echo " Launching HELIOS installer ..."
echo ""
python3 "${SCRIPT_DIR}/main.py" "$@"
