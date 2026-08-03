#!/usr/bin/env python3
from __future__ import annotations
import argparse, json, shutil, subprocess
from datetime import datetime, timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'reports/branch-agents/agent-fleet-autopilot.json'
MD=ROOT/'reports/branch-agents/agent-fleet-autopilot.md'
CATALOG=ROOT/'reports/branch-agents/agent-fleet-control-catalog.json'

DOMAINS={
 'github-control':['.github','scripts/github','branch','workflow','wiki','pages','codex','runner'],
 'azure-hybrid':['infra','bicep','azure','keyvault','foundry','fabric','purview','entra','cosmos','sql'],
 'local-compute':['src/native','cmake','ollama','wsl','docker','local','xcore'],
 'csharp-gui':['src/gui','src/core','.cs','gui','dashboard','jrpg','control'],
 'fsharp-learning':['src/analytics','.fs','score','rank','predict','learning','analytics'],
 'python-aihub':['scripts/agents','scripts/analysis','.py','hermes','fleet','aihub','llm'],
 'knowledge-center':['docs','.md','jstor','knowledge','absorb','prune','merge']
}
TOOLS=['git','gh','az','dotnet','cmake','python3','docker','wsl','ollama','codex','claude']

IDE_MESH=[
 {'id':'aihub-command-ide','role':'single command IDE: Codex + GitHub Copilot + Azure + APIs','bestFor':'one report/GUI command center for repo edits, Copilot suggestions, Azure what-if, and API bridges','safeCommand':'python3 scripts/integrations/aihub_command_ide.py && python3 scripts/dashboard/generate-gui.py'},
 {'id':'codex-cli','role':'terminal coding agent','bestFor':'repo edits, tests, review packets','safeCommand':'codex --model gpt-5.5'},
 {'id':'github-ide','role':'GitHub web/Codespaces IDE','bestFor':'PRs, issues, branch compare, runners','safeCommand':'gh codespace list'},
 {'id':'github-copilot-ide','role':'GitHub Copilot IDE / CLI assistant','bestFor':'inline coding help, PR explanations, command suggestions, paired review with Codex and Azure IDE lanes','safeCommand':'gh extension list | rg copilot || gh extension install github/gh-copilot'},
 {'id':'azure-ide','role':'Azure Portal / Cloud Shell / what-if lane','bestFor':'Key Vault, Bicep, Foundry, Fabric, Purview planning','safeCommand':'python3 scripts/azure/azure_what_if.py'},
 {'id':'chatgpt-api','role':'OpenAI / ChatGPT API bridge','bestFor':'LLM scoring, summaries, tool routing, prompt packs','safeCommand':'python3 scripts/integrations/aihub_connectivity_guide.py'},
 {'id':'visual-studio-winui','role':'C# / WinUI / MAUI IDE lane','bestFor':'typed contracts, GUI shell, Windows app work','safeCommand':'dotnet build HELIOS.Platform.slnx'},
 {'id':'local-native-ide','role':'C++ native performance lane','bestFor':'hot paths, memory, graph kernels, CMake checks','safeCommand':'cmake -S src/native/HELIOS.Native.Performance -B .build/native'}
]

