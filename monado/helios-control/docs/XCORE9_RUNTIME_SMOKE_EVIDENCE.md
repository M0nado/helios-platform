# XCore9 runtime smoke evidence index

This index defines the runtime smoke evidence artifact paths for all required matrix modes.
The evidence files are generated per run and uploaded by CI; they are not committed to Git.

## Evidence artifacts

| Mode | Summary | Data |
| --- | --- | --- |
| Local Windows | `docs/evidence/xcore9-runtime-matrix/local-windows-smoke.md` | `docs/evidence/xcore9-runtime-matrix/local-windows-smoke.json` |
| Local Docker | `docs/evidence/xcore9-runtime-matrix/local-docker-smoke.md` | `docs/evidence/xcore9-runtime-matrix/local-docker-smoke.json` |
| Hybrid Windows + Docker fleet | `docs/evidence/xcore9-runtime-matrix/hybrid-windows-docker-fleet-smoke.md` | `docs/evidence/xcore9-runtime-matrix/hybrid-windows-docker-fleet-smoke.json` |

## How evidence is produced

- Local generation command:
  - `pwsh ./monado/helios-control/scripts/Invoke-XCore9RuntimeMatrixSmoke.ps1 -Mode local-windows`
  - `pwsh ./monado/helios-control/scripts/Invoke-XCore9RuntimeMatrixSmoke.ps1 -Mode local-docker`
  - `pwsh ./monado/helios-control/scripts/Invoke-XCore9RuntimeMatrixSmoke.ps1 -Mode hybrid-windows-docker-fleet`
- CI workflow: `.github/workflows/xcore9-runtime-matrix-validate.yml`

All smoke outputs are non-destructive and validation-first.
