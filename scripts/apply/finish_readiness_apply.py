#!/usr/bin/env python3
from __future__ import annotations
import argparse,json,os,shlex,subprocess,sys
from datetime import datetime, timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'reports/apply/finish-readiness-apply.json'
MD=ROOT/'reports/apply/finish-readiness-apply.md'
SAFE_STEPS=[
    {'id':'bootstrap-tools','command':'scripts/setup/bootstrap-local-tools.sh','mutates':'local .tools only','apply':True},
    {'id':'secret-preflight','command':'python3 scripts/security/secret_preflight.py','mutates':'reports only','apply':True},
    {'id':'apply-gate-preflight','command':'python3 scripts/security/apply_gate_preflight.py','mutates':'reports only','apply':True},
    {'id':'hermes-fleet-readiness','command':'python3 scripts/agents/hermes_fleet_readiness.py','mutates':'reports only','apply':True},
    {'id':'azure-what-if-plan','command':'python3 scripts/azure/azure_what_if.py','mutates':'reports only; Azure what-if when authenticated','apply':True},
    {'id':'finish-readiness','command':'python3 scripts/integrations/readiness_score.py','mutates':'reports only','apply':True},
    {'id':'dashboard','command':'python3 scripts/dashboard/generate-gui.py','mutates':'status-site dashboard only','apply':True},
    {'id':'build-graph-quick','command':'python3 scripts/build_graph/build_graph.py run --profile quick --changed-only --max-workers 4','mutates':'reports only','apply':True},
]

def tool_path_env():
    tools=ROOT/'.tools'
    paths=[tools/'dotnet',tools/'gh/bin',tools/'azcli-venv/bin']
    env=os.environ.copy()
    env['PATH']=':'.join(str(p) for p in paths)+':'+env.get('PATH','')
    return env

def run_step(step, apply):
    result={'id':step['id'],'command':step['command'],'mutates':step['mutates'],'status':'planned','exitCode':None,'tail':[]}
    if not apply or not step.get('apply'):
        return result
    proc=subprocess.run(step['command'],cwd=ROOT,shell=True,text=True,capture_output=True,timeout=600,env=tool_path_env())
    result.update({'status':'passed' if proc.returncode==0 else 'failed','exitCode':proc.returncode,'tail':(proc.stdout+proc.stderr).splitlines()[-12:]})
    return result

def main():
    parser=argparse.ArgumentParser(description='Plan or apply safe finish-readiness automation steps.')
    parser.add_argument('--apply', action='store_true', help='execute the safe local/report-only steps')
    parser.add_argument('--json', action='store_true')
    args=parser.parse_args()
    results=[]
    for step in SAFE_STEPS:
        results.append(run_step(step,args.apply))
        if results[-1]['status']=='failed':
            break
    payload={'generatedUtc':datetime.now(timezone.utc).isoformat(),'mode':'apply' if args.apply else 'plan','ok':all(r['status'] in {'planned','passed'} for r in results),'steps':results,'manualSteps':['Run `az login` before live Azure what-if/deploy operations.','Set HELIOS_AZURE_RESOURCE_GROUP or pass --resource-group for the target Azure group.','Review reports/apply/finish-readiness-apply.md before any production mutation.']}
    OUT.parent.mkdir(parents=True,exist_ok=True); OUT.write_text(json.dumps(payload,indent=2)+'\n')
    lines=['# Finish Readiness Apply Plan','',f"Generated: `{payload['generatedUtc']}`",'',f"Mode: **{payload['mode']}**",f"Status: {'PASS' if payload['ok'] else 'FAIL'}",'', '| Step | Status | Command | Mutates |','| --- | --- | --- | --- |']
    lines += [f"| `{r['id']}` | {r['status']} | `{r['command']}` | {r['mutates']} |" for r in results]
    lines += ['','## Manual steps','']+[f"- {step}" for step in payload['manualSteps']]
    MD.write_text('\n'.join(lines)+'\n')
    if args.json: print(json.dumps(payload,indent=2))
    else:
        print(f"Finish apply {payload['mode']}: {'PASS' if payload['ok'] else 'FAIL'}")
        print(f"Wrote {OUT.relative_to(ROOT)}")
        print(f"Wrote {MD.relative_to(ROOT)}")
    return 0 if payload['ok'] else 1
if __name__=='__main__': sys.exit(main())
