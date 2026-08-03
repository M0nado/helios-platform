#!/usr/bin/env python3
from __future__ import annotations

import json
import subprocess
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'reports/branch-intelligence/branch-absorption-multicloud-plan.json'
MD = ROOT / 'reports/branch-intelligence/branch-absorption-multicloud-plan.md'
GRADING = ROOT / 'reports/learning/complex-code-grading.compact.json'



def load_grading() -> dict[str, object]:
    if not GRADING.exists():
        return {'topKeep': [], 'topPrune': [], 'metrics': []}
    return json.loads(GRADING.read_text())

def git(*args: str) -> list[str]:
    try:
        return subprocess.check_output(['git', *args], cwd=ROOT, text=True, stderr=subprocess.DEVNULL).splitlines()
    except Exception:
        return []


def score_ref(raw: str) -> dict[str, object]:
    parts = raw.split('|', 2)
    ref, sha, subject = (parts + ['', '', ''])[:3]
    lower = f'{ref} {subject}'.lower()
    novelty = 0.78 if any(x in lower for x in ('aihub', 'hermes', 'xcore', 'gui', 'azure')) else 0.48
    control = 0.72 if any(x in lower for x in ('yaml', 'workflow', 'branch', 'github', 'azure')) else 0.44
    risk = 0.35 if any(x in lower for x in ('delete', 'secret', 'token', 'key', 'credential')) else 0.12
    performance = 0.70 if any(x in lower for x in ('native', 'c++', 'performance', 'fsharp', 'score')) else 0.45
    score = max(0.0, min(1.0, novelty * 0.30 + control * 0.26 + performance * 0.24 - risk * 0.20))
    action = 'absorb-now' if score >= 0.72 else 'test-and-rank' if score >= 0.52 else 'record-or-prune'
    return {'ref': ref, 'sha': sha, 'subject': subject, 'score': round(score, 3), 'action': action}


