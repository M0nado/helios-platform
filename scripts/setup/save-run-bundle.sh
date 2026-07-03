#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT"
STAMP="$(date -u +%Y%m%dT%H%M%SZ)"
OUT="${1:-.run/aihub-control-plane-$STAMP}"
mkdir -p "$OUT"
mkdir -p "$OUT/reports" "$OUT/status-site" "$OUT/config" "$OUT/scripts"
if [[ -d reports ]]; then cp -a reports/. "$OUT/reports/"; fi
if [[ -d status-site ]]; then cp -a status-site/. "$OUT/status-site/"; fi
cp -a config/aihub-learning-knowledge-store.example.json "$OUT/config/" 2>/dev/null || true
cat > "$OUT/RUN.md" <<'EOF'
# HELIOS AIHub saved run bundle

This bundle is report-only output that can be opened locally, archived, or uploaded as a CI artifact.

## Open

```bash
python3 -m http.server 8080 --directory status-site
```

Then open <http://127.0.0.1:8080/>.

## Refresh from repo root

```bash
export PATH="$PWD/.tools/dotnet:$PWD/.tools/gh/bin:$PWD/.tools/azcli-venv/bin:$PATH"
scripts/setup/simple-build.sh finish
scripts/setup/simple-build.sh save-run
```

## Important

- This bundle does not contain live credentials.
- Use Azure Key Vault / environment variables for secrets.
- Live GitHub/Azure mutations remain gated by auth and live flags.
EOF
cat > "$OUT/run-local.sh" <<'EOF'
#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"
python3 -m http.server "${PORT:-8080}" --directory status-site
EOF
chmod +x "$OUT/run-local.sh"
printf '%s\n' "$OUT" > .run/latest-aihub-bundle.txt
printf 'Saved AIHub run bundle to %s\n' "$OUT"
