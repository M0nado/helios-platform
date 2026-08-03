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

echo "bootstrap-local-tools tests passed"
