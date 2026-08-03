#!/usr/bin/env python3
from __future__ import annotations
import json, shutil, subprocess
from datetime import datetime, timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
CFG=ROOT/'config/aihub-supershell-vault.example.json'
OUT=ROOT/'reports/integrations/aihub-supershell-vault-wizard.json'
MD=ROOT/'reports/integrations/aihub-supershell-vault-wizard.md'

def ready(cmd): return bool(shutil.which(cmd))

def git_lines(*args):
    try: return subprocess.check_output(['git',*args],cwd=ROOT,text=True,stderr=subprocess.DEVNULL).splitlines()
    except Exception: return []

def main():
    cfg=json.loads(CFG.read_text())
    toolchain=[
        {'id':'local','label':'Local workstation','ready':ready('python3'),'setup':['scripts/setup/bootstrap-local-tools.sh','python3 scripts/dashboard/generate-gui.py'],'bestFor':'safe reports, GUI, local checks, Ollama, WSL2 bridges'},
        {'id':'wsl2','label':'WSL2/Linux lane','ready':ready('wsl'),'setup':['wsl --status','python3 scripts/integrations/aihub_connectivity_guide.py'],'bestFor':'Linux libraries, local agents, C++ native builds, Python AIHub glue'},
        {'id':'codespaces','label':'GitHub Codespaces','ready':ready('gh'),'setup':['gh codespace list','gh codespace ports visibility 8787:public -c <codespace>'],'bestFor':'hosted computer/sandbox, branch testing, web GUI preview'},
        {'id':'github-runner','label':'GitHub runner/actions','ready':ready('gh'),'setup':['gh workflow list','gh run list','python3 scripts/agents/branch_test_autofix_plan.py'],'bestFor':'YAML automation, PR checks, Pages/wiki publish with repo-live flags'},
        {'id':'azure-hybrid','label':'Azure hybrid cloud','ready':ready('az'),'setup':['az login','az group create -n helios-aihub-rg -l eastus','az keyvault create -g helios-aihub-rg -n helios-aihub-kv'],'bestFor':'Key Vault, Bicep, SQL, Cosmos/vector, cloud agents, Foundry/Fabric/Purview/Entra'}]
    guiModules=[
        {'id':'gui-command-center','owner':'C# + HTML/CSS/JS','submodules':['status dashboard','remote console AIHub tab','copyable command palette','provider cards','agent party cards','feedback panels'],'compareBy':['user effort','safe automation level','missing credentials','test status','learning value']},
        {'id':'usbwizard','owner':'C# orchestration + PowerShell/YAML docs','submodules':['bootable USB plan','Windows profile setup','partition/security/performance presets','offline vault handoff','driver/profile recovery'],'compareBy':['boot safety','partition risk','profile match','security baseline','rollback readiness']},
        {'id':'github-graph','owner':'YAML + GitHub CLI','submodules':['repos','branches','commits','issues','actions','runners','codespaces','pages','wiki'],'compareBy':['failing checks','stale branches','merge value','absorption novelty','autofix safety']},
        {'id':'azure-cloud-graph','owner':'Bicep + Azure CLI','submodules':['Key Vault','SQL','Cosmos/vector','Container Apps','Storage','Foundry','Fabric','Purview','Entra'],'compareBy':['cost cap','what-if diff','secret readiness','data gravity','rollback plan']},
        {'id':'profile-performance-security','owner':'C# + F# + C++','submodules':['profile scoring','partition plan','security baseline','performance kernels','agent learning telemetry'],'compareBy':['latency','memory pressure','risk','prediction confidence','test coverage']}]
    menus=[
        {'title':'Vault first setup','items':['choose local/wsl2/codespaces/azure-hybrid','paste keys only into vault/env boxes','generate az keyvault secret set commands','verify with secret preflight','never store secrets in static HTML']},
        {'title':'Branch and commit improvement','items':['show current branch graph','rank commit issues','run branch nervous-system specialty','absorb unique ideas','prune redundant low-signal work','generate PR notes']},
        {'title':'Automatic module fill','items':['compare every module/submodule','pick C# for orchestration/security','pick F# for prediction/scoring','pick C++ for hot paths','pick Python only for AIHub/Linux/provider edge','record decisions in JSON/MD ledgers']}]
    payload={'generatedUtc':datetime.now(timezone.utc).isoformat(),'config':cfg,'toolchain':toolchain,'guiModules':guiModules,'menus':menus,'recentBranches':git_lines('for-each-ref','--format=%(refname:short)|%(objectname:short)|%(subject)','refs/heads','refs/remotes')[:50],'recentCommits':git_lines('log','--all','--since=3 days ago','--pretty=%h %ad %s','--date=iso-strict')[:30],'vaultCommands':['az keyvault secret set --vault-name helios-aihub-kv --name OPENAI-API-KEY --value "$OPENAI_API_KEY"','az keyvault secret set --vault-name helios-aihub-kv --name GITHUB-TOKEN --value "$GITHUB_TOKEN"','az keyvault secret list --vault-name helios-aihub-kv -o table'],'runEverything':'scripts/setup/agent-runner-easy-setup.sh --agents auto --mode hybrid-cloud --profile full'}
    OUT.parent.mkdir(parents=True,exist_ok=True); OUT.write_text(json.dumps(payload,indent=2)+'\n')
    lines=['# AIHub SuperShell Vault Wizard','',f"Generated: `{payload['generatedUtc']}`",'','## Toolchain lanes']
    lines += [f"- **{t['label']}** ready={t['ready']}: {t['bestFor']}" for t in toolchain]
    lines += ['','## GUI modules']+[f"- **{m['id']}** ({m['owner']}): {', '.join(m['submodules'])}" for m in guiModules]
    lines += ['','## Vault commands']+[f"- `{c}`" for c in payload['vaultCommands']]
    lines += ['','## Run everything',f"`{payload['runEverything']}`"]
    MD.write_text('\n'.join(lines)+'\n')
    print(f'Wrote {OUT.relative_to(ROOT)}'); print(f'Wrote {MD.relative_to(ROOT)}')
if __name__=='__main__': main()
