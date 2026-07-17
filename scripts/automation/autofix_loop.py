#!/usr/bin/env python3
"""Plan and run a gated HELIOS autofix branch/PR lifecycle."""
from __future__ import annotations
import argparse, json, subprocess, datetime as dt, re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'reports/autofix'; FINAL=ROOT/'reports/final-gate/final-gate.json'
FIXERS={'csharp-compile':'python3 scripts/automation/fix_csharp_compile.py','fsharp-test':'python3 scripts/analytics/fsharp_test_report.py','azure-bicep':'python3 scripts/azure/bicep_report.py validate','cpp-native':'python3 scripts/native/benchmark_native.py','python-syntax':'python3 scripts/automation/code_fix_center.py','json-config':'python3 scripts/automation/code_fix_center.py','general':'python3 scripts/automation/code_fix_center.py'}
def sh(cmd):
 p=subprocess.run(cmd,cwd=ROOT,shell=True,text=True,capture_output=True); return {'command':cmd,'exitCode':p.returncode,'stdout':p.stdout[-3000:],'stderr':p.stderr[-3000:]}
def load_final():
 return json.loads(FINAL.read_text(encoding='utf-8')) if FINAL.exists() else {'failed':[],'firstBlocker':None}
def classify(result):
 text=(result.get('stderr','')+'\n'+result.get('stdout','')).lower(); cmd=result.get('command','').lower(); step=result.get('id','')
 if 'csharp' in step or ('dotnet build' in cmd and re.search(r'\bcs\d{4}\b', text)): return 'csharp-compile'
 if 'fsharp' in step or '.fsproj' in cmd: return 'fsharp-test'
 if 'az bicep' in cmd or 'deployment group' in cmd or 'bicep' in step: return 'azure-bicep'
 if 'cmake' in cmd or 'cpp' in step or 'native' in step: return 'cpp-native'
 if 'json' in step: return 'json-config'
 if 'py_compile' in cmd or 'python' in step: return 'python-syntax'
 return 'general'
def plan(mode):
 data=load_final(); failed=data.get('failed',[]); first=data.get('firstBlocker') or (failed[0] if failed else None); tasks=[]
 for i,r in enumerate(failed or ([first] if first else []),1):
  kind=classify(r); tasks.append({'id':f'autofix-{i}-{kind}','kind':kind,'sourceStep':r.get('id'),'command':r.get('command'),'fixer':FIXERS[kind],'priority':i,'ownerAgent':r.get('ownerAgent','unknown'),'rerun':r.get('command')})
 return {'schemaVersion':2,'mode':mode,'generatedUtc':dt.datetime.now(dt.timezone.utc).isoformat(),'firstTask':tasks[0] if tasks else None,'tasks':tasks,'finalGateStatus':data.get('status','unknown')}
def main():
 ap=argparse.ArgumentParser(); ap.add_argument('mode',choices=['plan','branch','apply','verify','pr','automerge'],nargs='?',default='plan'); ap.add_argument('--apply',action='store_true'); args=ap.parse_args(); OUT.mkdir(parents=True,exist_ok=True)
 payload=plan(args.mode); payload['actions']=[]; task=payload.get('firstTask')
 branch=f"autofix/{task['sourceStep'] if task else 'no-blocker'}".replace(' ','-')
 if args.mode=='branch' and task:
  payload['actions'].append(sh(f'git checkout -B {branch}'))
 if args.mode=='apply' and task:
  payload['actions'].append(sh(task['fixer']))
  payload['actions'].append(sh(task['rerun']))
 if args.mode=='verify':
  payload['actions'].append(sh('python3 scripts/automation/final_gate.py'))
 if args.mode=='pr' and args.apply:
  payload['actions'].append(sh(f'git push --set-upstream origin {branch} --force-with-lease'))
  payload['actions'].append(sh(f'gh pr create --fill --head {branch}'))
 elif args.mode=='pr':
  payload['actions'].append({'command':f'gh pr create --fill --head {branch}','dryRun':True})
 if args.mode=='automerge' and args.apply:
  payload['actions'].append(sh(f'gh pr merge {branch} --auto --squash'))
 elif args.mode=='automerge':
  payload['actions'].append({'command':f'gh pr merge {branch} --auto --squash','dryRun':True})
 (OUT/'autofix-plan.json').write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n')
 (OUT/'lifecycle.json').write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n')
 lines=['# Autofix Lifecycle','',f"Mode: `{args.mode}`",'']+[f"- {t['id']} from `{t['sourceStep']}` via `{t['fixer']}`" for t in payload['tasks']]
 if payload['actions']:
  lines += ['','## Actions']+[f"- `{a.get('command')}` → {a.get('exitCode', 'dry-run')}" for a in payload['actions']]
 (OUT/'lifecycle.md').write_text('\n'.join(lines)+'\n')
 (OUT/'autofix-plan.md').write_text('\n'.join(lines)+'\n')
 print(f"Wrote {(OUT/'lifecycle.md').relative_to(ROOT)}")
 return 1 if any(a.get('exitCode',0) not in (0,None) for a in payload['actions']) else 0
if __name__=='__main__': raise SystemExit(main())
