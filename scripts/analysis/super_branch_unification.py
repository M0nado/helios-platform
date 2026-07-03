#!/usr/bin/env python3
from __future__ import annotations
import argparse, json, subprocess
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'reports/branch-intelligence/super-branch-unification.json'
MD = ROOT / 'reports/branch-intelligence/super-branch-unification.md'
DOMAINS = {
    'csharp-gui-server-vault': ['.cs', '.csproj', 'src/core/', 'src/gui/', 'src/Security/'],
    'cpp-xcore-native': ['.cpp', '.hpp', '.h', 'src/native/', 'CMakeLists.txt'],
    'fsharp-analytics': ['.fs', '.fsproj', 'src/analytics/', 'tests/analytics/'],
    'python-automation-aihub': ['.py', 'scripts/', 'ai-integration/'],
    'yaml-github-codex': ['.yml', '.yaml', '.github/', 'scripts/codex/', 'config/github-mcp'],
    'bicep-azure-cloud': ['.bicep', 'infra/azure/', 'scripts/azure/'],
    'hermes-fleet-xcore': ['hermes', 'HERMES', 'scripts/agents/', 'config/hermes-fleet'],
    'security-vault': ['scripts/security/', 'vault', 'Security', 'keyvault'],
}

def git(args: list[str], ok_empty: bool = True) -> str:
    proc = subprocess.run(['git', *args], cwd=ROOT, text=True, capture_output=True)
    if proc.returncode != 0 and not ok_empty:
        raise SystemExit(proc.stderr.strip() or f"git {' '.join(args)} failed")
    return proc.stdout.strip()

def ref_names() -> list[str]:
    out = git(['for-each-ref', '--format=%(refname:short)', 'refs/heads', 'refs/remotes'])
    refs = []
    for line in out.splitlines():
        ref = line.strip()
        if not ref or ref == 'origin/HEAD' or ref.endswith('/HEAD'):
            continue
        refs.append(ref)
    return sorted(set(refs))

def changed_files(ref: str, base: str) -> list[str]:
    out = git(['diff', '--name-only', f'{base}...{ref}']) or git(['diff', '--name-only', ref])
    return [line for line in out.splitlines() if line]

def parse_commits(out: str) -> list[dict]:
    commits = []
    for line in out.splitlines():
        parts = line.split('\x1f')
        if len(parts) == 4:
            commits.append({'sha': parts[0], 'short': parts[1], 'date': parts[2], 'subject': parts[3]})
    return commits

def commits_since(ref: str, since: str) -> list[dict]:
    fmt = '%H%x1f%h%x1f%cI%x1f%s'
    out = git(['log', ref, f'--since={since}', f'--format={fmt}'])
    return parse_commits(out)

def recent_commits(ref: str, limit: int = 25) -> list[dict]:
    fmt = '%H%x1f%h%x1f%cI%x1f%s'
    return parse_commits(git(['log', ref, f'-{limit}', f'--format={fmt}']))

def detect_domains(files: list[str], subjects: list[str]) -> list[str]:
    haystack = files + subjects
    hits = []
    for domain, needles in DOMAINS.items():
        if any(any(needle.lower() in item.lower() for needle in needles) for item in haystack):
            hits.append(domain)
    return hits or ['general']

def merge_plan_for(branch: str, current: str, files: list[str], commits: list[dict], recent: list[dict]) -> dict:
    domains = detect_domains(files, [c['subject'] for c in (commits or recent)])
    if branch == current:
        action = 'target-super-branch'
        command = 'git status --short && ./finish.sh --full'
        reason = 'This is the current single integration branch receiving today\'s squashed work.'
    elif not commits and not files:
        action = 'already-contained-or-no-diff'
        command = f'git branch --contains {branch}'
        reason = 'No unique diff was detected against the current target.'
    else:
        action = 'manual-merge-review'
        command = f'git checkout {current} && git merge --no-ff {branch}'
        reason = 'Review first; this report does not auto-merge branches because conflicts/auth/CI need human approval.'
    return {
        'branch': branch, 'action': action, 'command': command, 'reason': reason,
        'commitCountSinceWindow': len(commits), 'commitsSinceWindow': commits[:20],
        'recentCommits': recent[:20], 'fileCountAgainstTarget': len(files), 'domains': domains,
    }

def main() -> int:
    parser = argparse.ArgumentParser(description='Create a safe one-super-branch consolidation plan for today\'s branch work.')
    parser.add_argument('--since', default=f"{datetime.now().date().isoformat()} 10:00", help='git log --since window, default: today at 10:00')
    parser.add_argument('--target', default='', help='target branch/ref; default: current branch')
    args = parser.parse_args()
    current = args.target or git(['branch', '--show-current']) or 'HEAD'
    refs = ref_names() or [current]
    plans = []
    seen = set()
    for ref in refs:
        key = git(['rev-parse', ref]) or ref
        # Keep same-commit refs visible only once unless one is current.
        if key in seen and ref != current:
            continue
        seen.add(key)
        commits = commits_since(ref, args.since)
        recent = recent_commits(ref)
        files = [] if ref == current else changed_files(ref, current)
        plans.append(merge_plan_for(ref, current, files, commits, recent))
    payload = {
        'generatedUtc': datetime.now(timezone.utc).isoformat(),
        'targetBranch': current,
        'since': args.since,
        'singleSuperBranchCommand': './finish.sh --full',
        'safeMode': 'No branch is merged, deleted, pushed, or force-updated by this report.',
        'branchCount': len(plans),
        'plans': plans,
        'nextSteps': [
            'Keep the current work branch as the single super integration branch unless another ref appears in this report as manual-merge-review.',
            'Run ./finish.sh --full to regenerate setup/readiness/deep integration reports.',
            'Review reports/branch-intelligence/super-branch-unification.md before any manual merge.',
        ],
    }
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(json.dumps(payload, indent=2) + '\n')
    lines = [
        '# Super Branch Unification Plan', '', f"Generated: `{payload['generatedUtc']}`", '',
        f"Target super branch: `{current}`", f"Since window: `{args.since}`", '', payload['safeMode'], '',
        f"One-command setup/deep processing: `{payload['singleSuperBranchCommand']}`", '',
        '| Branch | Action | Commits since window | Files vs target | Domains | Command |',
        '| --- | --- | ---: | ---: | --- | --- |',
    ]
    for plan in plans:
        lines.append(f"| `{plan['branch']}` | {plan['action']} | {plan['commitCountSinceWindow']} | {plan['fileCountAgainstTarget']} | {', '.join(plan['domains'])} | `{plan['command']}` |")
    lines += ['', '## Next steps', ''] + [f"- {step}" for step in payload['nextSteps']]
    for plan in plans:
        shown = plan['commitsSinceWindow'] or plan['recentCommits']
        if shown:
            heading = 'Commits since window' if plan['commitsSinceWindow'] else 'Latest commits (none matched the since window)'
            lines += ['', f"## {heading} on `{plan['branch']}`", '']
            lines += [f"- `{c['short']}` {c['date']} — {c['subject']}" for c in shown]
    MD.write_text('\n'.join(lines) + '\n')
    print(f"Wrote {OUT.relative_to(ROOT)}")
    print(f"Wrote {MD.relative_to(ROOT)}")
    return 0

if __name__ == '__main__':
    raise SystemExit(main())
