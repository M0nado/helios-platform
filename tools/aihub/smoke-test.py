#!/usr/bin/env python3
from __future__ import annotations
import json,os,sys,datetime as dt
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'reports/aihub'
def main():
 checks=[{'name':'pythonVersion','value':sys.version.split()[0]},{'name':'OPENAI_API_KEY','present':bool(os.getenv('OPENAI_API_KEY')),'secretPrinted':False},{'name':'contracts','present':(ROOT/'src/core/HELIOS.Platform.Contracts').exists()}]
 OUT.mkdir(parents=True,exist_ok=True); payload={'generatedUtc':dt.datetime.now(dt.timezone.utc).isoformat(),'checks':checks}; (OUT/'smoke-test.json').write_text(json.dumps(payload,indent=2)+'\n'); (OUT/'smoke-test.md').write_text('# AIHub Smoke Test\n\n'+'\n'.join(f"- {c['name']}: {c.get('present',c.get('value'))}" for c in checks)+'\n'); print(f"Wrote {(OUT/'smoke-test.md').relative_to(ROOT)}")
if __name__=='__main__': main()
