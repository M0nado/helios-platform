#!/usr/bin/env python3
from __future__ import annotations
import json
from datetime import datetime, timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
CFG=ROOT/'config/aihub-module-blueprint.json'
OUT=ROOT/'reports/learning/aihub-module-blueprint.json'
MD=ROOT/'reports/learning/aihub-module-blueprint.md'

def main():
    cfg=json.loads(CFG.read_text())
    rows=[]
    for module in cfg['modules']:
        for language in ['foundation','cpp','fsharp','python']:
            value=module[language]
            items=value if isinstance(value,list) else [value]
            rows.append({'module':module['id'],'language':language,'responsibilities':items,'count':len(items)})
    payload={'generatedUtc':datetime.now(timezone.utc).isoformat(),'principle':cfg['principle'],'languageTargets':cfg['languageTargets'],'modules':cfg['modules'],'rows':rows,'contractFiles':cfg.get('contractFiles',[]),'upgradeOrder':['csharp-foundation-contracts','cpp-rendering-performance-security','fsharp-deep-learning-security-performance-scoring','python-aihub-provider-linux-glue-only','yaml-bicep-json-md-automation-ledgers']}
    OUT.parent.mkdir(parents=True,exist_ok=True); OUT.write_text(json.dumps(payload,indent=2)+'\n')
    lines=['# AIHub Module Blueprint','',f"Generated: `{payload['generatedUtc']}`",'',payload['principle'],'','## Modules']
    for m in cfg['modules']:
        lines += [f"### {m['id']}",f"- Foundation: {m['foundation']}",f"- C++: {', '.join(m['cpp'])}",f"- F#: {', '.join(m['fsharp'])}",f"- Python: {', '.join(m['python'])}"]
    lines += ['','## Contract files']+[f'- `{x}`' for x in payload.get('contractFiles',[])]
    lines += ['','## Upgrade order']+[f"- {x}" for x in payload['upgradeOrder']]
    MD.write_text('\n'.join(lines)+'\n')
    print(f'Wrote {OUT.relative_to(ROOT)}'); print(f'Wrote {MD.relative_to(ROOT)}')
if __name__=='__main__': main()
