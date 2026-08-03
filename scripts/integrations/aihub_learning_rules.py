#!/usr/bin/env python3
from __future__ import annotations
import json
from datetime import datetime, timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'reports/integrations/aihub-learning-rules.json'
MD=ROOT/'reports/integrations/aihub-learning-rules.md'
ENGINES=['csharp-orchestrator','cpp-native','fsharp-learning','python-aihub','yaml-workflow','bicep-cloud','json-ledger','markdown-knowledge']
COMBOS=[
 {'id':'local-fast-cheap','llms':['ollama-local','local-web-model'],'agents':['hermes-scout','xcore-worker'],'tools':['filesystem-mcp','sql-vector-mcp'],'bestFor':'cheap iterative scans and learning loops'},
 {'id':'coding-quality','llms':['chatgpt-openai','codex-cli','claude'],'agents':['hermes-builder','xcore-native-optimizer'],'tools':['github-mcp','filesystem-mcp'],'bestFor':'code changes, tests, refactors, native/F# extraction'},
 {'id':'enterprise-cloud','llms':['m365-copilot','azure-foundry','github-copilot'],'agents':['hermes-cloud-operator','xcore-azure-operator'],'tools':['azure-mcp','runner-control-mcp'],'bestFor':'Azure/Bicep/Fabric/Foundry/Purview planning'},
 {'id':'prune-merge-learn','llms':['local-ollama','chatgpt-openai'],'agents':['hermes-reviewer','xcore-learning-scorer'],'tools':['filesystem-mcp','github-mcp'],'bestFor':'branch scoring, merge/prune recommendations, abstract idea capture'},
 {'id':'gui-operator','llms':['github-copilot','chatgpt-openai'],'agents':['hermes-gui-guide','xcore-runner-operator'],'tools':['model-router-mcp','runner-control-mcp'],'bestFor':'dashboard command selection, setup guidance, human-in-the-loop ops'}]
RULES=[
 {'rule':'Every output feeds the hub','detail':'reports, tests, failures, costs, speed, quality, and human choices are JSON/MD ledgers consumed by future routing.'},
 {'rule':'Specialize by evidence','detail':'LLMs, tools, prompts, MCP/MCR servers, and Hermes/XCore roles gain XP only after passing checks or improving cost/speed/quality.'},
 {'rule':'No hard max agents','detail':'fleet size is a function of budget, local/cloud capacity, queue depth, and marginal outcome quality.'},
 {'rule':'Local first, cloud when justified','detail':'prefer local/Ollama/Codespaces/repo runners before paid cloud; escalate when Pareto score is better.'},
 {'rule':'Language engines teach each other','detail':'C# sets contracts, F# scores, C++ accelerates, Python integrates, YAML/Bicep deploys, JSON/MD records lessons.'},
 {'rule':'Policy-gated automation','detail':'local setup, reports, tests, and policy-safe fixes can run automatically; live push/wiki/cloud/branch-delete actions require explicit live flags, scoped credentials, logs, and rollback plans.'}]
ALGORITHMS=['parallel fan-out/fan-in','multi-armed bandit routing','Pareto frontier ranking','Bayesian confidence update','natural-selection prompt promotion','chaos fallback testing','semantic dedupe','vector similarity search','SQL fact ledger scoring','Cosmos event memory','time-series trend learning','test-evidence weighting','novelty vs redundancy scoring','role/tool XP leveling','cost-quality-speed normalization','human feedback reinforcement']

def main():
 payload={'generatedUtc':datetime.now(timezone.utc).isoformat(),'principle':'The AIHub is the learning spine: every LLM/tool/MCP/MCR/Hermes/XCore combo contributes evidence, learns from results, and updates future specialization/routing while C# keeps the control plane and F# scores outcomes. Local safe work is automatic; live external mutation is explicit-flag and credential gated.','engines':ENGINES,'combos':COMBOS,'rules':RULES,'algorithms':ALGORITHMS,'liveFlagsReport':'reports/integrations/aihub-live-flags.md','ledgerTargets':['reports/integrations/*.json','reports/branch-agents/*.json','reports/learning/*.json','reports/build-graph/latest.json','future SQL/Cosmos/vector manifests'],'nextCommand':'scripts/setup/agent-runner-easy-setup.sh --agents 128 --mode hybrid-cloud --profile full'}
 OUT.parent.mkdir(parents=True,exist_ok=True); OUT.write_text(json.dumps(payload,indent=2)+'\n')
 lines=['# AIHub Learning Rules','',f"Generated: `{payload['generatedUtc']}`",'',payload['principle'],'','## Combo catalog','','| Combo | LLMs | Agents | Tools | Best for |','| --- | --- | --- | --- | --- |']
 for c in COMBOS: lines.append(f"| {c['id']} | {', '.join(c['llms'])} | {', '.join(c['agents'])} | {', '.join(c['tools'])} | {c['bestFor']} |")
 lines += ['','## Rules']+[f"- **{r['rule']}**: {r['detail']}" for r in RULES]
 lines += ['','## Algorithms']+[f"- {a}" for a in ALGORITHMS]
 lines += ['','## Engine teaching loop']+[f"- {e}" for e in ENGINES]
 lines += ['','## Next command',f"`{payload['nextCommand']}`"]
 MD.write_text('\n'.join(lines)+'\n')
 print(f'Wrote {OUT.relative_to(ROOT)}'); print(f'Wrote {MD.relative_to(ROOT)}')
 return 0
if __name__=='__main__': raise SystemExit(main())
