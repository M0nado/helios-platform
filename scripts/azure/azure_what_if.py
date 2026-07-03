#!/usr/bin/env python3
from __future__ import annotations
import argparse,json,os,shutil,subprocess,sys
from datetime import datetime, timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'reports/azure/what-if.json'
MD=ROOT/'reports/azure/what-if.md'

def main():
    parser=argparse.ArgumentParser(description='Safe Azure Bicep what-if wrapper.')
    parser.add_argument('--resource-group', default=os.environ.get('HELIOS_AZURE_RESOURCE_GROUP',''))
    parser.add_argument('--parameters', default='infra/azure/parameters/dev.json')
    parser.add_argument('--template-file', default='infra/azure/main.bicep')
    args=parser.parse_args()
    cmd=['az','deployment','group','what-if','--resource-group',args.resource_group,'--template-file',args.template_file,'--parameters',f'@{args.parameters}']
    if shutil.which('az') is None:
        status='skipped'; detail='az not found; run scripts/setup/bootstrap-local-tools.sh'
    elif not args.resource_group:
        status='skipped'; detail='resource group missing; set HELIOS_AZURE_RESOURCE_GROUP or pass --resource-group'
    else:
        p=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True,timeout=120)
        status='passed' if p.returncode==0 else 'failed'; detail='\n'.join((p.stdout+p.stderr).splitlines()[-20:])
    OUT.parent.mkdir(parents=True,exist_ok=True)
    payload={'generatedUtc':datetime.now(timezone.utc).isoformat(),'status':status,'command':' '.join(cmd),'detail':detail}
    OUT.write_text(json.dumps(payload,indent=2)+'\n')
    MD.write_text('\n'.join(['# Azure What-If','',f"Generated: `{payload['generatedUtc']}`",'',f"Status: **{status}**",'', '```bash', payload['command'], '```','', '```', detail, '```'])+'\n')
    print(f'Azure what-if: {status} ({detail.splitlines()[0] if detail else ""})')
    return 0 if status in {'passed','skipped'} else 1
if __name__=='__main__': sys.exit(main())
