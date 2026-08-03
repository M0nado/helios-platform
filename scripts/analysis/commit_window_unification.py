#!/usr/bin/env python3
from __future__ import annotations
import argparse, json, subprocess
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'reports/branch-intelligence/commit-window-unification.json'
MD = ROOT / 'reports/branch-intelligence/commit-window-unification.md'
DOMAINS = {
    'csharp-gui-core': ['.cs', '.csproj', 'src/gui/', 'src/core/'],
    'cpp-xcore-native': ['.cpp', '.hpp', '.h', 'src/native/', 'CMakeLists.txt'],
    'fsharp-analytics': ['.fs', '.fsproj', 'src/analytics/', 'tests/analytics/'],
    'python-automation-agents': ['.py', 'scripts/', 'ai-integration/'],
    'github-yaml-workflows': ['.github/', '.yml', '.yaml'],
    'azure-bicep-cloud': ['infra/azure/', '.bicep', 'scripts/azure/'],
    'hermes-fleet-xcore': ['hermes', 'HERMES', 'config/hermes-fleet', 'scripts/agents/'],
    'security-vault': ['scripts/security/', 'security', 'vault', 'secret'],
    'dashboard-super-gui': ['scripts/dashboard/', 'scripts/web/', 'status-site/'],
}

def git(args: list[str]) -> str:
    proc = subprocess.run(['git', *args], cwd=ROOT, text=True, capture_output=True)
    return proc.stdout.strip() if proc.returncode == 0 else ''

def commits_since(since: str, until: str, fallback_today: bool) -> tuple[str, str, list[dict]]:
    fmt = '%H%x1f%h%x1f%cI%x1f%s'
    cmd = ['log', '--all', f'--since={since}', f'--format={fmt}']
    if until:
        cmd.insert(3, f'--until={until}')
    out = git(cmd)
    commits = parse(out)
    effective = since
    effective_until = until
    if not commits and fallback_today:
        effective = f"{datetime.now(timezone.utc).date().isoformat()} 00:00"
        cmd = ['log', '--all', f'--since={effective}', f'--format={fmt}']
        if until:
            cmd.insert(3, f'--until={until}')
        commits = parse(git(cmd))
    return effective, effective_until, commits

def parse(out: str) -> list[dict]:
    rows = []
    for line in out.splitlines():
        parts = line.split('\x1f')
        if len(parts) == 4:
            rows.append({'sha': parts[0], 'short': parts[1], 'date': parts[2], 'subject': parts[3]})
    return rows

def files_for_commit(sha: str) -> list[str]:
    out = git(['show', '--name-only', '--format=', sha])
    return [line for line in out.splitlines() if line]

def domain_hits(files: list[str], subject: str) -> list[str]:
    haystack = files + [subject]
    hits = []
    for domain, needles in DOMAINS.items():
        if any(any(needle.lower() in item.lower() for needle in needles) for item in haystack):
            hits.append(domain)
    return hits or ['general']

def main() -> int:
    parser = argparse.ArgumentParser(description='Unify/score commits from a requested time window into one super workstream report.')
    parser.add_argument('--since', default=f"{datetime.now(timezone.utc).date().isoformat()} 00:00", help='requested git log --since window')
    parser.add_argument('--until', default='', help='optional git log --until window, e.g. "2 hours ago"')
    parser.add_argument('--no-fallback-today', action='store_true', help='do not fallback to today when requested window has no commits')
    args = parser.parse_args()
    effective_since, effective_until, commits = commits_since(args.since, args.until, not args.no_fallback_today)
    enriched = []
    counts = Counter()
    files_total = Counter()
    for commit in commits:
        files = files_for_commit(commit['sha'])
        domains = domain_hits(files, commit['subject'])
        counts.update(domains)
        files_total.update(files)
        enriched.append({**commit, 'fileCount': len(files), 'domains': domains, 'sampleFiles': files[:30]})
    payload = {
        'generatedUtc': datetime.now(timezone.utc).isoformat(),
        'requestedSince': args.since,
        'effectiveSince': effective_since,
        'requestedUntil': args.until,
        'effectiveUntil': effective_until,
        'fallbackUsed': effective_since != args.since,
        'commitCount': len(enriched),
        'domainCounts': dict(sorted(counts.items())),
        'uniqueFileCount': len(files_total),
        'commits': enriched,
        'combinedSuperCommands': [
            'python3 scripts/analysis/commit_window_unification.py',
            'python3 scripts/analysis/super_branch_unification.py',
            'python3 scripts/analysis/deep_branch_code_score.py',
            'python3 scripts/agents/branch_fix_agents.py --max-branches 88 --scan-origin',
            './finish.sh --full',
        ],
    }
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(json.dumps(payload, indent=2) + '\n')
    lines = ['# Commit Window Unification', '', f"Generated: `{payload['generatedUtc']}`", '', f"Requested since: `{args.since}`", f"Effective since: `{effective_since}`", f"Requested until: `{args.until or 'not set'}`", f"Effective until: `{effective_until or 'not set'}`", f"Fallback used: **{payload['fallbackUsed']}**", f"Commits: **{payload['commitCount']}**", f"Unique files touched: **{payload['uniqueFileCount']}**", '', '## Domain counts', '']
    lines += [f"- {k}: {v}" for k, v in payload['domainCounts'].items()] or ['- none']
    lines += ['', '## Combined super commands', ''] + [f"- `{cmd}`" for cmd in payload['combinedSuperCommands']]
    lines += ['', '| Commit | Date | Domains | Files | Subject |', '| --- | --- | --- | ---: | --- |']
    for commit in enriched:
        lines.append(f"| `{commit['short']}` | {commit['date']} | {', '.join(commit['domains'])} | {commit['fileCount']} | {commit['subject']} |")
    for commit in enriched:
        lines += ['', f"## `{commit['short']}` files", '']
        lines += [f"- `{f}`" for f in commit['sampleFiles']]
    MD.write_text('\n'.join(lines) + '\n')
    print(f"Wrote {OUT.relative_to(ROOT)}")
    print(f"Wrote {MD.relative_to(ROOT)}")
    return 0

if __name__ == '__main__':
    raise SystemExit(main())
