#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "${REPO_ROOT}"

if [[ "${1:-}" == "--devcontainer" ]]; then
  python3 scripts/dev/helios_dev_doctor.py --profile devcontainer
elif [[ "$#" -eq 0 ]]; then
  python3 scripts/dev/helios_dev_doctor.py --profile contract
else
  echo "Usage: $0 [--devcontainer]" >&2
  exit 2
fi
