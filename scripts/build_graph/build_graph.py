#!/usr/bin/env python3
from __future__ import annotations
import argparse,fnmatch,json,os,subprocess,time
from concurrent.futures import FIRST_COMPLETED, ThreadPoolExecutor, wait
from datetime import datetime, timezone
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
CFG=ROOT/'config/build-graph.json'
OUT=ROOT/'reports/build-graph/latest.json'
MD=ROOT/'reports/build-graph/latest.md'
BOOTSTRAP='scripts/setup/bootstrap-local-tools.sh'
STATUS_RANK={'failed':0,'passed':1,'skipped/dry-run':2,'skipped/dependency':3,'skipped/no-change':4}
PROFILES={
    'quick': {'python','control','dashboard','security','hermes'},
    'full': None,
    'ci': {'python','dotnet','fsharp','native','azure','control','dashboard','security','analysis','ai','hermes'},
    'azure': {'azure','setup'},
    'ai': {'ai','hermes','setup'},
    'native': {'native','setup'},
    'dotnet': {'dotnet','fsharp','setup'},
    'gui': {'gui','dashboard','dotnet','setup'},
    'security': {'security'},
}

def load(): return json.loads(CFG.read_text())['nodes']

def node_tags(node): return set(node.get('tags',[]))

def classify_result(result):
    if result.get('status','').startswith('skipped/'):
        return result['status']
    if result['exitCode'] != 0:
        return 'failed'
    command=result.get('command','').lower(); tail='\n'.join(result.get('tail',[])).lower()
    if 'dry-run' in command or 'dry run' in tail or 'dry-run' in tail:
        return 'skipped/dry-run'
    return 'passed'

def reason_from_text(text):
    lower=text.lower()
    if 'dotnet' in lower and ('not found' in lower or 'no such file' in lower): return 'dotnet not found'
    if 'az' in lower and ('not found' in lower or 'login' in lower or 'not logged' in lower or 'please run' in lower): return 'Azure CLI login required' if 'login' in lower or 'not logged' in lower else 'az not found'
    if 'cmake' in lower and ('not found' in lower or 'could not' in lower or 'error' in lower): return 'CMake configure/build failed'
    if 'dependency' in lower: return 'dependency did not pass'
    if 'not found' in lower: return 'required tool not found'
    if 'permission denied' in lower: return 'permission denied'
    return 'command failed'

def suggested_command(node, result):
    command=result.get('command') or node.get('command','')
    text=(command+'\n'+'\n'.join(result.get('tail',[]))).lower()
    if 'dotnet' in text and ('not found' in text or 'no such file' in text): return BOOTSTRAP
    if command.strip().startswith('az ') or ('az' in text and ('login' in text or 'not logged' in text)): return 'az login'
    if 'cmake' in command.lower() or 'cmake' in text: return 'cmake -S src/native/HELIOS.Native.Performance -B .build/native'
    if 'not found' in text: return BOOTSTRAP
    return command

def next_fixes(nodes, results):
    node_by_id={n['id']:n for n in nodes}; fixes=[]
    for result in results:
        if result.get('status') != 'failed': continue
        node=node_by_id.get(result['id'],{})
        text=(result.get('command') or node.get('command',''))+'\n'+'\n'.join(result.get('tail',[]))
        fixes.append({'node':result['id'],'command':suggested_command(node,result),'reason':reason_from_text(text)})
    return fixes

def ordered_results(results): return sorted(results,key=lambda r:(STATUS_RANK.get(r.get('status'),99),r.get('id','')))

def changed_files(base):
    candidates=[['git','diff','--name-only',f'{base}...HEAD'],['git','diff','--name-only',base],['git','diff','--name-only','HEAD']]
    for cmd in candidates:
        p=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True)
        if p.returncode==0:
            return [line.strip() for line in p.stdout.splitlines() if line.strip()]
    return []

def match_details(node, paths):
    globs=node.get('paths') or ['**/*']
    matches=[]
    for path in paths:
        for pattern in globs:
            if fnmatch.fnmatch(path, pattern) or fnmatch.fnmatch('/'+path, pattern):
                matches.append({'path':path,'pattern':pattern})
    return matches

def path_matches(node, paths):
    return bool(match_details(node, paths))


