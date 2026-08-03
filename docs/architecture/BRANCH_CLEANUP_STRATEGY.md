# Branch Cleanup Strategy

Use this strategy when creating a clean branch that keeps planned source/config/docs and removes duplicate generated output.

## Create a clean branch

```bash
git checkout -b cleanup/prune-generated-extras
python3 scripts/analysis/prune_generated_artifacts.py
git status --short
git add -A
git commit -m "Prune generated control-plane artifacts"
```

## What gets removed

- Generated reports under `reports/branch-intelligence/`, `reports/build-graph/`, `reports/codex/`, `reports/control-plane/`, `reports/integrations/`, and `reports/project-inventory/`
- Generated dashboard exports under `status-site/actions.md`, `status-site/index.html`, and `status-site/wiki-export/`
- Generated PR body `.github/PULL_REQUEST_BODY.md`

## What stays

- Source code and scripts
- Config/manifests
- Durable docs
- Azure/Bicep templates
- F# analytics library and tests
- `reports/README.md`
- `status-site/index.md`

## Regenerate when needed

```bash
./helios.sh all
./helios.sh dashboard
./helios.sh pr-update --dry-run
```

CI should publish full generated reports as artifacts instead of requiring every run to update committed JSON/Markdown output.