JOB_BLUEPRINTS=[
 {'job':'branch-absorption','primary':'hermes-scout','backup':'hermes-reviewer','domain':'github-control','ide':'github-ide','tools':['git','gh','python3'],'outputs':['branch packets','merge/prune report'],'avoidOverlap':'No builder edits branches until scout/reviewer mark keep/prune.'},
 {'job':'csharp-gui-orchestration','primary':'hermes-builder','backup':'xcore-worker','domain':'csharp-gui','ide':'visual-studio-winui','tools':['dotnet','python3'],'outputs':['GUI cards','contracts','control commands'],'avoidOverlap':'One C# owner updates contracts/UI while Python agents only regenerate reports.'},
 {'job':'fsharp-learning-score','primary':'xcore-learning-scorer','backup':'hermes-reviewer','domain':'fsharp-learning','ide':'visual-studio-winui','tools':['dotnet','python3'],'outputs':['XP scores','rankings','prediction notes'],'avoidOverlap':'Scorer owns ranking variables; reviewers consume scores instead of recomputing.'},
 {'job':'cpp-native-optimization','primary':'xcore-native-optimizer','backup':'xcore-worker','domain':'local-compute','ide':'local-native-ide','tools':['cmake','python3'],'outputs':['native assist notes','hot-path candidates'],'avoidOverlap':'Native optimizer owns C++ hot paths; GUI builders request kernels through contracts.'},
 {'job':'python-aihub-integration','primary':'hermes-builder','backup':'xcore-learning-scorer','domain':'python-aihub','ide':'codex-cli','tools':['python3','codex','gh'],'outputs':['provider glue','report JSON/MD','AIHub adapters','Copilot prompt handoff'],'avoidOverlap':'Python owns provider/report glue only; C# keeps orchestration and UI contracts.'},
 {'job':'copilot-codex-azure-ide','primary':'hermes-builder','backup':'xcore-model-router','domain':'python-aihub','ide':'github-copilot-ide','tools':['gh','codex','az','python3'],'outputs':['single command IDE','Copilot prompts','Codex task plan','Azure/API what-if handoff'],'avoidOverlap':'Copilot suggests IDE-level edits/commands, Codex executes repo patches, Azure lane only runs what-if/vault checks.'},
 {'job':'azure-vault-cloud-plan','primary':'hermes-vault-guide','backup':'xcore-azure-operator','domain':'azure-hybrid','ide':'azure-ide','tools':['az','python3'],'outputs':['what-if report','vault handoff','cloud readiness'],'avoidOverlap':'Azure operators run what-if/vault checks; no branch agent mutates cloud.'},
 {'job':'chatgpt-api-model-routing','primary':'xcore-model-router','backup':'hermes-reviewer','domain':'python-aihub','ide':'chatgpt-api','tools':['python3','codex'],'outputs':['model choice','prompt pack','cost/speed route'],'avoidOverlap':'Model router selects provider/model; worker agents do not independently switch models.'},
 {'job':'docs-knowledge-pack','primary':'hermes-reviewer','backup':'hermes-scout','domain':'knowledge-center','ide':'github-ide','tools':['git','python3'],'outputs':['docs notes','JSTOR/knowledge links','wiki dry-run'],'avoidOverlap':'Knowledge agents write rationale and docs while code agents stay on code lanes.'}
]

XCORE_TYPES=[
 {'id':'xcore-x5','style':'balanced local worker','bestFor':'small repo tasks, safe checks, dashboard refresh','cost':0.55,'speed':72,'quality':70,'parallelism':5},
 {'id':'xcore-x6','style':'parallel optimizer','bestFor':'multi-file refactor plans, branch compare, test matrix','cost':0.85,'speed':78,'quality':76,'parallelism':6},
 {'id':'xcore-x7','style':'native/performance specialist','bestFor':'C++ hot paths, CMake, memory/security scans','cost':1.15,'speed':82,'quality':82,'parallelism':7},
 {'id':'xcore-x8','style':'cloud/fleet coordinator','bestFor':'Azure/GitHub/Codespaces runners and larger agent groups','cost':1.45,'speed':88,'quality':86,'parallelism':8},
 {'id':'xcore-x9','style':'learning strategist','bestFor':'F# scoring, model routing, XP ledgers, prompt/tool promotion','cost':1.75,'speed':84,'quality':91,'parallelism':9}
]

