#!/usr/bin/env python3
from __future__ import annotations
import json, subprocess
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'reports/learning/code-learning-atlas.json'
MD = ROOT / 'reports/learning/code-learning-atlas.md'
DOMAINS = [
    {
        'id': 'csharp-system-gui-server-vault',
        'title': 'C# system: GUI, core, server, vault, contracts',
        'patterns': ['.cs', '.csproj', '.slnx', 'src/core/', 'src/gui/', 'src/Security/'],
        'bestFor': 'WinUI/MAUI-style UI, backend orchestration, contracts, security/vault boundaries, cloud sync, plugins, themes.',
        'learn': ['ViewModels and Views in src/gui/MonadoBlade.GUI', 'BackendServices and Integration in src/core/HELIOS.Platform', 'SecurityValidator and tests', 'Contracts separation', 'wwwroot/dashboard integration'],
        'commands': ['dotnet build HELIOS.Platform.slnx', 'dotnet test src/tests/HELIOS.Platform.Tests/HELIOS.Platform.Tests.csproj'],
    },
    {
        'id': 'cpp-xcore-performance',
        'title': 'C++ / XCore native performance backend',
        'patterns': ['.cpp', '.hpp', '.h', 'src/native/', 'CMakeLists.txt'],
        'bestFor': 'Native acceleration, hot-path performance, XCore bridge work, low-level platform integration.',
        'learn': ['CMake project layout', 'native performance entrypoints', 'interop boundary with C#', 'build artifacts under .build/native', 'profiling-ready code paths'],
        'commands': ['cmake -S src/native/HELIOS.Native.Performance -B .build/native'],
    },
    {
        'id': 'fsharp-analytics-math-prediction',
        'title': 'F# analytics, math, statistics, prediction',
        'patterns': ['.fs', '.fsproj', 'src/analytics/', 'tests/analytics/'],
        'bestFor': 'Math-heavy analytics, prediction models, immutable/statistical pipelines, correctness-oriented tests.',
        'learn': ['Models', 'Prediction', 'Statistics', 'analytics tests', 'parallel/math use cases'],
        'commands': ['dotnet test tests/analytics/HELIOS.Analytics.FSharp.Tests/HELIOS.Analytics.FSharp.Tests.fsproj'],
    },
    {
        'id': 'python-automation-aihub-agents',
        'title': 'Python automation, AIHub, agents, autorunner',
        'patterns': ['.py', 'scripts/', 'ai-integration/'],
        'bestFor': 'Repository automation, build graph orchestration, reports, readiness, AIHub/Codex task generation, safe setup.',
        'learn': ['build_graph runner', 'finish apply sequence', 'branch fix agents', 'readiness reports', 'dashboard generation'],
        'commands': ['python3 scripts/build_graph/build_graph.py run --profile quick --changed-only', './finish.sh --full'],
    },
    {
        'id': 'yaml-github-workflows-codex',
        'title': 'YAML GitHub workflows, Codex, MCP-style automation',
        'patterns': ['.yml', '.yaml', '.github/', 'scripts/codex/', 'github-mcp'],
        'bestFor': 'CI/CD, scheduled branch repair agents, Codex packet generation, safe PR/report automation.',
        'learn': ['branch-fix-agents workflow', 'build-graph workflow', 'control plane workflows', 'workflow validation', 'Codex task packet generation'],
        'commands': ['python3 scripts/control/validate_workflows.py', 'python3 scripts/codex/generate-codex-tasks.py'],
    },
    {
        'id': 'bicep-azure-super-cloud-vault',
        'title': 'Bicep/Azure super cloud, Key Vault, what-if',
        'patterns': ['.bicep', 'infra/azure/', 'scripts/azure/', 'azure', 'keyvault'],
        'bestFor': 'Safe cloud setup, Azure CLI readiness, Bicep modules, Key Vault, storage/network/observability, what-if planning.',
        'learn': ['infra/azure/main.bicep', 'Key Vault module', 'network/storage modules', 'azure connection pipeline', 'what-if wrapper'],
        'commands': ['python3 scripts/azure/azure_connection_pipeline.py --stage all', 'python3 scripts/azure/azure_what_if.py'],
    },
    {
        'id': 'hermes-xcore-deep-agents',
        'title': 'Hermes/Fleet/XCore deep agents',
        'patterns': ['hermes', 'HERMES', 'scripts/agents/', 'config/hermes-fleet', 'xcore', 'XCore'],
        'bestFor': 'Fleet readiness, agent specialization, XCore coordination, dry-run production safety, branch agent packets.',
        'learn': ['hermes fleet readiness', 'agent specializations', 'branch fix packets', 'deep agent readiness', 'Hermes Swift plans'],
        'commands': ['python3 scripts/agents/hermes_fleet_readiness.py', 'python3 scripts/agents/agent_specialization_matrix.py', 'python3 scripts/agents/branch_fix_agents.py --max-branches 88'],
    },
    {
        'id': 'super-gui-command-center',
        'title': 'Super GUI command center and local server',
        'patterns': ['scripts/dashboard/', 'scripts/web/', 'status-site/', 'src/gui/'],
        'bestFor': 'Human-facing command center, report links, one-click commands, local server rebuild/port flow.',
        'learn': ['generate-gui action list', 'status-site report links', 'helios-web local server', 'GUI/source alignment', 'dashboard cards'],
        'commands': ['python3 scripts/dashboard/generate-gui.py', 'python3 scripts/web/helios-web.py'],
    },
    {
        'id': 'deep-learning-cross-llm',
        'title': 'Deep learning / cross-LLM AI integration',
        'patterns': ['config/ai-services/', 'ai-integration/', 'openai', 'chatgpt', 'codex', 'claude', 'anthropic'],
        'bestFor': 'Provider routing, OpenAI/Azure OpenAI/Claude/Codex planning, local secret templates, AI coordination docs.',
        'learn': ['ai-services config', 'service weights', 'API key templates', 'ChatGPT prompts', 'Codex generation templates'],
        'commands': ['python3 scripts/integrations/full_integration_matrix.py', 'python3 scripts/integrations/super_stack_readiness.py'],
    },
    {
        'id': 'security-preflight-vault',
        'title': 'Security preflight and vault safety',
        'patterns': ['scripts/security/', 'security', 'vault', 'secret', 'apply_gate', 'config/security-preflight-allowlist.json'],
        'bestFor': 'Secret prevention, safe apply gates, allowlist triage, production mutation review.',
        'learn': ['secret preflight', 'apply gate preflight', 'allowlist config', 'vault terminology', 'strict-mode path'],
        'commands': ['python3 scripts/security/secret_preflight.py --strict', 'python3 scripts/security/apply_gate_preflight.py --strict'],
    },
]

