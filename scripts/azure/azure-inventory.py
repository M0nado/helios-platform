#!/usr/bin/env python3
from __future__ import annotations
import json, shutil, subprocess
from datetime import datetime, timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'reports/control-plane/azure-inventory.json'; MD=ROOT/'reports/control-plane/azure-inventory.md'
def run(cmd):
    if shutil.which(cmd[0]) is None: return {'ok':False,'available':False,'command':cmd,'detail':f'{cmd[0]} not found'}
    p=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True,timeout=30)
    return {'ok':p.returncode==0,'available':True,'command':cmd,'exitCode':p.returncode,'detail':(p.stdout or p.stderr).strip().splitlines()[:20]}
report={'generatedUtc':datetime.now(timezone.utc).isoformat(),'safeByDefault':True,'account':run(['az','account','show']),'bicep':run(['az','bicep','version']),'resourceGroups':run(['az','group','list','--query','[].{name:name,location:location}','-o','table']),'keyVaults':run(['az','keyvault','list','--query','[].{name:name,resourceGroup:resourceGroup,location:location}','-o','table']),'storageAccounts':run(['az','storage','account','list','--query','[].{name:name,resourceGroup:resourceGroup,location:primaryLocation}','-o','table']),'whatIfCommand':'az deployment group what-if --resource-group <resource-group> --template-file infra/azure/main.bicep --parameters @infra/azure/parameters/dev.json','desiredState':'config/azure-control.example.json'}
OUT.parent.mkdir(parents=True,exist_ok=True); OUT.write_text(json.dumps(report,indent=2,sort_keys=True)+'\n')
lines=['# Azure Inventory','',f"Generated: `{report['generatedUtc']}`",'', '| Area | Status | Detail |','| --- | --- | --- |']
for k,v in report.items():
    if isinstance(v,dict) and 'ok' in v: lines.append(f"| {k} | {'✅' if v.get('ok') else '⚠️'} | `{str(v.get('detail'))[:160]}` |")
MD.write_text('\n'.join(lines)+'\n'); print(f'Wrote {OUT.relative_to(ROOT)}'); print(f'Wrote {MD.relative_to(ROOT)}')
