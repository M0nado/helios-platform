#!/usr/bin/env python3
from __future__ import annotations

import hashlib
import json
import re
from collections import Counter, defaultdict
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'reports/learning/complex-code-grading.json'
COMPACT = ROOT / 'reports/learning/complex-code-grading.compact.json'
MD = ROOT / 'reports/learning/complex-code-grading.md'
SELF_NOTES = ROOT / 'reports/learning/aihub-self-learning-notes.json'
SKIP_DIRS = {'.git', '.tools', '.build', 'bin', 'obj', 'node_modules', 'reports', 'status-site', '__pycache__'}
EXTENSIONS = {'.cs', '.fs', '.fsx', '.cpp', '.cc', '.cxx', '.hpp', '.h', '.py', '.yml', '.yaml', '.json', '.md', '.bicep', '.ps1', '.sh'}
METRICS = [
    'lineCount', 'codeLineCount', 'commentLineCount', 'blankLineCount', 'fileBytes',
    'functionTokenCount', 'classTokenCount', 'importTokenCount', 'todoTokenCount', 'errorTokenCount',
    'securityTokenCount', 'performanceTokenCount', 'aiTokenCount', 'branchTokenCount', 'configTokenCount',
    'azureTokenCount', 'githubTokenCount', 'nativeTokenCount', 'fsharpTokenCount', 'csharpTokenCount',
    'pythonTokenCount', 'yamlTokenCount', 'jsonTokenCount', 'markdownTokenCount', 'uniqueTokenRatio',
    'duplicateLineRatio', 'selfNoteHitCount', 'thoughtTierHitCount', 'smallFileVariableHitCount',
    'connectionIdeaCount', 'compositePlanPartCount', 'situationVariableHitCount', 'complexityScore', 'maintainabilityScore', 'reuseScore', 'keepScore',
]
TOKEN_GROUPS = {
    'functionTokenCount': r'\b(function|def|let|member|static|public|private|constexpr|void|async)\b',
    'classTokenCount': r'\b(class|record|struct|interface|module|namespace|type)\b',
    'importTokenCount': r'\b(using|import|from|open|#include|uses:)\b',
    'todoTokenCount': r'\b(TODO|FIXME|HACK|XXX)\b',
    'errorTokenCount': r'\b(error|exception|fail|panic|throw|catch|warning)\b',
    'securityTokenCount': r'\b(secret|token|vault|auth|permission|policy|encrypt|credential|security)\b',
    'performanceTokenCount': r'\b(perf|performance|cache|memory|latency|parallel|vector|gpu|cpu|native|hot[-_ ]?path)\b',
    'aiTokenCount': r'\b(aihub|agent|hermes|xcore|llm|model|prompt|learning|score|vector)\b',
    'branchTokenCount': r'\b(branch|commit|merge|prune|absorb|autofix|pull request|ref)\b',
    'configTokenCount': r'\b(config|settings|flags|ledger|schema|manifest|json|yaml)\b',
    'azureTokenCount': r'\b(azure|bicep|arm|key vault|cosmos|fabric|foundry|purview|entra)\b',
    'githubTokenCount': r'\b(github|workflow|actions|runner|codespaces|pages|gh\s)\b',
    'nativeTokenCount': r'\b(c\+\+|native|cmake|constexpr|span|vector|pointer|memory)\b',
    'fsharpTokenCount': r'\b(f#|fsharp|\.fs|let|module|async|seq|score)\b',
    'csharpTokenCount': r'\b(c#|csharp|\.cs|namespace|record|interface|IReadOnlyList)\b',
    'pythonTokenCount': r'\b(python|\.py|def |argparse|subprocess|json\.dumps)\b',
    'yamlTokenCount': r'\b(yaml|yml|workflow_dispatch|jobs:|steps:)\b',
    'jsonTokenCount': r'\b(json|schema|manifest|\{\s*\")\b',
    'markdownTokenCount': r'\b(markdown|\.md|##|###|wiki|docs)\b',
}