def files() -> list[str]:
    proc = subprocess.run(['rg', '--files'], cwd=ROOT, text=True, capture_output=True)
    return [line for line in proc.stdout.splitlines() if line]

def score(path: str, patterns: list[str]) -> int:
    low = path.lower()
    return sum(3 if p.endswith('/') and p.lower() in low else 1 for p in patterns if p.lower() in low)

def sample(domain: dict, all_files: list[str], limit: int) -> list[dict]:
    rows = []
    for path in all_files:
        s = score(path, domain['patterns'])
        if s:
            rows.append({'path': path, 'score': s, 'kind': Path(path).suffix.lower() or 'directory/file'})
    rows.sort(key=lambda r: (-r['score'], r['path']))
    return rows[:limit]

def main() -> int:
    all_files = files()
    domains = []
    for domain in DOMAINS:
        examples = sample(domain, all_files, 50)
        domains.append({**domain, 'exampleCount': len(examples), 'examples': examples})
    payload = {'generatedUtc': datetime.now(timezone.utc).isoformat(), 'ok': True, 'domainCount': len(domains), 'maxExamplesPerDomain': 50, 'domains': domains}
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(json.dumps(payload, indent=2) + '\n')
    lines = ['# HELIOS Code Learning Atlas', '', f"Generated: `{payload['generatedUtc']}`", '', 'This report gives up to **50 high-signal files per code/domain type** plus what each area is best for and what to run.', '']
    for domain in domains:
        lines += [f"## {domain['title']}", '', f"Best for: {domain['bestFor']}", '', '### Learn first', '']
        lines += [f"- {item}" for item in domain['learn']]
        lines += ['', '### Commands', ''] + [f"- `{cmd}`" for cmd in domain['commands']]
        lines += ['', f"### Top files/signals ({domain['exampleCount']} of 50)", '']
        lines += [f"- `{row['path']}` — {row['kind']}" for row in domain['examples']] or ['- No files detected yet.']
        lines += ['']
    MD.write_text('\n'.join(lines) + '\n')
    print(f"Wrote {OUT.relative_to(ROOT)}")
    print(f"Wrote {MD.relative_to(ROOT)}")
    return 0

if __name__ == '__main__':
    raise SystemExit(main())
