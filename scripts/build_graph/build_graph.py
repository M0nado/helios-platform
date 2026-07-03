#!/usr/bin/env python3
from __future__ import annotations
import argparse,json,subprocess
from datetime import datetime, timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; CFG=ROOT/'config/build-graph.json'; OUT=ROOT/'reports/build-graph/build-graph.json'; MD=ROOT/'reports/build-graph/build-graph.md'
def load(): return json.loads(CFG.read_text())['nodes']
def write(nodes,results=None):
    OUT.parent.mkdir(parents=True,exist_ok=True); payload={'generatedUtc':datetime.now(timezone.utc).isoformat(),'nodes':nodes,'results':results or []}; OUT.write_text(json.dumps(payload,indent=2)+'\n')
    lines=['# HELIOS Build Graph','','| Node | Title | Command |','| --- | --- | --- |']+[f"| `{n['id']}` | {n['title']} | `{n['command']}` |" for n in nodes]
    if results:
        lines+=['','## Results','','| Node | Exit | Tail |','| --- | --- | --- |']+[f"| `{r['id']}` | {r['exitCode']} | `{str(r['tail'])[:160]}` |" for r in results]
    MD.write_text('\n'.join(lines)+'\n')
def run_node(n):
    p=subprocess.run(n['command'],cwd=ROOT,text=True,capture_output=True,shell=True,timeout=180)
    return {'id':n['id'],'exitCode':p.returncode,'tail':(p.stdout+p.stderr).splitlines()[-10:]}
def main():
    ap=argparse.ArgumentParser(); ap.add_argument('command',nargs='?',default='list',choices=['list','run','graph']); ap.add_argument('--node'); ap.add_argument('--all',action='store_true'); a=ap.parse_args(); nodes=load(); results=[]
    if a.command=='run':
        selected=nodes if a.all else [n for n in nodes if n['id']==a.node]
        if not selected: raise SystemExit('No matching node; use --node <id> or --all')
        for n in selected: results.append(run_node(n))
    write(nodes,results); print(f'Wrote {OUT.relative_to(ROOT)}'); print(f'Wrote {MD.relative_to(ROOT)}')
if __name__=='__main__': main()
