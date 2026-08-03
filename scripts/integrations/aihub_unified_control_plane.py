#!/usr/bin/env python3
from __future__ import annotations
import json, shutil, subprocess
from datetime import datetime, timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'reports/integrations/aihub-unified-control-plane.json'
MD=ROOT/'reports/integrations/aihub-unified-control-plane.md'

def lines(*args):
    try: return subprocess.check_output(['git',*args],cwd=ROOT,text=True,stderr=subprocess.DEVNULL).splitlines()
    except Exception: return []

def tool(name): return bool(shutil.which(name))

def load_json(path, fallback):
    try:
        return json.loads(path.read_text()) if path.exists() else fallback
    except Exception:
        return fallback

def self_learning_backbone_bridge():
    notes=load_json(ROOT/'reports/learning/aihub-self-learning-notes.json',{})
    grading=load_json(ROOT/'reports/learning/complex-code-grading.compact.json',{})
    situation_count=len(notes.get('situationVariables',[]))
    connection_count=len(notes.get('connectionIdeas',[]))
    composite_count=len(notes.get('compositeFilePlans',[]))
    top_keep=len(grading.get('topKeep',[]))
    return {
        'id':'csharp-self-learning-backbone-bridge',
        'display':'C# self-learning backbone bridge',
        'contract':'src/core/HELIOS.Platform.Contracts/AIHubEngineContracts.cs',
        'nativeAssist':'src/native/HELIOS.Native.Performance/include/helios/aihub_native_engine.hpp',
        'pythonSource':'scripts/integrations/aihub_unified_control_plane.py',
        'reportSource':'reports/learning/aihub-self-learning-notes.json',
        'stableJson':'config/aihub-unified-control-plane.example.json',
        'stableMarkdown':'docs/integration/AIHUB_UNIFIED_CONTROL_PLANE.md',
        'signals':{
            'situationVariables':situation_count,
            'connectionIdeas':connection_count,
            'compositePlans':composite_count,
            'topKeepItems':top_keep,
        },
        'orchestration': [
            'C# owns identity, policy, GUI state, typed APIs, and route decisions.',
            'F# owns score fusion and learning ranks before C# chooses the safe action.',
            'C++ is recommended only for native hot-path compare pressure.',
            'Python gathers reports, providers, CLI readiness, and artifact summaries for the GUI.',
        ],
        'safeCommands':[
            'python3 scripts/analysis/aihub_self_learning_notes.py',
            'python3 scripts/integrations/aihub_unified_control_plane.py',
            'python3 scripts/dashboard/generate-gui.py',
        ],
    }

