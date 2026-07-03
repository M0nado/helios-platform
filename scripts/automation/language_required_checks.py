#!/usr/bin/env python3
"""Fail when language-owned required merge checks are absent from final gate."""
from __future__ import annotations
import json, datetime as dt, re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; OWN=ROOT/'config/helios-language-ownership.json'; FINAL=ROOT/'reports/final-gate/final-gate.json'; OUT=ROOT/'reports/language-ownership'
def main():
 ownership=json.loads(OWN.read_text(encoding='utf-8'))['languages']
 final=json.loads(FINAL.read_text(encoding='utf-8')) if FINAL.exists() else {'results':[]}
 present={r.get('id') for r in final.get('results',[])}
 # During final_gate execution the current report may still be from the previous run,
 # so also inspect the final_gate command registry itself.
 fg=ROOT/'scripts/automation/final_gate.py'
 if fg.exists():
  present.update(re.findall(r'\{"id": "([^"]+)"', fg.read_text(encoding='utf-8')))
 rows=[]
 for lang,info in ownership.items():
  required=set(info.get('requiredChecks',[])); missing=sorted(required-present)
  rows.append({'language':lang,'ownerAgent':info.get('ownerAgent'),'requiredChecks':sorted(required),'missingFromFinalGate':missing,'passed':not missing})
 payload={'generatedUtc':dt.datetime.now(dt.timezone.utc).isoformat(),'results':rows,'passed':all(r['passed'] for r in rows)}
 OUT.mkdir(parents=True,exist_ok=True); (OUT/'required-checks.json').write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n')
 (OUT/'required-checks.md').write_text('# Language Required Checks\n\n'+'\n'.join(f"- {r['language']}: missing {r['missingFromFinalGate']}" for r in rows)+'\n')
 print(f"Wrote {(OUT/'required-checks.md').relative_to(ROOT)}")
 return 0 if payload['passed'] else 1
if __name__=='__main__': raise SystemExit(main())
