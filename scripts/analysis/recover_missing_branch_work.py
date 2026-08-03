#!/usr/bin/env python3
from __future__ import annotations
import argparse, json, subprocess
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'reports/branch-intelligence/recover-missing-branch-work.json'
MD = ROOT / 'reports/branch-intelligence/recover-missing-branch-work.md'
DEFAULT_WINDOWS = [
    ('requested-2026-july-30-31', '2026-07-30 00:00', '2026-08-01 00:00'),
    ('possible-2025-july-30-31', '2025-07-30 00:00', '2025-08-01 00:00'),
    ('current-session-2026-july-03', '2026-07-03 00:00', '2026-07-04 00:00'),
]
DOMAINS = {
    'csharp-backbone-frontend-orchestrator': ['.cs', '.csproj', 'src/core/', 'src/gui/', 'MonadoBlade', 'HELIOS.Platform'],
    'cpp-xcore-performance': ['.cpp', '.hpp', 'src/native/', 'CMakeLists.txt', 'XCore'],
    'fsharp-analytics-learning': ['.fs', '.fsproj', 'src/analytics/', 'tests/analytics/'],
    'python-learning-automation-agents': ['.py', 'scripts/', 'ai-integration/'],
    'html-super-gui': ['.html', 'status-site/', 'scripts/dashboard/', 'scripts/web/'],
    'super-cloud-azure-bicep': ['infra/azure/', '.bicep', 'scripts/azure/', 'keyvault'],
    'branch-merge-prune': ['branch', 'merge', 'prune', 'scripts/analysis/', 'branch-intelligence'],
    'deep-agent-gui': ['scripts/agents/', 'agent', 'Hermes', 'HERMES', 'fleet'],
    'github-workflow-codex': ['.github/', '.yml', '.yaml', 'scripts/codex/'],
    'security-vault': ['scripts/security/', 'secret', 'security', 'vault'],
}

def git(args: list[str]) -> tuple[int, str, str]:
    p = subprocess.run(['git', *args], cwd=ROOT, text=True, capture_output=True)
    return p.returncode, p.stdout.strip(), p.stderr.strip()

def run_fetch(enabled: bool) -> dict:
    if not enabled:
        return {'enabled': False, 'status': 'planned', 'command': 'git fetch --all --prune --tags'}
    code, out, err = git(['fetch', '--all', '--prune', '--tags'])
    return {'enabled': True, 'status': 'passed' if code == 0 else 'failed', 'command': 'git fetch --all --prune --tags', 'tail': (out + '\n' + err).splitlines()[-20:]}

def refs() -> list[dict]:
    code, out, _ = git(['for-each-ref', '--format=%(refname:short)|%(committerdate:iso8601)|%(objectname:short)', 'refs/heads', 'refs/remotes'])
    rows = []
    for line in out.splitlines():
        parts = line.split('|')
        if len(parts) == 3 and parts[0] and not parts[0].endswith('/HEAD'):
            rows.append({'name': parts[0], 'date': parts[1], 'sha': parts[2]})
    return rows

def commits(start: str, end: str) -> list[dict]:
    fmt = '%H%x1f%h%x1f%cI%x1f%D%x1f%s'
    code, out, _ = git(['log', '--all', f'--since={start}', f'--until={end}', f'--format={fmt}'])
    rows = []
    for line in out.splitlines():
        parts = line.split('\x1f')
        if len(parts) == 5:
            files = commit_files(parts[0])
            rows.append({'sha': parts[0], 'short': parts[1], 'date': parts[2], 'refs': parts[3], 'subject': parts[4], 'domains': domains(files, parts[4]), 'fileCount': len(files), 'sampleFiles': files[:25]})
    return rows

def commit_files(sha: str) -> list[str]:
    _, out, _ = git(['show', '--name-only', '--format=', sha])
    return [line for line in out.splitlines() if line]

def domains(files: list[str], subject: str) -> list[str]:
    haystack = files + [subject]
    found = []
    for domain, needles in DOMAINS.items():
        if any(any(n.lower() in item.lower() for n in needles) for item in haystack):
            found.append(domain)
    return found or ['general']

def main() -> int:
    parser = argparse.ArgumentParser(description='Recover visibility into missing July 30/31 branch work and today\'s super-session commits.')
    parser.add_argument('--fetch', action='store_true', help='run git fetch --all --prune --tags before scanning')
    args = parser.parse_args()
    fetch = run_fetch(args.fetch)
    ref_rows = refs()
    windows = []
    for name, start, end in DEFAULT_WINDOWS:
        found = commits(start, end)
        windows.append({'name': name, 'start': start, 'end': end, 'commitCount': len(found), 'commits': found})
    recovery = [
        'If July 30/31 work is not visible, run `git remote -v` and `git fetch --all --prune --tags`, then rerun this report with `--fetch`.',
        'Run `python3 scripts/agents/branch_fix_agents.py --max-branches 88 --scan-origin` after refs are fetched.',
        'Use generated packets under `reports/branch-agents/packets/` to fix branches in worktrees, then PR into the single super branch.',
        'Run `./finish.sh --full` after branch fixes to rebuild the super GUI, learning atlas, cloud readiness, and build graph.',
    ]
    payload = {'generatedUtc': datetime.now(timezone.utc).isoformat(), 'fetch': fetch, 'refCount': len(ref_rows), 'refs': ref_rows, 'windows': windows, 'recoverySteps': recovery, 'safeMode': 'Report-only by default; fetch is opt-in; no merges/deletes/force-pushes/cloud mutations.'}
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(json.dumps(payload, indent=2) + '\n')
    lines = ['# Recover Missing Branch Work', '', f"Generated: `{payload['generatedUtc']}`", '', payload['safeMode'], '', f"Refs visible: **{payload['refCount']}**", f"Fetch status: **{fetch['status']}**", '', '| Window | Start | End | Commits found |', '| --- | --- | --- | ---: |']
    for w in windows:
        lines.append(f"| {w['name']} | `{w['start']}` | `{w['end']}` | {w['commitCount']} |")
    lines += ['', '## Recovery steps', ''] + [f"- {step}" for step in recovery]
    lines += ['', '## Visible refs', ''] + [f"- `{r['name']}` {r['date']} `{r['sha']}`" for r in ref_rows]
    for w in windows:
        lines += ['', f"## Commits in {w['name']}", '']
        if not w['commits']:
            lines += ['- No commits are visible in this checkout for that window. Fetch remotes/tags or restore the missing remote refs, then rerun.']
        for c in w['commits']:
            lines += [f"- `{c['short']}` {c['date']} — {c['subject']} ({', '.join(c['domains'])}; {c['fileCount']} files)"]
    MD.write_text('\n'.join(lines) + '\n')
    print(f"Wrote {OUT.relative_to(ROOT)}")
    print(f"Wrote {MD.relative_to(ROOT)}")
    return 0

if __name__ == '__main__':
    raise SystemExit(main())
