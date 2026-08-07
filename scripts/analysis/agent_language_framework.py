#!/usr/bin/env python3
from __future__ import annotations
import json, shutil, subprocess
from collections import Counter, defaultdict
from datetime import datetime, timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'reports/learning/agent-language-framework.json'
MD=ROOT/'reports/learning/agent-language-framework.md'
SINCE='2025-07-29 00:00'

LANG_EXT={'.cs':'csharp','.csproj':'csharp','.fs':'fsharp','.fsproj':'fsharp','.cpp':'cpp','.hpp':'cpp','.h':'cpp','.cmake':'cpp','.py':'python','.ps1':'automation','.yml':'yaml','.yaml':'yaml','.bicep':'bicep','.md':'markdown','.json':'json','.html':'html'}
DOMAIN_TERMS={
 'csharp-core':['src/core','src/gui','.csproj','.cs','orchestrator','contract','vault','security','gui','maui','winui'],
 'cpp-native':['src/native','.cpp','.hpp','cmake','performance','render','memory','simd','native','hotpath'],
 'xcore-agent':['xcore','agent','worker','runtime','local','cloud','fleet','xp','model'],
 'fsharp-learning':['src/analytics','.fs','.fsproj','analytics','score','rank','predict','query','math','learning'],
 'python-aihub':['scripts/','.py','agent','hermes','fleet','aihub','llm','openai','codex','automation'],
 'yaml-workflow':['.github','.yml','.yaml','workflow','runner','codespace','pipeline','github'],
 'bicep-cloud':['infra','bicep','azure','keyvault','resource','what-if','cloud'],
 'markdown-jstor':['.md','docs','wiki','jstor','knowledge','absorb','prune','merge','learn'],
 'json-config':['.json','config','profile','manifest','matrix','ledger','packet']
}
ENGINE_BLUEPRINTS={
 'csharp-core':('C# secure orchestrator/framework','Owns stable contracts, GUI/JRPG shell, AI access policy, plugins, typed bridges, and safe command routing into C++, F#, Python, Hermes/Fleet, XCore, cloud, GitHub, and local web models.'),
 'cpp-native':('C++ native performance engine','Owns hot paths: memory/layout, rendering, SIMD, native ranking primitives, OCR/image preprocessing, vector search kernels, SQL/vector index acceleration, chaos/permutation kernels, and benchmarked libraries behind C# contracts.'),
 'xcore-agent':('XCore self-learning agent family','Owns local/cloud XCore agents, XP/progression bars, worker personas, fleet coordination, model selection, learning memory, task evolution, and C++/F#/Python engine dispatch through the C# control plane.'),
 'fsharp-learning':('F# analytics/deep-learning scorer','Owns async query composition, immutable scoring, prediction, branch/module/test ranking, prune/merge risk, agent allocation, multi-objective optimization, and explainable learning records.'),
 'python-aihub':('Python AIHub/Hermes/Fleet automation','Owns Linux/library glue, local/cloud agent launchers, multi-LLM adapters, report generation, setup, branch packets, and safe CLI wrappers.'),
 'yaml-workflow':('YAML GitHub automation control','Owns workflows, runners, Codespaces, artifacts, matrices, scheduled fleets, safe GitHub CLI pipeline control, and non-destructive repository automation.'),
 'bicep-cloud':('Bicep Azure super-cloud infrastructure','Owns Azure what-if, Key Vault, cloud resource layout, parameters, environment gates, cloud shell setup, and safe IaC planning.'),
 'markdown-jstor':('Markdown/JSTOR/wiki knowledge corpus','Owns research notes, wiki plans, architecture docs, absorption records, abstract ideas, test evidence, and prune/keep rationale.'),
 'json-config':('JSON configuration and learning ledgers','Owns agent persona configs, provider profiles, score ledgers, XP bars, selection matrices, module caches, and machine-readable report contracts.')
}
VARIABLES=['security','reliability','latency','throughput','memory','rendering','interop','testability','observability','auth','vault','cloud','cost','speed','accuracy','learning','prediction','ranking','merge-risk','prune-risk','redundancy','novelty','abstract-idea','module-fit','submodule-fit','branch-fit','agent-fit','fleet-fit','local-model-fit','cloud-model-fit','multi-llm-fit','provider-choice','data-size','parallelism','async-query','immutability','native-abi','gui-fit','jrpg-ui-fit','dashboard-fit','codespace-fit','runner-fit','yaml-risk','bicep-risk','jstor-value','docs-value','hermes-value','xcore-value','python-library-value','csharp-contract-value','fsharp-math-value','cpp-hotpath-value','maintenance','portability','linux-fit','windows-fit','rollback','dry-run-safety','human-review','benchmark-need']
DETAIL_CAPABILITIES={
 'cpp-native':['SIMD vector kernels','cache-aware array transforms','manual memory pools','zero-copy buffers','native rendering loops','GPU interop boundaries','OCR preprocessing','image tiling','audio DSP','binary parsers','compression kernels','crypto wrapper hot paths','lock-free queues','thread pinning experiments','C ABI exports','P/Invoke surfaces','FFI-safe structs','benchmark harnesses','perf counters','fuzzable native APIs','large file shingling','dedupe hashing','similarity kernels','vector index search','SQL extension acceleration','graph traversal kernels','merge diff accelerators','branch scoring accelerators','chaos simulation kernels','natural-selection tournament loops','matrix operations','stream compaction','allocator guards','crash isolation','native logging hooks','real-time telemetry collectors','render graph passes','sprite batching','particle updates','physics kernels','pathfinding kernels','model inference adapters','feature extraction','hardware probing','filesystem watchers','memory-mapped reads','parallel sort','top-k selection','bitmap transforms','latency histograms'],
 'python-aihub':['OpenAI adapter','local model adapter','Anthropic adapter','multi-LLM router','Hermes launcher','Fleet planner','XCore launcher','Linux setup','WSL2 setup guide','Windows CLI wrapper','GitHub CLI wrapper','Azure CLI wrapper','report writer','JSON normalizer','Markdown writer','YAML validator wrapper','Bicep what-if wrapper','vector DB client glue','SQL client glue','agent memory import','prompt packet generation','workflow artifact collection','Codespaces helper','runner preflight','cost estimator','speed/outcome tracker','XP report writer','safe dry-run gate','provider fallback','dashboard generation'],
 'yaml-workflow':['workflow dispatch','scheduled fleets','runner matrix','artifact upload','branch-fix packets','build graph CI','quality gates','Codespaces defaults','permissions minimization','OIDC login plan','Azure what-if','Bicep module layout','Key Vault references','storage account plan','network plan','monitoring plan','environment approvals','manual auth gates','secret-free variables','cloud shell setup','job summaries','dependency caching','dotnet setup','cmake setup','python setup','report publishing','wiki dry-run','PR comment plan','release gate','rollback job plan'],
 'bicep-cloud':['Azure what-if planning','Key Vault module','storage module','network module','identity module','monitoring module','Bicep parameters','resource group gate','subscription gate','cloud shell setup','managed identity','secret references','least privilege roles','diagnostic settings','private endpoint plan','cost tags','environment tags','rollback notes','module outputs','dependency graph','deployment dry-run','policy check','security baseline','location strategy','naming strategy','tenant boundary','resource locks','template spec plan','deployment history','manual approval gate'],
 'markdown-jstor':['architecture summary','merge rationale','prune rationale','abstract idea','literature note','JSTOR citation placeholder','benchmark explanation','test evidence','agent persona doc','XP progression doc','JRPG GUI behavior','Hermes history','XCore history','module map','submodule map','line-level insight','redundancy record','novelty record','risk note','security note','cloud note','workflow note','wiki page','setup guide','troubleshooting guide','decision log','cost/speed outcome','multi-model comparison','natural-selection result','chaos experiment note'],
 'json-config':['agent persona config','language score config','provider profile config','workflow inventory','module score cache','branch packet data','XP ledger','model cost table','vector collection manifest','SQL schema manifest','Bicep parameter plan','dashboard data','knowledge source map','allowlist','readiness summary','test matrix','merge/prune plan','abstract idea ledger','runner capability map','local/cloud capacity map','skill/tool registry','custom instruction bundle','sub-agent routing table','fleet composition','quality thresholds','selection variables','benchmark baselines','provider limits','auth state summary','report index']
}

