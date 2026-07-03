#!/usr/bin/env python3
from __future__ import annotations
import argparse, json, shutil, subprocess
from datetime import datetime, timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'reports/branch-agents/agent-fleet-control-catalog.json'
MD=ROOT/'reports/branch-agents/agent-fleet-control-catalog.md'

CAPS={
 'csharp-core':['AI access policy','secure orchestration','JRPG/super GUI shell','typed contracts','plugin loader','provider facade','vault facade','GitHub facade','Azure facade','MCP/MCR server facade','local web model facade','agent command bus','fleet status panels','XP bar UI','cost/speed outcome view','branch repair UI','runner setup UI','Codespaces UI','wiki publish UI','audit log','auth boundary','Entra boundary','Purview boundary','Fabric boundary','Foundry boundary','Microsoft 365 Copilot facade','Copilot connector','Ollama connector','Claude connector','Codex CLI connector'],
 'python-aihub':['Hermes local launcher','Hermes cloud launcher','XCore local launcher','XCore cloud launcher','fleet composer','provider fallback','OpenAI adapter','Claude adapter','Ollama adapter','Copilot CLI adapter','Codex CLI adapter','GitHub CLI adapter','Azure CLI adapter','MCP/MCR adapter','WSL2 helper','Windows helper','Linux helper','runner bootstrap','Codespaces bootstrap','wiki dry-run','cost optimizer','speed optimizer','outcome logger','XP ledger writer','model benchmark importer','SQL connector glue','Cosmos DB connector glue','vector DB connector glue','DQB/query broker glue','Fabric connector glue','Foundry connector glue','agent memory import','prompt pack exporter','sandbox setup'],
 'yaml-workflow':['workflow_dispatch controls','schedule controls','branch packet artifacts','runner matrix','self-hosted labels','Codespaces prebuild','safe permissions','OIDC auth plan','job summaries','artifact retention','concurrency groups','cache keys','tool bootstrap','dotnet lane','cmake lane','python lane','F# lane','Bicep lane','secret preflight lane','apply gate lane','wiki dry-run lane','PR comment lane','release gate','rollback lane','agent fleet lane','Hermes lane','XCore lane','multi-LLM lane','cost report lane','dashboard publish lane','marketplace validation lane','Codex task lane'],
 'bicep-cloud':['Key Vault','App Configuration','Storage','Cosmos DB','SQL Database','Container Apps','Azure Functions','Foundry project','AI Search/vector index','Log Analytics','Application Insights','Managed Identity','Role assignments','Private endpoints','VNet','Container Registry','Bicep modules','ARM compatibility','what-if','parameters','environment tags','cost tags','resource locks','diagnostic settings','cloud shell plan','runner identity','service principal fallback','policy assignments','Purview connection','Fabric workspace plan','M365 integration notes','deployment stack plan'],
 'markdown-jstor':['agent design docs','Hermes history','XCore history','JSTOR research notes','wiki page plan','abstract ideas','merge rationale','prune rationale','test evidence','benchmark narrative','cost/speed outcome','provider comparison','local/cloud tradeoff','JRPG GUI story','XP system rules','natural-selection notes','chaos experiment notes','6D correlation notes','vector/SQL schema docs','Cosmos DB docs','DQB docs','Foundry docs','Fabric docs','Purview docs','Entra docs','M365 Copilot docs','marketplace docs','runner docs','Codespaces docs','sandbox docs','takeover safety notes'],
 'json-config':['agent personas','fleet templates','XP ledgers','provider price table','provider speed table','outcome score table','runner labels','Codespaces config','tool registry','skill registry','custom instruction bundles','model routing rules','local capacity table','cloud capacity table','budget caps','SQL schema manifest','Cosmos manifest','vector index manifest','DQB route manifest','Bicep parameter manifest','GitHub permission manifest','Azure auth manifest','MCP/MCR server manifest','marketplace manifest','dashboard cards','branch packet manifests','merge/prune thresholds','natural-selection weights','chaos experiment weights','6D correlation weights','test matrix data','report index']
}
PROVIDERS=['local-ollama','local-web-model','openai','claude','github-copilot','microsoft-365-copilot','codex-cli','github-cli','azure-cli','azure-foundry','fabric','purview','entra','mcp-mcr-server','marketplace-agent']

