#!/usr/bin/env python3
from __future__ import annotations
import json,datetime as dt
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'reports/analytics'
def main():
 projects=[p.relative_to(ROOT).as_posix() for p in ROOT.glob('tests/analytics/**/*.fsproj')]; payload={'generatedUtc':dt.datetime.now(dt.timezone.utc).isoformat(),'projects':projects,'categories':{'deterministic':projects,'analyticsRegression':[],'predictionModel':[],'longRunning':[],'quarantined':[]}}
 OUT.mkdir(parents=True,exist_ok=True); (OUT/'fsharp-tests.json').write_text(json.dumps(payload,indent=2)+'\n'); (OUT/'fsharp-tests.md').write_text('# F# Analytics Tests\n\n'+'\n'.join(f"- {p}" for p in projects)+'\n'); print(f"Wrote {(OUT/'fsharp-tests.md').relative_to(ROOT)}")
if __name__=='__main__': main()
