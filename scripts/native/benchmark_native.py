#!/usr/bin/env python3
from __future__ import annotations
import json,shutil,subprocess,datetime as dt
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'reports/native'
def run(c):
 if not shutil.which(c[0]): return {'command':' '.join(c),'exitCode':None,'warning':f'{c[0]} unavailable'}
 p=subprocess.run(c,cwd=ROOT,text=True,capture_output=True); return {'command':' '.join(c),'exitCode':p.returncode,'stdout':p.stdout[-2000:],'stderr':p.stderr[-2000:]}
def main():
 res=[run(['cmake','-S','src/native/HELIOS.Native.Performance','-B','build/native-performance']),run(['cmake','--build','build/native-performance','--config','Release'])]; OUT.mkdir(parents=True,exist_ok=True); payload={'generatedUtc':dt.datetime.now(dt.timezone.utc).isoformat(),'results':res}; (OUT/'benchmark-results.json').write_text(json.dumps(payload,indent=2)+'\n'); (OUT/'benchmark-results.md').write_text('# Native Benchmark Baseline\n\n'+'\n'.join(f"- {r['command']}: {r.get('exitCode')}" for r in res)+'\n'); print(f"Wrote {(OUT/'benchmark-results.md').relative_to(ROOT)}"); return 1 if any(r.get('exitCode') not in (0,None) for r in res) else 0
if __name__=='__main__': raise SystemExit(main())
