#!/usr/bin/env python3
"""Central multi-language fix center for HELIOS completion blockers."""
from __future__ import annotations
import json, datetime as dt, re, argparse
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'reports/code-fix-center'
LANGS=[
 {'language':'csharp','agent':'C# Center','command':'python3 scripts/automation/fix_csharp_compile.py','patterns':[r'CS\d{4}',r'dotnet build',r'\.csproj'],'risk':'medium','ownedPaths':['src/core','tests']},
 {'language':'cpp','agent':'C++ Accelerator','command':'python3 scripts/native/benchmark_native.py','patterns':[r'cmake',r'\.cpp',r'\.vcxproj'],'risk':'medium','ownedPaths':['src/native']},
 {'language':'fsharp','agent':'F# Analytics Oracle','command':'python3 scripts/analytics/fsharp_test_report.py','patterns':[r'FS\d{4}',r'\.fsproj',r'FSharp'],'risk':'medium','ownedPaths':['src/analytics','tests/analytics']},
 {'language':'python','agent':'Python AIHub Connector','command':'python3 tools/aihub/smoke-test.py','patterns':[r'Traceback',r'py_compile',r'\.py'],'risk':'low','ownedPaths':['scripts','tools/aihub']},
 {'language':'azure','agent':'Azure Warden','command':'python3 scripts/azure/bicep_report.py what-if','patterns':[r'bicep',r'az deployment',r'ARM'],'risk':'high','ownedPaths':['infra','tools/azure']},
]
def read(path):
 p=ROOT/path
 return p.read_text(encoding='utf-8',errors='ignore') if p.exists() else ''
def main():
 ap=argparse.ArgumentParser(); ap.add_argument('--log',default='reports/final-gate/final-gate.json'); a=ap.parse_args()
 text=read(Path(a.log)); plans=[]
 for item in LANGS:
  matches=sum(1 for pat in item['patterns'] if re.search(pat,text,re.I))
  plans.append({**item,'mode':'copy-command','matchCount':matches,'priority':0 if matches else 99,'nextCommand':item['command']})
 plans=sorted(plans,key=lambda p:(p['priority'],p['language']))
 payload={'schemaVersion':2,'generatedUtc':dt.datetime.now(dt.timezone.utc).isoformat(),'sourceLog':a.log,'plans':plans,'firstRecommendation':plans[0] if plans else None}
 OUT.mkdir(parents=True,exist_ok=True); (OUT/'code-fix-center.json').write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n')
 (OUT/'code-fix-center.md').write_text('# Code Fix Center\n\n'+'\n'.join(f"- {p['agent']} [{p['language']}] priority {p['priority']}: `{p['command']}`" for p in plans)+'\n')
 print(f"Wrote {(OUT/'code-fix-center.md').relative_to(ROOT)}")
if __name__=='__main__': main()
