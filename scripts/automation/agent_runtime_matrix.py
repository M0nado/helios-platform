#!/usr/bin/env python3
from __future__ import annotations
import json, datetime as dt
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'reports/agents'
def main():
 cfg=json.loads((ROOT/'config/helios-agent-runtime.json').read_text()); agents=json.loads((ROOT/'config/helios-agents.json').read_text()).get('agents',[])
 payload={'generatedUtc':dt.datetime.now(dt.timezone.utc).isoformat(),'runtime':cfg,'declaredAgents':[a.get('name') for a in agents]}; OUT.mkdir(parents=True,exist_ok=True)
 (OUT/'runtime-matrix.json').write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n'); (OUT/'runtime-matrix.md').write_text('# Agent Runtime Matrix\n\n'+'\n'.join(f"- {a['agentId']}: {a['preferredModel']} -> {a['fallbackModel']}" for a in cfg['agents'])+'\n')
 print(f"Wrote {(OUT/'runtime-matrix.md').relative_to(ROOT)}")
if __name__=='__main__': main()
