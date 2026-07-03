#!/usr/bin/env python3
from __future__ import annotations
import json, shutil
from datetime import datetime, timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'reports/integrations/aihub-super-shell.json'
MD=ROOT/'reports/integrations/aihub-super-shell.md'
PROVIDERS=[
 {'id':'chatgpt-openai','display':'ChatGPT / OpenAI','cli':'python3','secret':'OPENAI_API_KEY','cost':'paid/cloud','quality':'high','speed':'high'},
 {'id':'github-copilot','display':'GitHub Copilot','cli':'gh','secret':'GH_TOKEN or gh auth','cost':'subscription','quality':'high','speed':'high'},
 {'id':'m365-copilot','display':'Microsoft 365 Copilot','cli':'az','secret':'tenant auth/manual browser','cost':'subscription','quality':'enterprise','speed':'medium'},
 {'id':'azure-foundry','display':'Azure AI Foundry','cli':'az','secret':'AZURE_CLIENT_ID/TENANT/SECRET or az login','cost':'paid/cloud','quality':'enterprise','speed':'high'},
 {'id':'claude','display':'Claude','cli':'claude','secret':'ANTHROPIC_API_KEY','cost':'paid/cloud','quality':'high','speed':'high'},
 {'id':'codex-cli','display':'Codex CLI','cli':'codex','secret':'tool auth','cost':'varies','quality':'coding','speed':'high'},
 {'id':'ollama-local','display':'Ollama local models','cli':'ollama','secret':'none','cost':'local compute','quality':'variable','speed':'local'},
 {'id':'visual-studio','display':'Visual Studio / VS tooling','cli':'dotnet','secret':'developer auth optional','cost':'local/subscription','quality':'build-debug','speed':'local'},
 {'id':'github-cli','display':'GitHub CLI / Actions / Pages / Wiki','cli':'gh','secret':'gh auth','cost':'mostly included','quality':'repo-control','speed':'high'},
 {'id':'azure-cli','display':'Azure CLI / Bicep / ARM','cli':'az','secret':'az login or service principal','cost':'cloud usage','quality':'cloud-control','speed':'medium'}]
VAULT_FIELDS=['OPENAI_API_KEY','ANTHROPIC_API_KEY','AZURE_CLIENT_ID','AZURE_TENANT_ID','AZURE_CLIENT_SECRET','HELIOS_AZURE_RESOURCE_GROUP','GH_TOKEN','M365_TENANT_ID','COPILOT_PROFILE','OLLAMA_HOST','COSMOS_CONNECTION','SQL_CONNECTION','VECTOR_STORE_URI','MCP_SERVER_REGISTRY']
SHELL_UNITS=['provider-router','vault-key-guide','cost-quality-speed-optimizer','prompt-specialty-learner','tool-specialty-learner','hermes-fleet-router','xcore-fleet-router','github-control-router','azure-bicep-router','codespaces-runner-router','wiki-pages-router','visual-studio-router','m365-copilot-router','fabric-foundry-router','sql-cosmos-vector-router']

def ok(cli): return shutil.which(cli) is not None

def main():
 providers=[]
 for p in PROVIDERS:
  row=dict(p); row['cliAvailable']=ok(p['cli']); row['recommendedUse']='local-first' if 'local' in p['cost'] else 'escalate when score beats local option'; providers.append(row)
 setup=[
  {'step':'Open GUI','command':'python3 scripts/dashboard/generate-gui.py','why':'shows provider, fleet, autopilot, and vault field guidance'},
  {'step':'Generate fleet catalog','command':'python3 scripts/agents/agent_fleet_control_catalog.py --agents 128 --mode hybrid-cloud','why':'lists Hermes/XCore fleets, MCP/MCR servers, prompts, tools'},
  {'step':'Generate autopilot','command':'python3 scripts/agents/agent_fleet_autopilot.py --agents 128 --mode hybrid-cloud','why':'chooses smart units and commands from repo/tool readiness'},
  {'step':'GitHub auth','command':'gh auth login','why':'enables repo/actions/codespaces/wiki/pages control after review'},
  {'step':'Azure auth','command':'az login','why':'enables Azure/Bicep/Foundry/Fabric/Purview what-if after review'},
  {'step':'Local models','command':'ollama serve','why':'uses local compute to reduce cost before cloud escalation'},
  {'step':'Full safe setup','command':'scripts/setup/agent-runner-easy-setup.sh --agents 128 --mode hybrid-cloud','why':'one command for tools, reports, graph, GUI'}]
 payload={'generatedUtc':datetime.now(timezone.utc).isoformat(),'principle':'AIHub Super Shell connects ChatGPT/OpenAI, GitHub/Codex/Copilot, Microsoft 365 Copilot, Claude, Ollama/local models, Visual Studio, Azure/Bicep/ARM, SQL/Cosmos/vector stores, and MCP/MCR servers through Hermes/XCore fleets. It does not store secrets; the GUI shows easy vault fields and copyable setup commands.','providers':providers,'vaultFields':VAULT_FIELDS,'shellUnits':SHELL_UNITS,'setup':setup,'optimizationLoop':['prefer local/free providers','estimate cost/speed/quality','route to Hermes or XCore unit','select prompt/tool/specialty','run safe check','record outcome and XP','promote winning prompt/tool/provider','fallback on failure','summarize in GUI'],'yamlBicepJsonApiPlan':['GitHub Actions for CI/autopilot artifacts','Pages/wiki dry-run publishing','Bicep/ARM what-if before deploy','JSON provider and XP ledgers','API profiles for OpenAI/Claude/Copilot/Azure/Ollama','Codespaces and runner labels for hybrid compute']}
 OUT.parent.mkdir(parents=True,exist_ok=True); OUT.write_text(json.dumps(payload,indent=2)+'\n')
 lines=['# AIHub Super Shell','',f"Generated: `{payload['generatedUtc']}`",'',payload['principle'],'','## Provider readiness','','| Provider | CLI | Available | Cost | Use |','| --- | --- | --- | --- | --- |']
 for p in providers: lines.append(f"| {p['display']} | `{p['cli']}` | {p['cliAvailable']} | {p['cost']} | {p['recommendedUse']} |")
 lines += ['','## Easy vault fields']+[f"- `{v}`" for v in VAULT_FIELDS]
 lines += ['','## Shell units']+[f"- {u}" for u in SHELL_UNITS]
 lines += ['','## One-command setup path']+[f"- **{s['step']}**: `{s['command']}` — {s['why']}" for s in setup]
 lines += ['','## Optimization loop']+[f"- {x}" for x in payload['optimizationLoop']]
 lines += ['','## YAML / Bicep / JSON / API plan']+[f"- {x}" for x in payload['yamlBicepJsonApiPlan']]
 MD.write_text('\n'.join(lines)+'\n')
 print(f'Wrote {OUT.relative_to(ROOT)}'); print(f'Wrote {MD.relative_to(ROOT)}')
 return 0
if __name__=='__main__': raise SystemExit(main())
