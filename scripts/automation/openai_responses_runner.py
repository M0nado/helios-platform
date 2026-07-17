#!/usr/bin/env python3
from __future__ import annotations
import argparse, json, os, datetime as dt
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'reports/openai'
def main():
 ap=argparse.ArgumentParser(); ap.add_argument('--task',default='summarize'); ap.add_argument('--report',action='append',default=[]); ap.add_argument('--apply',action='store_true'); a=ap.parse_args()
 payload={'generatedUtc':dt.datetime.now(dt.timezone.utc).isoformat(),'task':a.task,'reports':a.report,'apiKeyPresent':bool(os.getenv('OPENAI_API_KEY')),'liveCallExecuted':False,'note':'Dry-run scaffold; enable live calls only after provider policy is implemented.'}
 OUT.mkdir(parents=True,exist_ok=True); (OUT/'responses-run.json').write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n'); (OUT/'responses-run.md').write_text(f"# OpenAI Responses Runner\n\n- Task: {a.task}\n- API key present: {payload['apiKeyPresent']}\n- Live call executed: False\n")
 print(f"Wrote {(OUT/'responses-run.md').relative_to(ROOT)}")
if __name__=='__main__': main()