HERMES_MODEL_TYPES=[
 {'id':'NousResearch/Hermes-4.3-36B','family':'Hermes 4.3','parameters':'36B','base':'Seed-OSS-36B-Base','bestFor':'local or enterprise self-deployment, long-context planning, balanced fleet lead','mode':'hybrid reasoning/chat','source':'Nous release + Hugging Face'},
 {'id':'NousResearch/Hermes-4-14B','family':'Hermes 4','parameters':'14B','base':'Qwen 3 14B','bestFor':'smaller dense local reasoning, tool-format checks, low-cost party member','mode':'hybrid reasoning/chat','source':'Hugging Face'},
 {'id':'NousResearch/Hermes-4-70B','family':'Hermes 4','parameters':'70B','base':'Meta Llama 3.1 70B','bestFor':'high-quality fleet lead, complex coding/reasoning review, branch arbitration','mode':'hybrid reasoning/chat','source':'Hugging Face collection'},
 {'id':'NousResearch/Hermes-4-405B','family':'Hermes 4','parameters':'405B','base':'Meta Llama 3.1 405B','bestFor':'largest remote/cloud planner, deep synthesis, final architecture reviews','mode':'hybrid reasoning/chat','source':'Hugging Face collection'},
 {'id':'NousResearch/Hermes-3-Llama-3.1-8B','family':'Hermes 3','parameters':'8B','base':'Llama 3.1 8B','bestFor':'fast local scout, repo triage, small-agent background training','mode':'agentic function-calling/generalist','source':'Nous Hermes 3 + Hugging Face'},
 {'id':'NousResearch/Hermes-3-Llama-3.1-70B','family':'Hermes 3','parameters':'70B','base':'Llama 3.1 70B','bestFor':'multi-turn planning, branch merge review, instruction-following control','mode':'agentic function-calling/generalist','source':'Nous Hermes 3 collection'},
 {'id':'NousResearch/Hermes-3-Llama-3.1-405B','family':'Hermes 3','parameters':'405B','base':'Llama 3.1 405B','bestFor':'cloud-scale reasoning and long planning reports','mode':'agentic function-calling/generalist','source':'Nous Hermes 3 collection'},
 {'id':'NousResearch/Hermes-2-Pro-Llama-3-70B','family':'Hermes 2 Pro','parameters':'70B','base':'Llama 3 70B','bestFor':'function calling, JSON-mode report generation, schema-faithful agents','mode':'function calling/json mode','source':'Hugging Face'},
 {'id':'NousResearch/Hermes-2-Pro-Mistral-7B','family':'Hermes 2 Pro','parameters':'7B','base':'Mistral 7B','bestFor':'small function-calling worker, local JSON command planner','mode':'function calling/json mode','source':'Nous releases'},
 {'id':'NousResearch/Nous-Hermes-2-Mixtral-8x7B-DPO','family':'Nous Hermes 2','parameters':'8x7B MoE','base':'Mixtral 8x7B','bestFor':'mixture-of-experts routing, creative worker pools, DPO-tuned review','mode':'chat/instruction','source':'Nous releases'}
]
SUBAGENT_SLIDERS=[
 {'id':'research','label':'Repo research','default':6,'tools':['rg','git','reports']},
 {'id':'coding','label':'Code changes','default':4,'tools':['codex','dotnet','python3']},
 {'id':'testing','label':'Tests and build','default':5,'tools':['simple-build','py_compile','unittest']},
 {'id':'security','label':'Security/vault','default':3,'tools':['secret-preflight','apply-gate','az keyvault']},
 {'id':'cloud','label':'Azure/GitHub cloud','default':3,'tools':['az','gh','what-if']},
 {'id':'learning','label':'AIHub learning','default':7,'tools':['F# scoring','XP ledger','model routing']}
]
AUTO_PARTY_TASKS=[
 {'task':'merge-prune-branch','party':['hermes-scout','hermes-reviewer','xcore-x6','xcore-learning-scorer'],'model':'gpt-5.5','sliders':{'research':8,'coding':2,'testing':5,'security':4,'learning':7},'why':'Maximize repo research and scoring before any merge movement.'},
 {'task':'csharp-winui-gui','party':['hermes-builder','xcore-worker','xcore-x5','xcore-learning-scorer'],'model':'gpt-5.5','sliders':{'research':4,'coding':7,'testing':6,'learning':5},'why':'C# UI owner plus XCore worker, with tests kept high.'},
 {'task':'cpp-performance','party':['xcore-native-optimizer','xcore-x7','hermes-reviewer','xcore-learning-scorer'],'model':'gpt-5.5-pro','sliders':{'research':5,'coding':5,'testing':8,'security':6,'learning':6},'why':'Native specialist owns C++ while reviewer guards safety and scoring.'},
 {'task':'azure-vault-setup','party':['hermes-vault-guide','xcore-azure-operator','xcore-x8','hermes-reviewer'],'model':'gpt-5.4','sliders':{'research':4,'testing':5,'security':9,'cloud':9,'learning':4},'why':'Cloud/vault specialists dominate; no repo mutation needed.'},
 {'task':'aihub-model-routing','party':['xcore-model-router','xcore-x9','hermes-reviewer','hermes-builder'],'model':'gpt-5.3-codex','sliders':{'research':5,'coding':4,'testing':4,'learning':10},'why':'Learning/model routing optimized while builders only apply safe glue.'}
]

def load(path, default):
 try: return json.loads(path.read_text()) if path.exists() else default
 except Exception: return default

def rg_files():
 p=subprocess.run(['rg','--files'],cwd=ROOT,text=True,capture_output=True)
 return p.stdout.splitlines()

def score_domains(files):
 text=' '.join(files).lower(); scores={}
 for d,terms in DOMAINS.items(): scores[d]=sum(text.count(t.lower()) for t in terms)
 return dict(sorted(scores.items(),key=lambda kv:-kv[1]))

def tool_state():
 return {t: bool(shutil.which(t)) for t in TOOLS}

