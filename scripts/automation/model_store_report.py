#!/usr/bin/env python3
from __future__ import annotations
import json, os, datetime as dt
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'reports/model-store'
def main():
 cfg=json.loads((ROOT/'config/helios-model-store.json').read_text(encoding='utf-8'))
 for m in cfg.get('models',[]): m['secretsPresent']={s:bool(os.getenv(s)) for s in m.get('requiredSecrets',[])}
 payload={'generatedUtc':dt.datetime.now(dt.timezone.utc).isoformat(), **cfg}; OUT.mkdir(parents=True,exist_ok=True)
 (OUT/'models.json').write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n'); (OUT/'models.md').write_text('# HELIOS Model Store\n\n'+'\n'.join(f"- {m['modelId']} ({m['provider']}): {m['localCloudMode']}" for m in cfg.get('models',[]))+'\n')
 print(f"Wrote {(OUT/'models.md').relative_to(ROOT)}")
if __name__=='__main__': main()
