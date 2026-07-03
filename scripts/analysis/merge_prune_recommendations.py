#!/usr/bin/env python3
from __future__ import annotations
import json,subprocess
from datetime import datetime, timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
SRC=ROOT/'reports/branch-intelligence/branch-ranking.json'
OUT=ROOT/'reports/branch-intelligence/merge-prune-recommendations.json'
MD=ROOT/'reports/branch-intelligence/merge-prune-recommendations.md'
GRADING=ROOT/'reports/learning/complex-code-grading.compact.json'
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


def load_grades():
    if not GRADING.exists():
        return {}
    data=json.loads(GRADING.read_text())
    grades={}
    for bucket in ('topKeep','topPrune'):
        for item in data.get(bucket,[]):
            grades[item.get('path','')]=item
    return grades

def grade_summary(files, grades):
    matched=[grades[f] for f in files if f in grades]
    if not matched:
        return {'gradedFiles':0,'gradedLines':0,'avgKeepScore':0,'avgReuseScore':0,'avgDuplicateLineRatio':0,'keepFiles':[],'pruneFiles':[]}
    avg=lambda key: round(sum(float(item.get('metrics',{}).get(key,0)) for item in matched)/len(matched),4)
    return {
        'gradedFiles':len(matched),
        'gradedLines':sum(int(item.get('metrics',{}).get('lineCount',0)) for item in matched),
        'avgKeepScore':avg('keepScore'),
        'avgReuseScore':avg('reuseScore'),
        'avgDuplicateLineRatio':avg('duplicateLineRatio'),
        'keepFiles':[item['path'] for item in sorted(matched,key=lambda item:item.get('metrics',{}).get('keepScore',0),reverse=True)[:5]],
        'pruneFiles':[item['path'] for item in sorted(matched,key=lambda item:item.get('metrics',{}).get('duplicateLineRatio',0),reverse=True)[:5]],
    }

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

def recommendation(score, ci, files, risk, domains, grade):
    if not files: return 'prune-after-review'
    if grade.get('avgDuplicateLineRatio',0) >= 0.55 and grade.get('avgKeepScore',0) < 0.45: return 'prune-redundant-after-unique-parts-absorbed'
    if grade.get('avgKeepScore',0) >= 0.70 and risk!='high': return 'merge-or-absorb-high-value-code'
    if risk=='high': return 'manual-review'
    if 'hermes-fleet' in domains or 'control' in domains: return 'manual-review-priority'
    if score>=80 and ci>=50 and risk=='low': return 'merge-candidate'
    if score>=50 or grade.get('avgReuseScore',0) >= 0.48: return 'extract-ideas-only'
    return 'archive'

def main():
    ranked={b.get('name'):b for b in (json.loads(SRC.read_text()) if SRC.exists() else [])}
    grades=load_grades()
    branches=branch_names() or sorted(ranked)
    recs=[]
    for branch in branches:
        clean=branch.replace('remotes/','')
        if clean in {'main','master','work'} or clean.endswith('/main') or clean.endswith('/master'): continue
        files=changed_files(branch)
        rank=ranked.get(branch, ranked.get(clean, {}))
        score=rank.get('score',0); ci=rank.get('ci',{}).get('score',0)
        domains=detect_domains(files); risk=conflict_risk(files); grade=grade_summary(files,grades)
        recs.append({'branch':branch,'lastCommit':last_commit(branch),'score':score,'ciScore':ci,'fileCount':len(files),'gradedLineCount':grade['gradedLines'],'domains':domains,'conflictRisk':risk,'complexGrade':grade,'recommendation':recommendation(score,ci,files,risk,domains,grade),'reason':'Safe recommendation only; no branch is deleted or merged automatically. Complex grading affects keep/absorb/prune scoring before any deletion or merge.'})
    payload={'generatedUtc':datetime.now(timezone.utc).isoformat(),'base':'HEAD','complexGradingSource':str(GRADING.relative_to(ROOT)),'recommendations':recs}
    OUT.parent.mkdir(parents=True,exist_ok=True); OUT.write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n')
    lines=['# Branch Merge / Prune Recommendations','','No branches are merged or deleted by this report. Complex code grading influences merge/absorb/prune recommendations and carries scores/line counts into this report.','','| Branch | Last commit | Files | Graded lines | Keep | Reuse | Dup | Domains | Risk | Recommendation |','| --- | --- | ---: | ---: | ---: | ---: | ---: | --- | --- | --- |']
    lines += [f"| `{r['branch']}` | {r['lastCommit'] or ''} | {r['fileCount']} | {r['gradedLineCount']} | {r['complexGrade']['avgKeepScore']} | {r['complexGrade']['avgReuseScore']} | {r['complexGrade']['avgDuplicateLineRatio']} | {', '.join(r['domains'])} | {r['conflictRisk']} | {r['recommendation']} |" for r in recs]
    MD.write_text('\n'.join(lines)+'\n'); print(f'Wrote {OUT.relative_to(ROOT)}'); print(f'Wrote {MD.relative_to(ROOT)}')
if __name__=='__main__': main()