def plan_units(scores,tools,catalog,agents,mode):
 providers=catalog.get('providers',[])
 base=[]
 for domain,score in scores.items():
  if score<=0: continue
  if domain=='github-control': req=['gh','git']; cmds=['gh auth status','gh workflow list','gh codespace list','python3 scripts/agents/branch_fix_agents.py --max-branches 88']
  elif domain=='azure-hybrid': req=['az']; cmds=['az account show','python3 scripts/azure/azure_connection_pipeline.py --stage all','python3 scripts/azure/azure_what_if.py']
  elif domain=='local-compute': req=['cmake','python3']; cmds=['cmake -S src/native/HELIOS.Native.Performance -B .build/native','python3 scripts/agents/agent_fleet_control_catalog.py --mode local-first']
  elif domain=='csharp-gui': req=['dotnet']; cmds=['dotnet build HELIOS.Platform.slnx','python3 scripts/dashboard/generate-gui.py']
  elif domain=='fsharp-learning': req=['dotnet']; cmds=['dotnet test tests/analytics/HELIOS.Analytics.FSharp.Tests/HELIOS.Analytics.FSharp.Tests.fsproj','python3 scripts/analysis/language_decision_matrix.py']
  elif domain=='python-aihub': req=['python3']; cmds=['python3 scripts/agents/hermes_fleet_readiness.py','python3 scripts/agents/agent_fleet_control_catalog.py']
  else: req=['python3']; cmds=['python3 scripts/analysis/document_code_absorption_ranker.py','python3 scripts/analysis/code_learning_atlas.py']
  readiness=sum(1 for r in req if tools.get(r))/max(1,len(req))
  base.append({'unit':domain,'score':score,'readiness':readiness,'recommendedAgents':max(1, min(max(agents or 8,1), max(2,score//50+1))),'mode':mode if mode!='auto' else ('local-first' if readiness>=0.75 else 'setup-first'),'requiredTools':req,'providers':providers[:6],'commands':cmds,'status':'ready' if readiness==1 else 'needs-setup'})
 return base

def gui_actions(units):
 actions=[]
 for u in units:
  actions.append({'label':f"Plan {u['unit']} fleet",'command':f"python3 scripts/agents/agent_fleet_autopilot.py --unit {u['unit']} --agents {u['recommendedAgents']}",'safe':'reports only'})
  actions.append({'label':f"Run {u['unit']} checks",'command':' && '.join(u['commands'][:2]),'safe':'local checks/report-only unless command itself prompts for auth'})
 return actions

def optimal_job_board(units, tools):
 unit_map={u.get('unit'):u for u in units}
 board=[]
 used_primary=set()
 for job in JOB_BLUEPRINTS:
  readiness=sum(1 for tool in job['tools'] if tools.get(tool))/max(1,len(job['tools']))
  domain=unit_map.get(job['domain'], {})
  primary=job['primary']
  if primary in used_primary:
   primary=f"{primary} (secondary shift)"
  used_primary.add(job['primary'])
  board.append({**job,'primary':primary,'readiness':round(readiness,2),'recommendedAgents':domain.get('recommendedAgents',2),'mode':domain.get('mode','local-first'),'status':'ready' if readiness==1 else 'needs-setup','handoff':f"{job['primary']} -> {job['backup']} -> {job['ide']}"})
 return board


def smart_auto_party(units):
 score_map={u.get('unit'):u.get('score',0) for u in units}
 party=[]
 for task in AUTO_PARTY_TASKS:
  total=sum(score_map.values()) or 1
  task_score=max(50, min(99, int((sum(score_map.values())/len(score_map or {'x':1}))/max(1,total)*100)+70))
  party.append({**task,'confidence':task_score,'command':f"python3 scripts/agents/agent_fleet_autopilot.py --unit {task['task']} --agents {len(task['party'])} --mode hybrid-cloud"})
 return party


def setup_center(units):
 return [
  {'name':'Clean slate reports','command':'rm -rf reports/build-graph reports/branch-agents/agent-fleet-autopilot.json reports/branch-agents/agent-fleet-autopilot.md','note':'optional local report cleanup only'},
  {'name':'Bootstrap local tools','command':'scripts/setup/bootstrap-local-tools.sh','note':'installs local .tools copies'},
  {'name':'Generate smart catalog','command':'python3 scripts/agents/agent_fleet_control_catalog.py --agents 128 --mode hybrid-cloud','note':'no hard max, report-only'},
  {'name':'Generate autopilot plan','command':'python3 scripts/agents/agent_fleet_autopilot.py --agents 128 --mode hybrid-cloud','note':'selects units/providers/tools'},
  {'name':'Refresh GUI','command':'python3 scripts/dashboard/generate-gui.py','note':'writes status-site/index.html'},
  {'name':'Manual live auth','command':'gh auth login && az login','note':'only when ready for live GitHub/Azure operations'}]

def main():
 parser=argparse.ArgumentParser(description='Smart autopilot for agent/fleet units; report-only by default.')
 parser.add_argument('--agents',type=int,default=0)
 parser.add_argument('--mode',choices=['auto','local-first','hybrid-cloud','cloud-runner','codespaces'],default='auto')
 parser.add_argument('--unit',default='all')
 args=parser.parse_args()
 catalog=load(CATALOG,{})
 files=rg_files(); scores=score_domains(files); tools=tool_state()
 units=plan_units(scores,tools,catalog,args.agents,args.mode)
 if args.unit!='all': units=[u for u in units if u['unit']==args.unit]
 payload={'generatedUtc':datetime.now(timezone.utc).isoformat(),'requestedAgents':args.agents,'requestedMode':args.mode,'unitFilter':args.unit,'principle':'Autopilot is report-only: it scores repo domains, tool readiness, local/cloud capacity signals, and catalog providers to choose smart fleet units. The GUI can copy commands, but live GitHub/Azure/wiki/cloud mutation still requires explicit auth and human review.','tools':tools,'domainScores':scores,'units':units,'guiActions':gui_actions(units),'ideMesh':IDE_MESH,'optimalJobBoard':optimal_job_board(units,tools),'xcoreTypes':XCORE_TYPES,'hermesModelTypes':HERMES_MODEL_TYPES,'subagentSliders':SUBAGENT_SLIDERS,'smartAutoParty':smart_auto_party(units),'setupCenter':setup_center(units),'dataCenters':['reports JSON/MD','status-site GUI','SQL/Cosmos/vector manifests','knowledge absorption ledgers','agent XP/fleet XP ledgers'],'learningLoops':['score domain','select agent/tool/provider','run safe check','record outcome','raise/lower XP','promote prompt/tool/MCP server','prune redundant work','keep abstract ideas']}
 OUT.parent.mkdir(parents=True,exist_ok=True); OUT.write_text(json.dumps(payload,indent=2)+'\n')
 lines=['# Agent Fleet Autopilot','',f"Generated: `{payload['generatedUtc']}`",'',payload['principle'],'','## Smart units','','| Unit | Score | Status | Agents | Mode | Required tools |','| --- | ---: | --- | ---: | --- | --- |']
 for u in units: lines.append(f"| {u['unit']} | {u['score']} | {u['status']} | {u['recommendedAgents']} | {u['mode']} | {', '.join(u['requiredTools'])} |")
 lines += ['','## Unified IDE mesh']+[f"- `{i['id']}` — {i['role']} / {i['bestFor']} (`{i['safeCommand']}`)" for i in payload['ideMesh']]
 lines += ['','## Non-overlap optimal job board','', '| Job | Primary | Backup | IDE | Status | Handoff |', '| --- | --- | --- | --- | --- | --- |']
 for j in payload['optimalJobBoard']: lines.append(f"| {j['job']} | {j['primary']} | {j['backup']} | {j['ide']} | {j['status']} | {j['handoff']} |")
 lines += ['','## XCore type differences']+[f"- `{x['id']}` — {x['style']}; {x['bestFor']} (parallelism {x['parallelism']}, cost {x['cost']})" for x in payload['xcoreTypes']]
 lines += ['','## Real Hermes model types (looked up from Nous/Hugging Face)']+[f"- `{h['id']}` — {h['family']} {h['parameters']} on {h['base']}; {h['bestFor']} ({h['mode']})" for h in payload['hermesModelTypes']]
 lines += ['','## Smart autoparty by task']+[f"- **{p['task']}**: {', '.join(p['party'])} using `{p['model']}` — {p['why']}" for p in payload['smartAutoParty']]
 lines += ['','## Subagent sliders']+[f"- {s['label']}: default {s['default']} / tools {', '.join(s['tools'])}" for s in payload['subagentSliders']]
 lines += ['','## GUI actions']+[f"- **{a['label']}**: `{a['command']}` — {a['safe']}" for a in payload['guiActions']]
 lines += ['','## Setup center']+[f"- **{s['name']}**: `{s['command']}` — {s['note']}" for s in payload['setupCenter']]
 lines += ['','## Learning loops']+[f"- {x}" for x in payload['learningLoops']]
 MD.write_text('\n'.join(lines)+'\n')
 print(f'Wrote {OUT.relative_to(ROOT)}'); print(f'Wrote {MD.relative_to(ROOT)}')
 return 0
if __name__=='__main__': raise SystemExit(main())
