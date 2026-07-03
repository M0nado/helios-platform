#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT"
export PATH="$ROOT/.tools/dotnet:$ROOT/.tools/gh/bin:$ROOT/.tools/azcli-venv/bin:$PATH"
if ! command -v dotnet >/dev/null 2>&1; then
  scripts/setup/bootstrap-local-tools.sh
  export PATH="$ROOT/.tools/dotnet:$ROOT/.tools/gh/bin:$ROOT/.tools/azcli-venv/bin:$PATH"
fi
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
python3 - "$OUT_ROOT" <<'PYZIP'
import sys
import zipfile
from pathlib import Path

out = Path(sys.argv[1])
for rid in ("win-x64", "linux-x64"):
    source = out / rid
    archive = out / f"{rid}.zip"
    with zipfile.ZipFile(archive, "w", zipfile.ZIP_DEFLATED) as bundle:
        for item in sorted(source.rglob("*")):
            if item.is_file():
                bundle.write(item, item.relative_to(out))
PYZIP
mkdir -p "$OUT_ROOT/web-sandbox"
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
cat > "$OUT_ROOT/serve-web-sandbox.sh" <<'EOF'
#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"
PORT="${PORT:-8787}"
echo "Serving HELIOS EXE web sandbox at http://127.0.0.1:${PORT}/web-sandbox/"
python3 -m http.server "$PORT"
EOF
chmod +x "$OUT_ROOT/serve-web-sandbox.sh" || true
cat > "$OUT_ROOT/serve-web-sandbox.cmd" <<'EOF'
@echo off
setlocal
cd /d "%~dp0"
if "%PORT%"=="" set PORT=8787
echo Serving HELIOS EXE web sandbox at http://127.0.0.1:%PORT%/web-sandbox/
python -m http.server %PORT%
EOF
cat > "$OUT_ROOT/web-sandbox/index.html" <<'EOF'
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>HELIOS EXE Web Sandbox</title>
  <style>
    :root { color-scheme: dark; font-family: Inter, Segoe UI, system-ui, sans-serif; background: #07111f; color: #eef6ff; }
    body { margin: 0; min-height: 100vh; background: radial-gradient(circle at top left, #1e7bff55, transparent 34rem), #07111f; }
    main { max-width: 1100px; margin: 0 auto; padding: 3rem 1.25rem; }
    .hero, .card { border: 1px solid #57b8ff55; border-radius: 24px; background: #0d1d33dd; box-shadow: 0 20px 70px #0008; }
    .hero { padding: 2rem; margin-bottom: 1rem; }
    h1 { margin: 0 0 .5rem; font-size: clamp(2rem, 5vw, 4rem); }
    .grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(260px, 1fr)); gap: 1rem; }
    .card { padding: 1.25rem; }
    a.button, button { display: inline-flex; align-items: center; gap: .5rem; border: 0; border-radius: 999px; padding: .8rem 1rem; margin: .25rem .25rem .25rem 0; background: linear-gradient(135deg, #54d6ff, #8f7cff); color: #061120; font-weight: 800; text-decoration: none; cursor: pointer; }
    code, pre { background: #061120; color: #bbf7d0; border-radius: 12px; }
    pre { padding: 1rem; overflow-x: auto; }
    .pill { display: inline-flex; border: 1px solid #57b8ff77; border-radius: 999px; padding: .35rem .7rem; color: #9ee7ff; margin-right: .4rem; }
  </style>
</head>
<body>
  <main>
    <section class="hero">
      <span class="pill">Sandbox</span><span class="pill">No secrets</span><span class="pill">Local download</span>
      <h1>HELIOS runnable EXE</h1>
      <p>Use this local web sandbox to download the Windows EXE bundle or launch the Linux/WSL build from one easy page.</p>
      <a class="button" href="../win-x64.zip" download>⬇ Download Windows bundle</a>
      <a class="button" href="../linux-x64.zip" download>⬇ Download Linux bundle</a>
      <a class="button" href="../win-x64/HELIOS.Platform.exe" download>⚡ Download EXE only</a>
    </section>
    <section class="grid">
      <article class="card">
        <h2>1. Start this page</h2>
        <pre><code>./serve-web-sandbox.sh</code></pre>
        <p>Then open <code>http://127.0.0.1:8787/web-sandbox/</code>.</p>
      </article>
      <article class="card">
        <h2>2. Windows run</h2>
        <pre><code>run-helios.cmd</code></pre>
        <p>Or double-click <code>win-x64\HELIOS.Platform.exe</code> after extracting the ZIP.</p>
      </article>
      <article class="card">
        <h2>3. Linux / WSL run</h2>
        <pre><code>./run-helios.sh</code></pre>
        <p>The launcher uses repo-local <code>.tools/dotnet</code> first, then system <code>dotnet</code>.</p>
      </article>
    </section>
  </main>
</body>
</html>
EOF
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

## Web sandbox

Start the local sandbox page:

```bash
./serve-web-sandbox.sh
```

Then open <http://127.0.0.1:8787/web-sandbox/> to download the Windows/Linux ZIP bundles or the Windows EXE directly.

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
printf 'Web sandbox: %s\n' "$OUT_ROOT/web-sandbox/index.html"
printf 'Start sandbox: PORT=8787 %s\n' "$OUT_ROOT/serve-web-sandbox.sh"
