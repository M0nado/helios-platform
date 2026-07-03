#!/usr/bin/env python3
from __future__ import annotations
import json, socket
from datetime import datetime, timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'reports/integrations/aihub-connectivity-guide.json'
MD=ROOT/'reports/integrations/aihub-connectivity-guide.md'
PORTS=[
 {'id':'dashboard','port':8787,'command':'python3 scripts/web/helios-web.py --port 8787','purpose':'HELIOS GUI/control center'},
 {'id':'ollama','port':11434,'command':'ollama serve','purpose':'local LLM/models'},
 {'id':'aihub-api','port':8790,'command':'python3 scripts/integrations/aihub_super_shell.py','purpose':'AIHub setup/report API placeholder'},
 {'id':'hermes-local','port':8791,'command':'python3 scripts/agents/hermes_fleet_readiness.py','purpose':'Hermes/Fleet local readiness'},
 {'id':'xcore-local','port':8792,'command':'python3 scripts/agents/agent_fleet_autopilot.py --unit local-compute','purpose':'XCore local planning'},
 {'id':'mcp-filesystem','port':8801,'command':'python3 scripts/analysis/code_learning_atlas.py','purpose':'filesystem/repo knowledge MCP-style lane'},
 {'id':'mcp-github','port':8802,'command':'python3 scripts/github/github-inventory.py','purpose':'GitHub control/readiness lane'},
 {'id':'mcp-azure','port':8803,'command':'python3 scripts/azure/azure_connection_pipeline.py --stage all','purpose':'Azure/Bicep/what-if lane'},
 {'id':'mcp-sql-vector','port':8804,'command':'python3 scripts/analysis/document_code_absorption_ranker.py','purpose':'SQL/vector/Cosmos manifest lane'}]
TUNNELS=[
 {'id':'github-codespaces-forward','command':'gh codespace ports visibility 8787:public','safe':'manual; only after auth and review'},
 {'id':'ssh-local-forward','command':'ssh -L 8787:127.0.0.1:8787 <host>','safe':'manual tunnel'},
 {'id':'cloudflared-placeholder','command':'cloudflared tunnel --url http://127.0.0.1:8787','safe':'manual if installed'},
 {'id':'dev-tunnel-placeholder','command':'devtunnel host -p 8787','safe':'manual if installed'}]
GUIDE=[
 {'phase':'clean-slate','steps':['run safe preflights','regenerate AIHub reports','regenerate autopilot','open GUI','review before auth/mutation']},
 {'phase':'smart-learning','steps':['absorb docs/code','rank modules/submodules','score branches/commits','promote unique ideas','prune duplicate/redundant plans']},
 {'phase':'smart-commit','steps':['run build graph quick/full','review report next fixes','commit only after tests','generate PR metadata','never auto-push without review']},
 {'phase':'local-cloud-hybrid','steps':['prefer local Ollama/local ports','use Codespaces/runners for burst compute','use Azure what-if before cloud changes','record cost/speed/quality outcome']},
 {'phase':'gui-operator','steps':['copy setup commands','fill vault/env fields outside static GUI','select Hermes/XCore unit','choose local/cloud mode','rerun dashboard']}]

def open_port(port:int)->bool:
 s=socket.socket(); s.settimeout(0.15)
 try: return s.connect_ex(('127.0.0.1',port))==0
 finally: s.close()

def main():
 ports=[dict(p,open=open_port(p['port'])) for p in PORTS]
 payload={'generatedUtc':datetime.now(timezone.utc).isoformat(),'principle':'Local ports, tunnels, MCR/MCP-style lanes, cloud shell, Codespaces, and Azure/GitHub runners are all treated as selectable connectivity options for Hermes/XCore agents. The guide is safe/report-only and keeps secrets outside static files.','ports':ports,'tunnels':TUNNELS,'guide':GUIDE,'oneCommand':'scripts/setup/agent-runner-easy-setup.sh --agents 128 --mode hybrid-cloud','reports':['reports/integrations/aihub-super-shell.md','reports/branch-agents/agent-fleet-autopilot.md','reports/learning/document-code-absorption-ranker.md','reports/branch-intelligence/super-branch-unification.md']}
 OUT.parent.mkdir(parents=True,exist_ok=True); OUT.write_text(json.dumps(payload,indent=2)+'\n')
 lines=['# AIHub Connectivity Guide','',f"Generated: `{payload['generatedUtc']}`",'',payload['principle'],'','## Local ports','','| Service | Port | Open | Purpose | Command |','| --- | ---: | --- | --- | --- |']
 for p in ports: lines.append(f"| {p['id']} | {p['port']} | {p['open']} | {p['purpose']} | `{p['command']}` |")
 lines += ['','## Tunnels / remote access']+[f"- **{t['id']}**: `{t['command']}` — {t['safe']}" for t in TUNNELS]
 lines += ['','## Automatic learning / pruning / commit guide']
 for g in GUIDE: lines += [f"### {g['phase']}"]+[f"- {s}" for s in g['steps']]
 lines += ['','## One command',f"`{payload['oneCommand']}`"]
 lines += ['','## Related reports']+[f"- `{r}`" for r in payload['reports']]
 MD.write_text('\n'.join(lines)+'\n')
 print(f'Wrote {OUT.relative_to(ROOT)}'); print(f'Wrote {MD.relative_to(ROOT)}')
 return 0
if __name__=='__main__': raise SystemExit(main())
