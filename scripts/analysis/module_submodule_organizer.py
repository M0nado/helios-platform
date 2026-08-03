#!/usr/bin/env python3
from __future__ import annotations

import argparse
import hashlib
import json
from collections import defaultdict
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'reports/learning/module-submodule-organizer.json'
MD = ROOT / 'reports/learning/module-submodule-organizer.md'
GRADING = ROOT / 'reports/learning/complex-code-grading.compact.json'
SKIP = {'.git', '.tools', '.build', 'bin', 'obj', 'reports', 'status-site', '__pycache__'}
EXTS = {'.cs', '.fs', '.fsx', '.cpp', '.hpp', '.h', '.py', '.yml', '.yaml', '.json', '.md', '.bicep', '.sh', '.ps1', '.css', '.js', '.html'}

MODULE_RULES = [
    ('gui-command-center', 'remote-console-ui', 'C# + CSS/JS', ['gui', 'remote-console', 'wwwroot', 'winui', 'dashboard', 'css', 'html', 'javascript'], lambda p: 'wwwroot/remote-console' in p or 'src/gui' in p or 'WinUI' in p),
    ('core-orchestration', 'contracts-and-routing', 'C#', ['core', 'contract', 'routing', 'security', 'orchestration', 'platform'], lambda p: p.startswith('src/core/') or p.endswith('.csproj')),
    ('aihub-learning-engine', 'fsharp-scoring-analytics', 'F#', ['analytics', 'learning', 'score', 'prediction', 'fsharp', 'fsproj'], lambda p: p.startswith('src/analytics/') or p.endswith(('.fs', '.fsx', '.fsproj'))),
    ('native-performance-security', 'cpp-hot-paths', 'C++', ['native', 'performance', 'memory', 'render', 'checksum', 'cpp', 'hpp', 'security'], lambda p: p.startswith('src/native/') or p.endswith(('.cpp', '.hpp', '.h'))),
    ('python-aihub-agent-glue', 'agents-and-analysis', 'Python', ['agent', 'aihub', 'analysis', 'integration', 'llm', 'linux', 'python'], lambda p: p.startswith('scripts/agents/') or p.startswith('scripts/analysis/') or p.startswith('scripts/integrations/') or p.endswith('.py')),
    ('github-yaml-automation', 'workflows-runners-codespaces', 'YAML', ['github', 'workflow', 'runner', 'branch', 'codespace', 'yaml'], lambda p: p.startswith('.github/workflows/') or p.endswith(('.yml', '.yaml'))),
    ('azure-bicep-cloud', 'azure-vault-whatif', 'Bicep/ARM', ['azure', 'vault', 'bicep', 'cloud', 'what-if', 'identity'], lambda p: p.startswith('scripts/azure/') or p.endswith('.bicep') or 'azure' in p.lower()),
    ('config-ledgers', 'json-policy-and-learning-ledgers', 'JSON', ['config', 'policy', 'ledger', 'json', 'profile', 'rules'], lambda p: p.startswith('config/') or p.endswith('.json')),
    ('docs-knowledge', 'markdown-wiki-knowledge', 'Markdown', ['docs', 'markdown', 'wiki', 'knowledge', 'readme'], lambda p: p.endswith('.md') or p.startswith('docs/')),
    ('setup-ops', 'local-runner-bootstrap', 'Shell/PowerShell', ['setup', 'bootstrap', 'shell', 'runner', 'finish', 'local'], lambda p: p.startswith('scripts/setup/') or p.endswith(('.sh', '.ps1'))),
    ('tests-validation', 'automated-checks', 'Tests', ['test', 'validation', 'unittest', 'pytest', 'checks'], lambda p: p.startswith('tests/')),
]

