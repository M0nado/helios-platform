#!/usr/bin/env python3
from __future__ import annotations
import json, subprocess
from datetime import datetime, timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'reports/branch-agents/branch-test-autofix-plan.json'
MD=ROOT/'reports/branch-agents/branch-test-autofix-plan.md'

AGENT_SHELL_COMBOS={
 'hermes-csharp-orchestrator':{'type':'Hermes','shell':'local-web-gui + dotnet shell','llms':['ChatGPT/OpenAI','GitHub Copilot','local Ollama'],'bestFor':'GUI-safe orchestration, contracts, settings, and PR summaries'},
 'xcore-native-optimizer':{'type':'XCore','shell':'native build shell + cmake','llms':['local Ollama','Claude','Codex CLI'],'bestFor':'C++ hot paths, memory, rendering, security-sensitive comparisons'},
 'xcore-learning-scorer':{'type':'XCore','shell':'dotnet/F# analytics shell','llms':['ChatGPT/OpenAI','local Ollama'],'bestFor':'F# learning scores, predictions, branch value, and prune thresholds'},
 'hermes-python-aihub':{'type':'Hermes','shell':'Python AIHub shell + WSL2/Linux bridge','llms':['ChatGPT/OpenAI','Claude','Codex CLI','local Ollama'],'bestFor':'provider glue, agents, LLM routing, Linux tooling, and report generation'},
 'hermes-github-runner':{'type':'Hermes','shell':'GitHub CLI + Actions runner shell','llms':['GitHub Copilot','Codex CLI','ChatGPT/OpenAI'],'bestFor':'workflow recreation, branch packets, PR comments, Pages artifacts'},
 'xcore-azure-whatif':{'type':'XCore','shell':'Azure CLI + Bicep what-if shell','llms':['Microsoft 365 Copilot','ChatGPT/OpenAI','Claude'],'bestFor':'Key Vault, Bicep, cloud validation, cost/risk scoring'},
 'hermes-config-ledger':{'type':'Hermes','shell':'JSON/YAML config shell','llms':['ChatGPT/OpenAI','local Ollama'],'bestFor':'schemas, ledgers, live flags, model/tool settings'},
 'hermes-knowledge-pruner':{'type':'Hermes','shell':'Markdown/wiki knowledge shell','llms':['ChatGPT/OpenAI','Claude','local Ollama'],'bestFor':'code/document absorption, abstract idea retention, duplicate pruning'},
 'hermes-xcore-branch-nervous-system':{'type':'Hermes+XCore','shell':'multi-shell: git + dotnet + cmake + python + gh + az','llms':['ChatGPT/OpenAI','Claude','Codex CLI','GitHub Copilot','local Ollama'],'bestFor':'automatic branch testing, prune/merge recommendations, and cross-language evidence'}
}
PRUNE_THRESHOLDS={'absorbNow':0.82,'testAndMerge':0.64,'recordIdea':0.42,'pruneOrRewrite':'below 0.42 or duplicate with failing checks'}
AUTO_CONNECT_PLAN=[
 {'step':'absorb-context','command':'python3 scripts/analysis/branch_absorption_multicloud_plan.py','gui':'refresh refs, commits, top scored branch cards'},
 {'step':'test-specialties','command':'python3 scripts/agents/branch_test_autofix_plan.py','gui':'assign Hermes/XCore party members, shells, LLM combos, and checks'},
 {'step':'prune-merge','command':'python3 scripts/analysis/merge_prune_recommendations.py','gui':'show keep/merge/prune recommendations with evidence'},
 {'step':'branch-packets','command':'python3 scripts/agents/branch_fix_agents.py --max-branches 88','gui':'create isolated work packets for safe autofix'},
 {'step':'finish-dashboard','command':'scripts/setup/simple-build.sh finish','gui':'one-button finish path for GUI and remote console'}
]

SPECIALTIES={
 'csharp-gui-core':{'agent':'hermes-csharp-orchestrator','language':'C#','checks':['dotnet build HELIOS.Platform.slnx'],'fixStyle':'typed contracts, GUI/UX, secure orchestration'},
 'cpp-native-xcore':{'agent':'xcore-native-optimizer','language':'C++','checks':['cmake -S src/native/HELIOS.Native.Performance -B .build/native','cmake --build .build/native'],'fixStyle':'hot paths, memory, rendering, C ABI'},
 'fsharp-learning':{'agent':'xcore-learning-scorer','language':'F#','checks':['dotnet test tests/analytics/HELIOS.Analytics.FSharp.Tests/HELIOS.Analytics.FSharp.Tests.fsproj'],'fixStyle':'scoring, async queries, prediction, ranking'},
 'python-aihub':{'agent':'hermes-python-aihub','language':'Python','checks':['python3 -m py_compile scripts/agents/branch_test_autofix_plan.py scripts/build_graph/build_graph.py'],'fixStyle':'AIHub glue, reports, setup, agents'},
 'yaml-github':{'agent':'hermes-github-runner','language':'YAML','checks':['python3 scripts/control/validate_workflows.py'],'fixStyle':'workflows, runners, Codespaces, artifacts'},
 'bicep-azure':{'agent':'xcore-azure-whatif','language':'Bicep/ARM','checks':['python3 scripts/azure/azure_connection_pipeline.py --stage all'],'fixStyle':'what-if, Key Vault, Azure safety'},
 'json-config':{'agent':'hermes-config-ledger','language':'JSON','checks':['python3 -m json.tool config/build-graph.json >/dev/null'],'fixStyle':'schemas, ledgers, provider/tool configs'},
 'docs-knowledge':{'agent':'hermes-knowledge-pruner','language':'Markdown','checks':['python3 scripts/analysis/document_code_absorption_ranker.py'],'fixStyle':'absorb unique ideas, prune redundant docs'},
 'branch-nervous-system':{'agent':'hermes-xcore-branch-nervous-system','language':'C#/F#/C++','checks':['python3 scripts/integrations/aihub_integration_graph.py','python3 scripts/build_graph/build_graph.py run --profile full --max-workers 2'],'fixStyle':'branch issue graph, F# priority scoring, C++ hot-path comparison, C# orchestration routing'}}