def expand_dependencies(selected, all_nodes):
    node_by_id={n['id']:n for n in all_nodes}
    ordered=[]; seen=set()
    def visit(node):
        if node['id'] in seen: return
        for dep in node.get('dependsOn',[]):
            if dep in node_by_id: visit(node_by_id[dep])
        seen.add(node['id']); ordered.append(node)
    for node in selected: visit(node)
    return ordered

def select_nodes(nodes, args):
    selected=nodes if args.all else ([n for n in nodes if n['id']==args.node] if args.node else nodes)
    if args.profile:
        profile_tags=PROFILES.get(args.profile)
        if profile_tags is None:
            selected=nodes
        else:
            selected=[n for n in selected if node_tags(n)&profile_tags or n.get('critical')]
    if args.tag:
        requested=set(args.tag)
        selected=[n for n in selected if node_tags(n)&requested or (args.include_critical and n.get('critical'))]
    changed=[]; selection_reasons={}
    if args.changed_only:
        changed=changed_files(args.base)
        selected_ids=set()
        for n in selected:
            matches=match_details(n, changed)
            if matches:
                selected_ids.add(n['id']); selection_reasons[n['id']]={'reason':'changed-path','matches':matches[:10]}
            elif args.include_critical and n.get('critical'):
                selected_ids.add(n['id']); selection_reasons[n['id']]={'reason':'critical','matches':[]}
            else:
                selection_reasons[n['id']]={'reason':'skipped/no-change','matches':[]}
        selected=[n for n in selected if n['id'] in selected_ids]
    before_deps={n['id'] for n in selected}
    selected=expand_dependencies(selected,nodes)
    for n in selected:
        selection_reasons.setdefault(n['id'], {'reason':'dependency' if n['id'] not in before_deps else 'selected','matches':[]})
    return selected, changed, selection_reasons

def skip_result(node, status, reason):
    return {'id':node['id'],'command':node.get('command',''),'exitCode':0,'tail':[reason],'status':status,'durationSeconds':0.0,'tags':node.get('tags',[])}

def run_node(n):
    start=time.monotonic(); timeout=int(n.get('timeoutSeconds',180))
    try:
        p=subprocess.run(n['command'],cwd=ROOT,text=True,capture_output=True,shell=True,timeout=timeout)
        result={'id':n['id'],'command':n['command'],'exitCode':p.returncode,'tail':(p.stdout+p.stderr).splitlines()[-10:],'durationSeconds':round(time.monotonic()-start,3),'tags':n.get('tags',[])}
    except subprocess.TimeoutExpired as exc:
        output=((exc.stdout or '')+(exc.stderr or '')) if isinstance(exc.stdout,str) or isinstance(exc.stderr,str) else ''
        result={'id':n['id'],'command':n['command'],'exitCode':124,'tail':(output.splitlines()+[f'timed out after {timeout}s'])[-10:],'durationSeconds':round(time.monotonic()-start,3),'tags':n.get('tags',[])}
    result['status']=classify_result(result)
    return result

def run_nodes(nodes, max_workers=4):
    node_by_id={n['id']:n for n in nodes}; remaining={n['id'] for n in nodes}; results={}; running={}
    with ThreadPoolExecutor(max_workers=max(1,max_workers)) as executor:
        while remaining or running:
            launched=False
            for node_id in list(remaining):
                node=node_by_id[node_id]; deps=node.get('dependsOn',[])
                failed_deps=[dep for dep in deps if dep in results and results[dep]['status']!='passed']
                missing_selected=[dep for dep in deps if dep not in node_by_id]
                if failed_deps or missing_selected:
                    reason='blocked by dependency: '+', '.join(failed_deps+missing_selected)
                    results[node_id]=skip_result(node,'skipped/dependency',reason); remaining.remove(node_id); launched=True; continue
                if all(dep in results for dep in deps):
                    running[executor.submit(run_node,node)]=node_id; remaining.remove(node_id); launched=True
            if running:
                done,_=wait(running.keys(),return_when=FIRST_COMPLETED)
                for fut in done:
                    node_id=running.pop(fut); results[node_id]=fut.result()
            elif remaining and not launched:
                # Cycle or dependency outside selected set: skip deterministically.
                for node_id in list(remaining):
                    results[node_id]=skip_result(node_by_id[node_id],'skipped/dependency','dependency cycle or missing selected dependency')
                    remaining.remove(node_id)
    return [results[n['id']] for n in nodes if n['id'] in results]

