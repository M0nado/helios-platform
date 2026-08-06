#!/usr/bin/env python3
"""Identify Python automation that should stay Python vs move into C#/F#/C++ engines."""
from __future__ import annotations
import datetime as dt, json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'reports/language-ownership'
RULES=[
 ('csharp-core',['final_gate','deep_setup','gui_runner','validate_report','github_takeover','setup_repository'],'C# orchestration core: typed contracts, approvals, CLI/GUI, GitHub/Azure wrappers, report validation'),
 ('fsharp-analytics',['score','scoring','learning','summarize','category','model_cost','language_aware','merge_decision','agent_xp','party'],'F# analytics: ranking, scoring, XP, cost/speed optimization, prediction, merge decisions'),
 ('cpp-performance',['conflict','benchmark','native','diff','forecast'],'C++ acceleration: diff matrices, conflict overlap, hot-path scanning, native benchmarks'),
 ('python-glue',['openai','provider','aihub','microsoft365','bicep','vault','policy','render','store','hermes','xcore','autoconnect'],'Python glue: provider SDKs, AIHub, Linux runner helpers, report rendering, Azure CLI parsing')]
def classify(path:Path):
 text='/'.join(path.parts).lower(); hits=[]
 for target,keys,reason in RULES:
  if any(key in text for key in keys): hits.append({'target':target,'reason':reason})
 return hits or [{'target':'review','reason':'No strong signal; review manually before promoting out of Python'}]
def main():
 files=sorted(ROOT.glob('scripts/**/*.py'))+sorted(ROOT.glob('tools/**/*.py'))
 items=[{'path':str(path.relative_to(ROOT)),'recommendations':classify(path)} for path in files]
 counts={}
 for item in items:
  target=item['recommendations'][0]['target']; counts[target]=counts.get(target,0)+1
 payload={'generatedUtc':dt.datetime.now(dt.timezone.utc).isoformat(),'principle':'Keep C# as the through-line orchestration core, promote stable scoring to F#, hot-path overlap/diff work to C++, and keep provider/report/Linux glue in Python.','counts':counts,'items':items}
 OUT.mkdir(parents=True,exist_ok=True); (OUT/'python-language-split.json').write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n')
 lines=['# Python Language Split Report','',payload['principle'],'','## Counts']+[f"- {k}: {v}" for k,v in sorted(counts.items())]
 lines += ['','## Candidates']
 for item in items:
  rec=item['recommendations'][0]
  lines.append(f"- `{item['path']}` -> **{rec['target']}**: {rec['reason']}")
 (OUT/'python-language-split.md').write_text('\n'.join(lines)+'\n')
 print(f"Wrote {(OUT/'python-language-split.md').relative_to(ROOT)}")
if __name__=='__main__': main()
