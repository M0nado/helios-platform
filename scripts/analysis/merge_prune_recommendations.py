#!/usr/bin/env python3
from __future__ import annotations
import json,subprocess
from datetime import datetime, timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
SRC=ROOT/'reports/branch-intelligence/branch-ranking.json'
OUT=ROOT/'reports/branch-intelligence/merge-prune-recommendations.json'
MD=ROOT/'reports/branch-intelligence/merge-prune-recommendations.md'
DOMAINS={
    'control':['scripts/control/','scripts/build_graph/'],
    'hermes-fleet':['hermes','HERMES','scripts/agents/','config/hermes-fleet'],
    'csharp':['.cs','.csproj','src/core/','src/gui/'],
    'fsharp':['.fs','.fsproj','src/analytics/','tests/analytics/'],
    'native-cpp':['.cpp','.hpp','.h','src/native/','CMakeLists.txt'],
    'python':['.py','scripts/'],
    'azure':['infra/azure/','scripts/azure/','.bicep'],
    'security':['scripts/security/','docs/security/','Security'],
    'gui':['src/gui/','WinUI','MAUI','status-site/'],
}

def git(cmd):
    p=subprocess.run(['git',*cmd],cwd=ROOT,text=True,capture_output=True)
    return p.stdout.strip() if p.returncode==0 else ''

def branch_names():
    out=git(['branch','-a','--format=%(refname:short)'])
    names=[]
    for line in out.splitlines():
        name=line.strip()
        if not name or name in {'HEAD','origin/HEAD'} or ' -> ' in name: continue
        names.append(name)
    return sorted(set(names))

def changed_files(branch, base='HEAD'):
    out=git(['diff','--name-only',f'{base}...{branch}']) or git(['diff','--name-only',branch])
    return [line for line in out.splitlines() if line]

def last_commit(branch):
    return git(['log','-1','--format=%cI',branch]) or None

def detect_domains(files):
    hits=[]
    for domain,needles in DOMAINS.items():
        if any(any(needle in f for needle in needles) for f in files): hits.append(domain)
    return hits or ['unknown']

def conflict_risk(files):
    if len(files)>250: return 'high'
    hot=sum(1 for f in files if f.startswith(('scripts/build_graph/','scripts/control/','config/','.github/workflows/')))
    if hot>=5 or len(files)>75: return 'medium'
    return 'low'

def recommendation(score, ci, files, risk, domains):
    if not files: return 'prune-after-review'
    if risk=='high': return 'manual-review'
    if 'hermes-fleet' in domains or 'control' in domains: return 'manual-review-priority'
    if score>=80 and ci>=50 and risk=='low': return 'merge-candidate'
    if score>=50: return 'extract-ideas-only'
    return 'archive'

def main():
    ranked={b.get('name'):b for b in (json.loads(SRC.read_text()) if SRC.exists() else [])}
    branches=branch_names() or sorted(ranked)
    recs=[]
    for branch in branches:
        clean=branch.replace('remotes/','')
        if clean in {'main','master','work'} or clean.endswith('/main') or clean.endswith('/master'): continue
        files=changed_files(branch)
        rank=ranked.get(branch, ranked.get(clean, {}))
        score=rank.get('score',0); ci=rank.get('ci',{}).get('score',0)
        domains=detect_domains(files); risk=conflict_risk(files)
        recs.append({'branch':branch,'lastCommit':last_commit(branch),'score':score,'ciScore':ci,'fileCount':len(files),'domains':domains,'conflictRisk':risk,'recommendation':recommendation(score,ci,files,risk,domains),'reason':'Safe recommendation only; no branch is deleted or merged automatically.'})
    payload={'generatedUtc':datetime.now(timezone.utc).isoformat(),'base':'HEAD','recommendations':recs}
    OUT.parent.mkdir(parents=True,exist_ok=True); OUT.write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n')
    lines=['# Branch Merge / Prune Recommendations','','No branches are merged or deleted by this report.','','| Branch | Last commit | Files | Domains | Risk | Recommendation |','| --- | --- | ---: | --- | --- | --- |']
    lines += [f"| `{r['branch']}` | {r['lastCommit'] or ''} | {r['fileCount']} | {', '.join(r['domains'])} | {r['conflictRisk']} | {r['recommendation']} |" for r in recs]
    MD.write_text('\n'.join(lines)+'\n'); print(f'Wrote {OUT.relative_to(ROOT)}'); print(f'Wrote {MD.relative_to(ROOT)}')
if __name__=='__main__': main()
