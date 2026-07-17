#!/usr/bin/env bash
set -euo pipefail
root="${1:-.}"
az bicep install
az bicep build --file "$root/infra/bicep/main.bicep" --stdout >/dev/null
echo "Bicep build passed."

