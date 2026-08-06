#!/usr/bin/env python3
"""Render language-specific JRPG party formations for merge automation."""
from __future__ import annotations
import json, datetime as dt
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; CONFIG=ROOT/'config/helios-party-formations.json'; OUT=ROOT/'reports/agents'
def main():
 data=json.loads(CONFIG.read_text(encoding='utf-8')); formations=data.get('formations',[])
 payload={'generatedUtc':dt.datetime.now(dt.timezone.utc).isoformat(),'formations':formations,'recommendedDefault':'full-merge-raid'}
 OUT.mkdir(parents=True,exist_ok=True); (OUT/'party-formations.json').write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n')
 lines=['# Agent Party Formations','','| Formation | Agents | Required Checks | XP |','| --- | --- | --- | ---: |']
 for f in formations: lines.append(f"| {f['label']} | {', '.join(f['agents'])} | {', '.join(f['requiredChecks'])} | {f['xpReward']} |")
 (OUT/'party-formations.md').write_text('\n'.join(lines)+'\n')
 print(f"Wrote {(OUT/'party-formations.md').relative_to(ROOT)}")
if __name__=='__main__': main()