GRADE_VARIABLES = [
    'lineCount', 'keepScore', 'reuseScore', 'complexityScore', 'duplicateLineRatio', 'moduleFitScore', 'submoduleFitScore',
    'engineFitScore', 'agentFitScore', 'pathClarityScore', 'securitySignalScore', 'performanceSignalScore', 'guiSignalScore',
    'learningSignalScore', 'automationSignalScore', 'cloudSignalScore', 'testSignalScore', 'docsSignalScore', 'configSignalScore',
    'mergeIssueScore', 'todoDebtScore', 'placeholderRiskScore', 'generatedArtifactRiskScore', 'largeFileRiskScore', 'smallUtilityScore',
    'crossLanguageBridgeScore', 'csharpBackboneScore', 'fsharpAnalyticsScore', 'cppHotPathScore', 'pythonGlueScore', 'yamlPipelineScore',
    'azureVaultScore', 'knowledgeLedgerScore', 'branchAbsorptionScore', 'pruneSafetyScore', 'deleteReadinessScore', 'fixReadinessScore',
    'selfOptimizationScore', 'copyRoundPriorityScore', 'learningCarryOverScore', 'moduleCompletenessScore', 'submoduleCompletenessScore',
    'dependencyClarityScore', 'namingConsistencyScore', 'ownershipClarityScore', 'runtimeCriticalityScore', 'userFacingScore',
    'dataFlowScore', 'observabilityScore', 'overallPlacementScore'
]

AGENTS = [
    {'id': 'csharp-guardian', 'engine': 'C#', 'skills': ['contracts', 'GUI', 'security', 'routing']},
    {'id': 'fsharp-oracle', 'engine': 'F#', 'skills': ['grading', 'prediction', 'analytics', 'learning']},
    {'id': 'cpp-sentinel', 'engine': 'C++', 'skills': ['performance', 'memory', 'rendering', 'native security']},
    {'id': 'python-bridge', 'engine': 'Python', 'skills': ['AIHub glue', 'LLM agents', 'Linux', 'report generation']},
    {'id': 'yaml-recreator', 'engine': 'YAML', 'skills': ['GitHub Actions', 'branch recreation', 'CI buttons']},
    {'id': 'azure-vaultsmith', 'engine': 'Bicep/CLI', 'skills': ['Key Vault', 'hybrid cloud', 'what-if']},
    {'id': 'hermes-xcore-pruner', 'engine': 'Hermes/XCore', 'skills': ['branch tests', 'absorption', 'prune advice']},
]

EXPECTED_SCAFFOLDS = [
    {'module': 'gui-command-center', 'submodule': 'winui3-shell', 'suggestedPath': 'src/gui/HELIOS.Gui.CommandCenter/App.xaml.cs', 'engine': 'C#', 'why': 'front-facing WinUI 3 command center shell'},
    {'module': 'gui-command-center', 'submodule': 'native-render-bridge', 'suggestedPath': 'src/native/HELIOS.Native.Performance/include/helios/gui_render_bridge.hpp', 'engine': 'C++', 'why': 'optional low-cost render/performance bridge for GUI hot paths'},
    {'module': 'usbwizard', 'submodule': 'device-profile-orchestration', 'suggestedPath': 'src/core/HELIOS.Platform.Contracts/UsbWizardProfileContracts.cs', 'engine': 'C#', 'why': 'secure object model for USBWizard profile, partition, and policy decisions'},
    {'module': 'usbwizard', 'submodule': 'partition-performance-native', 'suggestedPath': 'src/native/HELIOS.Native.Performance/include/helios/usbwizard_partition_engine.hpp', 'engine': 'C++', 'why': 'native checksum, partition, memory, and performance planning helpers'},
    {'module': 'aihub-learning-engine', 'submodule': 'placement-prediction', 'suggestedPath': 'src/analytics/HELIOS.Analytics.FSharp/AIHub/ModulePlacementPredictor.fs', 'engine': 'F#', 'why': 'deep scoring model for module placement, split, merge, and optimization prediction'},
    {'module': 'python-aihub-agent-glue', 'submodule': 'external-agent-bridge', 'suggestedPath': 'scripts/integrations/aihub_external_agent_bridge.py', 'engine': 'Python', 'why': 'LLM/Linux/provider bridge only where Python gives leverage'},
    {'module': 'github-yaml-automation', 'submodule': 'issue-scaffold-review', 'suggestedPath': '.github/workflows/module-scaffold-review.yml', 'engine': 'YAML', 'why': 'report-only issue/scaffold review lane'},
    {'module': 'azure-bicep-cloud', 'submodule': 'vault-storage-fabric', 'suggestedPath': 'infra/aihub-vault-storage.bicep', 'engine': 'Bicep/ARM', 'why': 'Azure Key Vault/storage/fabric plan for hybrid control'},
]


def load_grading() -> dict[str, object]:
    if not GRADING.exists():
        return {}
    data = json.loads(GRADING.read_text())
    out = {}
    for bucket in ('topKeep', 'topPrune'):
        for item in data.get(bucket, []):
            out[item.get('path', '')] = item
    return out


