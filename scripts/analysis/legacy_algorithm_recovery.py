#!/usr/bin/env python3
from __future__ import annotations
import json, subprocess
from collections import defaultdict
from datetime import datetime, timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'reports/learning/legacy-algorithm-recovery.json'
MD=ROOT/'reports/learning/legacy-algorithm-recovery.md'
TERMS=['optimization','algorithm','parallel','cache','retry','backoff','chaos','scoring','ranking','prediction','analytics','vector','sql','cosmos','render','memory','gui','workflow','bicep','azure','github','runner','codespaces','agent','hermes','xcore','learning','prune','merge','absorb','jstor','cost','speed','quality']
KINDS={'.cs':'csharp','.fs':'fsharp','.cpp':'cpp','.hpp':'cpp','.h':'cpp','.py':'python','.md':'markdown','.yml':'yaml','.yaml':'yaml','.bicep':'bicep','.json':'json','.ps1':'powershell'}

def files():
 p=subprocess.run(['rg','--files'],cwd=ROOT,text=True,capture_output=True)
 return p.stdout.splitlines()

def safe_text(path):
 try:
  p=ROOT/path
  if p.stat().st_size>250_000: return ''
  return p.read_text(errors='ignore')
 except Exception: return ''

def main():
 rows=[]; domain_counts=defaultdict(int)
 for f in files():
  if f.startswith(('reports/','status-site/','.git/','.tools/')): continue
  text=(f+' '+safe_text(f)[:20000]).lower()
  hits=[t for t in TERMS if t in text]
  if not hits: continue
  kind=KINDS.get(Path(f).suffix.lower(),'other')
  score=len(hits)*5 + min(text.count('test'),20) + min(text.count('performance'),20) + min(text.count('agent'),20)
  domain_counts[kind]+=1
  rows.append({'path':f,'kind':kind,'score':score,'hits':hits[:20],'suggestedUse':suggest(f,hits,kind)})
 rows=sorted(rows,key=lambda r:(-r['score'],r['path']))[:250]
 payload={'generatedUtc':datetime.now(timezone.utc).isoformat(),'principle':'Recover older repository code/docs/algorithms as reusable ideas for the AIHub full framework. Keep unique ideas, route hot paths to C++, scoring to F#, orchestration/GUI to C#, automation to Python, and cloud/workflow/docs to YAML/Bicep/Markdown/JSON.','domainCounts':dict(domain_counts),'recovered':rows,'noMaxAgents':'Agent count is budget/capacity/outcome-driven, not hard-coded; Hermes/XCore fleets can scale up or down from local to cloud runners.'}
 OUT.parent.mkdir(parents=True,exist_ok=True); OUT.write_text(json.dumps(payload,indent=2)+'\n')
 lines=['# Legacy Algorithm Recovery','',f"Generated: `{payload['generatedUtc']}`",'',payload['principle'],'',payload['noMaxAgents'],'','## Domain counts']+[f"- {k}: {v}" for k,v in sorted(domain_counts.items())]
 lines += ['','## Recovered candidates','','| Score | Kind | Path | Hits | Suggested use |','| ---: | --- | --- | --- | --- |']
 for r in rows[:100]: lines.append(f"| {r['score']} | {r['kind']} | `{r['path']}` | {', '.join(r['hits'])} | {r['suggestedUse']} |")
 MD.write_text('\n'.join(lines)+'\n')
 print(f'Wrote {OUT.relative_to(ROOT)}'); print(f'Wrote {MD.relative_to(ROOT)}')
 return 0

def suggest(path,hits,kind):
 h=set(hits); low=path.lower()
 if 'render' in h or 'memory' in h or kind=='cpp': return 'C++/XCore hot-path or native acceleration candidate'
 if {'scoring','ranking','prediction','analytics','learning'} & h or kind=='fsharp': return 'F# learning/scoring/optimization candidate'
 if 'gui' in h or kind=='csharp': return 'C# GUI/orchestration/contract candidate'
 if {'agent','hermes','xcore','workflow','github','azure'} & h or kind=='python': return 'Python AIHub automation/Hermes-XCore glue candidate'
 if kind in {'yaml','bicep','json'}: return 'YAML/Bicep/JSON setup and cloud/control-plane candidate'
 return 'Knowledge absorption / docs / abstract idea candidate'
if __name__=='__main__': raise SystemExit(main())
