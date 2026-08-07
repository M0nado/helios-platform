#!/usr/bin/env python3
from __future__ import annotations
import json, shutil, subprocess
from collections import defaultdict
from datetime import datetime, timezone
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'reports/learning/module-submodule-test-matrix.json'
MD = ROOT / 'reports/learning/module-submodule-test-matrix.md'
LANG = {'.cs':'csharp','.csproj':'csharp','.fs':'fsharp','.fsproj':'fsharp','.cpp':'cpp','.hpp':'cpp','.h':'cpp','.py':'python','.bicep':'bicep','.yml':'yaml','.yaml':'yaml'}
ROOTS = ['src/core/HELIOS.Platform','src/gui/MonadoBlade.GUI','src/native/HELIOS.Native.Performance','src/analytics/HELIOS.Analytics.FSharp','src/Security','scripts','config','infra/azure','.github/workflows','ai-integration']
TEST_ROOTS = ['tests','src/tests','tests/analytics','src/core/HELIOS.Platform/Tests']

def files() -> list[str]:
    if shutil.which('rg'):
        p = subprocess.run(['rg','--files'], cwd=ROOT, text=True, capture_output=True)
        if p.returncode in (0, 1):
            return [x for x in p.stdout.splitlines() if x]
    p = subprocess.run(['git', 'ls-files'], cwd=ROOT, text=True, capture_output=True)
    return [x for x in p.stdout.splitlines() if x] if p.returncode == 0 else []

def lang(path: str) -> str:
    low=path.lower()
    if low.startswith('src/native/') or low.endswith('cmakelists.txt'):
        return 'cpp'
    return LANG.get(Path(path).suffix.lower(), 'other')

def module_for(path: str) -> tuple[str,str]:
    for root in ROOTS:
        if path.startswith(root + '/') or path == root:
            rest = path[len(root):].strip('/')
            sub = rest.split('/',1)[0] if rest else '.'
            return root, sub
    top = path.split('/',1)[0]
    return top, '.'

def is_test(path: str) -> bool:
    low=path.lower()
    return any(path.startswith(r + '/') for r in TEST_ROOTS) or 'test' in low or 'tests' in low

def related_tests(module: str, submodule: str, all_files: list[str]) -> list[str]:
    needles = [Path(module).name.lower(), submodule.lower()]
    if submodule in {'.','src','scripts','config'}:
        needles = [Path(module).name.lower()]
    tests=[]
    for f in all_files:
        if not is_test(f):
            continue
        low=f.lower()
        if any(n and n != '.' and n in low for n in needles):
            tests.append(f)
    return tests[:30]

def hermes_xcore_flags(paths: list[str]) -> list[str]:
    flags=[]
    hay=' '.join(paths).lower()
    if any(x in hay for x in ['hermes','fleet','scripts/agents','agent']): flags.append('hermes/fleet/agent')
    if any(x in hay for x in ['xcore','src/native','cmakelists','performance']): flags.append('xcore/native/performance')
    if any(x in hay for x in ['azure','bicep','cloud','keyvault']): flags.append('super-cloud')
    if any(x in hay for x in ['dashboard','gui','status-site','monadoblade']): flags.append('agent/super-gui')
    return flags

def command_for(module: str, langs: dict, flags: list[str]) -> list[str]:
    cmds=[]
    if langs.get('csharp'): cmds.append('dotnet build HELIOS.Platform.slnx')
    if langs.get('fsharp'): cmds.append('dotnet test tests/analytics/HELIOS.Analytics.FSharp.Tests/HELIOS.Analytics.FSharp.Tests.fsproj')
    if langs.get('cpp') or 'xcore/native/performance' in flags: cmds.append('cmake -S src/native/HELIOS.Native.Performance -B .build/native')
    if langs.get('python'): cmds.append('python3 -m py_compile scripts/build_graph/build_graph.py scripts/agents/branch_fix_agents.py')
    if 'hermes/fleet/agent' in flags: cmds.append('python3 scripts/agents/hermes_fleet_readiness.py')
    if 'super-cloud' in flags: cmds.append('python3 scripts/azure/azure_connection_pipeline.py --stage all')
    if 'agent/super-gui' in flags: cmds.append('python3 scripts/dashboard/generate-gui.py')
    return list(dict.fromkeys(cmds))[:10]

def main() -> int:
    all_files=files()
    buckets=defaultdict(list)
    for f in all_files:
        buckets[module_for(f)].append(f)
    rows=[]
    for (module, submodule), paths in sorted(buckets.items()):
        counts=defaultdict(int)
        for p in paths: counts[lang(p)] += 1
        flags=hermes_xcore_flags(paths)
        tests=related_tests(module, submodule, all_files)
        rows.append({'module':module,'submodule':submodule,'fileCount':len(paths),'languageCounts':dict(sorted(counts.items())),'testCount':len(tests),'relatedTests':tests,'flags':flags,'recommendedCommands':command_for(module, counts, flags),'sampleFiles':paths[:25]})
    payload={'generatedUtc':datetime.now(timezone.utc).isoformat(),'ok':True,'moduleSubmoduleCount':len(rows),'principle':'C# is the secure AI-access orchestration core; C++, F#, and Python are partner engines for performance/XCore, analytics/search/math, and Linux/AIHub/Hermes/Fleet/library automation.','rows':rows}
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(json.dumps(payload, indent=2)+'\n')
    lines=['# Module / Submodule / Test Matrix','',f"Generated: `{payload['generatedUtc']}`",'',payload['principle'],'','| Module | Submodule | Files | Tests | Languages | Flags | Commands |','| --- | --- | ---: | ---: | --- | --- | --- |']
    for r in rows:
        langs=', '.join(f"{k}:{v}" for k,v in r['languageCounts'].items() if v)
        flags=', '.join(r['flags'])
        cmds='<br>'.join(f"`{c}`" for c in r['recommendedCommands'])
        lines.append(f"| `{r['module']}` | `{r['submodule']}` | {r['fileCount']} | {r['testCount']} | {langs} | {flags} | {cmds} |")
    for r in rows:
        if r['flags'] or r['testCount']:
            lines += ['', f"## {r['module']} / {r['submodule']}", '', 'Related tests:'] + [f"- `{t}`" for t in r['relatedTests']]
            lines += ['Sample files:'] + [f"- `{f}`" for f in r['sampleFiles']]
    MD.write_text('\n'.join(lines)+'\n')
    print(f"Wrote {OUT.relative_to(ROOT)}")
    print(f"Wrote {MD.relative_to(ROOT)}")
    return 0
if __name__=='__main__': raise SystemExit(main())
