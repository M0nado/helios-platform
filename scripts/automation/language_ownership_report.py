#!/usr/bin/env python3
"""Render the HELIOS language ownership map that keeps Python as glue."""
from __future__ import annotations
import json, datetime as dt
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; CONFIG=ROOT/'config/helios-language-ownership.json'; OUT=ROOT/'reports/language-ownership'
def main():
 data=json.loads(CONFIG.read_text(encoding='utf-8'))
 languages=data.get('languages',{})
 payload={'generatedUtc':dt.datetime.now(dt.timezone.utc).isoformat(),'decisionOrder':data.get('decisionOrder',[]),'languages':languages,'pythonBoundary':'Python is limited to AIHub/provider glue, reports, and compatibility wrappers; durable UI/core/math/native logic belongs to C#/F#/C++.'}
 OUT.mkdir(parents=True,exist_ok=True); (OUT/'language-ownership.json').write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n')
 lines=['# HELIOS Language Ownership','','| Language | Role | Owner Agent | Merge Weight | Required Checks |','| --- | --- | --- | ---: | --- |']
 for name,info in languages.items():
  lines.append(f"| {name} | {info['role']} | {info['ownerAgent']} | {info['mergeWeight']} | {', '.join(info.get('requiredChecks',[]))} |")
 (OUT/'language-ownership.md').write_text('\n'.join(lines)+'\n')
 print(f"Wrote {(OUT/'language-ownership.md').relative_to(ROOT)}")
if __name__=='__main__': main()
