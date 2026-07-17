#!/usr/bin/env python3
from __future__ import annotations
import json, datetime as dt
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'reports/agents'
def load(path, default):
 p=ROOT/path
 return json.loads(p.read_text(encoding='utf-8')) if p.exists() else default
def main():
 runtime=load('config/helios-agent-runtime.json', {'agents':[]}); progress=load('config/helios-agent-progression.json', {'classes':[],'partySlots':[],'shop':[]}); classes=progress.get('classes',[])
 party=[]; fleet_xp=0
 for idx,agent in enumerate(runtime.get('agents',[])):
  klass=classes[idx % len(classes)] if classes else {'label':'Agent','specializations':[],'abilities':[]}
  xp=25+10*len(agent.get('allowedPaths',[]))+5*len(agent.get('requiredTools',[]))+15*len(agent.get('reportOutputs',[])); fleet_xp+=xp
  party.append({'slot':progress.get('partySlots',['member'])[idx % max(1,len(progress.get('partySlots',['member'])))],'agentId':agent.get('agentId'),'class':klass.get('label'),'level':max(1,xp//50),'xp':xp,'specializations':klass.get('specializations',[]),'subSpecializations':agent.get('allowedPaths',[]),'abilities':klass.get('abilities',[]),'preferredModel':agent.get('preferredModel'),'fallbackModel':agent.get('fallbackModel'),'setupCommand':'./tools/helios.ps1 agents runtime'})
 payload={'generatedUtc':dt.datetime.now(dt.timezone.utc).isoformat(),'fleet':{'name':progress.get('fleet',{}).get('name'),'xp':fleet_xp,'level':max(1,fleet_xp//100)},'party':party,'shop':progress.get('shop',[])}
 OUT.mkdir(parents=True,exist_ok=True); (OUT/'agent-party.json').write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n'); (OUT/'agent-party.md').write_text('# Agent Party\n\n'+f"Fleet level {payload['fleet']['level']} XP {fleet_xp}\n\n"+'\n'.join(f"- {p['slot']}: {p['agentId']} {p['class']} Lv {p['level']}" for p in party)+'\n')
 print(f"Wrote {(OUT/'agent-party.md').relative_to(ROOT)}")
if __name__=='__main__': main()
