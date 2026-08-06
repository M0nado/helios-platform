#!/usr/bin/env python3
"""Render the detailed HELIOS language optimization matrix."""
from __future__ import annotations
import json, datetime as dt
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; CONFIG=ROOT/'config/helios-language-optimization-matrix.json'; OUT=ROOT/'reports/language-ownership'
def main():
 data=json.loads(CONFIG.read_text(encoding='utf-8')); languages=data.get('languages',{})
 payload={'generatedUtc':dt.datetime.now(dt.timezone.utc).isoformat(),**data}
 OUT.mkdir(parents=True,exist_ok=True); (OUT/'language-optimization-matrix.json').write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n')
 lines=['# HELIOS Language Optimization Matrix','',data.get('principle',''),'','| Language | Role | Best use count | Required checks |','| --- | --- | ---: | --- |']
 for name,info in languages.items():
  lines.append(f"| {name} | {info.get('role','')} | {len(info.get('bestFor',[]))} | {', '.join(info.get('requiredChecks',[]))} |")
 lines += ['','## Decision heuristics']+[f"- {item}" for item in data.get('decisionHeuristics',[])]
 for name,info in languages.items():
  lines += ['',f"## {name}", '', 'Best for:']+[f"- {item}" for item in info.get('bestFor',[])]
  if info.get('avoidFor'): lines += ['', 'Avoid for:']+[f"- {item}" for item in info.get('avoidFor',[])]
 (OUT/'language-optimization-matrix.md').write_text('\n'.join(lines)+'\n')
 print(f"Wrote {(OUT/'language-optimization-matrix.md').relative_to(ROOT)}")
if __name__=='__main__': main()
