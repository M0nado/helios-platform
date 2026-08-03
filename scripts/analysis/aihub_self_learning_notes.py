#!/usr/bin/env python3
from __future__ import annotations

import json
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'reports/learning/aihub-self-learning-notes.json'
MD = ROOT / 'reports/learning/aihub-self-learning-notes.md'
ORGANIZER = ROOT / 'reports/learning/module-submodule-organizer.json'
LOOP = ROOT / 'reports/learning/aihub-learning-feedback-loop.json'
GRADING = ROOT / 'reports/learning/complex-code-grading.compact.json'

SITUATION_VARIABLES = [
    'branch-pressure', 'merge-distance', 'scaffold-gap', 'empty-file-risk', 'incoming-uniqueness',
    'local-stability', 'agent-confidence', 'engine-mismatch', 'hot-path-urgency', 'security-sensitivity',
    'gui-user-impact', 'docs-memory-value', 'cloud-readiness', 'vault-dependency', 'test-coverage-gap',
    'naming-drift', 'module-bone-strength', 'submodule-joint-fit', 'copy-round-value', 'delete-review-need'
]

SMALL_FILE_VARIABLES = [
    'name-shape', 'path-depth', 'module-fit', 'submodule-fit', 'line-weight', 'small-utility', 'import-clarity',
    'export-clarity', 'doc-signal', 'test-signal', 'security-signal', 'perf-signal', 'gui-signal', 'data-signal',
    'config-signal', 'agent-signal', 'branch-signal', 'cloud-signal', 'scaffold-bone', 'call-pattern', 'reuse-novelty',
    'merge-risk', 'delete-risk', 'quality-lift', 'connection-potential'
]


def load(path: Path, fallback):
    if not path.exists():
        return fallback
    return json.loads(path.read_text())


def thought_tiers(notes: list[dict[str, object]], organizer: dict[str, object]) -> dict[str, list[str]]:
    modules = [m.get('module', '') for m in organizer.get('modules', [])[:8]]
    scaffolds = [s.get('suggestedPath', '') for s in organizer.get('missingScaffolds', [])[:6]]
    return {
        'short': [f"Keep `{module}` in its natural submodule lane." for module in modules[:6]],
        'mid': [f"Connect `{module}` to its best C#/F#/C++/Python owner before copying or merging more files." for module in modules[:6]],
        'long': [f"Use `{scaffold}` as a report-only target when missing work should become a real module file after review." for scaffold in scaffolds],
        'abstract': [
            'Treat modules as bones, submodules as joints, files as muscles, and notes as memory so future branches attach naturally instead of piling up.',
            'Let C# keep the skeleton understandable, F# judge motion and prediction, C++ strengthen hot-path tendons, and Python connect outside senses only where it adds leverage.',
        ],
    }


def connection_ideas(organizer: dict[str, object], grading: dict[str, object]) -> list[dict[str, object]]:
    placements = organizer.get('placements', [])
    top_keep = grading.get('topKeep', [])
    ideas = []
    for placement in placements[:20]:
        for keep in top_keep[:8]:
            if placement.get('module') in keep.get('path', '') or placement.get('engine') in ' -> '.join(keep.get('engineRoute', [])):
                ideas.append({
                    'from': placement.get('path'),
                    'to': keep.get('path'),
                    'value': placement.get('variableScores', {}).get('connection-potential') or placement.get('variableScores', {}).get('overallPlacementScore', 0),
                    'why': f"Shared module/engine signal: {placement.get('module')} using {placement.get('engine')}",
                })
                break
    return ideas[:20]


def composite_file_plans(organizer: dict[str, object], grading: dict[str, object]) -> list[dict[str, object]]:
    small_parts = [p for p in organizer.get('placements', []) if int(p.get('lineCount') or 0) <= 180][:30]
    plans = []
    by_module: dict[str, list[dict[str, object]]] = {}
    for part in small_parts:
        by_module.setdefault(part.get('module', 'unknown'), []).append(part)
    for module, parts in by_module.items():
        if len(parts) < 2:
            continue
        engines = sorted({p.get('engine', '') for p in parts})
        plans.append({
            'module': module,
            'advancedFileIdea': f"modules/{module}/composite/{module.replace('-', '_')}_advanced_plan.md",
            'parts': [p.get('path') for p in parts[:8]],
            'engines': engines,
            'optimizationEye': ['speed', 'quality', 'naming', 'size', 'scaffold-bone', 'merge-safety'],
            'note': 'Report-only idea: combine these small parts into an advanced design file after review, preserving unique value and pruning duplicate noise.',
        })
    return plans[:12]


def document_module_thoughts(organizer: dict[str, object]) -> list[dict[str, object]]:
    docs = [p for p in organizer.get('placements', []) if p.get('module') == 'docs-knowledge' or p.get('path', '').endswith('.md')]
    return [{
        'path': item.get('path'),
        'short': 'Document belongs to the knowledge layer.',
        'mid': f"Tie `{item.get('path')}` to module `{item.get('submodule')}` and keep it close to the report or code it explains.",
        'long': 'If this doc contains instructions, preserve unique operational knowledge, remove stale duplicates, and convert durable steps into GUI/help cards.',
        'abstract': 'Documentation is the memory surface; it should teach the agents how the code wants to be shaped next time.',
    } for item in docs[:20]]


