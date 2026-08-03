#!/usr/bin/env bash
set -euo pipefail
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
PROFILE_DIR="${HELIOS_BROWSER_PROFILE_DIR:-$ROOT_DIR/.helios/browser-profile}"
PRIVATE_DIR="${HELIOS_PRIVATE_DIR:-$ROOT_DIR/.helios/private}"
mkdir -p "$PROFILE_DIR" "$PRIVATE_DIR"

OPEN_ONLY="false"
BROWSER="${HELIOS_BROWSER:-}"
while [[ $# -gt 0 ]]; do
  case "$1" in
    --open-only) OPEN_ONLY="true" ;;
    --browser) shift; BROWSER="${1:-}" ;;
    *) echo "Unknown option: $1" >&2; exit 2 ;;
  esac
  shift || true
done

find_browser() {
  if [[ -n "$BROWSER" ]] && command -v "$BROWSER" >/dev/null 2>&1; then echo "$BROWSER"; return 0; fi
  for candidate in microsoft-edge msedge google-chrome chromium chromium-browser xdg-open open; do
    if command -v "$candidate" >/dev/null 2>&1; then echo "$candidate"; return 0; fi
  done
  return 1
}

launch() {
  local name="$1" url="$2" browser="$3"
  printf 'Opening %s: %s\n' "$name" "$url"
  case "$browser" in
    microsoft-edge|msedge|google-chrome|chromium|chromium-browser)
      "$browser" --user-data-dir="$PROFILE_DIR" --app="$url" >/dev/null 2>&1 &
      ;;
    *)
      "$browser" "$url" >/dev/null 2>&1 &
      ;;
  esac
}

cat > "$PRIVATE_DIR/README.md" <<'EOF'
# HELIOS private local data

This folder is intentionally ignored by git. Put manual exports, temporary downloaded reports, and browser/app scratch data here. Do not copy these files into commits unless they are sanitized durable docs/config.
EOF

if [[ "$OPEN_ONLY" != "true" ]]; then
  (cd "$ROOT_DIR" && python3 scripts/integrations/app_automation.py)
  (cd "$ROOT_DIR" && python3 scripts/dashboard/generate-gui.py)
fi

browser="$(find_browser || true)"
if [[ -z "$browser" ]]; then
  echo "No supported browser opener found. Open http://127.0.0.1:8787/ after running ./helios.sh dashboard." >&2
  exit 1
fi

launch "HELIOS GUI" "http://127.0.0.1:8787/" "$browser"
launch "Microsoft 365 Copilot" "${COPILOT_WEB_URL:-https://copilot.microsoft.com}" "$browser"
launch "ChatGPT" "https://chatgpt.com" "$browser"
launch "Claude" "https://claude.ai" "$browser"
launch "GitHub" "https://github.com" "$browser"
launch "Azure Cloud Shell" "https://shell.azure.com" "$browser"
launch "Slack" "https://app.slack.com" "$browser"
launch "Linear" "https://linear.app" "$browser"

cat <<EOF

Local HELIOS app shell launched.
- Browser profile/scratch: $PROFILE_DIR
- Private exports: $PRIVATE_DIR
- Nothing in .helios/ is committed to git.
- Use official app/browser sign-in pages; this script does not read cookies or credentials.
EOF