def main() -> None:
    grading = load_grading()
    refs = git('for-each-ref', '--format=%(refname:short)|%(objectname:short)|%(subject)', 'refs/heads', 'refs/remotes')[:160]
    commits = git('log', '--all', '--since=14 days ago', '--pretty=%h|%ad|%s', '--date=short')[:120]
    scored_refs = sorted((score_ref(r) for r in refs), key=lambda r: r['score'], reverse=True)[:40]
    units = [
        {
            'id': 'repo-recreate-map',
            'ui': 'Repository, branch, commit, issue, PR, artifact, Pages, and workflow recreation graph',
            'controls': ['fetch all refs', 'compare selected branch', 'open commit lineage', 'export graph packet'],
            'githubYaml': ['actions/checkout@v4 fetch-depth: 0', 'gh api repository/branches/commits/issues when token is available', 'upload branch absorption reports', 'publish Pages dashboard when repo-live enabled'],
            'commands': ['git fetch --all --prune --tags', 'python3 scripts/analysis/branch_absorption_multicloud_plan.py'],
        },
        {
            'id': 'branch-absorb-score',
            'ui': 'Branch score, code absorption ranker, autofix packet, and merge/prune recommendation panel',
            'controls': ['rank by learning value', 'rank by security risk', 'rank by C++ hot path pressure', 'assign Hermes/XCore specialty'],
            'githubYaml': ['run branch-test-autofix-plan', 'run document-code-absorption-ranker', 'run aihub-module-blueprint', 'attach markdown report to artifact'],
            'commands': ['python3 scripts/agents/branch_test_autofix_plan.py', 'python3 scripts/analysis/document_code_absorption_ranker.py'],
        },
        {
            'id': 'multicloud-text-console',
            'ui': 'Interactive local, WSL2, Codespaces, GitHub runner, Azure, Fabric, Foundry, SQL, Cosmos, and vector setup text console',
            'controls': ['choose local/cloud mode', 'copy vault command', 'toggle repo-live/cloud-live flags', 'show cost/speed/quality tradeoffs'],
            'githubYaml': ['matrix mode: local, wsl2, codespaces, github-runner, azure-hybrid', 'save reports only unless live flags enable scoped mutation', 'emit setup summary for the GUI'],
            'commands': ['python3 scripts/integrations/aihub_supershell_vault_wizard.py', 'python3 scripts/integrations/aihub_unified_control_plane.py'],
        },
        {
            'id': 'simple-build-finish-ui',
            'ui': 'One-click absorb, quick, full, clean, and finish buttons for the GUI and remote console',
            'controls': ['absorb branches', 'quick validation', 'full validation', 'finish readiness apply plan', 'clean generated build output'],
            'githubYaml': ['run simple-build absorb on branch change', 'run simple-build quick for PRs', 'run simple-build full on manual/schedule'],
            'commands': ['scripts/setup/simple-build.sh absorb', 'scripts/setup/simple-build.sh quick', 'scripts/setup/simple-build.sh finish'],
        },
    ]
    finish_phases = [
        'Generate branch absorption plan and scored ref packet.',
        'Run Hermes/XCore branch test/autofix specialty routing.',
        'Run document/code absorption ranker and module blueprint reports.',
        'Refresh dashboard so the GUI has the latest branches, commits, commands, and multicloud text.',
        'Use live flags to decide whether to stay report-only or apply scoped repo/cloud actions.',
    ]
    payload = {
        'generatedUtc': datetime.now(timezone.utc).isoformat(),
        'refs': refs,
        'scoredRefs': scored_refs,
        'commits': commits,
        'units': units,
        'complexGradingInfluence': {
            'source': str(GRADING.relative_to(ROOT)),
            'metrics': grading.get('metrics', [])[:30],
            'topKeep': grading.get('topKeep', [])[:10],
            'topPrune': grading.get('topPrune', [])[:10],
            'effect': 'Complex grading feeds absorption, branch testing, merge/prune recommendations, and GUI score/line displays before any mutation.',
        },
        'finishPhases': finish_phases,
        'multicloudModes': ['local', 'wsl2', 'codespaces', 'github-runner', 'azure-hybrid', 'cloud-shell'],
        'languageEngines': [
            'C# IBranchAbsorptionRanker and BranchAbsorptionFinishSettings own GUI-safe decisions, vault-safe settings, and orchestration shape.',
            'F# absorptionScore, absorptionAction, and finish scoring own learning/math decisions for branch value, risk, and final readiness.',
            'C++ branch_graph_complexity and branch_finish_priority own high-speed branch graph comparison, hot-path pressure, and native ranking helpers.',
            'Python stays report-only glue for GitHub/YAML/multicloud text UI, Linux/agent/LLM bridges, and simple command generation.',
        ],
        'uiText': 'Use this finished plan to recreate repository/branch/commit context in the GUI, rank what needs improvement, run branch testing, absorb unique ideas, carry complex grading scores/lines into merge/prune decisions, and use simple build buttons instead of command walls.',
    }
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(json.dumps(payload, indent=2) + '\n')
    lines = ['# Branch Absorption Multicloud Plan', '', f"Generated: `{payload['generatedUtc']}`", '', payload['uiText'], '', '## Finish phases']
    lines += [f'{index + 1}. {phase}' for index, phase in enumerate(finish_phases)]
    lines += ['', '## Units']
    for unit in units:
        lines += [f"### {unit['id']}", f"- UI: {unit['ui']}"]
        lines += [f"- Control: {control}" for control in unit['controls']]
        lines += [f"- Command: `{command}`" for command in unit['commands']]
        lines += [f"- YAML: {yaml}" for yaml in unit['githubYaml']]
    lines += ['', '## Complex grading influence', payload['complexGradingInfluence']['effect'], '', '### Top keep parts']
    lines += [f"- `{g.get('path','')}` keep `{g.get('metrics',{}).get('keepScore')}` lines `{g.get('metrics',{}).get('lineCount')}` route `{ ' -> '.join(g.get('engineRoute', [])) }`" for g in payload['complexGradingInfluence']['topKeep'][:8]]
    lines += ['', '### Top prune/rewrite candidates']
    lines += [f"- `{g.get('path','')}` action `{g.get('action','')}` duplicate `{g.get('metrics',{}).get('duplicateLineRatio')}` lines `{g.get('metrics',{}).get('lineCount')}`" for g in payload['complexGradingInfluence']['topPrune'][:8]]
    lines += ['', '## Language engines'] + [f'- {x}' for x in payload['languageEngines']]
    lines += ['', '## Top scored refs'] + [f"- `{r['ref']}` `{r['sha']}` score `{r['score']}` action `{r['action']}` — {r['subject']}" for r in scored_refs[:20]]
    lines += ['', '## Recent commits'] + [f'- `{c}`' for c in commits[:30]]
    MD.write_text('\n'.join(lines) + '\n')
    print(f'Wrote {OUT.relative_to(ROOT)}')
    print(f'Wrote {MD.relative_to(ROOT)}')


if __name__ == '__main__':
    main()
