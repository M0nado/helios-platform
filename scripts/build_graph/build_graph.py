#!/usr/bin/env python3
from __future__ import annotations
import argparse,json,subprocess
from datetime import datetime, timezone
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
CFG=ROOT/'config/build-graph.json'
OUT=ROOT/'reports/build-graph/latest.json'
MD=ROOT/'reports/build-graph/latest.md'

BOOTSTRAP='scripts/setup/bootstrap-local-tools.sh'

def load():
    return json.loads(CFG.read_text())['nodes']

def classify_result(result):
    if result['exitCode'] != 0:
        return 'failed'
    command=result.get('command','').lower()
    tail='\n'.join(result.get('tail',[])).lower()
    if 'dry-run' in command or 'dry run' in tail or 'dry-run' in tail:
        return 'skipped/dry-run'
    return 'passed'

def reason_from_text(text):
    lower=text.lower()
    if 'dotnet' in lower and ('not found' in lower or 'no such file' in lower):
        return 'dotnet not found'
    if 'az' in lower and ('not found' in lower or 'login' in lower or 'not logged' in lower or 'please run' in lower):
        return 'Azure CLI login required' if 'login' in lower or 'not logged' in lower else 'az not found'
    if 'cmake' in lower and ('not found' in lower or 'could not' in lower or 'error' in lower):
        return 'CMake configure/build failed'
    if 'not found' in lower:
        return 'required tool not found'
    if 'permission denied' in lower:
        return 'permission denied'
    return 'command failed'

def suggested_command(node, result):
    command=result.get('command') or node.get('command','')
    text=(command+'\n'+'\n'.join(result.get('tail',[]))).lower()
    if 'dotnet' in text and ('not found' in text or 'no such file' in text):
        return BOOTSTRAP
    if command.strip().startswith('az ') or ('az' in text and ('login' in text or 'not logged' in text)):
        return 'az login'
    if 'cmake' in command.lower() or 'cmake' in text:
        return 'cmake -S ... -B ...'
    if 'not found' in text:
        return BOOTSTRAP
    return command

def next_fixes(nodes, results):
    node_by_id={n['id']:n for n in nodes}
    fixes=[]
    for result in results:
        if result.get('status') != 'failed':
            continue
        node=node_by_id.get(result['id'],{})
        text=(result.get('command') or node.get('command',''))+'\n'+'\n'.join(result.get('tail',[]))
        fixes.append({'node':result['id'],'command':suggested_command(node,result),'reason':reason_from_text(text)})
    return fixes

def ordered_results(results):
    rank={'failed':0,'passed':1,'skipped/dry-run':2}
    return sorted(results,key=lambda r:(rank.get(r.get('status'),99),r.get('id','')))

def write(nodes,results=None):
    results=results or []
    for result in results:
        result['status']=classify_result(result)
    ordered=ordered_results(results)
    fixes=next_fixes(nodes,ordered)
    OUT.parent.mkdir(parents=True,exist_ok=True)
    payload={'generatedUtc':datetime.now(timezone.utc).isoformat(),'nodes':nodes,'results':ordered,'nextFixes':fixes}
    OUT.write_text(json.dumps(payload,indent=2)+'\n')
    lines=['# HELIOS Build Graph','','| Node | Title | Command |','| --- | --- | --- |']+[f"| `{n['id']}` | {n['title']} | `{n['command']}` |" for n in nodes]
    if ordered:
        lines+=['','## Results','','| Status | Node | Exit | Tail |','| --- | --- | --- | --- |']
        lines += [f"| {r['status']} | `{r['id']}` | {r['exitCode']} | `{str(r['tail'])[:160]}` |" for r in ordered]
    if fixes:
        lines+=['','## Next fixes','','| Node | Command | Reason |','| --- | --- | --- |']
        lines += [f"| `{f['node']}` | `{f['command']}` | {f['reason']} |" for f in fixes]
    MD.write_text('\n'.join(lines)+'\n')
    return ordered, fixes

def run_node(n):
    p=subprocess.run(n['command'],cwd=ROOT,text=True,capture_output=True,shell=True,timeout=180)
    return {'id':n['id'],'command':n['command'],'exitCode':p.returncode,'tail':(p.stdout+p.stderr).splitlines()[-10:]}

def print_summary(results, fixes):
    counts={'failed':0,'passed':0,'skipped/dry-run':0}
    for result in results:
        counts[result['status']]=counts.get(result['status'],0)+1
    print(f"Summary: {counts.get('failed',0)} failed, {counts.get('passed',0)} passed, {counts.get('skipped/dry-run',0)} skipped/dry-run")
    if fixes:
        print('Next fixes:')
        for fix in fixes[:5]:
            print(f"- {fix['node']}: {fix['command']} ({fix['reason']})")

def main():
    ap=argparse.ArgumentParser(); ap.add_argument('command',nargs='?',default='list',choices=['list','run','graph']); ap.add_argument('--node'); ap.add_argument('--all',action='store_true'); a=ap.parse_args(); nodes=load(); results=[]
    if a.command=='run':
        selected=nodes if a.all else [n for n in nodes if n['id']==a.node]
        if not selected: raise SystemExit('No matching node; use --node <id> or --all')
        for n in selected: results.append(run_node(n))
    ordered, fixes=write(nodes,results); print(f'Wrote {OUT.relative_to(ROOT)}'); print(f'Wrote {MD.relative_to(ROOT)}'); print_summary(ordered, fixes)
if __name__=='__main__': main()
