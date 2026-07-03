#!/usr/bin/env python3
from __future__ import annotations
import json, subprocess, sys
from datetime import datetime, timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'reports/integrations/aihub-integration-graph.json'
MD=ROOT/'reports/integrations/aihub-integration-graph.md'

def git_lines(*args):
    try:
        return subprocess.check_output(['git',*args],cwd=ROOT,text=True,stderr=subprocess.DEVNULL).splitlines()
    except Exception:
        return []

def main():
    commits=git_lines('log','--all','--since=3 days ago','--pretty=%h %ad %s','--date=iso-strict')[:40]
    refs=git_lines('for-each-ref','--format=%(refname:short)','refs/heads','refs/remotes')[:80]
    status=git_lines('status','--short')[:60]
    nodes=[
        {'id':'csharp-orchestrator','label':'C# secure orchestration backbone','kind':'framework','files':['src/core/HELIOS.Platform.Contracts/AIHubEngineContracts.cs']},
        {'id':'fsharp-learning','label':'F# learning, prediction, scoring, async queries','kind':'engine','files':['src/analytics/HELIOS.Analytics.FSharp/AIHub/AiHubLearningEngine.fs']},
        {'id':'cpp-native','label':'C++ native performance, memory, vectorized hot paths','kind':'engine','files':['src/native/HELIOS.Native.Performance/include/helios/aihub_native_engine.hpp']},
        {'id':'python-aihub','label':'Python AIHub automation, Linux libraries, provider glue','kind':'integration','files':['scripts/integrations/aihub_full_framework.py','scripts/agents/agent_fleet_autopilot.py']},
        {'id':'hermes-xcore','label':'Hermes/XCore local-cloud self-learning agent fleets','kind':'agents','files':['scripts/agents/agent_fleet_control_catalog.py']},
        {'id':'github-control','label':'GitHub YAML, runners, Codespaces, Pages, wiki, branch testing','kind':'control-plane','files':['.github/workflows/build-graph-automation.yml','scripts/agents/branch_test_autofix_plan.py']},
        {'id':'azure-control','label':'Azure Bicep/ARM, Key Vault, SQL, Cosmos, vector, Fabric/Foundry lanes','kind':'cloud','files':['scripts/azure/azure_connection_pipeline.py','scripts/azure/azure_what_if.py']},
        {'id':'knowledge-ledger','label':'JSON/Markdown/Bicep/YAML absorption ledgers and documentation knowledge','kind':'knowledge','files':['scripts/analysis/document_code_absorption_ranker.py','config/language-decision-variables.json']},
        {'id':'interactive-gui','label':'Reactive dashboard for graph, choices, toggles, reports, setup','kind':'gui','files':['scripts/dashboard/generate-gui.py','status-site/index.html']},
        {'id':'branch-nervous-system','label':'Hermes/XCore branch tester with F# priority scoring and C++ comparison hot paths','kind':'agent-specialty','files':['scripts/agents/branch_test_autofix_plan.py','src/analytics/HELIOS.Analytics.FSharp/AIHub/AiHubLearningEngine.fs','src/native/HELIOS.Native.Performance/include/helios/aihub_native_engine.hpp']}
    ]
    edges=[
        ['csharp-orchestrator','fsharp-learning','score modules/submodules and predict best route'],
        ['csharp-orchestrator','cpp-native','delegate hot paths and memory-sensitive algorithms'],
        ['csharp-orchestrator','python-aihub','launch provider, Linux, API, and local/cloud tooling'],
        ['python-aihub','hermes-xcore','hydrate prompts/tools/MCP/MCR lanes and fleets'],
        ['hermes-xcore','fsharp-learning','send outcomes, XP, cost, quality, speed, and learning signals'],
        ['github-control','knowledge-ledger','record commits, branches, workflow results, wiki/pages docs'],
        ['azure-control','knowledge-ledger','record cloud topology, vault, SQL/Cosmos/vector readiness'],
        ['knowledge-ledger','csharp-orchestrator','feed reusable abstractions and safety gates back into routing'],
        ['interactive-gui','github-control','toggle levels and copy live-safe commands'],
        ['interactive-gui','azure-control','surface cloud setup and what-if lanes'],
        ['branch-nervous-system','fsharp-learning','prioritize branch and commit issues'],
        ['branch-nervous-system','cpp-native','compare large commit/module graphs cheaply'],
        ['branch-nervous-system','csharp-orchestrator','apply clean safe route decisions']
    ]
    mermaid=['flowchart LR']+[f"  {a}[{a}] -->|{label}| {b}[{b}]" for a,b,label in edges]
    payload={'generatedUtc':datetime.now(timezone.utc).isoformat(),'nodes':nodes,'edges':[{'from':a,'to':b,'label':l} for a,b,l in edges],'mermaid':'\n'.join(mermaid),'recentCommits':commits,'visibleRefs':refs,'repoIssues':status,'guiButtons':['Bootstrap tools','Generate integration graph','Run branch nervous system','Run full build graph','Refresh dashboard','Toggle live flags','Open GitHub wiki/pages plan','Open Azure what-if plan'],'setupCommand':'scripts/setup/agent-runner-easy-setup.sh --agents auto --mode hybrid-cloud --profile full','liveFlagGuide':'Use config/aihub-live-flags.example.json as the template; local-safe automation can run automatically, external push/delete/cloud mutation stays behind explicit scoped live flags, logs, and rollback plans.'}
    OUT.parent.mkdir(parents=True,exist_ok=True); OUT.write_text(json.dumps(payload,indent=2)+'\n')
    lines=['# AIHub Integration Graph','',f"Generated: `{payload['generatedUtc']}`",'', '```mermaid', payload['mermaid'], '```','','## Nodes']
    lines += [f"- **{n['id']}** ({n['kind']}): {n['label']} — `{', '.join(n['files'])}`" for n in nodes]
    lines += ['','## Recent commits']+[f"- `{c}`" for c in commits[:20]]
    lines += ['','## Repository issue/status signals']+[f"- `{i}`" for i in status[:30]]
    lines += ['','## GUI buttons']+[f"- {b}" for b in payload['guiButtons']]
    lines += ['','## Setup',f"- `{payload['setupCommand']}`",f"- {payload['liveFlagGuide']}"]
    MD.write_text('\n'.join(lines)+'\n')
    print(f'Wrote {OUT.relative_to(ROOT)}')
    print(f'Wrote {MD.relative_to(ROOT)}')
if __name__=='__main__': main()
