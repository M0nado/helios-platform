#!/usr/bin/env python3
from __future__ import annotations
import json, datetime as dt
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'reports/code-fix-center'
LANGS=[('csharp','C# Center','python3 scripts/automation/fix_csharp_compile.py'),('cpp','C++ Accelerator','python3 scripts/native/benchmark_native.py'),('fsharp','F# Analytics Oracle','python3 scripts/analytics/fsharp_test_report.py'),('python','Python AIHub Connector','python3 tools/aihub/smoke-test.py'),('azure','Azure Warden','python3 scripts/azure/bicep_report.py what-if')]
def main():
 plans=[{'language':l,'agent':a,'command':c,'mode':'copy-command','risk':'medium' if l in {'azure','cpp'} else 'low'} for l,a,c in LANGS]
 payload={'generatedUtc':dt.datetime.now(dt.timezone.utc).isoformat(),'plans':plans}
 OUT.mkdir(parents=True,exist_ok=True); (OUT/'code-fix-center.json').write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n'); (OUT/'code-fix-center.md').write_text('# Code Fix Center\n\n'+'\n'.join(f"- {p['agent']} [{p['language']}]: `{p['command']}`" for p in plans)+'\n')
 print(f"Wrote {(OUT/'code-fix-center.md').relative_to(ROOT)}")
if __name__=='__main__': main()
