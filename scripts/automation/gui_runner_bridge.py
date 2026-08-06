#!/usr/bin/env python3
"""Safe local/cloud command bridge for the HELIOS control center."""
from __future__ import annotations
import argparse, json, subprocess, datetime as dt, re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; CONFIG=ROOT/'config/helios-gui-commands.json'; OUT=ROOT/'reports/operator-dashboard/runs'
def load_commands():
 data=json.loads(CONFIG.read_text(encoding='utf-8')) if CONFIG.exists() else {'commands':[]}
 raw=data.get('commands',data if isinstance(data,list) else [])
 if not raw and isinstance(data,dict):
  raw=[]
  for group in data.get('groups',[]):
   for idx,button in enumerate(group.get('buttons',[])):
    item=dict(button); item.setdefault('id', (group.get('name','group')+'-'+item.get('label',str(idx))).lower().replace(' ','-')); item.setdefault('group', group.get('name'))
    raw.append(item)
 return {str(c.get('id',c.get('label',''))):c for c in raw if isinstance(c,dict)}
def redact(text):
 return re.sub(r'(?i)(token|key|secret|password)=\S+', r'\1=***', text[-6000:])
def run(cmd,execute):
 if not execute: return {'command':cmd,'dryRun':True,'exitCode':None,'stdout':'','stderr':''}
 p=subprocess.run(cmd,cwd=ROOT,shell=True,text=True,capture_output=True)
 return {'command':cmd,'dryRun':False,'exitCode':p.returncode,'stdout':redact(p.stdout),'stderr':redact(p.stderr)}
def main():
 ap=argparse.ArgumentParser(); ap.add_argument('command_id',nargs='?',default='list'); ap.add_argument('--local-run',action='store_true'); ap.add_argument('--cloud-dispatch',action='store_true'); args=ap.parse_args(); cmds=load_commands(); OUT.mkdir(parents=True,exist_ok=True)
 if args.command_id=='list':
  payload={'generatedUtc':dt.datetime.now(dt.timezone.utc).isoformat(),'commands':list(cmds.values())}
 else:
  item=cmds.get(args.command_id)
  if not item: raise SystemExit(f'Unknown GUI command id: {args.command_id}')
  mode=item.get('mode','copy'); execute=args.local_run and mode in {'local-run','copy'} and item.get('risk','low')!='danger'
  payload={'generatedUtc':dt.datetime.now(dt.timezone.utc).isoformat(),'commandId':args.command_id,'mode':'cloud-dispatch' if args.cloud_dispatch else ('local-run' if args.local_run else 'copy'),'risk':item.get('risk','low'),'result':run(item['command'],execute)}
 path=OUT/'latest.json'; path.write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n')
 (OUT/'latest.md').write_text('# GUI Runner Bridge\n\n'+json.dumps(payload,indent=2)+'\n')
 print(f"Wrote {path.relative_to(ROOT)}")
 return 1 if payload.get('result',{}).get('exitCode') not in (0,None) else 0
if __name__=='__main__': raise SystemExit(main())
