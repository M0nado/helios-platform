#!/usr/bin/env python3
from __future__ import annotations
import json, datetime as dt
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'reports/helios-store'
def load(path, default):
 p=ROOT/path
 return json.loads(p.read_text(encoding='utf-8')) if p.exists() else default
def main():
 payload={'generatedUtc':dt.datetime.now(dt.timezone.utc).isoformat(),'agents':load('config/helios-agents.json',{}).get('agents',[]),'runtime':load('config/helios-agent-runtime.json',{}),'models':load('config/helios-model-store.json',{}).get('models',[]),'capabilities':load('config/helios-capabilities.json',{}).get('capabilities',[]),'guiCommands':load('config/helios-gui-commands.json',{})}
 OUT.mkdir(parents=True,exist_ok=True); (OUT/'store.json').write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n'); (OUT/'store.md').write_text(f"# HELIOS Store\n\n- Agents: {len(payload['agents'])}\n- Models: {len(payload['models'])}\n")
 print(f"Wrote {(OUT/'store.md').relative_to(ROOT)}")
if __name__=='__main__': main()
