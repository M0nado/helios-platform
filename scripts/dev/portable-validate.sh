#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
CONTRACT_ONLY=false

if [[ "${1:-}" == "--contract-only" ]]; then
  CONTRACT_ONLY=true
elif [[ "$#" -gt 0 ]]; then
  echo "Usage: $0 [--contract-only]" >&2
  exit 2
fi

cd "${REPO_ROOT}"

python3 scripts/dev/helios_dev_doctor.py --profile contract
python3 -m unittest discover -s scripts/dev/tests -p "test_*.py" -v
python3 -m unittest plugins/helios-control-fabric/scripts/test_helios.py -v

if [[ "${CONTRACT_ONLY}" == "true" ]]; then
  echo "HELIOS cockpit contract validation passed."
  exit 0
fi

python3 scripts/dev/helios_dev_doctor.py --profile devcontainer
shellcheck \
  .devcontainer/onCreateCommand.sh \
  scripts/dev/bootstrap-cockpit.sh \
  scripts/dev/devsetup.sh \
  scripts/dev/portable-validate.sh \
  scripts/setup/bootstrap-local-tools.sh \
  verify-setup.sh
python3 scripts/control/validate_automation_routes.py
python3 -m unittest discover \
  -s scripts/control/tests \
  -p "test_validate_automation_routes.py" \
  -v
dotnet test monado/helios-control/Helios.Connect.sln --configuration Release
cmake \
  -S src/native/HELIOS.Native.Performance \
  -B build/dev-cockpit/native \
  -DBUILD_TESTING=ON \
  -DCMAKE_BUILD_TYPE=Release
cmake --build build/dev-cockpit/native --config Release --parallel 2
ctest \
  --test-dir build/dev-cockpit/native \
  --build-config Release \
  --output-on-failure

echo "HELIOS portable validation passed. Windows/WPF remains in the hosted Windows lane."
