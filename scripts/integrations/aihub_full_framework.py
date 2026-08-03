#!/usr/bin/env python3
from __future__ import annotations
import json, subprocess
from datetime import datetime, timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'reports/integrations/aihub-full-framework.json'
MD=ROOT/'reports/integrations/aihub-full-framework.md'
DOMAINS=['cross-llm-routing','hermes-xcore-fleets','azure-hybrid','github-automation','codespaces-runners','local-ports-tunnels','vault-keys','vector-sql-cosmos','fsharp-learning','cpp-hotpaths','csharp-gui-orchestrator','python-aihub-glue','yaml-bicep-json','smart-prune-merge','knowledge-absorption','prompt-tool-evolution']
ALGORITHMS=['multi-armed-bandit provider choice','Pareto cost-speed-quality ranking','semantic dedupe','branch risk scoring','module/submodule fitness','natural-selection prompt promotion','chaos fallback testing','vector similarity','SQL fact ledger','Cosmos event memory','Bayesian confidence','time-series trend scoring','test evidence weighting','novelty/redundancy split','XP leveling']
COMMANDS=['scripts/setup/bootstrap-local-tools.sh','python3 scripts/integrations/aihub_super_shell.py','python3 scripts/integrations/aihub_connectivity_guide.py','python3 scripts/integrations/aihub_learning_rules.py','python3 scripts/agents/agent_fleet_control_catalog.py --agents 128 --mode hybrid-cloud','python3 scripts/agents/agent_fleet_autopilot.py --agents 128 --mode hybrid-cloud','python3 scripts/analysis/document_code_absorption_ranker.py','python3 scripts/analysis/legacy_algorithm_recovery.py','python3 scripts/analysis/knowledge_absorption_engine.py','python3 scripts/build_graph/build_graph.py run --profile full --max-workers 2','python3 scripts/dashboard/generate-gui.py']

def git_recent():
 p=subprocess.run(['git','log','--all','--since=3 days ago','--date=iso-strict','--pretty=format:%h|%ad|%s'],cwd=ROOT,text=True,capture_output=True)
 return [{'hash':a,'date':b,'subject':c} for line in p.stdout.splitlines() for a,b,c in [line.split('|',2)]]

def main():
 payload={'generatedUtc':datetime.now(timezone.utc).isoformat(),'principle':'AIHub Full Framework ties every safe setup/report/control layer together: cross-LLM providers, Hermes/XCore fleets, Azure hybrid, GitHub/Codespaces/runners/wiki, local ports/tunnels, vault field guidance, vector/SQL/Cosmos memory plans, language engines, smart pruning, smart commits, prompt/tool evolution, and dashboard launch controls.','domains':DOMAINS,'algorithms':ALGORITHMS,'commands':COMMANDS,'recentCommits':git_recent(),'guiText':'Run the one-command setup, review generated reports, fill vault/env fields outside the static GUI, authenticate GitHub/Azure only when ready, then use the dashboard copy buttons to launch Hermes/XCore fleet units and safe checks.','launchCommand':'scripts/setup/agent-runner-easy-setup.sh --agents 128 --mode hybrid-cloud --profile full','noMaxAgents':'No hard maximum: Hermes/XCore fleet counts are chosen by budget, local/cloud capacity, queue depth, and measured outcome quality.', 'integrationCoverage':{'target':'100-percent-connected','covered':['cross LLM','Hermes/XCore fleets','AIHub shell','C# GUI','C++ hot paths','F# learning','Python automation','YAML workflows','Bicep/ARM cloud','JSON ledgers','Markdown/JSTOR knowledge','GitHub/Codespaces/runners','Azure/Fabric/Foundry/Purview/Entra','SQL/Cosmos/vector','MCP/MCR servers','local ports/tunnels','smart prune/merge','branch test/autofix','legacy algorithm recovery']}, 'safeBoundary':'This framework does not store keys. Local reports/tests/setup and policy-safe local fixes can be automatic; live push/wiki/pages/branch-delete/Azure mutation requires explicit live flags, scoped credentials, logs, and rollback plans.'}
 OUT.parent.mkdir(parents=True,exist_ok=True); OUT.write_text(json.dumps(payload,indent=2)+'\n')
 lines=['# AIHub Full Framework','',f"Generated: `{payload['generatedUtc']}`",'',payload['principle'],'','## GUI text',payload['guiText'],'','## Launch command',f"`{payload['launchCommand']}`",'','## Domains']+[f"- {d}" for d in DOMAINS]
 lines += ['','## Learning / optimization algorithms']+[f"- {a}" for a in ALGORITHMS]
 lines += ['','## Run-everything command sequence']+[f"- `{c}`" for c in COMMANDS]
 lines += ['','## Last three days visible commits']+[f"- `{c['hash']}` {c['date']} — {c['subject']}" for c in payload['recentCommits'][:80]]
 lines += ['','## Safety boundary',payload['safeBoundary']]
 MD.write_text('\n'.join(lines)+'\n')
 print(f'Wrote {OUT.relative_to(ROOT)}'); print(f'Wrote {MD.relative_to(ROOT)}')
 return 0
if __name__=='__main__': raise SystemExit(main())