def iter_files() -> list[Path]:
    out: list[Path] = []
    for path in ROOT.rglob('*'):
        if not path.is_file() or any(part in SKIP_DIRS for part in path.parts):
            continue
        if path.suffix.lower() in EXTENSIONS:
            out.append(path)
    return sorted(out)


def module_name(path: Path) -> str:
    rel = path.relative_to(ROOT)
    parts = rel.parts
    if len(parts) >= 3 and parts[0] == 'src':
        return '/'.join(parts[:3])
    if len(parts) >= 2 and parts[0] in {'scripts', 'config', '.github'}:
        return '/'.join(parts[:2])
    return parts[0]


def normalized_lines(text: str) -> list[str]:
    return [re.sub(r'\s+', ' ', line.strip()).lower() for line in text.splitlines() if line.strip()]


def clamp(value: float) -> float:
    return max(0.0, min(1.0, value))


def token_count(pattern: str, text: str) -> int:
    return len(re.findall(pattern, text, flags=re.IGNORECASE))


def load_self_notes() -> dict[str, object]:
    if not SELF_NOTES.exists():
        return {'noteText': '', 'smallVariables': [], 'connections': [], 'compositeParts': set()}
    data = json.loads(SELF_NOTES.read_text())
    note_text = ' '.join([n.get('note', '') + ' ' + n.get('title', '') for n in data.get('notes', [])])
    thought_text = ' '.join(' '.join(v) for v in data.get('thoughtTiers', {}).values())
    composite_parts = {part for plan in data.get('compositeFilePlans', []) for part in plan.get('parts', [])}
    return {
        'noteText': note_text + ' ' + thought_text,
        'smallVariables': data.get('smallFileVariables', []),
        'situationVariables': data.get('situationVariables', []),
        'connections': data.get('connectionIdeas', []),
        'compositeParts': composite_parts,
    }


