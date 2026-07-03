#!/usr/bin/env python3
from __future__ import annotations
import json, shutil
from datetime import datetime, timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'reports/integrations/readiness-score.json'
MD=ROOT/'reports/integrations/readiness-score.md'
FINISH_JSON=ROOT/'reports/integrations/finish-readiness.json'
FINISH_MD=ROOT/'reports/integrations/finish-readiness.md'

def load(path, default):
    p=ROOT/path
    if not p.exists(): return default
    try: return json.loads(p.read_text())
    except json.JSONDecodeError: return default

def bool_check(id, target, ok): return {'id':id,'target':target,'ok':bool(ok)}

def category_score(items): return round(100*sum(1 for item in items if item['ok'])/len(items)) if items else 0

base_checks=[
 bool_check('command-center','helios.sh',Path('helios.sh').exists()),
 bool_check('execution-order','config/execution-order.json',Path('config/execution-order.json').exists()),
 bool_check('secrets-map','config/secrets-map.example.json',Path('config/secrets-map.example.json').exists()),
 bool_check('cross-access-profiles','config/cross-access-profiles.example.json',Path('config/cross-access-profiles.example.json').exists()),
 bool_check('github-inventory','scripts/github/github-inventory.py',Path('scripts/github/github-inventory.py').exists()),
 bool_check('azure-inventory','scripts/azure/azure-inventory.py',Path('scripts/azure/azure-inventory.py').exists()),
 bool_check('repo-inventory','scripts/analysis/repo_inventory.py',Path('scripts/analysis/repo_inventory.py').exists()),
 bool_check('hybrid-gap-analysis','scripts/analysis/hybrid_gap_analysis.py',Path('scripts/analysis/hybrid_gap_analysis.py').exists()),
 bool_check('gui-dashboard','scripts/dashboard/generate-gui.py',Path('scripts/dashboard/generate-gui.py').exists()),
 bool_check('web-server','scripts/web/helios-web.py',Path('scripts/web/helios-web.py').exists()),
 bool_check('pr-update-helper','scripts/github/update-pr-from-reports.py',Path('scripts/github/update-pr-from-reports.py').exists()),
 bool_check('python3-cli','python3',shutil.which('python3') is not None),
 bool_check('git-cli','git',shutil.which('git') is not None),
 bool_check('github-cli','gh',shutil.which('gh') is not None),
 bool_check('azure-cli','az',shutil.which('az') is not None),
 bool_check('dotnet-cli','dotnet',shutil.which('dotnet') is not None),
 bool_check('cmake-cli','cmake',shutil.which('cmake') is not None),
]
build=load(Path('reports/build-graph/latest.json'), {'counts':{},'results':[]})
secret=load(Path('reports/security/secret-preflight.json'), {'ok':False})
apply_gate=load(Path('reports/security/apply-gate-preflight.json'), {'ok':False})
hermes=load(Path('reports/hermes-fleet/latest.json'), {'ok':False})
azure_what_if=load(Path('reports/azure/what-if.json'), {'status':'missing'})
control=load(Path('reports/control-plane/control-summary.json'), {})
results=build.get('results',[])
def node_ok(tag):
    tagged=[r for r in results if tag in r.get('tags',[])]
    if not tagged:
        return True
    return all(r.get('status') in {'passed','skipped/dry-run'} for r in tagged)