def main():
    providers=[
        {'id':'github','display':'GitHub control','areas':['repos','branches','commits','issues','actions','runners','codespaces','pages','wiki','packages'],'cli':'gh','ready':tool('gh'),'safeToggle':'repo-live'},
        {'id':'azure','display':'Azure hybrid cloud','areas':['Key Vault','Bicep/ARM','SQL','Cosmos','Functions','Container Apps','Foundry','Fabric','Purview','Entra'],'cli':'az','ready':tool('az'),'safeToggle':'cloud-live'},
        {'id':'codex-chatgpt','display':'Codex / ChatGPT / OpenAI','areas':['code review','agent prompts','tool routing','learning summaries','model comparisons'],'cli':'codex/openai','ready':tool('codex'),'safeToggle':'aihub-live'},
        {'id':'local-models','display':'Ollama/local models','areas':['local inference','private learning','cheap routing','offline fallback'],'cli':'ollama','ready':tool('ollama'),'safeToggle':'auto-local'},
        {'id':'microsoft-copilot','display':'Microsoft Copilot / 365','areas':['enterprise knowledge','Graph/365','docs','Teams/SharePoint handoff'],'cli':'m365/copilot','ready':False,'safeToggle':'enterprise-live'},
        {'id':'claude','display':'Claude agent lane','areas':['long-context comparison','architecture review','prompt pack contrast'],'cli':'claude','ready':tool('claude'),'safeToggle':'aihub-live'}]
    party=[
        {'id':'hermes-branch-sentinel','family':'Hermes','icon':'🛡️','xp':74,'fleetPoints':18,'type':'branch/autofix','specialization':'GitHub branches, commits, issues, PR safety, merge/prune packets','deploy':['local','codespaces','github-runner']},
        {'id':'xcore-native-sprinter','family':'XCore','icon':'⚡','xp':81,'fleetPoints':22,'type':'C++ performance','specialization':'hot paths, memory, graph comparisons, native scoring clusters','deploy':['local','wsl2','self-hosted-runner']},
        {'id':'xcore-fsharp-oracle','family':'XCore','icon':'🔮','xp':79,'fleetPoints':21,'type':'F# learning','specialization':'prediction, async queries, scoring, ranking, branch issue priority','deploy':['local','azure-container','github-runner']},
        {'id':'hermes-cloud-keeper','family':'Hermes','icon':'☁️','xp':68,'fleetPoints':16,'type':'Azure/Bicep','specialization':'Key Vault, Bicep what-if, SQL/Cosmos/vector, hybrid cloud policy','deploy':['azure','codespaces','cloud-shell']},
        {'id':'hermes-gui-guide','family':'Hermes','icon':'🎮','xp':72,'fleetPoints':20,'type':'GUI/UX','specialization':'dashboard cards, party member controls, settings, safe command copy, feedback loops','deploy':['status-site','remote-console','pages']}]
    settings=[
        {'id':'plan-only','label':'Plan only','description':'Generate reports and GUI with no live mutation.'},
        {'id':'auto-local','label':'Auto local','description':'Automatically run local-safe checks, learning, branch packets, and dashboard refresh.'},
        {'id':'repo-live','label':'Repo live','description':'Allow scoped GitHub operations after gh auth and live flag enablement.'},
        {'id':'cloud-live','label':'Cloud live','description':'Allow Azure what-if/deploy lanes after az login, Key Vault, cost caps, and rollback setup.'},
        {'id':'full-live','label':'Full hybrid','description':'Coordinate local/cloud/GitHub/AI lanes with all explicit live flags and logs.'}]
    modules=[
        {'module':'C# orchestration foundation','submodules':['secure API/vault/control flags','GUI state and command center','engine route planner','agent/fleet registry'],'fills':'Owns the stable frame and passes work to F#/C++/Python/Hermes/XCore.'},
        {'module':'F# learning engine','submodules':['branch issue priority','prediction scoring','agent XP ranking','module/submodule merge selection'],'fills':'Mathematical ranking and deep decision scoring for everyone.'},
        {'module':'C++ native performance engine','submodules':['weighted similarity','priority clusters','memory pressure scoring','large graph comparison'],'fills':'Fast comparison and hot-path execution for repo/commit/agent graphs.'},
        {'module':'Python AIHub glue','submodules':['provider discovery','Linux/library bridges','report generation','tool bootstrap'],'fills':'Only the integration glue where Python libraries and Linux automation are best.'},
        {'module':'YAML/Bicep/JSON/MD control fabric','submodules':['GitHub workflows','Azure IaC','live flags/config ledgers','wiki/docs absorption'],'fills':'Keeps pipelines, cloud, settings, and knowledge constantly upgradable.'}]
    backbone_bridge=self_learning_backbone_bridge()
    payload={'generatedUtc':datetime.now(timezone.utc).isoformat(),'providers':providers,'partyMembers':party,'settings':settings,'modules':modules,'selfLearningBackboneBridge':backbone_bridge,'recentCommits':lines('log','--all','--since=3 days ago','--pretty=%h %ad %s','--date=short')[:25],'branchIssues':lines('status','--short')[:50],'commands':['scripts/setup/bootstrap-local-tools.sh','python3 scripts/integrations/aihub_unified_control_plane.py','python3 scripts/agents/branch_test_autofix_plan.py','python3 scripts/integrations/aihub_integration_graph.py','python3 scripts/build_graph/build_graph.py run --profile full --max-workers 2','python3 scripts/dashboard/generate-gui.py'],'vaultPlan':['Use Azure Key Vault for API keys and service credentials when cloud-live is enabled.','Use local environment variables or dev secret files only for auto-local.','Never store pasted keys in the static GUI; copy commands and save via vault/CLI paths.'],'githubPlan':['Graph repos/branches/commits/issues/actions/runners/Codespaces/Pages/wiki as selectable units.','Run branch nervous-system checks before merge/prune/autofix.','Publish dashboards through Pages only when repo-live flag is enabled.'],'azurePlan':['Run what-if before mutation.','Represent Bicep/ARM, SQL, Cosmos/vector, Key Vault, Foundry/Fabric/Purview/Entra as cloud modules.','Keep cost caps, rollback requirements, and logs in live flags.']}
    OUT.parent.mkdir(parents=True,exist_ok=True); OUT.write_text(json.dumps(payload,indent=2)+'\n')
    md=['# AIHub Unified Control Plane','',f"Generated: `{payload['generatedUtc']}`",'','## Party members']
    md += [f"- {p['icon']} **{p['id']}** ({p['family']}): XP {p['xp']}, fleet points {p['fleetPoints']} — {p['specialization']}" for p in party]
    md += ['','## Providers']+[f"- **{p['display']}**: {', '.join(p['areas'])} — CLI `{p['cli']}` ready={p['ready']}" for p in providers]
    md += ['','## Module/submodule fill plan']+[f"- **{m['module']}**: {', '.join(m['submodules'])}. {m['fills']}" for m in modules]
    md += ['','## C# self-learning backbone bridge',f"- **{backbone_bridge['display']}** connects `{backbone_bridge['contract']}` with `{backbone_bridge['pythonSource']}` and `{backbone_bridge['reportSource']}`; stable docs live at `{backbone_bridge['stableMarkdown']}` and `{backbone_bridge['stableJson']}`."]
    md += [f"- {item}" for item in backbone_bridge['orchestration']]
    md += ['','## Commands']+[f"- `{c}`" for c in payload['commands']]
    MD.write_text('\n'.join(md)+'\n')
    print(f'Wrote {OUT.relative_to(ROOT)}'); print(f'Wrote {MD.relative_to(ROOT)}')
if __name__=='__main__': main()
