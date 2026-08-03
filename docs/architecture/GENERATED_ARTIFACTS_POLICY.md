# Generated Artifacts Policy

HELIOS generates many useful reports: branch intelligence, Codex task packets, inventories, readiness scores, PR bodies, dashboard exports, and wiki exports. These are useful outputs, but they should not all be committed on every run.

## Keep in git

- Source scripts under `scripts/`
- Durable docs under `docs/`
- Config/manifests under `config/`
- Bicep/templates under `infra/`
- Project source under `src/` and `tests/`
- `reports/README.md` as a placeholder and usage note
- `status-site/index.md` as the durable dashboard landing source

## Do not commit by default

- `reports/**` generated JSON/Markdown artifacts
- `reports/codex/tasks/**` generated task packets
- `status-site/index.html`
- `status-site/actions.md`
- `status-site/wiki-export/**`
- `.github/PULL_REQUEST_BODY.md`

## Regenerate

```bash
./helios.sh all
python3 scripts/github/update-pr-from-reports.py --dry-run
```

CI should upload generated reports as artifacts. Local users should use `./helios.sh dashboard` to view the generated GUI.
