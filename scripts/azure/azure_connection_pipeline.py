#!/usr/bin/env python3
from __future__ import annotations
import argparse,json,os,shutil,subprocess,sys
from datetime import datetime, timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'reports/azure/connection-pipeline.json'
MD=ROOT/'reports/azure/connection-pipeline.md'

def run(cmd, timeout=60):
    p=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True,timeout=timeout)
    return {'command':' '.join(cmd),'ok':p.returncode==0,'exitCode':p.returncode,'tail':(p.stdout+p.stderr).splitlines()[-12:]}

def ensure_login():
    if shutil.which('az') is None:
        return {'status':'planned','detail':'az not found; run scripts/setup/bootstrap-local-tools.sh'}
    account=run(['az','account','show'],timeout=30)
    if account['ok']:
        return {'status':'passed','detail':'Azure CLI already authenticated','check':account}
    cid=os.environ.get('AZURE_CLIENT_ID'); tenant=os.environ.get('AZURE_TENANT_ID'); secret=os.environ.get('AZURE_CLIENT_SECRET')
    if cid and tenant and secret:
        login=run(['az','login','--service-principal','--username',cid,'--password',secret,'--tenant',tenant],timeout=120)
        return {'status':'passed' if login['ok'] else 'failed','detail':'service principal login attempted','check':login}
    return {'status':'planned','detail':'Azure CLI installed but not authenticated; run az login or set AZURE_CLIENT_ID/AZURE_TENANT_ID/AZURE_CLIENT_SECRET'}

def bicep_check():
    if shutil.which('az') is None:
        return {'status':'planned','detail':'az not found; run scripts/setup/bootstrap-local-tools.sh'}
    version=run(['az','bicep','version'],timeout=60)
    if version['ok']:
        build=run(['az','bicep','build','--file','infra/azure/main.bicep'],timeout=120)
        return {'status':'passed' if build['ok'] else 'planned','detail':'Bicep build completed' if build['ok'] else 'Bicep build could not complete in this environment; see tail','check':build}
    return {'status':'planned','detail':'Bicep CLI unavailable or network/proxy blocked version lookup; see tail','check':version}

def main():
    parser=argparse.ArgumentParser(description='Safe Azure connection pipeline for local/CI readiness.')
    parser.add_argument('--stage', choices=['all','auth','bicep'], default='all')
    parser.add_argument('--json', action='store_true')
    args=parser.parse_args()
    steps=[]
    if args.stage in {'all','auth'}: steps.append({'id':'auth', **ensure_login()})
    if args.stage in {'all','bicep'}: steps.append({'id':'bicep', **bicep_check()})
    ok=all(step['status'] in {'passed','planned'} for step in steps)
    payload={'generatedUtc':datetime.now(timezone.utc).isoformat(),'stage':args.stage,'ok':ok,'steps':steps,'manualSteps':['az login','export HELIOS_AZURE_RESOURCE_GROUP=helios-dev-rg','python3 scripts/azure/azure_what_if.py']}
    OUT.parent.mkdir(parents=True,exist_ok=True); OUT.write_text(json.dumps(payload,indent=2)+'\n')
    lines=['# Azure Connection Pipeline','',f"Generated: `{payload['generatedUtc']}`",'',f"Stage: **{args.stage}**",f"Status: {'PASS' if ok else 'FAIL'}",'', '| Step | Status | Detail |','| --- | --- | --- |']
    lines += [f"| {s['id']} | {s['status']} | {s['detail']} |" for s in steps]
    lines += ['','## Manual/live cloud steps','']+[f"- `{step}`" for step in payload['manualSteps']]
    MD.write_text('\n'.join(lines)+'\n')
    if args.json: print(json.dumps(payload,indent=2))
    else:
        print(f"Azure connection pipeline {args.stage}: {'PASS' if ok else 'FAIL'}")
        for step in steps: print(f"- {step['id']}: {step['status']} ({step['detail']})")
    return 0 if ok else 1
if __name__=='__main__': sys.exit(main())
