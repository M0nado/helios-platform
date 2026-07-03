#!/usr/bin/env python3
from __future__ import annotations
import json, shutil
from datetime import datetime, timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'reports/integrations/readiness-score.json'; MD=ROOT/'reports/integrations/readiness-score.md'
checks=[
 ('command-center','helios.sh',Path('helios.sh').exists()),
 ('execution-order','config/execution-order.json',Path('config/execution-order.json').exists()),
 ('secrets-map','config/secrets-map.example.json',Path('config/secrets-map.example.json').exists()),
 ('cross-access-profiles','config/cross-access-profiles.example.json',Path('config/cross-access-profiles.example.json').exists()),
 ('github-inventory','scripts/github/github-inventory.py',Path('scripts/github/github-inventory.py').exists()),
 ('azure-inventory','scripts/azure/azure-inventory.py',Path('scripts/azure/azure-inventory.py').exists()),
 ('repo-inventory','scripts/analysis/repo_inventory.py',Path('scripts/analysis/repo_inventory.py').exists()),
 ('hybrid-gap-analysis','scripts/analysis/hybrid_gap_analysis.py',Path('scripts/analysis/hybrid_gap_analysis.py').exists()),
 ('gui-dashboard','scripts/dashboard/generate-gui.py',Path('scripts/dashboard/generate-gui.py').exists()),
 ('web-server','scripts/web/helios-web.py',Path('scripts/web/helios-web.py').exists()),
 ('pr-update-helper','scripts/github/update-pr-from-reports.py',Path('scripts/github/update-pr-from-reports.py').exists()),
 ('python3-cli','python3',shutil.which('python3') is not None),
 ('git-cli','git',shutil.which('git') is not None),
 ('github-cli','gh',shutil.which('gh') is not None),
 ('azure-cli','az',shutil.which('az') is not None),
 ('dotnet-cli','dotnet',shutil.which('dotnet') is not None),
 ('cmake-cli','cmake',shutil.which('cmake') is not None),
]
score=round(100*sum(1 for _,_,ok in checks if ok)/len(checks))
payload={'generatedUtc':datetime.now(timezone.utc).isoformat(),'score':score,'checks':[{'id':i,'target':t,'ok':ok} for i,t,ok in checks], 'notes':'Score reflects local readiness and repo wiring. Provider auth readiness is reported separately in cross-access profiles.'}
OUT.parent.mkdir(parents=True,exist_ok=True); OUT.write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n')
lines=['# HELIOS Readiness Score','',f"Generated: `{payload['generatedUtc']}`",'',f"Score: **{score}%**",'','| Check | Target | Status |','| --- | --- | --- |']
for i,t,ok in checks: lines.append(f"| {i} | `{t}` | {'✅' if ok else '⚠️'} |")
MD.write_text('\n'.join(lines)+'\n'); print(f'Wrote {OUT.relative_to(ROOT)}'); print(f'Wrote {MD.relative_to(ROOT)}')