def run(args):
 p=subprocess.run(args,cwd=ROOT,text=True,capture_output=True)
 return p.stdout.splitlines()

def files():
 if shutil.which('rg'):
  p=subprocess.run(['rg','--files'],cwd=ROOT,text=True,capture_output=True)
  if p.returncode in (0,1):
   return [x for x in p.stdout.splitlines() if x]
 p=subprocess.run(['git','ls-files'],cwd=ROOT,text=True,capture_output=True)
 return [x for x in p.stdout.splitlines() if x] if p.returncode==0 else []

def lang(path):
 if path.endswith('CMakeLists.txt'): return 'cpp'
 return LANG_EXT.get(Path(path).suffix.lower(),'other')

def git_lines(args):
 return run(['git']+args)

def classify(text):
 low=text.lower(); scores={k:sum(1 for t in terms if t in low) for k,terms in DOMAIN_TERMS.items()}
 best=max(scores,key=scores.get)
 return best if scores[best] else 'docs-jstor-knowledge'

def modules(all_files):
 buckets=defaultdict(list)
 for f in all_files:
  parts=f.split('/')
  key='/'.join(parts[:2]) if len(parts)>1 else parts[0]
  buckets[key].append(f)
 rows=[]
 for module,paths in sorted(buckets.items()):
  counts=Counter(lang(p) for p in paths)
  domain=classify(' '.join([module]+paths[:40]))
  tests=[p for p in all_files if ('test' in p.lower() or '/tests/' in p.lower()) and any(x.lower() in p.lower() for x in module.split('/'))][:20]
  score=counts.get('csharp',0)*2+counts.get('cpp',0)*3+counts.get('fsharp',0)*4+counts.get('python',0)*2+len(tests)*2+len(paths)
  rows.append({'module':module,'domain':domain,'score':score,'fileCount':len(paths),'languages':dict(counts),'tests':tests,'sampleFiles':paths[:20]})
 return sorted(rows,key=lambda r:(-r['score'],r['module']))

