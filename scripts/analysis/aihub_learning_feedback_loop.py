#!/usr/bin/env python3
from __future__ import annotations

import json
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'reports/learning/aihub-learning-feedback-loop.json'
MD = ROOT / 'reports/learning/aihub-learning-feedback-loop.md'

SOURCES = {
    'complexGrading': ROOT / 'reports/learning/complex-code-grading.compact.json',
    'branchAbsorption': ROOT / 'reports/branch-intelligence/branch-absorption-multicloud-plan.json',
    'mergePrune': ROOT / 'reports/branch-intelligence/merge-prune-recommendations.json',
    'branchAutofix': ROOT / 'reports/branch-agents/branch-test-autofix-plan.json',
    'agentFleet': ROOT / 'reports/branch-agents/agent-fleet-control-catalog.json',
    'languageProfiles': ROOT / 'reports/learning/aihub-language-skill-profiles.json',
    'moduleOrganizer': ROOT / 'reports/learning/module-submodule-organizer.json',
}


def load(path: Path, fallback):
    if not path.exists():
        return fallback
    return json.loads(path.read_text())


def summarize() -> dict[str, object]:
    complex_data = load(SOURCES['complexGrading'], {'topKeep': [], 'topPrune': [], 'modules': {}})
    absorption = load(SOURCES['branchAbsorption'], {'complexGradingInfluence': {}, 'scoredRefs': []})
    merge = load(SOURCES['mergePrune'], {'recommendations': []})
    autofix = load(SOURCES['branchAutofix'], {'agentShellCombos': {}, 'autoConnectPlan': []})
    fleet = load(SOURCES['agentFleet'], {'agentTypes': [], 'promptPacks': [], 'specializationVariables': []})
    profiles = load(SOURCES['languageProfiles'], {'profiles': []})
    organizer = load(SOURCES['moduleOrganizer'], {'modules': [], 'placements': []})
    top_keep = complex_data.get('topKeep', [])[:12]
    top_prune = complex_data.get('topPrune', [])[:12]
    transfer_edges = [
        {'from': 'complex-grading', 'to': 'F# learning engine', 'teaches': 'keep/reuse/duplicate/complexity score weights'},
        {'from': 'complex-grading', 'to': 'C# orchestration', 'teaches': 'safe routing, module ownership, GUI command state'},
        {'from': 'complex-grading', 'to': 'C++ native helpers', 'teaches': 'when comparisons need native boost'},
        {'from': 'complex-grading', 'to': 'Hermes/XCore agents', 'teaches': 'which branch packets keep, absorb, prune, or rewrite'},
        {'from': 'merge-prune', 'to': 'branch absorption', 'teaches': 'branch-level keep/prune score and graded line context'},
        {'from': 'branch-autofix', 'to': 'multi-shell LLM routing', 'teaches': 'best shell/model combo per specialty'},
        {'from': 'language profiles', 'to': 'module blueprint', 'teaches': 'optimal C#/F#/C++/Python placement'},
        {'from': 'module organizer', 'to': 'everyone', 'teaches': 'organized module/submodule ownership tree and placement commands'},
    ]
    easy_command = 'scripts/setup/simple-build.sh learn'
    return {
        'generatedUtc': datetime.now(timezone.utc).isoformat(),
        'easyCommand': easy_command,
        'sourceReports': {k: str(v.relative_to(ROOT)) for k, v in SOURCES.items()},
        'topKeep': top_keep,
        'topPrune': top_prune,
        'mergeRecommendations': merge.get('recommendations', [])[:12],
        'branchSignals': absorption.get('scoredRefs', [])[:12],
        'agentShellCombos': autofix.get('agentShellCombos', {}),
        'autoConnectPlan': autofix.get('autoConnectPlan', []),
        'fleetLearningTargets': {
            'agentTypes': fleet.get('agentTypes', [])[:20],
            'promptPacks': fleet.get('promptPacks', [])[:10],
            'specializationVariables': fleet.get('specializationVariables', [])[:40],
        },
        'languageProfiles': profiles.get('profiles', [])[:8],
        'moduleTree': organizer.get('modules', [])[:20],
        'modulePlacements': organizer.get('placements', [])[:40],
        'transferEdges': transfer_edges,
        'principle': 'Everyone learns from the same compact evidence: C# keeps the system safe and connected, F# owns most scoring, C++ accelerates heavy comparisons, Python is glue only when useful, and Hermes/XCore agents carry the lessons into branch packets and fleet XP.',
    }


def main() -> int:
    payload = summarize()
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(json.dumps(payload, indent=2) + '\n')
    lines = ['# AIHub Learning Feedback Loop', '', payload['principle'], '', f"Easy command: `{payload['easyCommand']}`", '', '## Transfer edges']
    lines += [f"- `{edge['from']}` → `{edge['to']}`: {edge['teaches']}" for edge in payload['transferEdges']]
    lines += ['', '## Top keep/absorb lessons']
    lines += [f"- `{item.get('path','')}` keep `{item.get('metrics',{}).get('keepScore')}` lines `{item.get('metrics',{}).get('lineCount')}` route `{ ' -> '.join(item.get('engineRoute', [])) }`" for item in payload['topKeep'][:10]]
    lines += ['', '## Top prune/rewrite lessons']
    lines += [f"- `{item.get('path','')}` action `{item.get('action','')}` duplicate `{item.get('metrics',{}).get('duplicateLineRatio')}` lines `{item.get('metrics',{}).get('lineCount')}`" for item in payload['topPrune'][:10]]
    lines += ['', '## Module/submodule learning']
    lines += [f"- `{m.get('module','')}` — {m.get('fileCount',0)} files, submodules: {', '.join(s.get('name','') for s in m.get('submodules',[])[:5])}" for m in payload.get('moduleTree',[])[:12]]
    lines += ['', '## Merge/prune learning']
    lines += [f"- `{item.get('branch','')}` recommendation `{item.get('recommendation','')}` graded lines `{item.get('gradedLineCount',0)}` keep `{item.get('complexGrade',{}).get('avgKeepScore',0)}`" for item in payload['mergeRecommendations'][:10]]
    lines += ['', '## Auto-connect plan']
    lines += [f"- `{step.get('command','')}` — {step.get('gui','')}" for step in payload['autoConnectPlan']]
    MD.write_text('\n'.join(lines) + '\n')
    print(f'Wrote {OUT.relative_to(ROOT)}')
    print(f'Wrote {MD.relative_to(ROOT)}')
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