def write(nodes,results=None,metadata=None):
    results=results or []
    for result in results: result['status']=classify_result(result)
    ordered=ordered_results(results); fixes=next_fixes(nodes,ordered); metadata=metadata or {}
    OUT.parent.mkdir(parents=True,exist_ok=True)
    counts={status:sum(1 for r in ordered if r['status']==status) for status in STATUS_RANK}
    payload={'generatedUtc':datetime.now(timezone.utc).isoformat(),'metadata':metadata,'nodes':nodes,'results':ordered,'counts':counts,'nextFixes':fixes}
    OUT.write_text(json.dumps(payload,indent=2)+'\n')
    lines=['# HELIOS Build Graph','',f"Generated: `{payload['generatedUtc']}`",'', '## Summary','']
    lines += [f"- **{status}**: {count}" for status,count in counts.items() if count]
    if metadata.get('changedFiles') is not None:
        lines += ['', '## Changed files', ''] + [f"- `{p}`" for p in metadata.get('changedFiles',[])[:100]]
        reasons=metadata.get('selectionReasons',{})
        if reasons:
            lines += ['', '## Selection reasons', '', '| Node | Reason | Match |', '| --- | --- | --- |']
            for node_id, detail in sorted(reasons.items()):
                match=', '.join(f"{m.get('path')} ⇢ {m.get('pattern')}" for m in detail.get('matches',[])[:3])
                lines.append(f"| `{node_id}` | {detail.get('reason')} | `{match}` |")
    lines += ['', '## Nodes','','| Node | Title | Tags | Command |','| --- | --- | --- | --- |']+[f"| `{n['id']}` | {n['title']} | `{','.join(n.get('tags',[]))}` | `{n['command']}` |" for n in nodes]
    if ordered:
        lines+=['','## Results','','| Status | Node | Exit | Seconds | Tail |','| --- | --- | --- | --- | --- |']
        lines += [f"| {r['status']} | `{r['id']}` | {r['exitCode']} | {r.get('durationSeconds',0)} | `{str(r['tail'])[:160]}` |" for r in ordered]
    if fixes:
        lines+=['','## Next fixes','','| Node | Command | Reason |','| --- | --- | --- |']
        lines += [f"| `{f['node']}` | `{f['command']}` | {f['reason']} |" for f in fixes]
    MD.write_text('\n'.join(lines)+'\n')
    return ordered, fixes

def print_summary(results, fixes):
    counts={}
    for result in results: counts[result['status']]=counts.get(result['status'],0)+1
    print('Summary: '+', '.join(f'{counts.get(status,0)} {status}' for status in STATUS_RANK))
    if fixes:
        print('Next fixes:')
        for fix in fixes[:5]: print(f"- {fix['node']}: {fix['command']} ({fix['reason']})")

def main():
    ap=argparse.ArgumentParser(description='Run and report HELIOS build graph nodes.')
    ap.add_argument('command',nargs='?',default='list',choices=['list','run','graph'])
    ap.add_argument('--node'); ap.add_argument('--all',action='store_true'); ap.add_argument('--profile',choices=sorted(PROFILES)); ap.add_argument('--tag',action='append')
    ap.add_argument('--parallel',action='store_true',default=True); ap.add_argument('--max-workers',type=int,default=int(os.environ.get('HELIOS_BUILD_GRAPH_WORKERS','4')))
    ap.add_argument('--changed-only',action='store_true'); ap.add_argument('--base',default='origin/main'); ap.add_argument('--include-critical',action='store_true',default=True)
    args=ap.parse_args(); nodes=load(); selected, changed, selection_reasons=select_nodes(nodes,args); results=[]
    if args.command=='run':
        if not selected: raise SystemExit('No matching node; use --node <id>, --all, --profile, or --tag')
        results=run_nodes(selected,args.max_workers)
    metadata={'selectedNodes':[n['id'] for n in selected], 'changedFiles':changed if args.changed_only else None, 'selectionReasons':selection_reasons, 'profile':args.profile, 'tags':args.tag or []}
    ordered, fixes=write(selected if args.command=='run' else nodes,results,metadata); print(f'Wrote {OUT.relative_to(ROOT)}'); print(f'Wrote {MD.relative_to(ROOT)}'); print_summary(ordered, fixes)
if __name__=='__main__': main()