def commits():
 lines=git_lines(['log','--all',f'--since={SINCE}','--date=iso-strict','--pretty=format:%H%x09%ad%x09%s','--name-only'])
 out=[]; cur=None
 for line in lines:
  if '\t' in line:
   if cur: out.append(cur)
   h,d,s=line.split('\t',2); cur={'hash':h[:12],'date':d,'subject':s,'files':[],'domain':classify(s)}
  elif cur and line.strip(): cur['files'].append(line.strip())
 if cur: out.append(cur)
 for c in out: c['domain']=classify(c['subject']+' '+' '.join(c['files'][:30]))
 return out

def branches():
 names=git_lines(['for-each-ref','--format=%(refname:short)|%(committerdate:iso-strict)|%(objectname:short)|%(subject)','refs/heads','refs/remotes'])
 rows=[]
 for n in names:
  parts=n.split('|',3)
  if len(parts)==4: rows.append({'ref':parts[0],'date':parts[1],'commit':parts[2],'subject':parts[3],'domain':classify(parts[0]+' '+parts[3])})
 return rows

def engine_records():
 records=[]
 for key,(title,shape) in ENGINE_BLUEPRINTS.items():
  capabilities=(DETAIL_CAPABILITIES.get(key, []) + [f'{v} optimized for {title}' for v in VARIABLES])
  tests={
   'csharp-core':['dotnet build HELIOS.Platform.slnx','dotnet test src/tests/HELIOS.Platform.Tests/HELIOS.Platform.Tests.csproj'],
   'cpp-native':['cmake -S src/native/HELIOS.Native.Performance -B .build/native','cmake --build .build/native'],
   'xcore-agent':['python3 scripts/agents/agent_specialization_matrix.py','python3 scripts/integrations/deep_agent_readiness.py'],
   'fsharp-learning':['dotnet test tests/analytics/HELIOS.Analytics.FSharp.Tests/HELIOS.Analytics.FSharp.Tests.fsproj'],
   'python-aihub':['python3 -m py_compile scripts/agents/branch_fix_agents.py scripts/build_graph/build_graph.py','python3 scripts/agents/hermes_fleet_readiness.py'],
   'yaml-workflow':['python3 -m json.tool config/build-graph.json >/dev/null','python3 scripts/control/validate_workflows.py'],
   'bicep-cloud':['python3 scripts/azure/azure_connection_pipeline.py --stage all','python3 scripts/azure/azure_what_if.py'],
   'markdown-jstor':['python3 scripts/analysis/document_code_absorption_ranker.py','python3 scripts/analysis/code_learning_atlas.py'],
   'json-config':['python3 -m json.tool config/build-graph.json >/dev/null','python3 -m json.tool config/language-decision-variables.json >/dev/null']
  }[key]
  records.append({'id':key,'title':title,'shape':shape,'variables':VARIABLES,'capabilities':capabilities,'tests':tests})
 return records

