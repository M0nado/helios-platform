#!/usr/bin/env python3
from __future__ import annotations
import argparse,fnmatch,json,subprocess
from datetime import datetime, timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; CFG=ROOT/'config/build-graph.json'; OUT=ROOT/'reports/build-graph/build-graph.json'; MD=ROOT/'reports/build-graph/build-graph.md'
READINESS_NODE={'id':'full-stack-readiness','title':'Full-stack readiness','command':'python3 scripts/integrations/readiness_score.py','paths':['scripts/integrations/**','config/**','reports/**','src/**','tests/**']}
def load():
    nodes=json.loads(CFG.read_text())['nodes']
    if not any(n['id']==READINESS_NODE['id'] for n in nodes): nodes.append(READINESS_NODE)
    return nodes
def write(nodes,results=None):
    OUT.parent.mkdir(parents=True,exist_ok=True); payload={'generatedUtc':datetime.now(timezone.utc).isoformat(),'nodes':nodes,'results':results or []}; OUT.write_text(json.dumps(payload,indent=2)+'\n')
    lines=['# HELIOS Build Graph','','| Node | Title | Command |','| --- | --- | --- |']+[f"| `{n['id']}` | {n['title']} | `{n['command']}` |" for n in nodes]
    if results:
        lines+=['','## Results','','| Node | Exit | Tail |','| --- | --- | --- |']+[f"| `{r['id']}` | {r['exitCode']} | `{str(r['tail'])[:160]}` |" for r in results]
    MD.write_text('\n'.join(lines)+'\n')
def run_node(n):
    p=subprocess.run(n['command'],cwd=ROOT,text=True,capture_output=True,shell=True,timeout=180)
    return {'id':n['id'],'exitCode':p.returncode,'tail':(p.stdout+p.stderr).splitlines()[-10:]}
def changed_paths():
    proc=subprocess.run(['git','status','--short'],cwd=ROOT,text=True,capture_output=True,check=True)
    paths=[]
    for line in proc.stdout.splitlines():
        if not line: continue
        path=line[3:]
        if ' -> ' in path: path=path.split(' -> ',1)[1]
        paths.append(path)
    return paths
def matches(node,paths):
    patterns=node.get('paths') or []
    return any(fnmatch.fnmatch(path,pat) for path in paths for pat in patterns)
def select_nodes(nodes,args):
    if args.all: selected=list(nodes)
    elif args.node: selected=[n for n in nodes if n['id']==args.node]
    elif args.changed_only:
        paths=changed_paths(); selected=[n for n in nodes if matches(n,paths)]
    else: selected=[]
    if args.include_readiness and not any(n['id']==READINESS_NODE['id'] for n in selected):
        readiness=next((n for n in nodes if n['id']==READINESS_NODE['id']),READINESS_NODE)
        selected.append(readiness)
    return selected
def main():
    ap=argparse.ArgumentParser(); ap.add_argument('command',nargs='?',default='list',choices=['list','run','graph']); ap.add_argument('--node'); ap.add_argument('--all',action='store_true'); ap.add_argument('--changed-only',action='store_true'); ap.add_argument('--include-readiness',action='store_true'); ap.add_argument('--dry-run',action='store_true'); a=ap.parse_args(); nodes=load(); results=[]
    if a.command=='run':
        selected=select_nodes(nodes,a)
        if not selected: raise SystemExit('No matching node; use --node <id>, --all, --changed-only, or --include-readiness')
        for n in selected:
            if a.dry_run: results.append({'id':n['id'],'exitCode':0,'tail':[f"dry-run: {n['command']}"]})
            else: results.append(run_node(n))
    write(nodes,results); print(f'Wrote {OUT.relative_to(ROOT)}'); print(f'Wrote {MD.relative_to(ROOT)}')
if __name__=='__main__': main()
