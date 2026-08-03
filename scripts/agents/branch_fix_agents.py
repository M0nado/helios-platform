#!/usr/bin/env python3
from __future__ import annotations
import argparse, json, re, subprocess
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'reports/branch-agents/branch-fix-agents.json'
MD = ROOT / 'reports/branch-agents/branch-fix-agents.md'
PACKET_DIR = ROOT / 'reports/branch-agents/packets'
SPECIALIZATION_CONFIG = ROOT / 'config/agent-specializations.json'
DOMAINS = {
    'csharp-gui': ['src/gui/', '.cs', '.csproj', 'MonadoBlade.GUI'],
    'csharp-core-vault': ['src/core/', 'src/Security/', 'vault', 'Security'],
    'cpp-xcore': ['src/native/', '.cpp', '.hpp', 'CMakeLists.txt'],
    'fsharp-analytics': ['src/analytics/', 'tests/analytics/', '.fs', '.fsproj'],
    'python-automation': ['scripts/', '.py', 'ai-integration/'],
    'github-yaml-codex': ['.github/', '.yml', '.yaml', 'scripts/codex/', 'github-mcp'],
    'deep-learning-ai-combo': ['config/ai-services/', 'chatgpt', 'openai', 'claude', 'anthropic', 'ai-integration/'],
    'azure-bicep': ['infra/azure/', '.bicep', 'scripts/azure/', 'keyvault'],
    'hermes-fleet': ['hermes', 'HERMES', 'scripts/agents/', 'config/hermes-fleet'],
    'security': ['scripts/security/', 'secret', 'apply_gate', 'security-preflight'],
    'dashboard-server': ['scripts/dashboard/', 'scripts/web/', 'status-site/'],
}
SPECIALISTS = {
    'csharp-gui': 'C# GUI/WinUI specialist',
    'csharp-core-vault': 'C# backend/security/vault specialist',
    'cpp-xcore': 'C++ XCore performance specialist',
    'fsharp-analytics': 'F# analytics/prediction specialist',
    'python-automation': 'Python automation/AIHub specialist',
    'github-yaml-codex': 'GitHub Actions/Codex MCP specialist',
    'deep-learning-ai-combo': 'Deep learning and cross-LLM integration specialist',
    'azure-bicep': 'Azure/Bicep/Key Vault specialist',
    'hermes-fleet': 'Hermes/Fleet/XCore agent specialist',
    'security': 'Security preflight specialist',
    'dashboard-server': 'Super GUI/local server specialist',
}
CHECKS = {
    'csharp-gui': ['dotnet build HELIOS.Platform.slnx'],
    'csharp-core-vault': ['dotnet test src/tests/HELIOS.Platform.Tests/HELIOS.Platform.Tests.csproj'],
    'cpp-xcore': ['cmake -S src/native/HELIOS.Native.Performance -B .build/native'],
    'fsharp-analytics': ['dotnet test tests/analytics/HELIOS.Analytics.FSharp.Tests/HELIOS.Analytics.FSharp.Tests.fsproj'],
    'python-automation': ['python3 -m py_compile scripts/build_graph/build_graph.py scripts/apply/finish_readiness_apply.py'],
    'github-yaml-codex': ['python3 scripts/control/validate_workflows.py'],
    'deep-learning-ai-combo': ['python3 scripts/integrations/full_integration_matrix.py', 'python3 scripts/integrations/super_stack_readiness.py'],
    'azure-bicep': ['python3 scripts/azure/azure_connection_pipeline.py --stage all'],
    'hermes-fleet': ['python3 scripts/agents/hermes_fleet_readiness.py'],
    'security': ['python3 scripts/security/secret_preflight.py', 'python3 scripts/security/apply_gate_preflight.py'],
    'dashboard-server': ['python3 scripts/dashboard/generate-gui.py'],
}

def load_specializations() -> dict:
    if not SPECIALIZATION_CONFIG.exists():
        return {}
    try:
        return json.loads(SPECIALIZATION_CONFIG.read_text()).get('specializations', {})
    except json.JSONDecodeError:
        return {}

SPECIALIZATION_DETAILS = load_specializations()
for domain, detail in SPECIALIZATION_DETAILS.items():
    if detail.get('checks'):
        CHECKS[domain] = detail['checks']
    if detail.get('title'):
        SPECIALISTS[domain] = detail['title']

def git(args: list[str]) -> str:
    proc = subprocess.run(['git', *args], cwd=ROOT, text=True, capture_output=True)
    return proc.stdout.strip() if proc.returncode == 0 else ''

def current_branch() -> str:
    return git(['branch', '--show-current']) or 'HEAD'

def all_refs(include_remote_scan: bool) -> list[str]:
    refs = []
    out = git(['for-each-ref', '--format=%(refname:short)', 'refs/heads', 'refs/remotes'])
    refs.extend(line.strip() for line in out.splitlines() if line.strip())
    if include_remote_scan:
        remote = git(['remote', 'get-url', 'origin'])
        if remote:
            ls = git(['ls-remote', '--heads', 'origin'])
            for line in ls.splitlines():
                if 'refs/heads/' in line:
                    refs.append('origin/' + line.rsplit('refs/heads/', 1)[1])
    cleaned = []
    for ref in refs:
        if ref in {'origin/HEAD', 'HEAD'} or ref.endswith('/HEAD') or ' -> ' in ref:
            continue
        cleaned.append(ref)
    return sorted(set(cleaned or [current_branch()]))

def changed_files(ref: str, target: str) -> list[str]:
    if ref == target:
        out = subprocess.run(['rg', '--files'], cwd=ROOT, text=True, capture_output=True).stdout
    else:
        out = git(['diff', '--name-only', f'{target}...{ref}']) or git(['diff', '--name-only', ref])
    return [line for line in out.splitlines() if line]