def agent_fleets():
 archetypes=['hermes-local-python-agent','hermes-cloud-python-agent','fleet-coordinator','xcore-native-agent','fsharp-scoring-agent','csharp-orchestrator-agent','multi-llm-router','jrpg-gui-guide-agent','github-branch-repair-agent','super-cloud-what-if-agent']
 return [{'id':a,'domain':classify(a),'local':True,'cloud':a not in {'xcore-native-agent'},'connectors':['csharp-core','python-aihub']+(['fsharp-learning'] if 'scoring' in a or 'router' in a else [])+(['xcore-agent'] if 'xcore' in a else []),'selectionSignals':VARIABLES[:20]} for a in archetypes]

def main():
 all_files=files(); mod=modules(all_files); com=commits(); br=branches(); engines=engine_records()
 payload={'generatedUtc':datetime.now(timezone.utc).isoformat(),'since':SINCE,'principle':'C# is the secure core/orchestrator and JRPG/super GUI access layer; C++, XCore, F#, and Python are specialized engines selected by measured module/submodule/branch/test fit. Hermes/Fleet and XCore are local/cloud self-learning agent families coordinated through C# and Python, scored by F#, and accelerated by C++ where useful.','engines':engines,'agentFleets':agent_fleets(),'moduleFramework':mod,'branchFramework':br,'commitFramework':com,'rankedComparisons':{'topModules':mod[:50],'topCommits':com[:50],'visibleBranches':br[:88]},'mergePruneRules':['keep unique ideas even when code is pruned','prefer C++ for repeated hot paths and rendering/native memory work','prefer F# for scoring/prediction/ranking/async query engines','prefer Python for agent/library/Linux/setup glue','keep C# thin, typed, secure, observable, and responsible for AI access policy','require tests and report evidence before merge/prune action']}
 OUT.parent.mkdir(parents=True,exist_ok=True); OUT.write_text(json.dumps(payload,indent=2)+'\n')
 lines=['# Agent Language Framework System','',f"Generated: `{payload['generatedUtc']}`",'',payload['principle'],'','## Engines']
 for e in engines:
  lines += [f"### {e['title']}",'',e['shape'],'',f"Variables: {len(e['variables'])}",'','Tests:']+[f"- `{t}`" for t in e['tests']]+['','Sample capabilities:']+[f"- {c}" for c in e['capabilities'][:20]]+['']
 lines += ['## Agent fleets','','| Agent | Domain | Local | Cloud | Connectors |','| --- | --- | --- | --- | --- |']
 for a in payload['agentFleets']:
  lines.append(f"| `{a['id']}` | {a['domain']} | {a['local']} | {a['cloud']} | {', '.join(a['connectors'])} |")
 lines += ['','## Top module/submodule comparisons','','| Score | Module | Domain | Files | Languages | Tests |','| ---: | --- | --- | ---: | --- | ---: |']
 for r in mod[:50]: lines.append(f"| {r['score']} | `{r['module']}` | {r['domain']} | {r['fileCount']} | {', '.join(f'{k}:{v}' for k,v in r['languages'].items())} | {len(r['tests'])} |")
 lines += ['','## Visible branch/commit framework since 2025-07-29','','| Kind | Ref/Commit | Domain | Subject |','| --- | --- | --- | --- |']
 for b in br[:88]: lines.append(f"| branch | `{b['ref']}` | {b['domain']} | {b['subject']} |")
 for c in com[:50]: lines.append(f"| commit | `{c['hash']}` | {c['domain']} | {c['subject']} |")
 lines += ['','## Merge / prune / absorb rules']+[f"- {x}" for x in payload['mergePruneRules']]
 MD.write_text('\n'.join(lines)+'\n')
 print(f'Wrote {OUT.relative_to(ROOT)}'); print(f'Wrote {MD.relative_to(ROOT)}')
 return 0
if __name__=='__main__': raise SystemExit(main())
