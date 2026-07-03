#!/usr/bin/env python3
from __future__ import annotations
import argparse, json, datetime as dt, os
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'reports/audit'; LOG=OUT/'automation-events.jsonl'
def main():
 ap=argparse.ArgumentParser(); ap.add_argument('--command',default=''); ap.add_argument('--risk',default='low'); ap.add_argument('--outcome',default='recorded'); ap.add_argument('--policy-result',default='unknown'); ap.add_argument('--report',action='append',default=[]); a=ap.parse_args(); OUT.mkdir(parents=True,exist_ok=True)
 event={'schemaVersion':2,'utc':dt.datetime.now(dt.timezone.utc).isoformat(),'actor':os.getenv('GITHUB_ACTOR') or os.getenv('USER') or 'unknown','context':'github-actions' if os.getenv('GITHUB_ACTIONS') else 'local','command':a.command,'risk':a.risk,'policyResult':a.policy_result,'reports':a.report,'outcome':a.outcome}; LOG.open('a',encoding='utf-8').write(json.dumps(event,sort_keys=True)+'\n'); (OUT/'latest.md').write_text('# Latest Automation Audit\n\n'+json.dumps(event,indent=2)+'\n')
 print(f"Wrote {(OUT/'latest.md').relative_to(ROOT)}")
if __name__=='__main__': main()
