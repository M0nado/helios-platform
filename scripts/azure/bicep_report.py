#!/usr/bin/env python3
"""Run or plan Azure Bicep commands and capture structured reports."""
from __future__ import annotations
import argparse, json, os, shutil, subprocess, datetime as dt, re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'reports/azure'; TEMPLATE='infra/azure/main.bicep'
def run(cmd,execute):
 if not execute: return {'command':' '.join(cmd),'dryRun':True,'exitCode':None,'stdout':'','stderr':''}
 p=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True); return {'command':' '.join(cmd),'dryRun':False,'exitCode':p.returncode,'stdout':p.stdout[-12000:],'stderr':p.stderr[-12000:]}
def parse_stream(text):
 lower=text.lower(); return {'resourceGroups':sorted(set(re.findall(r"resourceGroups/([A-Za-z0-9_.-]+)",text))), 'keyVault':re.findall(r'Microsoft.KeyVault/[^\s,\"]+',text), 'storage':re.findall(r'Microsoft.Storage/[^\s,\"]+',text), 'observability':re.findall(r'Microsoft.Insights/[^\s,\"]+',text), 'network':re.findall(r'Microsoft.Network/[^\s,\"]+',text), 'outputs':{}, 'whatIfChanges':[line.strip() for line in text.splitlines() if any(k in line.lower() for k in ('create','modify','delete','ignore'))][:80], 'failedResources':[line.strip() for line in text.splitlines() if 'failed' in line.lower() or 'error' in line.lower()][:80], 'hasErrors':'error' in lower or 'failed' in lower}
def main():
 ap=argparse.ArgumentParser(); ap.add_argument('mode',choices=['build','validate','what-if','deploy','outputs','inventory']); ap.add_argument('--apply',action='store_true'); a=ap.parse_args(); rg=os.getenv('HELIOS_RESOURCE_GROUP','helios-hermes-xcore-rg')
 commands={'build':['az','bicep','build','--file',TEMPLATE],'validate':['az','deployment','group','validate','--resource-group',rg,'--template-file',TEMPLATE],'what-if':['az','deployment','group','what-if','--resource-group',rg,'--template-file',TEMPLATE],'deploy':['az','deployment','group','create','--resource-group',rg,'--template-file',TEMPLATE],'outputs':['az','deployment','group','list','--resource-group',rg,'--query','[0].properties.outputs'],'inventory':['az','resource','list','--resource-group',rg]}
 execute=bool(shutil.which('az')) and (a.mode!='deploy' or a.apply)
 result=run(commands[a.mode],execute); parsed=parse_stream(result.get('stdout','')+'\n'+result.get('stderr',''))
 payload={'schemaVersion':2,'mode':a.mode,'generatedUtc':dt.datetime.now(dt.timezone.utc).isoformat(),'resourceGroup':rg,'template':TEMPLATE,'azAvailable':bool(shutil.which('az')),'applied':bool(a.apply),'result':result,'parsed':parsed,'reviewSafe':a.mode!='deploy' or bool(a.apply)}
 OUT.mkdir(parents=True,exist_ok=True); (OUT/f'{a.mode}.json').write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n')
 alias={'build':'bicep-build','validate':'validate','what-if':'what-if','deploy':'deploy','outputs':'outputs','inventory':'readiness-inventory'}[a.mode]
 (OUT/f'{alias}.json').write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n')
 (OUT/f'{a.mode}.md').write_text(f"# Azure {a.mode}\n\n- Dry run: {result['dryRun']}\n- Exit: {result['exitCode']}\n- Failed resources: {len(parsed['failedResources'])}\n- What-if changes: {len(parsed['whatIfChanges'])}\n")
 print(f"Wrote {(OUT/f'{a.mode}.md').relative_to(ROOT)}")
 return 1 if result['exitCode'] not in (0,None) else 0
if __name__=='__main__': raise SystemExit(main())
