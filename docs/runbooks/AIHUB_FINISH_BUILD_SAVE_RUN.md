# Finish, build, save, and run AIHub locally

Use this when the repo has the AIHub reports, fleet planning, learning store, and dashboard changes and you want to finish the generated artifacts, validate them, save them, and open the GUI.

## 1. Put local tools on PATH

```bash
export PATH="$PWD/.tools/dotnet:$PWD/.tools/gh/bin:$PWD/.tools/azcli-venv/bin:$PATH"
```

## 2. Finish the report set

```bash
scripts/setup/simple-build.sh finish
```

This refreshes branch absorption, branch autofix packets, merge/prune recommendations, complex grading, fleet packets, finish readiness, module organization, learning feedback, self-learning notes, the SQL/vector knowledge-store plan, and the static dashboard.

## 3. Run a build smoke check

```bash
scripts/setup/simple-build.sh quick
```

For the bigger report/build graph, use:

```bash
scripts/setup/simple-build.sh full
```

## 4. Save a runnable bundle

```bash
scripts/setup/simple-build.sh save-run
```

The bundle is written under `.run/aihub-control-plane-<UTC timestamp>/`, and the latest path is recorded at `.run/latest-aihub-bundle.txt`.

## 5. Open the saved dashboard

```bash
cd "$(cat .run/latest-aihub-bundle.txt)"
./run-local.sh
```

Then open <http://127.0.0.1:8080/>.

## 6. Keep it safe

- Do not paste secrets into the static dashboard.
- Use `az login`, `gh auth login`, Key Vault, or environment variables only when you intentionally enable live cloud/repo lanes.
- `save-run` archives generated reports and the static dashboard; it does not create databases or mutate GitHub/Azure.

## 7. Build a runnable EXE bundle

```bash
scripts/setup/simple-build.sh exe
```

That command runs the finish/save flow, publishes framework-dependent runnable builds to `.run/helios-exe-<UTC timestamp>/`, and records the latest path in `.run/latest-helios-exe.txt`.

To build only the EXE bundle without rerunning the finish reports:

```bash
SKIP_FINISH=1 scripts/setup/build-run-exe.sh
```

Run the Windows executable from the generated bundle:

```cmd
run-helios.cmd
```

Or run it directly:

```cmd
win-x64\HELIOS.Platform.exe
```

On Linux/WSL, run:

```bash
./run-helios.sh
```

## 8. Open the EXE in a local web sandbox

```bash
scripts/setup/simple-build.sh exe-web
```

That creates the same EXE bundle plus a local `web-sandbox/index.html` page with download buttons for the Windows bundle, Linux bundle, and Windows EXE. To serve it locally:

```bash
"$(cat .run/latest-helios-exe.txt)/serve-web-sandbox.sh"
```

Then open <http://127.0.0.1:8787/web-sandbox/>.

For a faster sandbox refresh that skips the finish/save report pass:

```bash
SKIP_FINISH=1 scripts/setup/build-run-exe.sh
"$(cat .run/latest-helios-exe.txt)/serve-web-sandbox.sh"
```
