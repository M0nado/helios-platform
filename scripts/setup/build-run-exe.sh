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
  dotnet publish "$PROJECT" -c "$CONFIGURATION" -r "$rid" --self-contained false -p:PublishSingleFile=false -o "$OUT_ROOT/$rid" --nologo
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
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$(dirname "$0")/linux-x64"
if [[ -x ./HELIOS.Platform ]]; then
  "$ROOT/.tools/dotnet/dotnet" HELIOS.Platform.dll "$@" 2>/dev/null || dotnet HELIOS.Platform.dll "$@"
else
  "$ROOT/.tools/dotnet/dotnet" HELIOS.Platform.dll "$@" 2>/dev/null || dotnet HELIOS.Platform.dll "$@"
fi
EOF
chmod +x "$OUT_ROOT/run-helios.sh" || true
cat > "$OUT_ROOT/README.md" <<'EOF'
# HELIOS runnable build

This folder contains framework-dependent HELIOS runnable outputs.

## Windows

Double-click or run:

```cmd
run-helios.cmd
```

Direct executable:

```cmd
win-x64\HELIOS.Platform.exe
```

## Linux / WSL

```bash
./run-helios.sh
```

## Dashboard bundle

If generated, the latest AIHub dashboard bundle path is recorded in:

```text
.run/latest-aihub-bundle.txt
```

This build is framework-dependent. Install .NET 8 runtime on the target machine if needed, or run from this repo with `.tools/dotnet` available.
EOF
printf '%s\n' "$OUT_ROOT" > .run/latest-helios-exe.txt
printf 'Built runnable HELIOS outputs in %s\n' "$OUT_ROOT"
printf 'Windows exe: %s\n' "$OUT_ROOT/win-x64/HELIOS.Platform.exe"
printf 'Linux launcher: %s\n' "$OUT_ROOT/run-helios.sh"