def grade_file(path: Path, duplicate_counts: Counter[str], self_notes: dict[str, object]) -> dict[str, object]:
    text = path.read_text(errors='ignore')
    lines = text.splitlines()
    nonblank = [line for line in lines if line.strip()]
    comments = [line for line in nonblank if line.lstrip().startswith(('#', '//', '/*', '*', '<!--'))]
    normalized = normalized_lines(text)
    tokens = re.findall(r'[A-Za-z_][A-Za-z0-9_]{2,}', text.lower())
    unique_tokens = len(set(tokens))
    duplicate_lines = sum(1 for line in normalized if duplicate_counts[line] > 1)
    rel = str(path.relative_to(ROOT))
    note_text = str(self_notes.get('noteText', '')).lower()
    small_variables = list(self_notes.get('smallVariables', []))
    connections = list(self_notes.get('connections', []))
    situation_variables = list(self_notes.get('situationVariables', []))
    composite_parts = set(self_notes.get('compositeParts', set()))
    metrics: dict[str, float] = {
        'lineCount': len(lines),
        'codeLineCount': max(0, len(nonblank) - len(comments)),
        'commentLineCount': len(comments),
        'blankLineCount': len(lines) - len(nonblank),
        'fileBytes': len(text.encode()),
        'uniqueTokenRatio': round(unique_tokens / max(1, len(tokens)), 4),
        'duplicateLineRatio': round(duplicate_lines / max(1, len(normalized)), 4),
        'selfNoteHitCount': 1 if rel.lower() in note_text or path.name.lower() in note_text else 0,
        'thoughtTierHitCount': sum(1 for token in module_name(path).lower().replace('/', ' ').split() if token and token in note_text),
        'smallFileVariableHitCount': sum(1 for variable in small_variables if variable.replace('-', '') in text.lower().replace('-', '').replace('_', '')),
        'connectionIdeaCount': sum(1 for idea in connections if idea.get('from') == rel or idea.get('to') == rel),
        'compositePlanPartCount': 1 if rel in composite_parts else 0,
        'situationVariableHitCount': sum(1 for variable in situation_variables if any(piece in (text + ' ' + rel).lower() for piece in variable.split('-'))),
    }
    for name, pattern in TOKEN_GROUPS.items():
        metrics[name] = token_count(pattern, text)
    complexity = clamp((metrics['functionTokenCount'] / 28.0) + (metrics['classTokenCount'] / 18.0) + (metrics['lineCount'] / 900.0) + (metrics['errorTokenCount'] / 18.0))
    maintainability = clamp(1.0 - (metrics['duplicateLineRatio'] * 0.45) - (complexity * 0.25) + (metrics['commentLineCount'] / max(1.0, metrics['lineCount']) * 0.18))
    note_boost = clamp((metrics['selfNoteHitCount'] * 0.08) + (metrics['thoughtTierHitCount'] * 0.025) + (metrics['connectionIdeaCount'] * 0.05) + (metrics['compositePlanPartCount'] * 0.06) + (metrics['situationVariableHitCount'] * 0.01))
    reuse = clamp((metrics['aiTokenCount'] * 0.018) + (metrics['performanceTokenCount'] * 0.016) + (metrics['securityTokenCount'] * 0.018) + (metrics['branchTokenCount'] * 0.014) + metrics['uniqueTokenRatio'] * 0.36 + note_boost)
    keep = clamp(reuse * 0.42 + maintainability * 0.30 + (1.0 - metrics['duplicateLineRatio']) * 0.18 + (1.0 - complexity) * 0.10 + note_boost * 0.20)
    metrics['complexityScore'] = round(complexity, 4)
    metrics['maintainabilityScore'] = round(maintainability, 4)
    metrics['reuseScore'] = round(reuse, 4)
    metrics['keepScore'] = round(keep, 4)
    action = 'keep-unique-good-part' if keep >= 0.74 else 'absorb-small-idea-through-fsharp-score' if reuse >= 0.48 else 'prune-or-rewrite-trash-after-csharp-review' if metrics['duplicateLineRatio'] >= 0.42 or maintainability < 0.38 else 'review-with-fsharp-and-csharp'
    security_signal = clamp(metrics['securityTokenCount'] / 12.0)
    performance_signal = clamp(metrics['performanceTokenCount'] / 12.0 + metrics['nativeTokenCount'] / 18.0)
    learning_signal = clamp(metrics['aiTokenCount'] / 16.0 + metrics['fsharpTokenCount'] / 20.0)
    provider_glue_signal = clamp(metrics['pythonTokenCount'] / 18.0)
    engine_route = ['C# orchestration', 'F# complex scoring']
    if performance_signal >= 0.50 or complexity >= 0.70:
        engine_route.append('C++ native comparison')
    if provider_glue_signal >= 0.55:
        engine_route.append('Python provider/Linux glue')
    signals = []
    for key in ('aiTokenCount', 'performanceTokenCount', 'securityTokenCount', 'branchTokenCount', 'azureTokenCount', 'githubTokenCount'):
        if metrics[key] > 0:
            signals.append(f'{key}={int(metrics[key])}')
    return {
        'path': str(path.relative_to(ROOT)),
        'module': module_name(path),
        'extension': path.suffix.lower(),
        'action': action,
        'metrics': {name: metrics.get(name, 0) for name in METRICS},
        'engineRoute': engine_route,
        'engineSignals': {'security': round(security_signal,4), 'performance': round(performance_signal,4), 'learning': round(learning_signal,4), 'providerGlue': round(provider_glue_signal,4)},
        'signals': signals[:8],
        'contentHash': hashlib.sha256(text.encode(errors='ignore')).hexdigest()[:16],
    }


