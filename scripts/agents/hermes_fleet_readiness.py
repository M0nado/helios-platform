#!/usr/bin/env python3
from __future__ import annotations
import json,os,shutil,subprocess,sys
from datetime import datetime, timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
CFG=ROOT/'config/hermes-fleet.example.json'
OUT=ROOT/'reports/hermes-fleet/latest.json'
MD=ROOT/'reports/hermes-fleet/latest.md'

def run(cmd):
    if shutil.which(cmd[0]) is None: return {'ok':False,'detail':f'{cmd[0]} not found'}
    p=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True,timeout=15)
    return {'ok':p.returncode==0,'detail':(p.stdout or p.stderr).strip().splitlines()[:4]}

def main():
    cfg=json.loads(CFG.read_text()) if CFG.exists() else {}
    providers=cfg.get('providers',{})
    checks={
        'config': {'ok':CFG.exists(),'detail':str(CFG.relative_to(ROOT))},
        'python': run(['python3','--version']),
        'git': run(['git','--version']),
        'codex': run(['codex','--version']) if shutil.which('codex') else {'ok':False,'detail':'codex not found'},
        'openaiEnv': {'ok':bool(os.environ.get(providers.get('openai',{}).get('env','OPENAI_API_KEY'))),'detail':'OPENAI_API_KEY configured' if os.environ.get('OPENAI_API_KEY') else 'OPENAI_API_KEY not set'},
        'azureOpenAIEnv': {'ok':bool(os.environ.get('AZURE_OPENAI_ENDPOINT') and os.environ.get('AZURE_OPENAI_API_KEY')),'detail':'Azure OpenAI env configured' if os.environ.get('AZURE_OPENAI_ENDPOINT') and os.environ.get('AZURE_OPENAI_API_KEY') else 'Azure OpenAI env not set'},
    }
    required=['config','python','git']
    ok=all(checks[name]['ok'] for name in required)
    OUT.parent.mkdir(parents=True,exist_ok=True)
    payload={'generatedUtc':datetime.now(timezone.utc).isoformat(),'fleetName':cfg.get('fleetName','hermes-fleet-production'),'mode':cfg.get('mode','dry-run'),'ok':ok,'checks':checks,'nextActions':['Set OPENAI_API_KEY or Azure OpenAI env for live AI routing.','Run az login only if Azure-backed fleet automation is enabled.','Keep production mutations behind dry-run/apply gates.']}
    OUT.write_text(json.dumps(payload,indent=2)+'\n')
    lines=['# Hermes/Fleet Readiness','',f"Generated: `{payload['generatedUtc']}`",'',f"Fleet: `{payload['fleetName']}`",f"Mode: `{payload['mode']}`",f"Status: {'PASS' if ok else 'WARN'}`",'', '| Check | Status | Detail |','| --- | --- | --- |']
    lines += [f"| {name} | {'✅' if data['ok'] else '⚠️'} | `{data['detail']}` |" for name,data in checks.items()]
    lines += ['','## Next actions','']+[f"- {action}" for action in payload['nextActions']]
    MD.write_text('\n'.join(lines)+'\n')
    print(f"Hermes/Fleet readiness: {'PASS' if ok else 'WARN'}")
    return 0 if ok else 1
if __name__=='__main__': sys.exit(main())
