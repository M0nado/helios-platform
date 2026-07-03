#!/usr/bin/env python3
from __future__ import annotations
import json, subprocess
from collections import Counter, defaultdict
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'reports/branch-intelligence/deep-branch-code-score.json'
MD = ROOT / 'reports/branch-intelligence/deep-branch-code-score.md'
EXTENSIONS = {
    '.cs': 'csharp', '.csproj': 'csharp', '.sln': 'csharp', '.slnx': 'csharp',
    '.cpp': 'cpp', '.cc': 'cpp', '.cxx': 'cpp', '.c': 'cpp', '.h': 'cpp', '.hpp': 'cpp',
    '.fs': 'fsharp', '.fsproj': 'fsharp',
    '.py': 'python',
    '.yml': 'yaml', '.yaml': 'yaml',
    '.bicep': 'bicep',
    '.json': 'config-json', '.md': 'docs', '.sh': 'shell',
}
DOMAINS = {
    'csharp-gui': ['src/gui/', 'MonadoBlade.GUI', '.csproj', '.cs'],
    'csharp-core-server-vault': ['src/core/', 'src/Security/', 'vault', 'SecurityValidator'],
    'cpp-xcore-performance': ['src/native/', 'CMakeLists.txt', '.cpp', '.hpp'],
    'fsharp-analytics-prediction': ['src/analytics/', 'tests/analytics/', '.fs', '.fsproj'],
    'python-automation-aihub': ['scripts/', 'ai-integration/', '.py'],
    'yaml-github-codex-ci': ['.github/workflows/', '.yml', '.yaml', 'scripts/codex/', 'github-mcp'],
    'bicep-azure-cloud-vault': ['infra/azure/', '.bicep', 'keyvault', 'scripts/azure/'],
    'hermes-fleet-xcore-agents': ['hermes', 'HERMES', 'scripts/agents/', 'config/hermes-fleet'],
    'security-preflight': ['scripts/security/', 'security-preflight', 'secret', 'apply_gate'],
    'dashboard-super-gui': ['scripts/dashboard/', 'status-site/', 'scripts/web/'],
}
PLANS = {
    'csharp-gui': ['dotnet build HELIOS.Platform.slnx', 'python3 scripts/integrations/full_integration_matrix.py'],
    'csharp-core-server-vault': ['dotnet test src/tests/HELIOS.Platform.Tests/HELIOS.Platform.Tests.csproj', 'python3 scripts/security/apply_gate_preflight.py'],
    'cpp-xcore-performance': ['cmake -S src/native/HELIOS.Native.Performance -B .build/native', 'python3 scripts/build_graph/build_graph.py run --tag native --max-workers 2'],
    'fsharp-analytics-prediction': ['dotnet test tests/analytics/HELIOS.Analytics.FSharp.Tests/HELIOS.Analytics.FSharp.Tests.fsproj'],
    'python-automation-aihub': ['python3 -m py_compile scripts/build_graph/build_graph.py scripts/apply/finish_readiness_apply.py', './finish.sh --full'],
    'yaml-github-codex-ci': ['python3 scripts/control/validate_workflows.py', 'gh auth login'],
    'bicep-azure-cloud-vault': ['python3 scripts/azure/azure_connection_pipeline.py --stage all', 'python3 scripts/azure/azure_what_if.py'],
    'hermes-fleet-xcore-agents': ['python3 scripts/agents/hermes_fleet_readiness.py', 'cp config/hermes-fleet.example.json config/hermes-fleet.local.json'],
    'security-preflight': ['python3 scripts/security/secret_preflight.py --strict', 'python3 scripts/security/apply_gate_preflight.py --strict'],
    'dashboard-super-gui': ['python3 scripts/dashboard/generate-gui.py', 'python3 scripts/web/helios-web.py'],
}

def git(args: list[str]) -> str:
    proc = subprocess.run(['git', *args], cwd=ROOT, text=True, capture_output=True)
    return proc.stdout.strip() if proc.returncode == 0 else ''

def refs() -> list[str]:
    out = git(['for-each-ref', '--format=%(refname:short)', 'refs/heads', 'refs/remotes'])
    found = []
    for line in out.splitlines():
        ref = line.strip()
        if ref and ref != 'origin/HEAD' and not ref.endswith('/HEAD'):
            found.append(ref)
    current = git(['branch', '--show-current']) or 'HEAD'
    return sorted(set(found or [current]))

