#!/usr/bin/env python3
from __future__ import annotations
import json, os, datetime as dt
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'reports/hermes-xcore'
def main():
 cfg=json.loads((ROOT/'config/helios-hermes-xcore-models.json').read_text(encoding='utf-8'))
 for p in cfg.get('packs',[]): p['endpointPresent']=bool(os.getenv(p.get('endpointEnv','')))
 payload={'generatedUtc':dt.datetime.now(dt.timezone.utc).isoformat(), **cfg}; OUT.mkdir(parents=True,exist_ok=True)
 (OUT/'models.json').write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n'); (OUT/'models.md').write_text('# Hermes/XCore Models\n\n'+'\n'.join(f"- {p['modelId']}: {p['mode']}" for p in cfg.get('packs',[]))+'\n')
 print(f"Wrote {(OUT/'models.md').relative_to(ROOT)}")
if __name__=='__main__': main()
