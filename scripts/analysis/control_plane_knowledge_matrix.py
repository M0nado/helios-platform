#!/usr/bin/env python3
from __future__ import annotations
import json, shutil, subprocess
from datetime import datetime, timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
CONFIG=ROOT/'config/knowledge-sources.json'
OUT=ROOT/'reports/learning/control-plane-knowledge-matrix.json'
MD=ROOT/'reports/learning/control-plane-knowledge-matrix.md'
AREAS={
 'github-workflows':['.github/workflows','workflow','runner','actions'],
 'github-wiki-docs':['.github','wiki','docs','README','GUIDE'],
 'codespaces':['codespace','devcontainer','.devcontainer'],
 'cli-control':['helios.sh','scripts/control','scripts/github','gh '],
 'pipelines-runners':['pipeline','runner','ci','quality','build'],
 'branch-agents':['scripts/agents/branch_fix_agents.py','branch-fix','agent'],
 'hermes-agents':['hermes','HERMES','fleet','scripts/agents/hermes_fleet_readiness.py'],
 'xcore-agents':['xcore','XCore','src/native','CMakeLists'],
 'language-absorption':['language','learning','absorption','optimizer','matrix'],
 'testing-pruning':['test','prune','merge','cleanup','validation'],
 'jstor-other':['jstor','literature','research','corpus']
}

def rg_files():
 if shutil.which('rg'):
  p=subprocess.run(['rg','--files'],cwd=ROOT,text=True,capture_output=True)
  if p.returncode in (0,1):
   return [x for x in p.stdout.splitlines() if x]
 p=subprocess.run(['git','ls-files'],cwd=ROOT,text=True,capture_output=True)
 return [x for x in p.stdout.splitlines() if x] if p.returncode==0 else []

def git_log():
 p=subprocess.run(['git','log','--all','--date=iso','--pretty=format:%h%x1f%cd%x1f%s','--since=2025-07-30 20:00'],cwd=ROOT,text=True,capture_output=True)
 rows=[]
 for line in p.stdout.splitlines():
  parts=line.split('\x1f')
  if len(parts)==3: rows.append({'short':parts[0],'date':parts[1],'subject':parts[2]})
 return rows

def hits(area, files, commits):
 needles=[n.lower() for n in AREAS[area]]
 file_hits=[f for f in files if any(n in f.lower() for n in needles)][:80]
 commit_hits=[c for c in commits if any(n in c['subject'].lower() for n in needles)][:40]
 return file_hits, commit_hits

def main():
 cfg=json.loads(CONFIG.read_text())
 files=rg_files(); commits=git_log(); rows=[]
 for area in AREAS:
  fh,ch=hits(area, files, commits)
  rows.append({'area':area,'fileHitCount':len(fh),'commitHitCount':len(ch),'files':fh,'commits':ch})
 payload={'generatedUtc':datetime.now(timezone.utc).isoformat(),'ok':True,'knowledgeSources':cfg['sources'],'areas':rows,'principle':'C# owns secure control; GitHub/CLI/wiki/Codespaces/runners automate safely; Hermes and XCore are agent engines; JSTOR is planned as an approved external research source; reports remain non-mutating unless explicitly authenticated.'}
 OUT.parent.mkdir(parents=True,exist_ok=True); OUT.write_text(json.dumps(payload,indent=2)+'\n')
 lines=['# Control Plane Knowledge Matrix','',f"Generated: `{payload['generatedUtc']}`",'',payload['principle'],'','## Knowledge sources','']
 for name,src in cfg['sources'].items():
  lines += [f"### {name}",f"Role: {src['role']}",f"Safe mode: {src['safeMode']}"]+[f"- `{c}`" for c in src['commands']]+['']
 lines += ['## Areas','','| Area | File hits | Commit hits |','| --- | ---: | ---: |']
 for r in rows: lines.append(f"| {r['area']} | {r['fileHitCount']} | {r['commitHitCount']} |")
 for r in rows:
  lines += ['',f"## {r['area']}",'','Files:']+[f"- `{f}`" for f in r['files'][:50]]
  lines += ['Commits:']+[f"- `{c['short']}` {c['date']} — {c['subject']}" for c in r['commits'][:25]]
 MD.write_text('\n'.join(lines)+'\n')
 print(f"Wrote {OUT.relative_to(ROOT)}"); print(f"Wrote {MD.relative_to(ROOT)}")
 return 0
if __name__=='__main__': raise SystemExit(main())
