#!/usr/bin/env python3
from __future__ import annotations
import json,shutil,subprocess,datetime as dt,time
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'reports/native'
def run(c):
 if not shutil.which(c[0]): return {'command':' '.join(c),'exitCode':None,'warning':f'{c[0]} unavailable'}
 p=subprocess.run(c,cwd=ROOT,text=True,capture_output=True); return {'command':' '.join(c),'exitCode':p.returncode,'stdout':p.stdout[-2000:],'stderr':p.stderr[-2000:]}
def overlap_python(left,right):
 normalized={p.replace('\\','/') for p in left}; return sum(1 for p in right if p.replace('\\','/') in normalized)
def main():
 start=time.perf_counter(); sample_left=[f'src/core/File{i}.cs' for i in range(5000)]+[f'src/native/File{i}.cpp' for i in range(5000)]; sample_right=[f'src/native/File{i}.cpp' for i in range(2500,7500)]+[f'src/analytics/File{i}.fs' for i in range(5000)]
 overlap=overlap_python(sample_left,sample_right); elapsed_ms=(time.perf_counter()-start)*1000
 res=[run(['cmake','-S','src/native/HELIOS.Native.Performance','-B','build/native-performance']),run(['cmake','--build','build/native-performance','--config','Release'])]
 OUT.mkdir(parents=True,exist_ok=True); payload={'generatedUtc':dt.datetime.now(dt.timezone.utc).isoformat(),'nativeHeader':'src/native/HELIOS.Native.Performance/include/helios/merge_analysis.hpp','sampleOverlap':overlap,'sampleElapsedMs':round(elapsed_ms,3),'baselineStatus':'native-cpp-header-ready-python-baseline','results':res}
 (OUT/'benchmark-results.json').write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n'); (OUT/'merge-analysis-benchmark.json').write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n')
 (OUT/'benchmark-results.md').write_text('# Native Benchmark Baseline\n\n'+f"- Sample overlap: {overlap}\n- Sample elapsed ms: {elapsed_ms:.3f}\n"+'\n'.join(f"- {r['command']}: {r.get('exitCode')}" for r in res)+'\n')
 print(f"Wrote {(OUT/'benchmark-results.md').relative_to(ROOT)}"); return 1 if any(r.get('exitCode') not in (0,None) for r in res) else 0
if __name__=='__main__': raise SystemExit(main())
