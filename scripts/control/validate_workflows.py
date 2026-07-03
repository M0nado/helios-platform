#!/usr/bin/env python3
from __future__ import annotations
import json, re, shutil, subprocess, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
WORKFLOWS = ROOT / '.github' / 'workflows'
OUT_JSON = ROOT / 'reports' / 'control-plane' / 'workflow-validation.json'
OUT_MD = ROOT / 'reports' / 'control-plane' / 'workflow-validation.md'

KEY_LINE = re.compile(r'^(?P<indent>\s*)(?P<key>[A-Za-z0-9_.$/{}\[\]\-]+):(?P<rest>.*)$')
REQUIRED_TOP = {'name', 'on', 'jobs'}

def ruby_parse(path: Path) -> tuple[bool, str]:
    if not shutil.which('ruby'):
        return False, 'ruby not available; used stdlib structural validation only'
    code = "require 'yaml'; YAML.load_file(ARGV[0]); puts 'ok'"
    proc = subprocess.run(['ruby', '-e', code, str(path)], cwd=ROOT, text=True, capture_output=True)
    return proc.returncode == 0, (proc.stdout or proc.stderr).strip()

def structural_validate(path: Path) -> list[str]:
    lines = path.read_text().splitlines()
    issues: list[str] = []
    top_keys: set[str] = set()
    for idx, line in enumerate(lines, 1):
        if not line.strip() or line.lstrip().startswith('#'):
            continue
        if line.startswith('\t'):
            issues.append(f'line {idx}: tabs are not allowed in workflow YAML')
        m = KEY_LINE.match(line)
        if not m:
            continue
        indent = len(m.group('indent'))
        key = m.group('key').strip('"\'')
        rest = m.group('rest').strip()
        if indent == 0:
            top_keys.add(key)
        if key == 'run' and rest and rest not in {'|', '>'}:
            next_line = lines[idx] if idx < len(lines) else ''
            if next_line.startswith(' ' * (indent + 2)) and not next_line.lstrip().startswith(('#', '-')):
                issues.append(f'line {idx}: multiline run step should use run: |')
    missing = REQUIRED_TOP - top_keys
    if missing:
        issues.append(f'missing top-level keys: {", ".join(sorted(missing))}')
    return issues

def main() -> int:
    results = []
    ok = True
    for path in sorted(WORKFLOWS.glob('*.yml')) + sorted(WORKFLOWS.glob('*.yaml')):
        issues = structural_validate(path)
        ruby_ok, ruby_detail = ruby_parse(path)
        file_ok = not issues and (ruby_ok or 'ruby not available' in ruby_detail)
        ok = ok and file_ok
        results.append({
            'file': str(path.relative_to(ROOT)),
            'ok': file_ok,
            'issues': issues,
            'parser': 'ruby-psych' if ruby_ok else 'stdlib-structural',
            'parserDetail': ruby_detail,
        })
    OUT_JSON.parent.mkdir(parents=True, exist_ok=True)
    OUT_JSON.write_text(json.dumps({'ok': ok, 'workflows': results}, indent=2) + '\n')
    lines = ['# Workflow Validation', '', '| Workflow | OK | Parser | Issues |', '|---|---:|---|---|']
    for r in results:
        lines.append(f"| {r['file']} | {'yes' if r['ok'] else 'no'} | {r['parser']} | {'; '.join(r['issues']) if r['issues'] else r['parserDetail']} |")
    OUT_MD.write_text('\n'.join(lines) + '\n')
    print(f'Wrote {OUT_JSON.relative_to(ROOT)} and {OUT_MD.relative_to(ROOT)}')
    if not ok:
        for r in results:
            if not r['ok']:
                print(f"{r['file']}: {r['issues'] or r['parserDetail']}", file=sys.stderr)
        return 1
    return 0

if __name__ == '__main__':
    raise SystemExit(main())
