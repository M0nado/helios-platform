#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT"
PORT="${PORT:-8787}"
OUT_ROOT="${1:-.run/helios-auto-exe}"
export SKIP_FINISH="${SKIP_FINISH:-1}"
export PATH="$ROOT/.tools/dotnet:$ROOT/.tools/gh/bin:$ROOT/.tools/azcli-venv/bin:$PATH"

scripts/setup/build-run-exe.sh "$OUT_ROOT"
BUNDLE="$(cat .run/latest-helios-exe.txt)"
URL="http://127.0.0.1:${PORT}/web-sandbox/"
printf '%s\n' "$URL" > .run/latest-helios-exe-url.txt

if [[ "${HELIOS_AUTO_SERVE:-1}" == "0" ]]; then
  printf 'Built HELIOS EXE web sandbox: %s\n' "$BUNDLE/web-sandbox/index.html"
  printf 'Serve later with: PORT=%s %s/serve-web-sandbox.sh\n' "$PORT" "$BUNDLE"
  printf 'URL: %s\n' "$URL"
  exit 0
fi

if [[ -f .run/latest-helios-exe-server.pid ]]; then
  old_pid="$(cat .run/latest-helios-exe-server.pid 2>/dev/null || true)"
  if [[ -n "$old_pid" ]] && kill -0 "$old_pid" >/dev/null 2>&1; then
    kill "$old_pid" >/dev/null 2>&1 || true
  fi
fi

mkdir -p .run
python3 -m http.server "$PORT" --directory "$BUNDLE" > .run/latest-helios-exe-server.log 2>&1 &
server_pid="$!"
printf '%s\n' "$server_pid" > .run/latest-helios-exe-server.pid
sleep 1
if ! kill -0 "$server_pid" >/dev/null 2>&1; then
  cat .run/latest-helios-exe-server.log >&2 || true
  exit 1
fi

printf 'HELIOS EXE web sandbox is running.\n'
printf 'URL: %s\n' "$URL"
printf 'PID: %s\n' "$server_pid"
printf 'Log: .run/latest-helios-exe-server.log\n'
printf 'Stop: kill %s\n' "$server_pid"

if [[ "${HELIOS_AUTO_OPEN:-1}" == "1" ]]; then
  if command -v xdg-open >/dev/null 2>&1; then
    xdg-open "$URL" >/dev/null 2>&1 || true
  elif command -v open >/dev/null 2>&1; then
    open "$URL" >/dev/null 2>&1 || true
  elif command -v cmd.exe >/dev/null 2>&1; then
    cmd.exe /c start "" "$URL" >/dev/null 2>&1 || true
  fi
fi
