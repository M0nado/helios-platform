#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
SCRIPT="$ROOT/scripts/setup/bootstrap-local-tools.sh"
TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT

mkdir -p "$TMP/bin"
cat >"$TMP/bin/gh" <<'EOF'
#!/usr/bin/env bash
[ "$1 $2" = "auth status" ]
EOF
cat >"$TMP/bin/az" <<'EOF'
#!/usr/bin/env bash
[ "$1 $2" = "account show" ]
EOF
chmod +x "$TMP/bin/gh" "$TMP/bin/az"

help_output="$(HOME="$TMP/home" PATH="$TMP/bin:$PATH" bash "$SCRIPT" --help)"
[[ "$help_output" == *"protected secret channel"* ]]

verify_output="$(HOME="$TMP/home" PATH="$TMP/bin:$PATH" bash "$SCRIPT" --verify)"
[[ "$verify_output" == *"GitHub CLI authentication: ready"* ]]
[[ "$verify_output" == *"Azure CLI authentication: ready"* ]]

set +e
PATH="/usr/bin:/bin" HOME="$TMP/home" bash "$SCRIPT" --verify >/dev/null 2>&1
status=$?
set -e
[ "$status" -ne 0 ]

# Setup entry points install tools without requiring an authenticated workstation.
for caller in \
  "$ROOT/scripts/setup/finish-easy-setup.sh" \
  "$ROOT/scripts/setup/agent-runner-easy-setup.sh" \
  "$ROOT/scripts/setup/helios-dev.sh"; do
  grep -q 'bootstrap-local-tools.sh --install-only' "$caller"
  grep -q 'XDG_DATA_HOME.*HOME/.local/share.*helios/tools' "$caller"
done

# The Python apply runner honors both the XDG default and its explicit override.
ROOT="$ROOT" HOME="$TMP/home" XDG_DATA_HOME="$TMP/xdg" python3 - <<'PY'
import importlib.util
import os
from pathlib import Path

root = Path(os.environ['ROOT'])
spec = importlib.util.spec_from_file_location(
    'finish_readiness_apply', root / 'scripts/apply/finish_readiness_apply.py'
)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)

xdg_tools = Path(os.environ['XDG_DATA_HOME']) / 'helios' / 'tools'
assert module.tool_path_env()['PATH'].startswith(str(xdg_tools / 'dotnet') + ':')

override = Path(os.environ['HOME']) / 'custom-tools'
os.environ['HELIOS_TOOLS_DIR'] = str(override)
assert module.tool_path_env()['PATH'].startswith(str(override / 'dotnet') + ':')
PY

echo "bootstrap-local-tools tests passed"
