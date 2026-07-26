#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "${REPO_ROOT}"

python3 scripts/dev/helios_dev_doctor.py --profile contract

if [[ -f "/.dockerenv" ]]; then
  exec bash scripts/dev/bootstrap-cockpit.sh
fi

echo
echo "The HELIOS configuration contract is valid."
echo "For the pinned toolchain, open this repository in GitHub Codespaces or run:"
echo "  Dev Containers: Reopen in Container"
echo
echo "No environment file, dependency graph, Git hook, or credential was changed."
