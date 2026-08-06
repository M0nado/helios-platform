#!/usr/bin/env python3
from __future__ import annotations
import json, os, datetime as dt
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'reports/microsoft365'
def main():
 cfg=json.loads((ROOT/'config/helios-copilot-m365.json').read_text(encoding='utf-8'))
 checks=[{'name':cfg['tenant']['tenantIdEnv'],'present':bool(os.getenv(cfg['tenant']['tenantIdEnv'])),'secretPrinted':False},{'name':cfg['tenant']['clientIdEnv'],'present':bool(os.getenv(cfg['tenant']['clientIdEnv'])),'secretPrinted':False}]
 payload={'generatedUtc':dt.datetime.now(dt.timezone.utc).isoformat(),'config':cfg,'checks':checks}
 OUT.mkdir(parents=True,exist_ok=True); (OUT/'copilot-readiness.json').write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n'); (OUT/'copilot-readiness.md').write_text('# Copilot/M365 Readiness\n\n'+'\n'.join(f"- {c['name']}: {'present' if c['present'] else 'missing'}" for c in checks)+'\n')
 print(f"Wrote {(OUT/'copilot-readiness.md').relative_to(ROOT)}")
if __name__=='__main__': main()
