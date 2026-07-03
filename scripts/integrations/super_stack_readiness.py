#!/usr/bin/env python3
from __future__ import annotations
import json,shutil
from datetime import datetime, timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'reports/integrations/super-stack-readiness.json'
MD=ROOT/'reports/integrations/super-stack-readiness.md'
DOMAINS=[
 ('csharp-gui-server-vault',['src/core/','src/gui/','src/server/','src/vault/','HELIOS.Platform.csproj'],['dotnet']),
 ('fsharp-analytics',['src/analytics/','tests/analytics/'],['dotnet']),
 ('cpp-xcore-native',['src/native/','CMakeLists.txt'],['cmake']),
 ('python-automation-aihub',['scripts/','ai-integration/'],['python3']),
 ('hermes-fleet-agents',['HERMES_SWIFT_','scripts/agents/','config/hermes-fleet.example.json'],['python3']),
 ('security-preflight-vault',['scripts/security/','src/Security/','installer/security/'],['python3']),
 ('azure-cloud-shell',['infra/azure/','scripts/azure/','cloud-integration/'],['az','python3']),
 ('github-codex-ci',['.github/workflows/','scripts/codex/','.github/'],['gh','python3']),
 ('super-gui-dashboard',['scripts/dashboard/','status-site/','src/gui/'],['python3']),
 ('branch-automerge-planning',['scripts/analysis/merge_prune_recommendations.py','reports/branch-intelligence/'],['git','python3']),
]

def exists_token(token):
    matches=list(ROOT.glob(token)) if any(ch in token for ch in '*?[') else []
    return bool(matches) or any(ROOT.glob(token+'**/*')) if token.endswith('/') else (ROOT/token).exists() or bool(matches)

def domain_status(name, paths, tools):
    path_hits=[p for p in paths if exists_token(p)]
    tool_status={tool: shutil.which(tool) is not None for tool in tools}
    ok=bool(path_hits) and all(tool_status.values())
    return {'name':name,'ok':ok,'pathsPresent':path_hits,'pathsMissing':[p for p in paths if p not in path_hits],'tools':tool_status}

def task_for(domain):
    missing_tools=[tool for tool,ok in domain['tools'].items() if not ok]
    if missing_tools:
        return {'area':domain['name'],'command':'scripts/setup/bootstrap-local-tools.sh','reason':'missing tools: '+', '.join(missing_tools)}
    if domain['pathsMissing']:
        return {'area':domain['name'],'command':'python3 scripts/apply/generate_finish_tasks.py','reason':'missing paths/modules: '+', '.join(domain['pathsMissing'][:3])}
    return {'area':domain['name'],'command':'python3 scripts/build_graph/build_graph.py run --tag '+domain['name'].split('-')[0],'reason':'ready for deeper validation'}

def main():
    domains=[domain_status(*item) for item in DOMAINS]
    fixes=[task_for(d) for d in domains if not d['ok']]
    payload={'generatedUtc':datetime.now(timezone.utc).isoformat(),'ok':all(d['ok'] for d in domains),'domains':domains,'nextFixes':fixes,'oneCommand':'./finish.sh --full','manualCloudAuth':['gh auth login','az login','export HELIOS_AZURE_RESOURCE_GROUP=helios-dev-rg']}
    OUT.parent.mkdir(parents=True,exist_ok=True); OUT.write_text(json.dumps(payload,indent=2)+'\n')
    lines=['# HELIOS Super Stack Readiness','',f"Generated: `{payload['generatedUtc']}`",'',f"Status: {'PASS' if payload['ok'] else 'NEEDS SETUP'}",'',f"One command: `{payload['oneCommand']}`",'', '| Domain | Status | Tools | Present paths | Missing paths |','| --- | --- | --- | --- | --- |']
    for d in domains:
        tools=', '.join(f"{k}:{'✅' if v else '⚠️'}" for k,v in d['tools'].items())
        lines.append(f"| {d['name']} | {'✅' if d['ok'] else '⚠️'} | {tools} | `{', '.join(d['pathsPresent'])}` | `{', '.join(d['pathsMissing'])}` |")
    if fixes:
        lines += ['','## Next fixes','','| Area | Command | Reason |','| --- | --- | --- |']+[f"| {f['area']} | `{f['command']}` | {f['reason']} |" for f in fixes]
    lines += ['','## Manual cloud auth','']+[f"- `{cmd}`" for cmd in payload['manualCloudAuth']]
    MD.write_text('\n'.join(lines)+'\n')
    print(f"Wrote {OUT.relative_to(ROOT)}"); print(f"Wrote {MD.relative_to(ROOT)}")
if __name__=='__main__': main()
