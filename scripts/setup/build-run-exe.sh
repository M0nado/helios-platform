#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT"
export PATH="$ROOT/.tools/dotnet:$ROOT/.tools/gh/bin:$ROOT/.tools/azcli-venv/bin:$PATH"
PROJECT="${HELIOS_EXE_PROJECT:-src/core/HELIOS.Platform.Minimal/HELIOS.Platform.csproj}"
CONFIGURATION="${CONFIGURATION:-Release}"
STAMP="$(date -u +%Y%m%dT%H%M%SZ)"
OUT_ROOT="${1:-.run/helios-exe-$STAMP}"
SKIP_FINISH="${SKIP_FINISH:-0}"
mkdir -p "$OUT_ROOT"
if [[ "$SKIP_FINISH" != "1" ]]; then
  scripts/setup/simple-build.sh finish
  scripts/setup/simple-build.sh save-run
fi
for rid in win-x64 linux-x64; do
  dotnet publish "$PROJECT" -c "$CONFIGURATION" -r "$rid" --self-contained true \
    -p:PublishSingleFile=true -p:DebugType=embedded -o "$OUT_ROOT/$rid" --nologo
done
cat > "$OUT_ROOT/run-helios.cmd" <<'EOF'
@echo off
setlocal
cd /d "%~dp0\win-x64"
if exist HELIOS.Platform.exe (
  HELIOS.Platform.exe %*
) else (
  dotnet HELIOS.Platform.dll %*
)
EOF
cat > "$OUT_ROOT/run-helios.sh" <<'EOF'
#!/usr/bin/env bash
set -euo pipefail
HERE="$(cd "$(dirname "$0")" && pwd)"
exec "$HERE/linux-x64/HELIOS.Platform" "$@"
EOF
chmod +x "$OUT_ROOT/run-helios.sh" || true
cat > "$OUT_ROOT/README.md" <<'EOF'
# HELIOS runnable build

This folder contains self-contained, single-file HELIOS launchers. A separate
.NET installation is not required.

## Windows

Double-click or run:

```cmd
run-helios.cmd
```

Direct executable (run it inside the HELIOS checkout, or pass `--repo PATH`):

```cmd
win-x64\HELIOS.Platform.exe
```

## Linux / WSL

```bash
./run-helios.sh
```

## One-button start

From the HELIOS repository root:

```cmd
path\to\run-helios.cmd start
```

or:

```bash
path/to/run-helios.sh start
```

Use `dashboard` instead of `start` to validate and serve the local dashboard.
Use `--dry-run` to preview the exact safe pipeline command.

## Safety

The launcher invokes the repository's existing safe setup and reporting paths.
It does not merge branches, publish releases, mutate Azure resources, or deploy
to production. Those actions remain behind repository and Azure approval gates.

## Dashboard bundle

If generated, the latest AIHub dashboard bundle path is recorded in:

```text
.run/latest-aihub-bundle.txt
```

The HELIOS source checkout is still required because the launcher executes the
versioned scripts and configuration from that checkout.
EOF
(
  cd "$OUT_ROOT"
  if command -v zip >/dev/null 2>&1; then
    zip -q -r helios-win-x64.zip win-x64 run-helios.cmd README.md
  fi
  tar -czf helios-linux-x64.tar.gz linux-x64 run-helios.sh README.md
  if command -v sha256sum >/dev/null 2>&1; then
    sha256sum win-x64/HELIOS.Platform.exe linux-x64/HELIOS.Platform \
      helios-linux-x64.tar.gz ${ZIP_CHECKSUM_FILE:-helios-win-x64.zip} 2>/dev/null > SHA256SUMS || \
      sha256sum win-x64/HELIOS.Platform.exe linux-x64/HELIOS.Platform helios-linux-x64.tar.gz > SHA256SUMS
  fi
)
printf '%s\n' "$OUT_ROOT" > .run/latest-helios-exe.txt
printf 'Built runnable HELIOS outputs in %s\n' "$OUT_ROOT"
printf 'Windows exe: %s\n' "$OUT_ROOT/win-x64/HELIOS.Platform.exe"
printf 'Linux launcher: %s\n' "$OUT_ROOT/run-helios.sh"
printf 'Checksums: %s\n' "$OUT_ROOT/SHA256SUMS"