TOOLS=['git','gh','az','dotnet','cmake','python3','docker','pwsh','wsl','ollama','codex','claude']
MCP_SERVERS=[
 {'id':'github-mcp','scope':'repos, issues, PRs, actions, wiki dry-runs','local':True,'cloud':True},
 {'id':'azure-mcp','scope':'resource inventory, what-if, Key Vault/Fabric/Foundry/Purview planning','local':True,'cloud':True},
 {'id':'filesystem-mcp','scope':'repo search, module/submodule maps, report writes','local':True,'cloud':False},
 {'id':'sql-vector-mcp','scope':'SQL, vector collections, Cosmos DB, DQB/query broker manifests','local':True,'cloud':True},
 {'id':'model-router-mcp','scope':'Ollama/local web models/OpenAI/Claude/Copilot/Codex routing','local':True,'cloud':True},
 {'id':'runner-control-mcp','scope':'GitHub runner labels, Codespaces, sandbox job catalogs','local':True,'cloud':True}
]
PROMPT_PACKS=[
 {'id':'hermes-builder','goal':'turn branch packets into safe code plans with tests','engines':['python-aihub','csharp-core','fsharp-learning']},
 {'id':'xcore-optimizer','goal':'move hot paths into C++ and route via XCore agents','engines':['xcore-agent','cpp-native','csharp-core']},
 {'id':'fleet-pruner','goal':'rank duplicate/redundant branches and keep abstract ideas','engines':['fsharp-learning','markdown-jstor','json-config']},
 {'id':'super-gui-operator','goal':'drive dashboard actions and explain status with XP bars','engines':['csharp-core','python-aihub','yaml-workflow']},
 {'id':'cloud-what-if-operator','goal':'plan Azure/Bicep/Fabric/Foundry/Purview changes without mutation','engines':['bicep-cloud','yaml-workflow','json-config']},
 {'id':'local-first-router','goal':'prefer Ollama/local web models, escalate only when outcome/cost says so','engines':['python-aihub','fsharp-learning','json-config']}
]
SPECIALIZATION_VARIABLES=['agent-xp','fleet-xp','role-depth','tool-access','prompt-pack-fit','mcp-server-fit','local-compute-fit','cloud-compute-fit','runner-fit','codespace-fit','budget-fit','latency-fit','quality-fit','safety-fit','rollback-fit','branch-domain-fit','language-engine-fit','model-context-fit','memory-fit','vector-fit','sql-fit','cosmos-fit','fabric-fit','foundry-fit','purview-fit','entra-fit','m365-copilot-fit','ollama-fit','codex-fit','claude-fit','github-cli-fit','azure-cli-fit','wiki-fit','marketplace-fit','sandbox-fit','learning-rate','natural-selection-score','chaos-score','novelty-score','redundancy-score','test-evidence-score','human-review-score']
AGENT_TYPES=['hermes-scout','hermes-builder','hermes-reviewer','hermes-codex-operator','hermes-cloud-operator','hermes-vault-guide','hermes-gui-guide','xcore-worker','xcore-native-optimizer','xcore-model-router','xcore-runner-operator','xcore-codespaces-operator','xcore-azure-operator','xcore-learning-scorer','xcore-marketplace-operator']

def cmd_ok(cmd):
 return shutil.which(cmd) is not None

def git_refs():
 p=subprocess.run(['git','for-each-ref','--format=%(refname:short)|%(objectname:short)|%(subject)','refs/heads','refs/remotes'],cwd=ROOT,text=True,capture_output=True)
 rows=[]
 for line in p.stdout.splitlines():
  parts=line.split('|',2)
  if len(parts)==3: rows.append({'ref':parts[0],'commit':parts[1],'subject':parts[2]})
 return rows

