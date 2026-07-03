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