categories={
 'python-automation':[bool_check('python-cli','python3',shutil.which('python3')), bool_check('python-build-graph','build graph',node_ok('python'))],
 'csharp-dotnet':[bool_check('dotnet-cli','dotnet',shutil.which('dotnet')), bool_check('dotnet-graph','dotnet nodes',node_ok('dotnet'))],
 'fsharp-analytics':[bool_check('analytics-path','src/analytics',Path('src/analytics').exists()), bool_check('fsharp-graph','fsharp nodes',node_ok('fsharp'))],
 'native-cmake':[bool_check('cmake-cli','cmake',shutil.which('cmake')), bool_check('native-graph','native nodes',node_ok('native'))],
 'azure':[bool_check('azure-cli','az',shutil.which('az')), bool_check('azure-what-if','reports/azure/what-if.json',azure_what_if.get('status') in {'passed','planned','skipped'}), bool_check('azure-report','azure status','azure' in control or azure_what_if.get('status') in {'passed','planned','skipped'})],
 'gui':[bool_check('dashboard-script','scripts/dashboard/generate-gui.py',Path('scripts/dashboard/generate-gui.py').exists()), bool_check('gui-graph','gui nodes',node_ok('gui'))],
 'security':[bool_check('secret-preflight','reports/security/secret-preflight.json',secret.get('ok')), bool_check('apply-gate-preflight','reports/security/apply-gate-preflight.json',apply_gate.get('ok'))],
 'ai-hermes-fleet':[bool_check('hermes-config','config/hermes-fleet.example.json',Path('config/hermes-fleet.example.json').exists()), bool_check('hermes-report','reports/hermes-fleet/latest.json',hermes.get('ok'))],
}
category_scores={name:category_score(items) for name,items in categories.items()}
score=round(100*sum(1 for c in base_checks if c['ok'])/len(base_checks))
finish_score=round(sum(category_scores.values())/len(category_scores))
blockers=[]
for name,items in categories.items():
    for item in items:
        if not item['ok']: blockers.append({'category':name,**item})
payload={'generatedUtc':datetime.now(timezone.utc).isoformat(),'score':score,'checks':base_checks,'notes':'Score reflects local readiness and repo wiring. Provider auth readiness is reported separately in cross-access profiles.'}
recommended=[]
if shutil.which('dotnet') is None: recommended.append({'area':'dotnet','command':'scripts/setup/bootstrap-local-tools.sh','reason':'dotnet not found'})
if shutil.which('az') is None: recommended.append({'area':'azure','command':'scripts/setup/bootstrap-local-tools.sh && az login','reason':'az not found or not authenticated'})
recommended.extend(build.get('nextFixes',[])[:10])
finish={'generatedUtc':payload['generatedUtc'],'score':finish_score,'categoryScores':category_scores,'categories':categories,'blockers':blockers,'recommendedFixes':recommended,'buildGraphCounts':build.get('counts',{}),'nextFixes':build.get('nextFixes',[])[:10]}
OUT.parent.mkdir(parents=True,exist_ok=True); OUT.write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n'); FINISH_JSON.write_text(json.dumps(finish,indent=2,sort_keys=True)+'\n')
lines=['# HELIOS Readiness Score','',f"Generated: `{payload['generatedUtc']}`",'',f"Score: **{score}%**",'','| Check | Target | Status |','| --- | --- | --- |']
for c in base_checks: lines.append(f"| {c['id']} | `{c['target']}` | {'✅' if c['ok'] else '⚠️'} |")
MD.write_text('\n'.join(lines)+'\n')
flines=['# HELIOS Finish Readiness','',f"Generated: `{finish['generatedUtc']}`",'',f"Score: **{finish_score}%**",'','| Category | Score |','| --- | ---: |']+[f"| {k} | {v}% |" for k,v in category_scores.items()]
if recommended:
    flines += ['', '## Recommended fixes', '', '| Area | Command | Reason |', '| --- | --- | --- |']+[f"| {r.get('area', r.get('node',''))} | `{r.get('command','')}` | {r.get('reason','')} |" for r in recommended]
if blockers:
    flines += ['','## Blockers','','| Category | Check | Target |','| --- | --- | --- |']+[f"| {b['category']} | {b['id']} | `{b['target']}` |" for b in blockers]
FINISH_MD.write_text('\n'.join(flines)+'\n')
print(f'Wrote {OUT.relative_to(ROOT)}'); print(f'Wrote {MD.relative_to(ROOT)}'); print(f'Wrote {FINISH_JSON.relative_to(ROOT)}'); print(f'Wrote {FINISH_MD.relative_to(ROOT)}')
