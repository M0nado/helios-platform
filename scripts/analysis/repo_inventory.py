#!/usr/bin/env python3
from __future__ import annotations
import json
from collections import Counter, defaultdict
from datetime import datetime, timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'reports/project-inventory/repo-inventory.json'; MD=ROOT/'reports/project-inventory/repo-inventory.md'
SKIP={'.git','bin','obj','.tools','.nuget'}
LANG={'.cs':'csharp','.fs':'fsharp','.fsproj':'fsharp-project','.csproj':'csharp-project','.cpp':'cpp','.hpp':'cpp','.h':'cpp','.py':'python','.ps1':'powershell','.sh':'shell','.bicep':'bicep','.yml':'yaml','.yaml':'yaml','.json':'json','.md':'markdown'}
files=[]
for p in ROOT.rglob('*'):
    if not p.is_file(): continue
    rel=p.relative_to(ROOT)
    if any(part in SKIP for part in rel.parts): continue
    if rel.parts[0] == 'reports' and len(rel.parts)>1 and rel.parts[1] not in {'project-inventory'}: continue
    suffix=p.suffix.lower(); files.append({'path':str(rel),'ext':suffix,'language':LANG.get(suffix,'other'),'top':rel.parts[0]})
by_lang=Counter(f['language'] for f in files); by_top=Counter(f['top'] for f in files)
projects=[f['path'] for f in files if f['ext'] in {'.csproj','.fsproj'} or f['path'].endswith(('.sln','.slnx','CMakeLists.txt','pyproject.toml','package.json'))]
modules=defaultdict(lambda:{'files':0,'languages':Counter(),'examples':[]})
for f in files:
    key=f['top']; modules[key]['files']+=1; modules[key]['languages'][f['language']]+=1
    if len(modules[key]['examples'])<5: modules[key]['examples'].append(f['path'])
payload={'generatedUtc':datetime.now(timezone.utc).isoformat(),'fileCount':len(files),'languageCounts':dict(by_lang),'topLevelCounts':dict(by_top),'projects':projects,'modules':{k:{'files':v['files'],'languages':dict(v['languages']),'examples':v['examples']} for k,v in sorted(modules.items())}}
OUT.parent.mkdir(parents=True,exist_ok=True); OUT.write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n')
lines=['# HELIOS Whole-Project Inventory','',f"Generated: `{payload['generatedUtc']}`",'',f"Files indexed: **{payload['fileCount']}**",'','## Languages','','| Language | Files |','| --- | ---: |']+[f"| {k} | {v} |" for k,v in by_lang.most_common()]
lines+=['','## Top-level modules','','| Module | Files | Dominant languages |','| --- | ---: | --- |']
for k,v in sorted(modules.items(), key=lambda kv: kv[1]['files'], reverse=True):
    langs=', '.join(f'{lk}:{lv}' for lk,lv in v['languages'].most_common(4)); lines.append(f"| `{k}` | {v['files']} | {langs} |")
lines+=['','## Project files']+[f'- `{p}`' for p in projects[:100]]
MD.write_text('\n'.join(lines)+'\n'); print(f'Wrote {OUT.relative_to(ROOT)}'); print(f'Wrote {MD.relative_to(ROOT)}')
