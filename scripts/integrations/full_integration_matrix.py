#!/usr/bin/env python3
from __future__ import annotations
import json, os, shutil, socket
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'reports/integrations/full-integration-matrix.json'
MD = ROOT / 'reports/integrations/full-integration-matrix.md'

DOMAINS = [
    {
        'id': 'csharp-core-gui-server-vault',
        'title': 'C# core + full GUI + server/vault framework',
        'paths': ['HELIOS.Platform.slnx', 'src/core/HELIOS.Platform', 'src/gui/MonadoBlade.GUI', 'src/Security', 'src/core/HELIOS.Platform/wwwroot'],
        'tools': ['dotnet'],
        'commands': ['dotnet build HELIOS.Platform.slnx'],
        'next': './finish.sh --full',
    },
    {
        'id': 'cpp-xcore-native-performance',
        'title': 'C++ XCore/native performance backend',
        'paths': ['src/native/HELIOS.Native.Performance/CMakeLists.txt', 'src/native/HELIOS.Native.Performance'],
        'tools': ['cmake'],
        'commands': ['cmake -S src/native/HELIOS.Native.Performance -B .build/native'],
        'next': 'cmake -S src/native/HELIOS.Native.Performance -B .build/native',
    },
    {
        'id': 'fsharp-analytics-predictions',
        'title': 'F# analytics, prediction, statistics lanes',
        'paths': ['src/analytics/HELIOS.Analytics.FSharp', 'tests/analytics/HELIOS.Analytics.FSharp.Tests'],
        'tools': ['dotnet'],
        'commands': ['dotnet test tests/analytics/HELIOS.Analytics.FSharp.Tests/HELIOS.Analytics.FSharp.Tests.fsproj'],
        'next': 'python3 scripts/build_graph/build_graph.py run --node analytics-tests --max-workers 2',
    },
    {
        'id': 'python-automation-aihub-autorrunner',
        'title': 'Python automation, AIHub integration, autorunner setup',
        'paths': ['scripts/build_graph/build_graph.py', 'scripts/apply/finish_readiness_apply.py', 'scripts/setup/finish-easy-setup.sh', 'ai-integration'],
        'tools': ['python3'],
        'commands': ['./finish.sh --full'],
        'next': './finish.sh --full',
    },
    {
        'id': 'yaml-github-codex-mcp-control',
        'title': 'GitHub YAML workflows, Codex/MCP-style control, safe PR automation',
        'paths': ['.github/workflows', 'config/github-mcp.example.json', 'scripts/codex/generate-codex-tasks.py', 'scripts/github/github-inventory.py'],
        'tools': ['git', 'gh', 'python3'],
        'commands': ['gh auth login', 'python3 scripts/codex/generate-codex-tasks.py'],
        'next': 'gh auth login',
    },
    {
        'id': 'bicep-azure-cloud-vault',
        'title': 'Azure CLI, Bicep, Key Vault, Cloud Shell readiness',
        'paths': ['infra/azure/main.bicep', 'infra/azure/modules/keyvault.bicep', 'infra/azure/parameters/dev.json', 'scripts/azure/azure_connection_pipeline.py'],
        'tools': ['az', 'python3'],
        'commands': ['az login', 'python3 scripts/azure/azure_connection_pipeline.py --stage all', 'python3 scripts/azure/azure_what_if.py'],
        'next': 'az login && export HELIOS_AZURE_RESOURCE_GROUP=helios-dev-rg',
    },
    {
        'id': 'hermes-fleet-xcore-agent-gui',
        'title': 'Hermes/Fleet, XCore specialist, agent GUI setup',
        'paths': ['scripts/agents/hermes_fleet_readiness.py', 'config/hermes-fleet.example.json', 'HERMES_SWIFT_COMPLETE_DELIVERY_INDEX.md', 'HERMES_SWIFT_OPTIMIZATION_EXECUTION_PLAN.md', 'src/gui/MonadoBlade.GUI', 'src/native/HELIOS.Native.Performance'],
        'tools': ['python3'],
        'commands': ['python3 scripts/agents/hermes_fleet_readiness.py', 'python3 scripts/integrations/deep_agent_readiness.py'],
        'next': 'cp config/hermes-fleet.example.json config/hermes-fleet.local.json',
    },
    {
        'id': 'cross-llm-ai-combo',
        'title': 'Cross-LLM AI combo: OpenAI/Azure OpenAI/Claude/Codex coordination',
        'paths': ['config/ai-services/ai-services-config.json', 'config/ai-services/service-weights.json', 'config/ai-services/api-keys.template.env', 'ai-integration/chatgpt-integration', 'ai-integration/codex-integration'],
        'tools': ['python3'],
        'env': ['OPENAI_API_KEY', 'AZURE_OPENAI_ENDPOINT', 'AZURE_OPENAI_API_KEY', 'ANTHROPIC_API_KEY'],
        'commands': ['cp config/ai-services/api-keys.template.env .env.local', 'python3 scripts/integrations/super_stack_readiness.py'],
        'next': 'configure provider keys in a local ignored environment file; do not commit secrets',
    },
    {
        'id': 'super-gui-local-server-port',
        'title': 'Super GUI dashboard + local web server port',
        'paths': ['scripts/dashboard/generate-gui.py', 'scripts/web/helios-web.py', 'status-site/index.html'],
        'tools': ['python3'],
        'ports': [8787],
        'commands': ['python3 scripts/dashboard/generate-gui.py', 'python3 scripts/web/helios-web.py'],
        'next': 'python3 scripts/dashboard/generate-gui.py',
    },
]

