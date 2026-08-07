#!/usr/bin/env python3
from __future__ import annotations
import json, re, shutil, subprocess
from collections import Counter, defaultdict
from datetime import datetime, timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'reports/learning/document-code-absorption-ranker.json'
MD=ROOT/'reports/learning/document-code-absorption-ranker.md'
EXTS={'.md':'markdown-doc','.bicep':'bicep-infra','.json':'json-config','.yml':'yaml-workflow','.yaml':'yaml-workflow','.cs':'csharp-code','.cpp':'cpp-code','.fs':'fsharp-code','.py':'python-code'}
IDEA_TERMS=['hermes','fleet','xcore','bicep','jstor','wiki','codespace','runner','agent','merge','prune','test','score','rank','learn','absorb','optimize','security','vault','gui','dashboard','analytics','prediction','performance','cloud','azure','openai','codex','llm']
BAD_TERMS=['todo','fixme','deprecated','duplicate','generated','temp','backup','old','obsolete']

def rg_files():
 if shutil.which('rg'):
  p=subprocess.run(['rg','--files'],cwd=ROOT,text=True,capture_output=True)
  candidates=p.stdout.splitlines() if p.returncode in (0,1) else []
 else:
  candidates=[]
 if not candidates:
  p=subprocess.run(['git','ls-files'],cwd=ROOT,text=True,capture_output=True)
  candidates=p.stdout.splitlines() if p.returncode==0 else []
 return [x for x in candidates if Path(x).suffix.lower() in EXTS]

def read(path):
 try: return (ROOT/path).read_text(errors='ignore')
 except Exception: return ''

def module(path):
 parts=path.split('/')
 return '/'.join(parts[:2]) if len(parts)>1 else parts[0]

def tokens(text):
 return re.findall(r'[A-Za-z][A-Za-z0-9_-]{2,}', text.lower())

def shingles(tok):
 return {' '.join(tok[i:i+4]) for i in range(max(0,len(tok)-3))}

def abstract_ideas(path,text):
 low=(path+' '+text[:20000]).lower(); found=[]
 for term in IDEA_TERMS:
  if term in low: found.append(term)
 return found[:20]

def main():
 files=rg_files(); all_shingles={}; docs=[]; module_counts=Counter()
 for f in files:
  text=read(f); tok=tokens(text); sh=shingles(tok); all_shingles[f]=sh; module_counts[module(f)] += 1
 # document frequency for uniqueness
 df=Counter()
 for sh in all_shingles.values(): df.update(sh)
 rows=[]
 for f in files:
  text=read(f); tok=tokens(text); sh=all_shingles[f]; ext=Path(f).suffix.lower(); kind=EXTS[ext]
  ideas=abstract_ideas(f,text); bad=sum(text.lower().count(t) for t in BAD_TERMS)
  unique=sum(1 for x in sh if df[x]==1); repeated=sum(1 for x in sh if df[x]>3)
  line_count=text.count('\n')+1 if text else 0
  score=min(100, len(ideas)*4 + min(30, unique//10) + min(20,line_count//80) - min(30,bad*2) - min(20,repeated//80))
  action='absorb-record-ideas' if score>=55 else ('keep-reference' if score>=30 else 'prune-or-dedupe-review')
  rows.append({'path':f,'module':module(f),'kind':kind,'lineCount':line_count,'ideaCount':len(ideas),'ideas':ideas,'uniqueShingles':unique,'repetitiveShingles':repeated,'badSignals':bad,'score':score,'action':action})
 rows.sort(key=lambda r:(-r['score'],r['path']))
 summary=Counter(r['action'] for r in rows); by_kind=Counter(r['kind'] for r in rows)
 payload={'generatedUtc':datetime.now(timezone.utc).isoformat(),'fileCount':len(rows),'actionCounts':dict(summary),'kindCounts':dict(by_kind),'topAbsorb':rows[:100],'pruneReview':[r for r in rows if r['action']=='prune-or-dedupe-review'][:100],'principle':'Markdown, Bicep, YAML, JSON, JSTOR/planned sources, and code are all absorbable knowledge objects: score unique ideas, preserve new concepts, and prune redundant/generated/low-signal material.'}
 OUT.parent.mkdir(parents=True,exist_ok=True); OUT.write_text(json.dumps(payload,indent=2)+'\n')
 lines=['# Document / Code Absorption Ranker','',f"Generated: `{payload['generatedUtc']}`",'',payload['principle'],'',f"Files scored: **{payload['fileCount']}**",'','## Action counts','']+[f"- {k}: {v}" for k,v in payload['actionCounts'].items()]
 lines += ['','## Kind counts','']+[f"- {k}: {v}" for k,v in payload['kindCounts'].items()]
 lines += ['','## Top absorb/keep ideas','','| Score | Action | Kind | Lines | Ideas | File |','| ---: | --- | --- | ---: | --- | --- |']
 for r in rows[:120]: lines.append(f"| {r['score']} | {r['action']} | {r['kind']} | {r['lineCount']} | {', '.join(r['ideas'])} | `{r['path']}` |")
 lines += ['','## Prune / dedupe review','','| Score | Kind | Bad | Repetitive | File |','| ---: | --- | ---: | ---: | --- |']
 for r in payload['pruneReview'][:80]: lines.append(f"| {r['score']} | {r['kind']} | {r['badSignals']} | {r['repetitiveShingles']} | `{r['path']}` |")
 MD.write_text('\n'.join(lines)+'\n')
 print(f"Wrote {OUT.relative_to(ROOT)}"); print(f"Wrote {MD.relative_to(ROOT)}")
 return 0
if __name__=='__main__': raise SystemExit(main())