def main() -> int:
    organizer = load(ORGANIZER, {'modules': [], 'missingScaffolds': [], 'emptyFiles': [], 'fixCandidates': [], 'changeCandidates': [], 'deleteCandidates': [], 'mergeOptimizationLanes': []})
    loop = load(LOOP, {'transferEdges': [], 'topKeep': [], 'topPrune': []})
    grading = load(GRADING, {'topKeep': [], 'topPrune': []})
    notes = []
    for module in organizer.get('modules', [])[:12]:
        notes.append({
            'type': 'module-memory',
            'title': f"Remember {module.get('module')} shape",
            'note': f"{module.get('module')} has {module.get('fileCount')} files, {module.get('lineCount', 0)} graded lines, and average keep {module.get('averageKeepScore', 0)}. Keep future work in matching submodules before adding new folders.",
            'helps': ['placement', 'merge review', 'future scaffolds'],
        })
    for scaffold in organizer.get('missingScaffolds', [])[:10]:
        notes.append({
            'type': 'missing-scaffold-memory',
            'title': f"Suggested scaffold: {scaffold.get('suggestedPath')}",
            'note': f"Do not create automatically; propose this {scaffold.get('engine')} file when {scaffold.get('module')}/{scaffold.get('submodule')} needs a real implementation. Reason: {scaffold.get('why')}.",
            'helps': ['scaffold planning', 'issue writing', 'module fill-in'],
        })
    for item in (organizer.get('fixCandidates', []) + organizer.get('changeCandidates', []) + organizer.get('deleteCandidates', []))[:12]:
        notes.append({
            'type': 'fix-change-delete-memory',
            'title': f"Review {item.get('path')}",
            'note': f"Marked `{item.get('suggestedAction')}` with agent `{item.get('recommendedAgent', {}).get('id')}`. Advice: {item.get('advice')}",
            'helps': ['safe fixes', 'merge cleanup', 'delete review'],
        })
    for edge in loop.get('transferEdges', [])[:10]:
        notes.append({
            'type': 'learning-transfer-memory',
            'title': f"{edge.get('from')} teaches {edge.get('to')}",
            'note': edge.get('teaches', ''),
            'helps': ['agent coaching', 'feedback loop'],
        })
    help_cards = [
        {'question': 'Where should a new file go?', 'answer': 'Run `scripts/setup/simple-build.sh organize`, check module/submodule placement, then use C# for orchestration, F# for scoring, C++ for hot paths, Python for AIHub/LLM/Linux glue.'},
        {'question': 'How do I handle someone else\'s branch?', 'answer': 'Use the two-source merge lanes: C# keeps local shape, Hermes/XCore compares incoming work, F# scores merge/split/delete choices, and C++ checks native hot paths.'},
        {'question': 'Can it delete or create files automatically?', 'answer': 'No. Notes and scaffold plans are report-only. Live deletion is disabled unless an explicit environment flag and review path are added.'},
        {'question': 'How does it self-learn?', 'answer': 'It records module memories, missing scaffolds, fix/change/delete candidates, and transfer edges into this notes report for the next organize/learn/finish round.'},
    ]
    payload = {
        'generatedUtc': datetime.now(timezone.utc).isoformat(),
        'purpose': 'Self-learning notebook for AIHub/Hermes/XCore so every organize, grade, absorb, prune, and scaffold round leaves reusable notes and help cards without mutating files.',
        'easyCommand': 'scripts/setup/simple-build.sh notes',
        'sourceReports': [str(p.relative_to(ROOT)) for p in [ORGANIZER, LOOP, GRADING]],
        'notes': notes,
        'helpCards': help_cards,
        'thoughtTiers': thought_tiers(notes, organizer),
        'smallFileVariables': SMALL_FILE_VARIABLES,
        'situationVariables': SITUATION_VARIABLES,
        'connectionIdeas': connection_ideas(organizer, grading),
        'compositeFilePlans': composite_file_plans(organizer, grading),
        'documentModuleThoughts': document_module_thoughts(organizer),
        'topKeepMemory': grading.get('topKeep', [])[:8],
        'topPruneMemory': grading.get('topPrune', [])[:8],
    }
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(json.dumps(payload, indent=2) + '\n')
    lines = ['# AIHub Self-Learning Notes', '', payload['purpose'], '', f"Easy command: `{payload['easyCommand']}`", '', '## Help cards']
    lines += [f"- **{card['question']}** {card['answer']}" for card in help_cards]
    lines += ['', '## Thought tiers']
    for tier, items in payload['thoughtTiers'].items():
        lines.append(f"### {tier}")
        lines += [f"- {item}" for item in items]
    lines += ['', '## 25 small-file variables']
    lines.append(', '.join(payload['smallFileVariables']))
    lines += ['', '## 20 situation variables']
    lines.append(', '.join(payload['situationVariables']))
    lines += ['', '## Cross-file connection ideas']
    lines += [f"- `{idea['from']}` -> `{idea['to']}` value `{idea['value']}`: {idea['why']}" for idea in payload['connectionIdeas']]
    lines += ['', '## Composite advanced file plans']
    lines += [f"- `{plan['advancedFileIdea']}` from {len(plan['parts'])} parts: {plan['note']}" for plan in payload['compositeFilePlans']]
    lines += ['', '## Document module thoughts']
    lines += [f"- `{d['path']}` short: {d['short']} mid: {d['mid']}" for d in payload['documentModuleThoughts']]
    lines += ['', '## Notes']
    lines += [f"- **{note['title']}** ({note['type']}): {note['note']}" for note in notes]
    lines += ['', '## Top keep memory']
    lines += [f"- `{item.get('path','')}` keep `{item.get('metrics',{}).get('keepScore')}`" for item in payload['topKeepMemory']]
    MD.write_text('\n'.join(lines) + '\n')
    print(f'Wrote {OUT.relative_to(ROOT)}')
    print(f'Wrote {MD.relative_to(ROOT)}')
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