def repo_files() -> list[str]:
    proc = subprocess.run(['rg', '--files'], cwd=ROOT, text=True, capture_output=True)
    if proc.returncode in (0, 1):
        return [line for line in proc.stdout.splitlines() if line]
    return []

def branch_files(ref: str, target: str, all_files: list[str]) -> list[str]:
    if ref == target:
        return all_files
    out = git(['diff', '--name-only', f'{target}...{ref}']) or git(['diff', '--name-only', ref])
    return [line for line in out.splitlines() if line]

def language_counts(files: list[str]) -> dict[str, int]:
    counts = Counter()
    for f in files:
        counts[EXTENSIONS.get(Path(f).suffix.lower(), 'other')] += 1
    return dict(sorted(counts.items()))

def domain_hits(files: list[str]) -> dict[str, int]:
    hits = defaultdict(int)
    for f in files:
        low = f.lower()
        for domain, needles in DOMAINS.items():
            if any(n.lower() in low for n in needles):
                hits[domain] += 1
    return dict(sorted(hits.items()))

def score_branch(lang: dict[str, int], domains: dict[str, int], is_target: bool) -> int:
    score = 20 if is_target else 10
    for key in ['csharp', 'cpp', 'fsharp', 'python', 'yaml', 'bicep']:
        if lang.get(key):
            score += 8
    score += min(30, len(domains) * 3)
    if {'csharp-gui', 'cpp-xcore-performance', 'fsharp-analytics-prediction', 'python-automation-aihub'} <= set(domains):
        score += 10
    return min(100, score)

def main() -> int:
    target = git(['branch', '--show-current']) or 'HEAD'
    all_files = repo_files()
    rows = []
    for ref in refs():
        files = branch_files(ref, target, all_files)
        lang = language_counts(files)
        domains = domain_hits(files)
        plans = []
        for domain in domains:
            plans.extend(PLANS.get(domain, []))
        # Preserve broad plans even if the current branch has no unique branch diff.
        if ref == target:
            for domain in DOMAINS:
                if domain not in domains and any(any(n.lower() in f.lower() for n in DOMAINS[domain]) for f in all_files):
                    domains[domain] = 0
                    plans.extend(PLANS.get(domain, []))
        rows.append({
            'branch': ref,
            'isTarget': ref == target,
            'fileCount': len(files),
            'languageCounts': lang,
            'domainHits': dict(sorted(domains.items())),
            'score': score_branch(lang, domains, ref == target),
            'recommendedAction': 'single-super-branch-target' if ref == target else 'manual-merge-review',
            'broadPlans': list(dict.fromkeys(plans))[:30],
        })
    payload = {
        'generatedUtc': datetime.now(timezone.utc).isoformat(),
        'targetBranch': target,
        'ok': True,
        'summary': 'Deep branch/code scoring for C#, C++, F#, Python, YAML, Bicep, Hermes/XCore, security, dashboard, and cross-automation plans.',
        'branches': sorted(rows, key=lambda r: (not r['isTarget'], -r['score'], r['branch'])),
    }
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(json.dumps(payload, indent=2) + '\n')
    lines = ['# Deep Branch Code Score', '', f"Generated: `{payload['generatedUtc']}`", '', f"Target branch: `{target}`", '', payload['summary'], '', '| Branch | Target | Score | Files | Languages | Domains | Action |', '| --- | --- | ---: | ---: | --- | --- | --- |']
    for row in payload['branches']:
        langs = ', '.join(f"{k}:{v}" for k, v in row['languageCounts'].items() if v)
        domains = ', '.join(f"{k}:{v}" for k, v in row['domainHits'].items())
        lines.append(f"| `{row['branch']}` | {'✅' if row['isTarget'] else ''} | {row['score']} | {row['fileCount']} | {langs} | {domains} | {row['recommendedAction']} |")
    for row in payload['branches']:
        lines += ['', f"## Broad plan for `{row['branch']}`", '']
        if row['broadPlans']:
            lines += [f"- `{cmd}`" for cmd in row['broadPlans']]
        else:
            lines += ['- No code-domain plan detected; run `python3 scripts/analysis/super_branch_unification.py` for merge context.']
    MD.write_text('\n'.join(lines) + '\n')
    print(f"Wrote {OUT.relative_to(ROOT)}")
    print(f"Wrote {MD.relative_to(ROOT)}")
    return 0

if __name__ == '__main__':
    raise SystemExit(main())
