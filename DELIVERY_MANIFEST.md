# HELIOS Delivery Manifest

Automation deliverables are validated by `.github/workflows/github-system.yml` and the scripts under `scripts/github/`.

- Root solution: `HELIOS.Platform.sln`
- Consolidation source map: `MERGE_SOURCE_MANIFEST.yaml`
- Workflow validator: `scripts/github/validate-workflows.py`
- Consolidation dry-run: `scripts/github/prepare-consolidation.py`
- Azure CLI setup helper: `scripts/setup/setup-azure-cli.sh`