def exists_token(token: str) -> bool:
    path = ROOT / token
    if any(ch in token for ch in '*?['):
        return bool(list(ROOT.glob(token)))
    return path.exists()

def tool_status(tools):
    return {tool: shutil.which(tool) is not None for tool in tools}

def port_state(port: int) -> dict:
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as sock:
        bound = sock.connect_ex(('127.0.0.1', port)) == 0
    return {'port': port, 'bound': bound, 'status': 'in-use' if bound else 'free'}

def analyze(domain):
    present = [p for p in domain.get('paths', []) if exists_token(p)]
    missing = [p for p in domain.get('paths', []) if p not in present]
    tools = tool_status(domain.get('tools', []))
    env = {name: bool(os.environ.get(name)) for name in domain.get('env', [])}
    ports = [port_state(p) for p in domain.get('ports', [])]
    required_ok = not missing and all(tools.values())
    env_ready = all(env.values()) if env else True
    # Local ports are informational: a free port means ready to start; bound means server may already be running.
    status = 'ready' if required_ok and env_ready else ('configured-no-secrets' if required_ok and not env_ready else 'needs-setup')
    return {
        'id': domain['id'], 'title': domain['title'], 'status': status,
        'pathsPresent': present, 'pathsMissing': missing, 'tools': tools, 'env': env, 'ports': ports,
        'commands': domain.get('commands', []), 'nextFix': domain.get('next', './finish.sh --full'),
    }

def fix_for(row):
    missing_tools = [k for k, v in row['tools'].items() if not v]
    missing_env = [k for k, v in row['env'].items() if not v]
    if missing_tools:
        return {'area': row['id'], 'command': 'scripts/setup/bootstrap-local-tools.sh', 'reason': 'missing tools: ' + ', '.join(missing_tools)}
    if row['pathsMissing']:
        return {'area': row['id'], 'command': row['nextFix'], 'reason': 'missing paths/config: ' + ', '.join(row['pathsMissing'][:4])}
    if missing_env:
        return {'area': row['id'], 'command': row['nextFix'], 'reason': 'local secrets/env not configured: ' + ', '.join(missing_env)}
    return None

def main() -> int:
    rows = [analyze(d) for d in DOMAINS]
    fixes = [f for f in (fix_for(r) for r in rows) if f]
    payload = {
        'generatedUtc': datetime.now(timezone.utc).isoformat(),
        'ok': all(r['status'] in {'ready', 'configured-no-secrets'} for r in rows),
        'oneCommand': './finish.sh --full',
        'safeMode': 'Report/setup automation only; cloud, GitHub, vault, and LLM provider operations require explicit human auth and local secrets.',
        'domains': rows,
        'nextFixes': fixes,
        'manualAuth': ['gh auth login', 'az login', 'export HELIOS_AZURE_RESOURCE_GROUP=helios-dev-rg'],
    }
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(json.dumps(payload, indent=2) + '\n')
    lines = [
        '# HELIOS Full Integration Matrix', '', f"Generated: `{payload['generatedUtc']}`", '',
        f"Status: {'PASS' if payload['ok'] else 'NEEDS SETUP'}", f"One command: `{payload['oneCommand']}`", '', payload['safeMode'], '',
        '| Domain | Status | Tools | Env | Ports | Present paths | Missing paths |',
        '| --- | --- | --- | --- | --- | --- | --- |',
    ]
    for r in rows:
        tools = ', '.join(f"{k}:{'✅' if v else '⚠️'}" for k, v in r['tools'].items()) or 'n/a'
        env = ', '.join(f"{k}:{'✅' if v else '⚠️'}" for k, v in r['env'].items()) or 'n/a'
        ports = ', '.join(f"{p['port']}:{p['status']}" for p in r['ports']) or 'n/a'
        lines.append(f"| {r['title']} | {r['status']} | {tools} | {env} | {ports} | `{', '.join(r['pathsPresent'])}` | `{', '.join(r['pathsMissing'])}` |")
    lines += ['', '## Command lanes', '']
    for r in rows:
        lines += [f"### {r['title']}"] + [f"- `{cmd}`" for cmd in r['commands']] + ['']
    if fixes:
        lines += ['## Next fixes', '', '| Area | Command | Reason |', '| --- | --- | --- |']
        lines += [f"| {f['area']} | `{f['command']}` | {f['reason']} |" for f in fixes]
    lines += ['', '## Manual auth / secrets', ''] + [f"- `{cmd}`" for cmd in payload['manualAuth']]
    MD.write_text('\n'.join(lines) + '\n')
    print(f"Wrote {OUT.relative_to(ROOT)}")
    print(f"Wrote {MD.relative_to(ROOT)}")
    return 0

if __name__ == '__main__':
    raise SystemExit(main())