def main() -> int:
    files = iter_files()
    self_notes = load_self_notes()
    line_counts: Counter[str] = Counter()
    for path in files:
        line_counts.update(normalized_lines(path.read_text(errors='ignore')))
    grades = [grade_file(path, line_counts, self_notes) for path in files]
    modules: dict[str, dict[str, object]] = {}
    grouped: dict[str, list[dict[str, object]]] = defaultdict(list)
    for grade in grades:
        grouped[str(grade['module'])].append(grade)
    for name, items in grouped.items():
        avg = lambda metric: round(sum(float(i['metrics'][metric]) for i in items) / max(1, len(items)), 4)
        modules[name] = {
            'fileCount': len(items),
            'lineCount': sum(int(i['metrics']['lineCount']) for i in items),
            'averageKeepScore': avg('keepScore'),
            'averageReuseScore': avg('reuseScore'),
            'averageDuplicateLineRatio': avg('duplicateLineRatio'),
            'recommendedKeeps': [i['path'] for i in sorted(items, key=lambda x: x['metrics']['keepScore'], reverse=True)[:5]],
            'recommendedPrunes': [i['path'] for i in sorted(items, key=lambda x: (x['metrics']['duplicateLineRatio'], -x['metrics']['keepScore']), reverse=True)[:5]],
        }
    top_keep = sorted(grades, key=lambda x: x['metrics']['keepScore'], reverse=True)[:30]
    top_prune = sorted(grades, key=lambda x: (x['metrics']['duplicateLineRatio'], -x['metrics']['keepScore']), reverse=True)[:30]
    payload = {
        'generatedUtc': datetime.now(timezone.utc).isoformat(),
        'purpose': 'Complex self-learning-aware module/submodule/line-of-code grading for keeping unique good parts, absorbing small ideas, and pruning redundant trash without deleting anything automatically. C# orchestrates, F# owns most scoring, C++ assists heavy comparisons, and Python is only provider/Linux glue when useful.',
        'metricCount': len(METRICS),
        'metrics': METRICS,
        'fileCount': len(grades),
        'modules': modules,
        'topKeep': top_keep,
        'topPrune': top_prune,
        'allGrades': grades,
    }
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(json.dumps(payload, indent=2) + '\n')
    COMPACT.write_text(json.dumps({'generatedUtc': payload['generatedUtc'], 'metrics': METRICS, 'topKeep': top_keep[:10], 'topPrune': top_prune[:10], 'modules': modules}, indent=2) + '\n')
    lines = ['# Complex Code Grading', '', payload['purpose'], '', f"Metrics: `{len(METRICS)}`", f"Files graded: `{len(grades)}`", '', '## self-learning-aware grading variables']
    lines += [f'{index + 1}. `{metric}`' for index, metric in enumerate(METRICS)]
    lines += ['', '## Module comparison', '', '| Module | Files | Lines | Keep | Reuse | Duplicate lines |', '| --- | ---: | ---: | ---: | ---: | ---: |']
    for name, module in sorted(modules.items(), key=lambda kv: kv[1]['averageKeepScore'], reverse=True):
        lines.append(f"| `{name}` | {module['fileCount']} | {module['lineCount']} | {module['averageKeepScore']} | {module['averageReuseScore']} | {module['averageDuplicateLineRatio']} |")
    lines += ['', '## Keep unique/good parts']
    lines += [f"- `{g['path']}` keep `{g['metrics']['keepScore']}` reuse `{g['metrics']['reuseScore']}` duplicate `{g['metrics']['duplicateLineRatio']}` route `{ ' -> '.join(g.get('engineRoute', [])) }` — {', '.join(g['signals'])}" for g in top_keep[:20]]
    lines += ['', '## Prune or rewrite redundant/trash candidates']
    lines += [f"- `{g['path']}` action `{g['action']}` keep `{g['metrics']['keepScore']}` duplicate `{g['metrics']['duplicateLineRatio']}` complexity `{g['metrics']['complexityScore']}` route `{ ' -> '.join(g.get('engineRoute', [])) }`" for g in top_prune[:20]]
    MD.write_text('\n'.join(lines) + '\n')
    print(f'Wrote {OUT.relative_to(ROOT)}')
    print(f'Wrote {COMPACT.relative_to(ROOT)}')
    print(f'Wrote {MD.relative_to(ROOT)}')
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
