#!/usr/bin/env python3
from __future__ import annotations
import json,shutil
from datetime import datetime, timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'reports/apply/finish-tasks.json'
MD=ROOT/'reports/apply/finish-tasks.md'

def load(path, default):
    p=ROOT/path
    if not p.exists(): return default
    try: return json.loads(p.read_text())
    except json.JSONDecodeError: return default

def task(title, area, priority, commands, detail):
    return {'title':title,'area':area,'priority':priority,'commands':commands,'detail':detail}

def main():
    finish=load(Path('reports/integrations/finish-readiness.json'), {})
    graph=load(Path('reports/build-graph/latest.json'), {})
    branches=load(Path('reports/branch-intelligence/merge-prune-recommendations.json'), {'recommendations':[]})
    tasks=[]
    tasks.append(task('Run safe finish apply sequence','automation','P0',['python3 scripts/apply/finish_readiness_apply.py --apply'],'Refresh bootstrap tools, safety preflights, readiness reports, dashboard, and quick build graph.'))
    if shutil.which('dotnet') is None:
        tasks.append(task('Install local .NET SDK','dotnet','P0',['scripts/setup/bootstrap-local-tools.sh'],'Required for C#/.NET, F# analytics, MAUI/WinUI readiness, and dotnet build/test lanes.'))
    if shutil.which('az') is None:
        tasks.append(task('Install Azure CLI locally','azure','P0',['scripts/setup/bootstrap-local-tools.sh'],'Required before Azure login, Bicep checks, and what-if planning.'))
    tasks.append(task('Authenticate Azure CLI','azure','P0',['az login','export HELIOS_AZURE_RESOURCE_GROUP=helios-dev-rg','python3 scripts/azure/azure_what_if.py'],'Needed for live Azure what-if; deployment remains outside automatic apply.'))
    tasks.append(task('Run full build graph profile','ci','P0',['python3 scripts/build_graph/build_graph.py run --profile full --max-workers 4'],'Exercises Python, .NET/F#, native/CMake, Azure, security, AI/Hermes, dashboard, and analysis lanes.'))
    tasks.append(task('Review branch merge/prune recommendations','branches','P1',['python3 scripts/analysis/merge_prune_recommendations.py'],'Review merge-candidate/manual-review branches before any merge; no branch is auto-merged.'))
    tasks.append(task('Harden security preflight strict mode','security','P1',['python3 scripts/security/secret_preflight.py --strict','python3 scripts/security/apply_gate_preflight.py --strict'],'Run strict preflight after allowlist cleanup to block unsafe changes in CI.'))
    tasks.append(task('Configure Hermes/Fleet local production settings','hermes-fleet','P1',['cp config/hermes-fleet.example.json config/hermes-fleet.local.json','python3 scripts/agents/hermes_fleet_readiness.py --config config/hermes-fleet.local.json'],'Local config is ignored by git; keep mode dry-run until production review.'))
    tasks.append(task('Run native CMake performance lane','native-cpp','P1',['python3 scripts/build_graph/build_graph.py run --tag native --max-workers 2'],'Validates XCore/native configure and build readiness.'))
    tasks.append(task('Run .NET/F# analytics lane','analytics','P1',['python3 scripts/build_graph/build_graph.py run --tag dotnet --max-workers 2'],'Validates .NET workloads and F# analytics tests when SDK is available.'))
    for fix in finish.get('recommendedFixes',[]):
        tasks.append(task(f"Apply recommended fix for {fix.get('area', fix.get('node','unknown'))}",fix.get('area','readiness'),'P0',[fix.get('command','')],fix.get('reason','')))
    for fix in graph.get('nextFixes',[]):
        tasks.append(task(f"Fix build graph node {fix.get('node')}",'build-graph','P0',[fix.get('command','')],fix.get('reason','')))
    for rec in branches.get('recommendations',[])[:20]:
        if rec.get('recommendation') in {'merge-candidate','manual-review-priority'}:
            tasks.append(task(f"Review branch {rec.get('branch')}",'branches','P2',[f"git log --oneline --decorate -20 {rec.get('branch')}",f"git diff --stat HEAD...{rec.get('branch')}"] ,f"Domains: {', '.join(rec.get('domains',[]))}; risk: {rec.get('conflictRisk')}"))
    # Deduplicate by title/commands.
    seen=set(); unique=[]
    for item in tasks:
        key=(item['title'],tuple(item['commands']))
        if key in seen: continue
        seen.add(key); unique.append(item)
    payload={'generatedUtc':datetime.now(timezone.utc).isoformat(),'count':len(unique),'tasks':unique}
    OUT.parent.mkdir(parents=True,exist_ok=True); OUT.write_text(json.dumps(payload,indent=2)+'\n')
    lines=['# HELIOS Finish Tasks','',f"Generated: `{payload['generatedUtc']}`",'',f"Tasks: **{payload['count']}**",'', '| Priority | Area | Task | Commands |','| --- | --- | --- | --- |']
    for item in unique:
        cmds='<br>'.join(f"`{cmd}`" for cmd in item['commands'] if cmd)
        lines.append(f"| {item['priority']} | {item['area']} | {item['title']}<br>{item['detail']} | {cmds} |")
    MD.write_text('\n'.join(lines)+'\n')
    print(f"Wrote {OUT.relative_to(ROOT)}")
    print(f"Wrote {MD.relative_to(ROOT)}")
if __name__=='__main__': main()
