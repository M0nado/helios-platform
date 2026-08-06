#!/usr/bin/env python3
from __future__ import annotations
import json, datetime as dt
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'reports/agents'
def read_json(path, default):
 p=ROOT/path
 return json.loads(p.read_text(encoding='utf-8')) if p.exists() else default
def main():
 runtime=read_json('config/helios-agent-runtime.json', {'agents':[]}); agents=[]
 for item in runtime.get('agents',[]):
  xp=10*len(item.get('allowedPaths',[]))+5*len(item.get('requiredTools',[]))
  agents.append({'agentId':item.get('agentId'),'xp':xp,'level':max(1,xp//10),'specialty':item.get('riskLevel','general'),'bestModel':item.get('preferredModel'),'fallbackModel':item.get('fallbackModel'),'allowedPaths':item.get('allowedPaths',[]),'currentBlocker':None,'nextTrainingTask':'run final gate and record outcome'})
 OUT.mkdir(parents=True,exist_ok=True); payload={'generatedUtc':dt.datetime.now(dt.timezone.utc).isoformat(),'agents':agents}
 (OUT/'agent-xp.json').write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n'); (OUT/'agent-xp.md').write_text('# Agent XP\n\n'+'\n'.join(f"- {a['agentId']}: level {a['level']} xp {a['xp']}" for a in agents)+'\n')
 print(f"Wrote {(OUT/'agent-xp.md').relative_to(ROOT)}")
if __name__=='__main__': main()
