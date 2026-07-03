#!/usr/bin/env python3
from __future__ import annotations
import argparse,json,datetime as dt
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'reports/learning'; LOG=OUT/'events.jsonl'
def main():
 ap=argparse.ArgumentParser(); ap.add_argument('--agent',default='unknown'); ap.add_argument('--task',default='unknown'); ap.add_argument('--model',default='unknown'); ap.add_argument('--success',action='store_true'); ap.add_argument('--cost',type=float,default=0.0); a=ap.parse_args(); OUT.mkdir(parents=True,exist_ok=True)
 LOG.open('a',encoding='utf-8').write(json.dumps({'utc':dt.datetime.now(dt.timezone.utc).isoformat(),**vars(a)})+'\n'); print(f"Wrote {LOG.relative_to(ROOT)}")
if __name__=='__main__': main()
