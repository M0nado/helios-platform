#!/usr/bin/env python3
"""Parse C# build failures into grouped repair hints."""
from __future__ import annotations
import json,re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'reports/autofix'
PATTERNS=[('missing-type',r"CS0246.*?The type or namespace name '([^']+)'"),('duplicate-type',r"CS0101.*?already contains a definition for '([^']+)'"),('missing-interface-member',r"CS0535.*?does not implement interface member '([^']+)'"),('nullable',r"CS86\d+"),('windows-targeting',r"NETSDK1100|EnableWindowsTargeting")]
def main():
    texts=[]
    for path in [ROOT/'reports/final-gate/final-gate.json', *ROOT.glob('src/core_build_release_attempt*.txt')]:
        if path.exists(): texts.append(path.read_text(errors='ignore'))
    text='\n'.join(texts); groups={k:[] for k,_ in PATTERNS}
    for kind,pat in PATTERNS:
        for m in re.finditer(pat,text): groups[kind].append(m.group(0)[:300])
    OUT.mkdir(parents=True,exist_ok=True); payload={'groups':groups,'nextActions':['fix duplicate models first','add missing shared DTOs/contracts','implement missing interface members','rerun dotnet build']}
    (OUT/'csharp-compile.json').write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n')
    lines=['# C# Compile Fix Hints','']
    for k,v in groups.items(): lines += [f'## {k}', '', *[f'- `{x}`' for x in v[:20]], '']
    (OUT/'csharp-compile.md').write_text('\n'.join(lines)+'\n'); print(f"Wrote {(OUT/'csharp-compile.md').relative_to(ROOT)}")
if __name__=='__main__': main()
