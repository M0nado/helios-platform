#!/usr/bin/env python3
from __future__ import annotations
import argparse,json,os,subprocess,datetime as dt
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'reports/security'
def tracked_env():
 p=subprocess.run(['git','ls-files'],cwd=ROOT,text=True,capture_output=True); return [x for x in p.stdout.splitlines() if (x.endswith('.env') or '/.env' in x) and not any(marker in x for marker in ('template.env', 'example.env', 'sample.env', '.env.template'))]
def main():
 ap=argparse.ArgumentParser(); ap.add_argument('mode',choices=['plan','verify'],nargs='?',default='verify'); a=ap.parse_args(); cfg=json.loads((ROOT/'config/helios-vault.json').read_text())
 checks=[{'name':s,'present':bool(os.getenv(s)),'secretPrinted':False} for s in cfg['requiredSecrets']]; env_files=tracked_env(); payload={'mode':a.mode,'generatedUtc':dt.datetime.now(dt.timezone.utc).isoformat(),'secrets':checks,'trackedEnvFiles':env_files,'passed':not env_files}
 OUT.mkdir(parents=True,exist_ok=True); (OUT/'vault-readiness.json').write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n'); (OUT/'vault-readiness.md').write_text('# Vault Readiness\n\n'+'\n'.join(f"- {c['name']}: {'present' if c['present'] else 'missing'}" for c in checks)+'\n')
 print(f"Wrote {(OUT/'vault-readiness.md').relative_to(ROOT)}"); return 0 if payload['passed'] else 1
if __name__=='__main__': raise SystemExit(main())
