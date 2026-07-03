#!/usr/bin/env python3
from __future__ import annotations

import argparse
import fnmatch
import json
import subprocess
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CFG = ROOT / 'config/build-graph.json'
OUT = ROOT / 'reports/build-graph/build-graph.json'
MD = ROOT / 'reports/build-graph/build-graph.md'
DEFAULT_IGNORED_CHANGED_PATHS = [
    'reports/**',
    'status-site/**',
    '.build/**',
    '.tools/**',
    '**/bin/**',
    '**/obj/**',
]


def load():
    return json.loads(CFG.read_text())['nodes']


def matches(path, pattern):
    return fnmatch.fnmatchcase(path, pattern) or fnmatch.fnmatchcase('/' + path, pattern)


def changed_files():
    commands = [
        ['git', 'diff', '--name-only', '--diff-filter=ACMRT', 'HEAD'],
        ['git', 'diff', '--name-only', '--diff-filter=ACMRT', '--cached', 'HEAD'],
        ['git', 'ls-files', '--others', '--exclude-standard'],
    ]
    files = []
    seen = set()
    for command in commands:
        result = subprocess.run(command, cwd=ROOT, text=True, capture_output=True, check=False)
        if result.returncode != 0:
            continue
        for line in result.stdout.splitlines():
            path = line.strip().replace('\\', '/')
            if path and path not in seen:
                seen.add(path)
                files.append(path)
    return files


def filter_changed(files, include_generated):
    if include_generated:
        return files, []
    included, ignored = [], []
    for path in files:
        if any(matches(path, pattern) for pattern in DEFAULT_IGNORED_CHANGED_PATHS):
            ignored.append(path)
        else:
            included.append(path)
    return included, ignored


def node_matches_changed(node, files):
    patterns = node.get('paths') or []
    if not patterns:
        return False
    return any(matches(path, pattern) for path in files for pattern in patterns)


def write(nodes, results=None, report=None):
    report = report or {}
    OUT.parent.mkdir(parents=True, exist_ok=True)
    payload = {
        'generatedUtc': datetime.now(timezone.utc).isoformat(),
        'nodes': nodes,
        'results': results or [],
        'report': report,
    }
    OUT.write_text(json.dumps(payload, indent=2) + '\n')
    lines = ['# HELIOS Build Graph', '']
    if report:
        lines += [
            '## Run Report',
            '',
            f"- Changed files: {report.get('changedFilesCount', 0)}",
            f"- Ignored changed files: {report.get('ignoredChangedFilesCount', 0)}",
            f"- Considered changed files: {report.get('consideredChangedFilesCount', 0)}",
            f"- Selected nodes: {report.get('selectedNodesCount', 0)}",
            f"- Include generated: {str(report.get('includeGenerated', False)).lower()}",
            f"- Dry run: {str(report.get('dryRun', False)).lower()}",
            '',
        ]
    lines += ['| Node | Title | Command |', '| --- | --- | --- |'] + [f"| `{n['id']}` | {n['title']} | `{n['command']}` |" for n in nodes]
    if results:
        lines += ['', '## Results', '', '| Node | Exit | Tail |', '| --- | --- | --- |'] + [f"| `{r['id']}` | {r['exitCode']} | `{str(r['tail'])[:160]}` |" for r in results]
    MD.write_text('\n'.join(lines) + '\n')


def run_node(n):
    p = subprocess.run(n['command'], cwd=ROOT, text=True, capture_output=True, shell=True, timeout=180)
    return {'id': n['id'], 'exitCode': p.returncode, 'tail': (p.stdout + p.stderr).splitlines()[-10:]}


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('command', nargs='?', default='list', choices=['list', 'run', 'graph'])
    ap.add_argument('--node')
    ap.add_argument('--all', action='store_true')
    ap.add_argument('--changed-only', action='store_true', help='Run only nodes whose path triggers match changed files.')
    ap.add_argument('--include-generated', action='store_true', help='Include generated files in changed-file matching.')
    ap.add_argument('--dry-run', action='store_true', help='Report selected nodes without executing commands.')
    a = ap.parse_args()
    nodes = load()
    results = []
    report = {'includeGenerated': a.include_generated, 'dryRun': a.dry_run}
    selected = nodes
    if a.changed_only:
        changed = changed_files()
        considered, ignored = filter_changed(changed, a.include_generated)
        selected = [n for n in nodes if node_matches_changed(n, considered)]
        report.update({
            'changedFilesCount': len(changed),
            'ignoredChangedFilesCount': len(ignored),
            'consideredChangedFilesCount': len(considered),
            'ignoredChangedFiles': ignored,
        })
    if a.command == 'run':
        if a.node:
            selected = [n for n in selected if n['id'] == a.node]
        elif not (a.all or a.changed_only):
            selected = []
        if not selected and not a.dry_run:
            raise SystemExit('No matching node; use --node <id>, --all, or --changed-only')
        if not a.dry_run:
            for n in selected:
                results.append(run_node(n))
    report['selectedNodesCount'] = len(selected)
    write(selected if (a.command == 'run' and (a.changed_only or a.node or a.all)) else nodes, results, report)
    print(f"Selected nodes: {len(selected)}")
    if a.changed_only:
        print(f"Ignored changed files: {report['ignoredChangedFilesCount']}")
    print(f'Wrote {OUT.relative_to(ROOT)}')
    print(f'Wrote {MD.relative_to(ROOT)}')


if __name__ == '__main__':
    main()
