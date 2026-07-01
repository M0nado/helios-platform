# X-Tier Python Conversion

This folder contains the Python conversion of the pasted Phase 1–3 bootstrap bundle.

## Files

- `xtier_bootstrap.py` — full Python runner for:
  - Phase 1 (DevDrive layout, cache relocation, WSL2 and Docker path policy)
  - Phase 2 (tooling install plan and GPU validation commands)
  - Phase 3 (Hyper-V/Sandbox enablement plan, Qdrant config, Hermes skeleton)

## Safe Usage

Dry-run (default):

```powershell
python scripts\x-tier\xtier_bootstrap.py
```

Execute actions:

```powershell
python scripts\x-tier\xtier_bootstrap.py --execute
```

Custom DevDrive root:

```powershell
python scripts\x-tier\xtier_bootstrap.py --devdrive-root D:\DevDrive
```

