#!/usr/bin/env python3
from __future__ import annotations
import argparse,json,os,shutil,subprocess,sys
from datetime import datetime, timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
DEFAULT_CFG=ROOT/'config/hermes-fleet.example.json'
OUT=ROOT/'reports/hermes-fleet/latest.json'
MD=ROOT/'reports/hermes-fleet/latest.md'

def run(cmd):
    if shutil.which(cmd[0]) is None: return {'ok':False,'detail':f'{cmd[0]} not found'}
    p=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True,timeout=15)
    return {'ok':p.returncode==0,'detail':(p.stdout or p.stderr).strip().splitlines()[:4]}

def main():
    parser=argparse.ArgumentParser(description='Hermes/Fleet readiness report.')
    parser.add_argument('--config', default=os.environ.get('HELIOS_HERMES_FLEET_CONFIG',''))
    parser.add_argument('--strict', action='store_true')
    parser.add_argument('--json', action='store_true')
    args=parser.parse_args()
    cfg_path=Path(args.config) if args.config else DEFAULT_CFG
    if not cfg_path.is_absolute(): cfg_path=ROOT/cfg_path
    cfg=json.loads(cfg_path.read_text()) if cfg_path.exists() else {}
    providers=cfg.get('providers',{})
    checks={
        'config': {'ok':cfg_path.exists(),'detail':str(cfg_path.relative_to(ROOT) if cfg_path.exists() and ROOT in cfg_path.parents else cfg_path)},
        'dryRunMode': {'ok':cfg.get('mode','dry-run')=='dry-run','detail':cfg.get('mode','missing')},
        'python': run(['python3','--version']),
        'git': run(['git','--version']),
        'codex': run(['codex','--version']) if shutil.which('codex') else {'ok':False,'detail':'codex not found'},
        'openaiEnv': {'ok':bool(os.environ.get(providers.get('openai',{}).get('env','OPENAI_API_KEY'))),'detail':'OPENAI_API_KEY configured' if os.environ.get('OPENAI_API_KEY') else 'OPENAI_API_KEY not set'},
        'azureOpenAIEnv': {'ok':bool(os.environ.get('AZURE_OPENAI_ENDPOINT') and os.environ.get('AZURE_OPENAI_API_KEY')),'detail':'Azure OpenAI env configured' if os.environ.get('AZURE_OPENAI_ENDPOINT') and os.environ.get('AZURE_OPENAI_API_KEY') else 'Azure OpenAI env not set'},
    }
    required=['config','python','git','dryRunMode']
    ok=all(checks[name]['ok'] for name in required)
    OUT.parent.mkdir(parents=True,exist_ok=True)
    payload={'generatedUtc':datetime.now(timezone.utc).isoformat(),'fleetName':cfg.get('fleetName','hermes-fleet-production'),'configPath':str(cfg_path.relative_to(ROOT) if cfg_path.exists() and ROOT in cfg_path.parents else cfg_path),'mode':cfg.get('mode','dry-run'),'ok':ok,'checks':checks,'nextActions':['Set OPENAI_API_KEY or Azure OpenAI env for live AI routing.','Run az login only if Azure-backed fleet automation is enabled.','Keep production mutations behind dry-run/apply gates.']}
    OUT.write_text(json.dumps(payload,indent=2)+'\n')
    lines=['# Hermes/Fleet Readiness','',f"Generated: `{payload['generatedUtc']}`",'',f"Fleet: `{payload['fleetName']}`",f"Mode: `{payload['mode']}`",f"Status: {'PASS' if ok else 'WARN'}`",'', '| Check | Status | Detail |','| --- | --- | --- |']
    lines += [f"| {name} | {'✅' if data['ok'] else '⚠️'} | `{data['detail']}` |" for name,data in checks.items()]
    lines += ['','## Next actions','']+[f"- {action}" for action in payload['nextActions']]
    MD.write_text('\n'.join(lines)+'\n')
    if args.json:
        print(json.dumps(payload,indent=2))
    else:
        print(f"Hermes/Fleet readiness: {'PASS' if ok else 'WARN'}")
    return 0 if ok or not args.strict else 1
if __name__=='__main__': sys.exit(main())
