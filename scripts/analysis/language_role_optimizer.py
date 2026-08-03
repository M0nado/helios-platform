#!/usr/bin/env python3
from __future__ import annotations
import json, subprocess
from collections import Counter, defaultdict
from datetime import datetime, timezone
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
CONFIG = ROOT / 'config/language-role-strategy.json'
OUT = ROOT / 'reports/learning/language-role-optimizer.json'
MD = ROOT / 'reports/learning/language-role-optimizer.md'
EXT_LANG = {'.cs':'csharp','.csproj':'csharp','.slnx':'csharp','.cpp':'cpp','.c':'cpp','.hpp':'cpp','.h':'cpp','.fs':'fsharp','.fsproj':'fsharp','.py':'python'}
TEST_HINTS = ['test', 'tests', 'spec', 'validation']
MODULE_ROOTS = ['src/core', 'src/gui', 'src/native', 'src/analytics', 'scripts', 'ai-integration', 'infra/azure', '.github/workflows']

def rg_files() -> list[str]:
    p = subprocess.run(['rg', '--files'], cwd=ROOT, text=True, capture_output=True)
    return [line for line in p.stdout.splitlines() if line]

def strategy() -> dict:
    return json.loads(CONFIG.read_text())

def current_lang(path: str) -> str:
    low = path.lower()
    if low.startswith('src/native/') or low.endswith('cmakelists.txt'):
        return 'cpp'
    return EXT_LANG.get(Path(path).suffix.lower(), 'other')

def preferred(path: str, cfg: dict) -> tuple[str, str]:
    low = path.lower()
    best = ('python' if low.startswith(('scripts/', 'ai-integration/')) else current_lang(path), 'no rule matched')
    best_weight = -1
    for rule in cfg.get('replacementRules', []):
        hits = sum(1 for item in rule['ifPathContains'] if item in low)
        if hits > best_weight and hits:
            best = (rule['prefer'], rule['reason'])
            best_weight = hits
    return best

def module_for(path: str) -> str:
    for root in MODULE_ROOTS:
        if path.startswith(root + '/') or path == root:
            return root
    return path.split('/', 1)[0]

def test_related(path: str) -> bool:
    low = path.lower()
    return any(h in low for h in TEST_HINTS)

def main() -> int:
    cfg = strategy()
    files = rg_files()
    rows = []
    module_counts = defaultdict(Counter)
    migration = []
    csharp_migration = []
    test_gaps = defaultdict(lambda: {'source': 0, 'tests': 0})
    for path in files:
        lang = current_lang(path)
        if lang == 'other':
            continue
        pref, reason = preferred(path, cfg)
        module = module_for(path)
        module_counts[module][lang] += 1
        if test_related(path):
            test_gaps[module]['tests'] += 1
        else:
            test_gaps[module]['source'] += 1
        row = {'path': path, 'module': module, 'current': lang, 'preferred': pref, 'reason': reason, 'isTest': test_related(path)}
        rows.append(row)
        if lang == 'python' and pref in {'cpp', 'fsharp', 'csharp'}:
            migration.append(row)
        if lang == 'csharp' and pref in {'cpp', 'fsharp', 'python'}:
            csharp_migration.append(row)
    modules = []
    for module, counts in sorted(module_counts.items()):
        total = sum(counts.values())
        tests = test_gaps[module]['tests']
        modules.append({'module': module, 'counts': dict(counts), 'sourceCount': test_gaps[module]['source'], 'testCount': tests, 'testRatio': round(tests / total, 3) if total else 0})
    payload = {
        'generatedUtc': datetime.now(timezone.utc).isoformat(),
        'roles': cfg['roles'],
        'moduleCount': len(modules),
        'modules': modules,
        'languageCounts': dict(Counter(r['current'] for r in rows)),
        'migrationCandidateCount': len(migration) + len(csharp_migration),
        'pythonReplacementCandidateCount': len(migration),
        'csharpReplacementCandidateCount': len(csharp_migration),
        'pythonReplacementCandidates': migration[:80],
        'csharpReplacementCandidates': csharp_migration[:120],
        'principle': 'C# is the reliable secure orchestrator/framework; C++ is preferred for performance/memory/rendering/XCore; F# is preferred for analytics/search/math/prediction; Python remains for Linux tooling, AIHub/library glue, reports, and cross-LLM adapters.'
    }
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(json.dumps(payload, indent=2) + '\n')
    lines = ['# Language Role Optimizer', '', f"Generated: `{payload['generatedUtc']}`", '', payload['principle'], '', '## Roles', '']
    for lang, role in payload['roles'].items():
        lines += [f"### {lang}", '', f"Role: {role['role']}", '', 'Use for:'] + [f"- {x}" for x in role['useFor']] + ['', 'Avoid for:'] + [f"- {x}" for x in role['avoidFor']] + ['']
    lines += ['## Module language/test ranking', '', '| Module | C# | C++ | F# | Python | Source | Tests | Test ratio |', '| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |']
    for m in modules:
        c = m['counts']
        lines.append(f"| `{m['module']}` | {c.get('csharp',0)} | {c.get('cpp',0)} | {c.get('fsharp',0)} | {c.get('python',0)} | {m['sourceCount']} | {m['testCount']} | {m['testRatio']} |")
    lines += ['', '## C# extraction/replacement candidates', '', 'These are C# files whose path signals suggest C++, F#, or Python may be a better implementation partner while C# stays the framework/orchestrator.', '', '| Current file | Preferred | Reason |', '| --- | --- | --- |']
    for r in csharp_migration[:120]:
        lines.append(f"| `{r['path']}` | {r['preferred']} | {r['reason']} |")
    lines += ['', '## Python replacement candidates', '', 'These are Python files whose path signals suggest C#, C++, or F# may be the better long-term implementation language. Keep Python when it is only report/setup/provider glue.', '', '| Current file | Preferred | Reason |', '| --- | --- | --- |']
    for r in migration[:80]:
        lines.append(f"| `{r['path']}` | {r['preferred']} | {r['reason']} |")
    MD.write_text('\n'.join(lines) + '\n')
    print(f"Wrote {OUT.relative_to(ROOT)}")
    print(f"Wrote {MD.relative_to(ROOT)}")
    return 0
if __name__ == '__main__':
    raise SystemExit(main())
