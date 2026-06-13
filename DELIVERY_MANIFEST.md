# HELIOS Delivery Manifest

Automation deliverables are validated by `.github/workflows/github-system.yml` and the scripts under `scripts/github/`.

- Root solution: `HELIOS.Platform.sln`
- Consolidation source map: `MERGE_SOURCE_MANIFEST.yaml`
- Workflow validator: `scripts/github/validate-workflows.py`
- Consolidation dry-run/gate: `scripts/github/prepare-consolidation.py`
- Compatibility wrapper: `scripts/github/prepare_consolidation.py`
- Gitmodules preview artifact: `artifacts/consolidation/gitmodules.preview`
- Azure CLI setup helper: `scripts/setup/setup-azure-cli.sh`
