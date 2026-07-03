#!/usr/bin/env python3
from __future__ import annotations
import json, shutil, argparse
from datetime import datetime, timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'reports/integrations/aihub-command-ide.json'
MD=ROOT/'reports/integrations/aihub-command-ide.md'
TOOLS=['codex','gh','az','python3','dotnet']
LANES=[
    {'id':'codex','label':'Codex repo executor','tool':'codex','safeCheck':'codex --help','purpose':'Repo edits, tests, review packets, model switches, code execution planning.'},
    {'id':'github-copilot','label':'GitHub Copilot IDE/CLI','tool':'gh','safeCheck':'gh extension list | rg copilot || gh extension install github/gh-copilot','purpose':'IDE pair suggestions, command suggestions, PR explanations, GitHub-aware assist.'},
    {'id':'azure','label':'Azure Cloud Shell / what-if','tool':'az','safeCheck':'az account show || true','purpose':'Key Vault, Bicep/ARM what-if, Azure SQL/Cosmos/vector, cloud readiness.'},
    {'id':'apis','label':'APIs and providers','tool':'python3','safeCheck':'python3 scripts/integrations/aihub_connectivity_guide.py','purpose':'OpenAI/ChatGPT API, GitHub API, Azure API, local/provider bridge reports.'},
]
ACTIONS=[
    {'label':'Open all-in-one command IDE report','command':'python3 scripts/integrations/aihub_command_ide.py','lanes':['codex','github-copilot','azure','apis']},
    {'label':'Codex + Copilot + Azure readiness','command':'codex --help && gh auth status && gh extension list | rg copilot && az account show','lanes':['codex','github-copilot','azure']},
    {'label':'Plan with Codex, suggest with Copilot, validate Azure','command':'codex --model gpt-5.5 && gh copilot suggest "make a safe HELIOS build fix" && python3 scripts/azure/azure_what_if.py','lanes':['codex','github-copilot','azure']},
    {'label':'API bridge inventory','command':'python3 scripts/integrations/aihub_connectivity_guide.py && python3 scripts/integrations/aihub_super_shell.py','lanes':['apis']},
    {'label':'Four-engine C#/F#/C++/Python optimize refresh','command':'dotnet build src/core/HELIOS.Platform.Contracts/HELIOS.Platform.Contracts.csproj --nologo && dotnet build src/analytics/HELIOS.Analytics.FSharp/HELIOS.Analytics.FSharp.fsproj --nologo && g++ -std=c++20 -I. -fsyntax-only /tmp/aihub_header_check.cpp && python3 -m py_compile scripts/integrations/aihub_command_ide.py scripts/integrations/aihub_learning_knowledge_store.py && python3 scripts/integrations/aihub_command_ide.py','lanes':['codex','apis']},
    {'label':'Tri-engine C#/F#/C++ optimize refresh','command':'dotnet build src/core/HELIOS.Platform.Contracts/HELIOS.Platform.Contracts.csproj --nologo && dotnet build src/analytics/HELIOS.Analytics.FSharp/HELIOS.Analytics.FSharp.fsproj --nologo && g++ -std=c++20 -I. -fsyntax-only /tmp/aihub_header_check.cpp && python3 scripts/integrations/aihub_command_ide.py','lanes':['codex','apis']},
    {'label':'Knowledge-baked code fix refresh','command':'python3 scripts/integrations/aihub_learning_knowledge_store.py && dotnet build src/analytics/HELIOS.Analytics.FSharp/HELIOS.Analytics.FSharp.fsproj --nologo && python3 scripts/analysis/complex_code_grading.py && python3 scripts/build_graph/build_graph.py run --profile quick --changed-only --max-workers 2','lanes':['codex','apis']},
    {'label':'Full safe refresh','command':'python3 scripts/agents/agent_fleet_autopilot.py --agents 128 --mode hybrid-cloud && python3 scripts/integrations/aihub_command_ide.py && python3 scripts/dashboard/generate-gui.py','lanes':['codex','github-copilot','azure','apis']},
]

def tool_state():
    return {t: bool(shutil.which(t)) for t in TOOLS}

def main():
    parser=argparse.ArgumentParser(description='Single command IDE mesh for Codex + GitHub Copilot + Azure + APIs.')
    parser.add_argument('--print-command', action='store_true', help='Print the recommended all-in-one safe command.')
    args=parser.parse_args()
    payload={'generatedUtc':datetime.now(timezone.utc).isoformat(),'principle':'One command IDE means one GUI/report surface that groups Codex, GitHub Copilot, Azure, APIs, C#/F#/C++/Python optimization, and knowledge-baked code fixing into copyable safe commands; it does not store secrets or run live mutations by default.','tools':tool_state(),'lanes':LANES,'actions':ACTIONS,'recommendedCommand':' && '.join([ACTIONS[0]['command'], 'python3 scripts/dashboard/generate-gui.py']),'secretPolicy':['Do not paste API keys into the static GUI.','Use Key Vault, environment variables, or provider CLIs.','Run live Azure/GitHub operations only after auth and live flags.']}
    OUT.parent.mkdir(parents=True,exist_ok=True)
    OUT.write_text(json.dumps(payload,indent=2)+'\n')
    md=['# AIHub Single Command IDE','',f"Generated: `{payload['generatedUtc']}`",'',payload['principle'],'','## Lanes']
    md += [f"- **{l['label']}** (`{l['tool']}`): {l['purpose']} Safe check: `{l['safeCheck']}`" for l in LANES]
    md += ['','## Copyable commands']+[f"- **{a['label']}**: `{a['command']}`" for a in ACTIONS]
    md += ['','## Secret policy']+[f'- {x}' for x in payload['secretPolicy']]
    MD.write_text('\n'.join(md)+'\n')
    if args.print_command:
        print(payload['recommendedCommand'])
    else:
        print(f'Wrote {OUT.relative_to(ROOT)}'); print(f'Wrote {MD.relative_to(ROOT)}')
if __name__=='__main__': main()
