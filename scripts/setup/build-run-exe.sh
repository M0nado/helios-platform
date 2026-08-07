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
if [[ -d "$OUT_ROOT" ]] && find "$OUT_ROOT" -mindepth 1 -print -quit | grep -q .; then
  printf 'Launcher output directory must be empty: %s\n' "$OUT_ROOT" >&2
  exit 1
fi
mkdir -p "$OUT_ROOT"
OUT_ROOT="$(cd "$OUT_ROOT" && pwd)"
if [[ "$SKIP_FINISH" != "1" ]]; then
  scripts/setup/simple-build.sh finish
  scripts/setup/simple-build.sh save-run
fi
# Materialize the committed source before publishing. This ensures local staged
# or unstaged edits can never leak into an executable attributed to HEAD.
SOURCE_COMMIT="$(git rev-parse --verify HEAD)"
PROJECT_REL="${PROJECT#./}"
if [[ "$PROJECT_REL" = /* || "/$PROJECT_REL/" = *"/../"* ]]; then
  printf 'Launcher project must be a repository-relative path: %s\n' "$PROJECT" >&2
  exit 1
fi
[[ "$(git cat-file -t "${SOURCE_COMMIT}:${PROJECT_REL}" 2>/dev/null || true)" == "blob" ]] || {
  printf 'Launcher project must be a tracked path in the source commit: %s\n' "$PROJECT" >&2
  exit 1
}
mkdir -p "$OUT_ROOT/repository"
git archive --format=tar "$SOURCE_COMMIT" | tar -xf - -C "$OUT_ROOT/repository"
git ls-tree -r --name-only "$SOURCE_COMMIT" > "$OUT_ROOT/repository/.helios-tracked-files"
printf '%s\n' "$SOURCE_COMMIT" > "$OUT_ROOT/repository/HELIOS_PACKAGE_COMMIT"
BUILD_ROOT="$(mktemp -d "${TMPDIR:-/tmp}/helios-launcher-source.XXXXXX")"
trap 'rm -rf "$BUILD_ROOT"' EXIT
git archive --format=tar "$SOURCE_COMMIT" | tar -xf - -C "$BUILD_ROOT"
for rid in win-x64 linux-x64; do
  dotnet publish "$BUILD_ROOT/$PROJECT_REL" -c "$CONFIGURATION" -r "$rid" --self-contained true \
    -p:PublishSingleFile=true -p:DebugType=embedded -o "$OUT_ROOT/$rid" --nologo
done
rm -rf "$BUILD_ROOT"
trap - EXIT
# Include an immutable source snapshot so the package is immediately usable
# without a second clone. git archive excludes .git, ignored local state,
# credentials, caches, databases, build output, and other generated files.
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

This folder contains self-contained, single-file HELIOS launchers and an
immutable repository snapshot. A separate .NET installation or source clone is
not required to launch readiness checks. Tool installation may still be needed
for build, test, GitHub, or Azure capabilities.

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

The bundled `repository/` directory is the exact source snapshot identified by
`repository/HELIOS_PACKAGE_COMMIT`. Pass `--repo PATH` to use another checkout.
EOF
(
  cd "$OUT_ROOT"
  if command -v zip >/dev/null 2>&1; then
    zip -q -r helios-portable-win-x64.zip win-x64 repository run-helios.cmd README.md
  fi
  tar -czf helios-portable-linux-x64.tar.gz linux-x64 repository run-helios.sh README.md
  if command -v sha256sum >/dev/null 2>&1; then
    sha256sum win-x64/HELIOS.Platform.exe linux-x64/HELIOS.Platform \
      helios-portable-linux-x64.tar.gz helios-portable-win-x64.zip 2>/dev/null > SHA256SUMS || \
      sha256sum win-x64/HELIOS.Platform.exe linux-x64/HELIOS.Platform helios-portable-linux-x64.tar.gz > SHA256SUMS
  fi
)
mkdir -p .run
printf '%s\n' "$OUT_ROOT" > .run/latest-helios-exe.txt
printf 'Built runnable HELIOS outputs in %s\n' "$OUT_ROOT"
printf 'Windows exe: %s\n' "$OUT_ROOT/win-x64/HELIOS.Platform.exe"
printf 'Linux launcher: %s\n' "$OUT_ROOT/run-helios.sh"
printf 'Checksums: %s\n' "$OUT_ROOT/SHA256SUMS"
