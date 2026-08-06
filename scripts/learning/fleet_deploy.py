#!/usr/bin/env python3
from __future__ import annotations
import json, datetime as dt
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'reports/fleet'
def load(path, default):
 p=ROOT/path
 return json.loads(p.read_text(encoding='utf-8')) if p.exists() else default
def main():
 shop=load('config/helios-agent-shop.json', {'agentTypes':[]}); party=load('reports/agents/agent-party.json', {'fleet':{'xp':0},'party':[]})
 fleet_xp=party.get('fleet',{}).get('xp',0); deploy=[]
 for a in shop.get('agentTypes',[]):
  deploy.append({**a,'affordable':fleet_xp>=a.get('cost',0),'status':'available' if fleet_xp>=a.get('cost',0) else 'locked','deployMode':'copy-command'})
 payload={'generatedUtc':dt.datetime.now(dt.timezone.utc).isoformat(),'fleetXp':fleet_xp,'deployableAgents':deploy}
 OUT.mkdir(parents=True,exist_ok=True); (OUT/'fleet-deploy.json').write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n'); (OUT/'fleet-deploy.md').write_text('# Fleet Deploy\n\n'+'\n'.join(f"- {a['label']} ({a['language']}): {a['status']} cost {a['cost']}" for a in deploy)+'\n')
 print(f"Wrote {(OUT/'fleet-deploy.md').relative_to(ROOT)}")
if __name__=='__main__': main()
