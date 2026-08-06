#!/usr/bin/env python3
from __future__ import annotations
import json, os, datetime as dt
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'reports/ai-providers'; CONFIG=ROOT/'config/helios-model-store.json'
def main():
 data=json.loads(CONFIG.read_text(encoding='utf-8')) if CONFIG.exists() else {'models':[]}
 models=data.get('models', data if isinstance(data,list) else [])
 checks=[]
 for m in models:
  secrets=m.get('requiredSecrets',[]) if isinstance(m,dict) else []
  checks.append({**m,'ready':all(os.getenv(s) for s in secrets),'missingSecrets':[s for s in secrets if not os.getenv(s)]})
 payload={'generatedUtc':dt.datetime.now(dt.timezone.utc).isoformat(),'providers':checks,'readyCount':sum(1 for c in checks if c.get('ready'))}
 OUT.mkdir(parents=True,exist_ok=True); (OUT/'provider-health.json').write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n')
 (OUT/'provider-health.md').write_text('# Provider Health\n\n'+'\n'.join(f"- {c.get('modelId',c.get('id','model'))}: {'ready' if c.get('ready') else 'missing secrets'}" for c in checks)+'\n')
 print(f"Wrote {(OUT/'provider-health.md').relative_to(ROOT)}")
if __name__=='__main__': main()