def classify(files: list[str]) -> dict[str, int]:
    hits = {domain: 0 for domain in DOMAINS}
    for file in files:
        low = file.lower()
        for domain, needles in DOMAINS.items():
            if any(needle.lower() in low for needle in needles):
                hits[domain] += 1
    return {k: v for k, v in hits.items() if v}

def safe_name(ref: str) -> str:
    return re.sub(r'[^A-Za-z0-9_.-]+', '_', ref).strip('_') or 'branch'

def packet_for(ref: str, target: str, limit_files: int) -> dict:
    files = changed_files(ref, target)
    domains = classify(files)
    primary = max(domains, key=domains.get) if domains else 'python-automation'
    checks = []
    for domain in domains or {primary: 1}:
        checks.extend(CHECKS.get(domain, []))
    checks = list(dict.fromkeys(checks))[:12]
    action = 'target-super-branch' if ref == target else 'fix-in-worktree-then-pr'
    worktree = f'.worktrees/branch-fix/{safe_name(ref)}'
    commands = [
        f'git fetch --all --prune',
        f'git worktree add {worktree} {ref}' if ref != target else 'git status --short',
        f'cd {worktree}' if ref != target else 'pwd',
        'scripts/setup/bootstrap-local-tools.sh',
        *checks,
        'python3 scripts/build_graph/build_graph.py run --profile quick --changed-only --max-workers 2',
    ]
    details = SPECIALIZATION_DETAILS.get(primary, {})
    return {
        'branch': ref,
        'target': target,
        'action': action,
        'primarySpecialist': SPECIALISTS.get(primary, 'General repo integration specialist'),
        'specialistFocus': details.get('focus', []),
        'specialistPaths': details.get('paths', []),
        'domains': domains,
        'fileCount': len(files),
        'sampleFiles': files[:limit_files],
        'commands': commands,
        'safety': 'Do not force-push, delete, deploy, or mutate cloud resources from this packet. Open/review a PR after checks pass.',
    }

def write_packet(packet: dict) -> str:
    PACKET_DIR.mkdir(parents=True, exist_ok=True)
    path = PACKET_DIR / f"{safe_name(packet['branch'])}.md"
    lines = [
        f"# Branch Fix Agent Packet: `{packet['branch']}`", '',
        f"Target branch: `{packet['target']}`", f"Action: **{packet['action']}**", f"Specialist: **{packet['primarySpecialist']}**", '',
        packet['safety'], '', '## Specialist focus', '',
    ]
    lines += [f"- {item}" for item in packet.get('specialistFocus', [])] or ['- General branch repair and integration.']
    lines += ['', '## Specialist paths', ''] + [f"- `{item}`" for item in packet.get('specialistPaths', [])]
    lines += ['', '## Domains', '',
    ]
    lines += [f"- {domain}: {count} files" for domain, count in packet['domains'].items()] or ['- No branch-specific domain hits; run general quick profile.']
    lines += ['', '## Commands', ''] + [f"- `{cmd}`" for cmd in packet['commands']]
    lines += ['', '## Sample files', ''] + [f"- `{file}`" for file in packet['sampleFiles']]
    path.write_text('\n'.join(lines) + '\n')
    return str(path.relative_to(ROOT))

def main() -> int:
    parser = argparse.ArgumentParser(description='Generate safe branch-fix agent packets for many blocking branches.')
    parser.add_argument('--target', default='', help='target branch; default current branch')
    parser.add_argument('--max-branches', type=int, default=88, help='maximum branch packets to generate')
    parser.add_argument('--sample-files', type=int, default=40, help='sample files per branch packet')
    parser.add_argument('--scan-origin', action='store_true', help='also ask origin for heads via git ls-remote')
    args = parser.parse_args()
    target = args.target or current_branch()
    packets = []
    for ref in all_refs(args.scan_origin)[:args.max_branches]:
        packet = packet_for(ref, target, args.sample_files)
        packet['packetPath'] = write_packet(packet)
        packets.append(packet)
    payload = {
        'generatedUtc': datetime.now(timezone.utc).isoformat(),
        'targetBranch': target,
        'requestedMaxBranches': args.max_branches,
        'branchPacketCount': len(packets),
        'safeMode': 'Agent packets and workflow only; no branch merge/delete/push/cloud mutation is performed.',
        'packets': packets,
    }
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(json.dumps(payload, indent=2) + '\n')
    lines = ['# Branch Fix Agents', '', f"Generated: `{payload['generatedUtc']}`", '', f"Target branch: `{target}`", '', payload['safeMode'], '', '| Branch | Action | Specialist | Files | Domains | Packet |', '| --- | --- | --- | ---: | --- | --- |']
    for packet in packets:
        domains = ', '.join(f"{k}:{v}" for k, v in packet['domains'].items()) or 'general'
        lines.append(f"| `{packet['branch']}` | {packet['action']} | {packet['primarySpecialist']} | {packet['fileCount']} | {domains} | `{packet['packetPath']}` |")
    lines += ['', '## Extreme workflow entrypoints', '', '- Manual workflow: `.github/workflows/branch-fix-agents.yml`', '- Local: `python3 scripts/agents/branch_fix_agents.py --max-branches 88 --scan-origin`', '- Full setup: `./finish.sh --full`']
    MD.write_text('\n'.join(lines) + '\n')
    print(f"Wrote {OUT.relative_to(ROOT)}")
    print(f"Wrote {MD.relative_to(ROOT)}")
    print(f"Wrote {len(packets)} packet(s) under {PACKET_DIR.relative_to(ROOT)}")
    return 0

if __name__ == '__main__':
    raise SystemExit(main())