def iter_files() -> list[Path]:
    files = []
    for path in ROOT.rglob('*'):
        if not path.is_file() or any(part in SKIP for part in path.parts):
            continue
        if path.suffix.lower() in EXTS:
            files.append(path)
    return sorted(files)


def file_is_empty(path: Path) -> bool:
    try:
        return path.stat().st_size == 0 or not path.read_text(errors='ignore').strip()
    except OSError:
        return False


def project_setup(files: list[Path]) -> dict[str, object]:
    rels = [str(p.relative_to(ROOT)) for p in files]
    roots = sorted({r.split('/')[0] for r in rels})
    signals = {
        'hasDotnet': any(r.endswith(('.csproj', '.fsproj', '.sln')) for r in rels),
        'hasNativeCpp': any(r.startswith('src/native/') or r.endswith(('.cpp', '.hpp', '.h')) for r in rels),
        'hasFSharpAnalytics': any(r.startswith('src/analytics/') or r.endswith(('.fs', '.fsproj')) for r in rels),
        'hasPythonAutomation': any(r.startswith('scripts/') and r.endswith('.py') for r in rels),
        'hasGitHubWorkflows': any(r.startswith('.github/workflows/') for r in rels),
        'hasRemoteConsoleGui': any('wwwroot/remote-console' in r for r in rels),
        'hasAzurePlan': any('azure' in r.lower() or r.endswith('.bicep') for r in rels),
    }
    return {'roots': roots, 'signals': signals, 'projectStyle': 'hybrid C# orchestration with F#/C++ engines and Python/YAML/Azure automation'}


def missing_scaffolds(existing: set[str]) -> list[dict[str, object]]:
    missing = []
    for scaffold in EXPECTED_SCAFFOLDS:
        if scaffold['suggestedPath'] not in existing:
            missing.append({**scaffold, 'operation': 'suggest-empty-scaffold-only', 'mutatesByDefault': False})
    return missing


def score_rule(rel: str, rule: tuple) -> int:
    module, submodule, _engine, keywords, predicate = rule
    lower = rel.lower()
    score = 100 if predicate(rel) else 0
    score += sum(8 for keyword in keywords if keyword in lower)
    score += 6 if module.split('-')[0] in lower else 0
    score += 4 if submodule.split('-')[0] in lower else 0
    return score


def classify(rel: str) -> tuple[str, str, str, str, list[dict[str, object]]]:
    scores = sorted(
        ({'module': r[0], 'submodule': r[1], 'engine': r[2], 'score': score_rule(rel, r)} for r in MODULE_RULES),
        key=lambda item: item['score'],
        reverse=True,
    )
    best = scores[0]
    if best['score'] > 0:
        return best['module'], best['submodule'], best['engine'], f"smart score {best['score']} selected {best['module']}/{best['submodule']}", scores[:4]
    return 'project-foundation', 'uncategorized-review', 'C# orchestration review', 'fallback review bucket for files needing explicit placement', scores[:4]


def stable_choice(key: str, candidates: list[dict[str, object]]) -> dict[str, object]:
    digest = int(hashlib.sha256(key.encode()).hexdigest()[:8], 16)
    return candidates[digest % len(candidates)]


def agent_for(engine: str, rel: str) -> dict[str, object]:
    direct = [a for a in AGENTS if a['engine'] in engine or engine in a['engine']]
    return stable_choice(rel, direct or AGENTS)



def read_text_sample(path: Path) -> str:
    try:
        return path.read_text(errors='ignore')[:12000]
    except OSError:
        return ''


def keyword_score(text: str, keywords: list[str], weight: int = 10) -> int:
    lower = text.lower()
    return min(100, sum(weight for keyword in keywords if keyword in lower))


