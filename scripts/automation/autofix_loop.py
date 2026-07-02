#!/usr/bin/env python3
"""Plan/apply HELIOS autofix tasks from final-gate failures."""
from __future__ import annotations
import argparse,json,re,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'reports/autofix'; FINAL=ROOT/'reports/final-gate/final-gate.json'
def classify(result):
    text=(result.get('stderr','')+'\n'+result.get('stdout','')).lower(); cmd=result.get('command','')
    if 'dotnet build' in cmd and ('cs' in text or 'error' in text): return 'csharp-compile'
    if 'dotnet test' in cmd and 'fsharp' in cmd.lower(): return 'fsharp-test'
    if 'az bicep' in cmd or 'deployment group' in cmd: return 'azure-bicep'
    if 'cmake' in cmd: return 'cpp-native'
    if 'json.tool' in cmd: return 'json-config'
    if 'py_compile' in cmd: return 'python-syntax'
    return 'general'
def main():
    ap=argparse.ArgumentParser(); ap.add_argument('mode',choices=['plan','apply'],nargs='?',default='plan'); args=ap.parse_args(); OUT.mkdir(parents=True,exist_ok=True)
    data=json.loads(FINAL.read_text()) if FINAL.exists() else {'failed':[]}
    tasks=[]
    for r in data.get('failed',[]):
        kind=classify(r); tasks.append({'kind':kind,'sourceStep':r.get('id'),'command':r.get('command'),'suggestedScript':'scripts/automation/fix_csharp_compile.py' if kind=='csharp-compile' else None})
    payload={'mode':args.mode,'tasks':tasks,'applied':[]}
    if args.mode=='apply':
        for t in tasks:
            if t.get('suggestedScript'):
                p=subprocess.run(['python3',t['suggestedScript']],cwd=ROOT,text=True,capture_output=True)
                payload['applied'].append({'task':t,'exitCode':p.returncode,'stdout':p.stdout[-2000:],'stderr':p.stderr[-2000:]})
    (OUT/'autofix-plan.json').write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n')
    (OUT/'autofix-plan.md').write_text('# Autofix Plan\n\n'+'\n'.join(f"- {t['kind']} from `{t['sourceStep']}`" for t in tasks)+'\n')
    print(f"Wrote {(OUT/'autofix-plan.md').relative_to(ROOT)}")
    return 0
if __name__=='__main__': raise SystemExit(main())
