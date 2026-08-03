#!/usr/bin/env python3
from __future__ import annotations
import json
from datetime import datetime, timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
CFG=ROOT/'config/aihub-language-skill-profiles.json'
OUT=ROOT/'reports/learning/aihub-language-skill-profiles.json'
MD=ROOT/'reports/learning/aihub-language-skill-profiles.md'

def main():
    cfg=json.loads(CFG.read_text())
    rows=[]
    for p in cfg['profiles']:
        rows.append({'language':p['language'],'role':p['role'],'skillCount':len(p['skills']),'rankCriteriaCount':len(p['rankOn']),'guideWords':len(p['hundredWordGuide'].split())})
    payload={'generatedUtc':datetime.now(timezone.utc).isoformat(),'principle':cfg['principle'],'profiles':cfg['profiles'],'summary':rows,'autoUpgradeLoop':['inventory code and reports','rank language fit','prefer C# foundation, C++ hot paths, F# scoring, Python libraries only when needed','generate GUI/module recommendations','run build graph','record learning ledger','refresh dashboard']}
    OUT.parent.mkdir(parents=True,exist_ok=True); OUT.write_text(json.dumps(payload,indent=2)+'\n')
    lines=['# AIHub Language Skill Profiles','',f"Generated: `{payload['generatedUtc']}`",'',payload['principle'],'']
    for p in cfg['profiles']:
        lines += [f"## {p['language']} — {p['role']}",p['hundredWordGuide'],'',f"Skills: {', '.join(p['skills'])}",f"Rank on: {', '.join(p['rankOn'])}",'']
    lines += ['## Automatic upgrade loop']+[f"- {x}" for x in payload['autoUpgradeLoop']]
    MD.write_text('\n'.join(lines)+'\n')
    print(f'Wrote {OUT.relative_to(ROOT)}'); print(f'Wrote {MD.relative_to(ROOT)}')
if __name__=='__main__': main()