def file_awareness(rel: str, path: Path, module: str, submodule: str, engine: str, metrics: dict[str, object], alternatives: list[dict[str, object]]) -> dict[str, object]:
    text = read_text_sample(path)
    suffix = path.suffix.lower()
    keep = float(metrics.get('keepScore', 0) or 0)
    duplicate = float(metrics.get('duplicateLineRatio', 0) or 0)
    lines = int(metrics.get('lineCount', 0) or 0)
    merge_issue = keyword_score(text, ['<<<<<<<', '=======', '>>>>>>>', 'merge conflict', 'conflict marker'], 35)
    todo_debt = keyword_score(text, ['todo', 'fixme', 'hack', 'not implemented', 'throw new notimplementedexception'], 12)
    placeholder = keyword_score(text, ['placeholder', 'stub', 'sample only', 'temporary', 'dummy'], 14)
    generated_risk = 85 if any(part in rel.lower() for part in ['generated', '.designer.', 'obj/', 'bin/']) else 0
    module_fit = min(100, int(alternatives[0].get('score', 0)) if alternatives else 0)
    submodule_fit = min(100, module_fit + (10 if submodule in rel.lower() else 0))
    engine_keywords = {
        'C#': ['class ', 'record ', 'namespace ', 'async ', 'security', 'controller'],
        'F#': ['module ', 'let ', 'type ', 'score', 'predict', 'analytics'],
        'C++': ['inline ', '#include', 'std::', 'constexpr', 'memory', 'performance'],
        'Python': ['def ', 'import ', 'json', 'argparse', 'agent', 'llm'],
        'YAML': ['workflow', 'jobs:', 'steps:', 'uses:', 'run:'],
    }
    engine_fit = keyword_score(text + ' ' + rel, engine_keywords.get(engine.split()[0], []), 12)
    variable_scores = {
        'lineCount': lines,
        'keepScore': keep,
        'reuseScore': float(metrics.get('reuseScore', 0) or 0),
        'complexityScore': float(metrics.get('complexityScore', 0) or 0),
        'duplicateLineRatio': duplicate,
        'moduleFitScore': module_fit,
        'submoduleFitScore': submodule_fit,
        'engineFitScore': engine_fit,
        'agentFitScore': min(100, module_fit + engine_fit // 4),
        'pathClarityScore': min(100, 20 + rel.count('/') * 8),
        'securitySignalScore': keyword_score(text + rel, ['security', 'secret', 'vault', 'token', 'credential', 'auth', 'policy']),
        'performanceSignalScore': keyword_score(text + rel, ['performance', 'memory', 'cpu', 'gpu', 'render', 'native', 'parallel']),
        'guiSignalScore': keyword_score(text + rel, ['gui', 'dashboard', 'button', 'html', 'css', 'winui', 'remote-console']),
        'learningSignalScore': keyword_score(text + rel, ['learning', 'score', 'analytics', 'predict', 'grade', 'rank']),
        'automationSignalScore': keyword_score(text + rel, ['workflow', 'automation', 'runner', 'build_graph', 'setup', 'finish']),
        'cloudSignalScore': keyword_score(text + rel, ['azure', 'cloud', 'vault', 'bicep', 'what-if', 'hybrid']),
        'testSignalScore': keyword_score(text + rel, ['test', 'unittest', 'pytest', 'assert', 'validation']),
        'docsSignalScore': keyword_score(text + rel, ['readme', 'docs', 'markdown', 'guide', '# ']),
        'configSignalScore': keyword_score(text + rel, ['config', 'json', 'settings', 'policy', 'profile']),
        'mergeIssueScore': merge_issue,
        'todoDebtScore': todo_debt,
        'placeholderRiskScore': placeholder,
        'generatedArtifactRiskScore': generated_risk,
        'largeFileRiskScore': 90 if lines > 1200 else 45 if lines > 600 else 0,
        'smallUtilityScore': 80 if 0 < lines <= 120 else 35 if lines <= 300 else 10,
        'crossLanguageBridgeScore': keyword_score(text + rel, ['interop', 'bridge', 'contract', 'native', 'python', 'fsharp', 'cpp', 'csharp']),
        'csharpBackboneScore': 90 if suffix == '.cs' or module == 'core-orchestration' else 0,
        'fsharpAnalyticsScore': 90 if suffix in {'.fs', '.fsx'} or module == 'aihub-learning-engine' else 0,
        'cppHotPathScore': 90 if suffix in {'.cpp', '.hpp', '.h'} or module == 'native-performance-security' else 0,
        'pythonGlueScore': 90 if suffix == '.py' or module == 'python-aihub-agent-glue' else 0,
        'yamlPipelineScore': 90 if suffix in {'.yml', '.yaml'} else 0,
        'azureVaultScore': keyword_score(text + rel, ['azure', 'vault', 'bicep', 'az ']),
        'knowledgeLedgerScore': keyword_score(text + rel, ['knowledge', 'ledger', 'report', 'markdown', 'json']),
        'branchAbsorptionScore': keyword_score(text + rel, ['branch', 'absorption', 'merge', 'prune', 'commit']),
        'pruneSafetyScore': max(0, 100 - int(duplicate * 120) - merge_issue),
        'deleteReadinessScore': min(100, int(duplicate * 100) + generated_risk + placeholder // 2) if keep < 45 else 0,
        'fixReadinessScore': min(100, merge_issue + todo_debt + placeholder),
        'selfOptimizationScore': min(100, module_fit // 2 + engine_fit // 2 + int(keep) // 3),
        'copyRoundPriorityScore': min(100, int(keep) + module_fit // 3 - int(duplicate * 30)),
        'learningCarryOverScore': min(100, int(keep) // 2 + keyword_score(text + rel, ['score', 'learn', 'report', 'feedback'], 8)),
        'moduleCompletenessScore': min(100, module_fit + (15 if lines > 0 else 0)),
        'submoduleCompletenessScore': submodule_fit,
        'dependencyClarityScore': keyword_score(text, ['using ', 'import ', '#include', 'open ', 'references', 'depends'], 10),
        'namingConsistencyScore': 85 if '-' in module and '-' in submodule else 55,
        'ownershipClarityScore': min(100, module_fit + engine_fit // 3),
        'runtimeCriticalityScore': keyword_score(text + rel, ['main', 'server', 'control', 'security', 'performance', 'runtime']),
        'userFacingScore': keyword_score(text + rel, ['gui', 'button', 'dashboard', 'html', 'css', 'console', 'winui']),
        'dataFlowScore': keyword_score(text + rel, ['json', 'report', 'payload', 'contract', 'model', 'schema']),
        'observabilityScore': keyword_score(text + rel, ['log', 'report', 'status', 'summary', 'dashboard', 'metrics']),
    }
    variable_scores['overallPlacementScore'] = round(sum(float(variable_scores.get(v, 0) or 0) for v in GRADE_VARIABLES if v != 'overallPlacementScore') / 49, 2)
    return {'scores': variable_scores, 'mergeIssue': merge_issue > 0, 'todoDebt': todo_debt > 0, 'placeholderRisk': placeholder > 0}

def action_for(metrics: dict[str, object], rel: str) -> str:
    keep = float(metrics.get('keepScore', 0) or 0)
    duplicate = float(metrics.get('duplicateLineRatio', 0) or 0)
    lines = int(metrics.get('lineCount', 0) or 0)
    if duplicate >= 0.45 and keep < 55:
        return 'mark-prune-review'
    if keep >= 80:
        return 'keep-and-promote'
    if lines > 900:
        return 'split-into-submodule-review'
    if rel.endswith(('.py', '.fs', '.cs', '.hpp', '.cpp')) and keep >= 55:
        return 'absorb-and-optimize'
    return 'place-and-learn'


def proposed_tree_path(module: str, submodule: str, rel: str) -> str:
    safe_rel = rel.replace('..', '').lstrip('/')
    return f'modules/{module}/{submodule}/{safe_rel}'


def build_payload(mode: str) -> dict[str, object]:
    grades = load_grading()
    all_files = iter_files()
    setup = project_setup(all_files)
    existing = {str(p.relative_to(ROOT)) for p in all_files}
    placements = []
    tree: dict[str, dict[str, list[str]]] = defaultdict(lambda: defaultdict(list))
    for path in all_files:
        rel = str(path.relative_to(ROOT))
        module, submodule, engine, reason, alternatives = classify(rel)
        grade = grades.get(rel, {})
        metrics = grade.get('metrics', {})
        awareness = file_awareness(rel, path, module, submodule, engine, metrics, alternatives)
        agent = agent_for(engine, rel)
        action = grade.get('action') or action_for({**metrics, **awareness['scores']}, rel)
        if file_is_empty(path):
            action = 'fill-empty-scaffold-or-delete-review'
        elif awareness['mergeIssue']:
            action = 'fix-merge-conflict-first'
        elif awareness['scores']['deleteReadinessScore'] >= 70:
            action = 'mark-delete-candidate'
        placement = {
            'path': rel,
            'module': module,
            'submodule': submodule,
            'engine': engine,
            'reason': reason,
            'alternativePlacements': alternatives,
            'lineCount': metrics.get('lineCount', 0),
            'keepScore': metrics.get('keepScore', 0),
            'reuseScore': metrics.get('reuseScore', 0),
            'complexityScore': metrics.get('complexityScore', 0),
            'duplicateLineRatio': metrics.get('duplicateLineRatio', 0),
            'variableScores': awareness['scores'],
            'programAwareness': {'mergeIssue': awareness['mergeIssue'], 'todoDebt': awareness['todoDebt'], 'placeholderRisk': awareness['placeholderRisk'], 'emptyFile': file_is_empty(path)},
            'suggestedAction': action,
            'recommendedAgent': agent,
            'proposedTreePath': proposed_tree_path(module, submodule, rel),
            'controlState': {'enabled': True, 'canChange': True, 'canDelete': action in {'mark-prune-review', 'mark-delete-candidate'}, 'mutatesByDefault': False, 'liveDeleteRequires': 'HELIOS_ORGANIZER_LIVE_DELETE=1'},
            'advice': f"{agent['id']} should {action.replace('-', ' ')} while C# keeps GUI/security orchestration and the learning loop records the result.",
        }
        placements.append(placement)
        tree[module][submodule].append(rel)
    modules = []
    for module, submodules in sorted(tree.items()):
        module_files = [f for files in submodules.values() for f in files]
        module_placements = [p for p in placements if p['module'] == module]
        modules.append({
            'module': module,
            'fileCount': len(module_files),
            'lineCount': sum(int(p.get('lineCount') or 0) for p in module_placements),
            'averageKeepScore': round(sum(float(p.get('keepScore') or 0) for p in module_placements) / max(1, len(module_placements)), 2),
            'submodules': [{'name': name, 'fileCount': len(files), 'files': files[:80]} for name, files in sorted(submodules.items())],
        })
    system_awareness = {
        'fileCount': len(placements),
        'moduleCount': len(tree),
        'languageExtensions': {ext: sum(1 for p in placements if p['path'].endswith(ext)) for ext in sorted(EXTS)},
        'importantIssueCount': sum(1 for p in placements if p['suggestedAction'] in {'fix-merge-conflict-first', 'mark-delete-candidate'}),
        'selfOptimizationRound': 'learning-and-copying-round',
        'projectStyle': setup['projectStyle'],
        'setupSignals': setup['signals'],
        'roots': setup['roots'],
    }
    controls = [
        {'label': 'Organize + grade + advise', 'command': 'scripts/setup/simple-build.sh organize', 'enabled': True, 'mutation': 'report-only'},
        {'label': 'Full absorb/prune learning', 'command': 'scripts/setup/simple-build.sh organize-full', 'enabled': True, 'mutation': 'report-only'},
        {'label': 'Turn off delete candidates', 'command': 'HELIOS_ORGANIZER_DELETE=0 scripts/setup/simple-build.sh organize', 'enabled': True, 'mutation': 'report-only'},
        {'label': 'Mark change/delete candidates only', 'command': 'HELIOS_ORGANIZER_MARK_ONLY=1 scripts/setup/simple-build.sh organize', 'enabled': True, 'mutation': 'report-only'},
        {'label': 'Lay empty scaffold plan', 'command': 'scripts/setup/simple-build.sh organize-scaffold-plan', 'enabled': True, 'mutation': 'report-only'},
        {'label': 'Live delete marked trash (explicit)', 'command': 'HELIOS_ORGANIZER_LIVE_DELETE=1 scripts/setup/simple-build.sh organize-delete-plan', 'enabled': False, 'mutation': 'requires explicit env flag and review'},
    ]
    return {
        'generatedUtc': datetime.now(timezone.utc).isoformat(),
        'mode': mode,
        'purpose': 'Automatic smart module/submodule placement map that figures out what each file is part of, grades it with 50 variables, marks keep/absorb/prune/change/delete/fix advice, and assigns an optimized agent without mutating files by default.',
        'principle': 'C# holds the organized service/GUI/security backbone, F# owns learning/scoring submodules, C++ owns hot-path/security/performance helpers, Python stays AIHub/agent/Linux glue, YAML/Bicep/JSON/MD keep automation and ledgers moving.',
        'gradeVariables': GRADE_VARIABLES,
        'systemAwareness': system_awareness,
        'missingScaffolds': missing_scaffolds(existing),
        'emptyFiles': [p for p in placements if p['programAwareness'].get('emptyFile')][:50],
        'mergeOptimizationLanes': [
            {'lane': 'you/local', 'agent': 'csharp-guardian', 'does': 'keeps repo shape, GUI/security, and final command flow stable'},
            {'lane': 'someone-else/incoming', 'agent': 'hermes-xcore-pruner', 'does': 'compares incoming branch/module ideas, unique files, and risky deletes without mutating'},
            {'lane': 'shared-learning', 'agent': 'fsharp-oracle', 'does': 'scores merge/split/copy/delete decisions and carries lessons to the next round'},
            {'lane': 'hot-path-check', 'agent': 'cpp-sentinel', 'does': 'flags native/performance/security files that should be split or optimized'},
        ],
        'controls': controls,
        'agentFleet': AGENTS,
        'modules': modules,
        'placements': placements,
        'deleteCandidates': [p for p in placements if p['suggestedAction'] in {'mark-prune-review', 'mark-delete-candidate'}][:50],
        'changeCandidates': [p for p in placements if p['suggestedAction'] in {'split-into-submodule-review', 'absorb-and-optimize'}][:50],
        'fixCandidates': [p for p in placements if p['suggestedAction'] in {'fix-merge-conflict-first', 'fill-empty-scaffold-or-delete-review'}][:50],
        'easyCommand': 'scripts/setup/simple-build.sh organize',
    }


def write_reports(payload: dict[str, object]) -> None:
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(json.dumps(payload, indent=2) + '\n')
    lines = ['# Module / Submodule Organizer', '', payload['purpose'], '', payload['principle'], '', f"Easy command: `{payload['easyCommand']}`", '', '## GUI control buttons']
    lines += [f"- **{c['label']}** — `{c['command']}` ({c['mutation']})" for c in payload['controls']]
    lines += ['', '## 50 grading variables']
    lines.append(', '.join(payload['gradeVariables']))
    lines += ['', '## Program/system awareness']
    lines += [f"- `{k}`: `{v}`" for k, v in payload['systemAwareness'].items() if k != 'languageExtensions']
    lines.append(f"- `languageExtensions`: `{payload['systemAwareness'].get('languageExtensions', {})}`")
    lines += ['', '## Missing/empty scaffold plan']
    lines += [f"- `{m['suggestedPath']}` ({m['engine']}): {m['why']} — {m['operation']}" for m in payload.get('missingScaffolds', [])]
    lines += [f"- empty `{e['path']}` → `{e['suggestedAction']}`" for e in payload.get('emptyFiles', [])]
    lines += ['', '## Two-source merge/optimization lanes']
    lines += [f"- `{lane['lane']}` via `{lane['agent']}`: {lane['does']}" for lane in payload.get('mergeOptimizationLanes', [])]
    lines += ['', '## Optimized fleet']
    lines += [f"- `{a['id']}` ({a['engine']}): {', '.join(a['skills'])}" for a in payload['agentFleet']]
    lines += ['', '## Proposed module tree']
    for module in payload['modules']:
        lines.append(f"### {module['module']} ({module['fileCount']} files, {module['lineCount']} graded lines, avg keep {module['averageKeepScore']})")
        for submodule in module['submodules']:
            lines.append(f"- `{submodule['name']}` — {submodule['fileCount']} files")
    lines += ['', '## Top placements by keep score', '', '| File | Proposed tree | Engine | Agent | Lines | Keep | Duplicate | Action |', '| --- | --- | --- | --- | ---: | ---: | ---: | --- |']
    for item in sorted(payload['placements'], key=lambda p: (p['keepScore'], p['lineCount']), reverse=True)[:100]:
        lines.append(f"| `{item['path']}` | `{item['proposedTreePath']}` | {item['engine']} | {item['recommendedAgent']['id']} | {item['lineCount']} | {item['keepScore']} | {item['duplicateLineRatio']} | {item['suggestedAction']} |")
    lines += ['', '## Marked fix/change/delete advice']
    for item in (payload.get('fixCandidates', []) + payload['deleteCandidates'] + payload['changeCandidates'])[:80]:
        lines.append(f"- `{item['path']}` → `{item['suggestedAction']}` via `{item['recommendedAgent']['id']}`: {item['advice']}")
    MD.write_text('\n'.join(lines) + '\n')


def main() -> int:
    parser = argparse.ArgumentParser(description='Generate smart module/submodule placement reports.')
    parser.add_argument('--mode', choices=['plan', 'mark-only', 'full'], default='plan')
    args = parser.parse_args()
    payload = build_payload(args.mode)
    write_reports(payload)
    print(f'Wrote {OUT.relative_to(ROOT)}')
    print(f'Wrote {MD.relative_to(ROOT)}')
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