def refs():
 p=subprocess.run(['git','for-each-ref','--format=%(refname:short)|%(objectname:short)|%(subject)','refs/heads','refs/remotes'],cwd=ROOT,text=True,capture_output=True)
 return [dict(zip(['ref','commit','subject'],line.split('|',2))) for line in p.stdout.splitlines() if line.count('|')>=2]

def pick_specialty(ref):
 text=(ref['ref']+' '+ref['subject']).lower()
 for key in SPECIALTIES:
  if any(t in text for t in key.split('-')): return key
 if any(x in text for x in ['azure','bicep','cloud']): return 'bicep-azure'
 if any(x in text for x in ['workflow','github','runner']): return 'yaml-github'
 if any(x in text for x in ['branch','merge','prune','autofix','test']): return 'branch-nervous-system'
 if any(x in text for x in ['agent','aihub','python','hermes']): return 'python-aihub'
 return 'csharp-gui-core'

def main():
 rows=[]
 for r in refs():
  spec=pick_specialty(r); s=SPECIALTIES[spec]
  rows.append({'ref':r['ref'],'commit':r['commit'],'subject':r['subject'],'specialty':spec,'agent':s['agent'],'language':s['language'],'checks':s['checks'],'shellCombo':AGENT_SHELL_COMBOS.get(s['agent'],{}),'prunePlan':['score novelty/security/performance/learning through C#/F#/C++ helpers','absorb unique ideas before pruning duplicate code','prefer auto-local fixes for safe generated/report/config changes','mark branch for merge/prune only after checks and rollback packet exist'],'autofixPlan':['create isolated worktree','run specialty checks','apply minimal safe fix only in matching language/module','rerun checks','record report and PR notes','auto-apply local fixes when policy allows; external push/delete uses explicit live flags and credentials']})
 payload={'generatedUtc':datetime.now(timezone.utc).isoformat(),'principle':'Automatic branch testing/autofix/prune uses safe isolated worktree packets. Hermes/XCore specialties pick the right language/tool chain, shell, LLM combo, and checks; local policy-safe fixes can auto-apply when enabled, while external push/delete/cloud steps require explicit live flags and credentials.','specialties':SPECIALTIES,'agentShellCombos':AGENT_SHELL_COMBOS,'pruneThresholds':PRUNE_THRESHOLDS,'autoConnectPlan':AUTO_CONNECT_PLAN,'branches':rows,'automationModes':['plan-only','auto-local','repo-live-with-explicit-flags','cloud-live-with-explicit-flags'], 'runAllCommand':'scripts/setup/simple-build.sh finish && python3 scripts/analysis/merge_prune_recommendations.py && python3 scripts/agents/branch_fix_agents.py --max-branches 88'}
 OUT.parent.mkdir(parents=True,exist_ok=True); OUT.write_text(json.dumps(payload,indent=2)+'\n')
 lines=['# Branch Test / Autofix Plan','',f"Generated: `{payload['generatedUtc']}`",'',payload['principle'],'','## Specialties']
 for k,s in SPECIALTIES.items(): lines += [f"### {k}",f"- Agent: `{s['agent']}`",f"- Language: {s['language']}",f"- Fix style: {s['fixStyle']}"]+[f"- Check: `{c}`" for c in s['checks']]
 lines += ['','## Auto-connect test/prune pipeline']+[f"- **{p['step']}**: `{p['command']}` — {p['gui']}" for p in AUTO_CONNECT_PLAN]
 lines += ['','## Agent shell / LLM combos']
 for agent,combo in AGENT_SHELL_COMBOS.items(): lines += [f"### {agent}",f"- Type: {combo['type']}",f"- Shell: `{combo['shell']}`",f"- LLMs: {', '.join(combo['llms'])}",f"- Best for: {combo['bestFor']}"]
 lines += ['','## Prune thresholds']+[f"- {k}: {v}" for k,v in PRUNE_THRESHOLDS.items()]
 lines += ['','## Branch packets','','| Ref | Specialty | Agent | Language | Shell/LLMs | Checks |','| --- | --- | --- | --- | --- | --- |']
 for r in rows[:120]:
  combo=r.get('shellCombo',{})
  shell=combo.get('shell','')
  llms=', '.join(combo.get('llms',[])[:3])
  lines.append(f"| `{r['ref']}` | {r['specialty']} | `{r['agent']}` | {r['language']} | {shell}<br>{llms} | {'<br>'.join('`'+c+'`' for c in r['checks'])} |")
 lines += ['','## Run all',f"`{payload['runAllCommand']}`"]
 MD.write_text('\n'.join(lines)+'\n')
 print(f'Wrote {OUT.relative_to(ROOT)}'); print(f'Wrote {MD.relative_to(ROOT)}')
 return 0
if __name__=='__main__': raise SystemExit(main())