def fleet_templates(target_agents=0, preferred_mode='auto'):
 sizes=[1,2,4,8,16,32,64]
 if target_agents and target_agents not in sizes: sizes.append(target_agents)
 out=[]
 for size in sorted(set(sizes)):
  mode=preferred_mode if preferred_mode!='auto' else ('local-first' if size<=8 else 'hybrid-cloud')
  out.append({'size':size,'mode':mode,'note':'No hard max: choose by budget, CPU/RAM/GPU, token budget, and outcome/cost score. Scale up/down dynamically from local to Codespaces/runners/cloud.','agents':AGENT_TYPES[:min(len(AGENT_TYPES),max(3,size//2+2))],'providers':PROVIDERS[:min(len(PROVIDERS),max(4,size//2+3))],'xpModel':{'agentXp':'per-task quality/speed/safety','fleetXp':'team outcome/cost/learning','levelUp':'unlock tools/prompts/MCP servers after passing checks'}})
 return out

def setup_steps(target_agents=0, preferred_mode='auto'):
 agent_arg=f' --agents {target_agents}' if target_agents else ''
 mode_arg=f' --mode {preferred_mode}' if preferred_mode!='auto' else ''
 return [
  {'area':'local-tools','command':'scripts/setup/bootstrap-local-tools.sh','safe':'local .tools only'},
  {'area':'runner-catalog','command':f'python3 scripts/agents/agent_fleet_control_catalog.py{agent_arg}{mode_arg}','safe':'reports only'},
  {'area':'branch-packets','command':'python3 scripts/agents/branch_fix_agents.py --max-branches 88','safe':'reports only'},
  {'area':'github-auth','command':'gh auth login','safe':'manual auth required before live GitHub control'},
  {'area':'azure-auth','command':'az login','safe':'manual auth required before live Azure control'},
  {'area':'codespaces','command':'gh codespace list','safe':'read-only inventory after auth'},
  {'area':'wiki','command':'python3 scripts/github/update-wiki-from-reports.py --dry-run','safe':'dry-run wiki plan'},
  {'area':'full-graph','command':'python3 scripts/build_graph/build_graph.py run --profile full --max-workers 2','safe':'reports/build/test outputs'},
  {'area':'dashboard','command':'python3 scripts/dashboard/generate-gui.py','safe':'status-site only'}]

def main():
 parser=argparse.ArgumentParser(description='Generate agent/fleet/provider/control catalog.')
 parser.add_argument('--agents',type=int,default=0,help='desired fleet size; 0 means templates only and no hard maximum')
 parser.add_argument('--mode',choices=['auto','local-first','hybrid-cloud','cloud-runner','codespaces'],default='auto')
 parser.add_argument('--budget',default='local-first',help='budget label for planning; report-only')
 args=parser.parse_args()
 tools={name:cmd_ok(name) for name in TOOLS}
 payload={'generatedUtc':datetime.now(timezone.utc).isoformat(),'requestedAgents':args.agents,'requestedMode':args.mode,'budgetMode':args.budget,'principle':'All agents are Hermes or XCore variants. Local-first, budget-aware fleets use free/local Ollama/web models and repo runners first, then paid/cloud providers only when outcome score justifies cost. C# exposes the safe GUI/control plane; Python launches agents/providers; F# scores outcome/cost/speed; C++ accelerates hot paths; XCore and Hermes carry XP, tools, prompts, and fleet roles.','capabilities':CAPS,'providers':PROVIDERS,'agentTypes':AGENT_TYPES,'fleetTemplates':fleet_templates(args.agents,args.mode),'setupSteps':setup_steps(args.agents,args.mode),'tools':tools,'mcpServers':MCP_SERVERS,'promptPacks':PROMPT_PACKS,'specializationVariables':SPECIALIZATION_VARIABLES,'refs':git_refs()[:88],'safety':'This catalog plans GitHub/Azure/Codespaces/wiki/runner control but does not push, delete branches, mutate cloud, or publish wiki without explicit human auth and separate commands.'}
 OUT.parent.mkdir(parents=True,exist_ok=True); OUT.write_text(json.dumps(payload,indent=2)+'\n')
 lines=['# Agent Fleet Control Catalog','',f"Generated: `{payload['generatedUtc']}`",'',payload['principle'],'','## Tool readiness']
 lines += [f"- {k}: {'available' if v else 'missing'}" for k,v in tools.items()]
 lines += ['','## MCP / MCR servers']+[f"- `{m['id']}` — {m['scope']} (local={m['local']}, cloud={m['cloud']})" for m in payload['mcpServers']]
 lines += ['','## Prompt packs / sub-agent guides']+[f"- `{p['id']}` — {p['goal']} ({', '.join(p['engines'])})" for p in payload['promptPacks']]
 lines += ['','## Specialization variables']+[f"- {v}" for v in payload['specializationVariables']]
 lines += ['','## Capability catalogs']
 for k,vals in CAPS.items(): lines += [f"### {k}",'']+[f"- {v}" for v in vals]+['']
 lines += ['## Fleet templates','','| Size | Mode | Agents | Providers |','| ---: | --- | --- | --- |']
 for f in payload['fleetTemplates']: lines.append(f"| {f['size']} | {f['mode']} | {', '.join(f['agents'])} | {', '.join(f['providers'])} |")
 lines += ['','## Safe setup / runner / takeover plan']+[f"- `{s['command']}` — {s['area']}: {s['safe']}" for s in payload['setupSteps']]
 lines += ['','## Visible refs for packets']+[f"- `{r['ref']}` {r['commit']} — {r['subject']}" for r in payload['refs']]
 lines += ['','## Safety boundary',payload['safety']]
 MD.write_text('\n'.join(lines)+'\n')
 print(f'Wrote {OUT.relative_to(ROOT)}'); print(f'Wrote {MD.relative_to(ROOT)}')
 return 0
if __name__=='__main__': raise SystemExit(main())
