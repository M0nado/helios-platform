#!/usr/bin/env python3
from __future__ import annotations
import argparse, subprocess, shutil
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'.github/PULL_REQUEST_BODY.md'
def read(path, fallback='Not generated yet.'):
    p=ROOT/path
    return p.read_text() if p.exists() else fallback
body=f"""## HELIOS Control Plane Update

### Command Center

Run locally:

```bash
./helios.sh all
./helios.sh dashboard
```

### Readiness

{read(Path('reports/integrations/readiness-score.md'))}

### Cross-Access Profiles

{read(Path('reports/integrations/cross-access-profiles.md'))}

### Branch Recommendations

{read(Path('reports/branch-intelligence/merge-prune-recommendations.md'))}

### Hybrid Gap Analysis

{read(Path('reports/project-inventory/hybrid-gap-analysis.md'))}

### Safety

- Secret values are never printed.
- GitHub org/enterprise, Azure subscription/tenant, Entra, Purview, and provider apply modes remain disabled unless explicitly implemented later behind `--apply` and reviewed scopes.
"""
OUT.write_text(body)
parser=argparse.ArgumentParser(); parser.add_argument('--apply',action='store_true'); parser.add_argument('--dry-run',action='store_true'); args=parser.parse_args()
print(f'Wrote {OUT.relative_to(ROOT)}')
if args.apply:
    if shutil.which('gh') is None: raise SystemExit('gh not found; run scripts/setup/bootstrap-local-tools.sh and gh auth login first')
    subprocess.run(['gh','pr','edit','--body-file',str(OUT)],cwd=ROOT,check=True)
elif args.dry_run:
    print(body[:2000])
