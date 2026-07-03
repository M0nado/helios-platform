#!/usr/bin/env python3
from __future__ import annotations
import json
from datetime import datetime, timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'reports/learning/language-engine-catalog.json'
MD=ROOT/'reports/learning/language-engine-catalog.md'
RANKER=ROOT/'reports/learning/document-code-absorption-ranker.json'
ENGINES={
 'csharp-orchestrator': {
  'shape':'secure AI-access framework, GUI shell, contracts, plugin host, cross-language orchestrator',
  'tests':['dotnet build HELIOS.Platform.slnx','dotnet test src/tests/HELIOS.Platform.Tests/HELIOS.Platform.Tests.csproj'],
  'capabilities':['secure AI access gateway','GUI shell and MVVM','service orchestration','contract and DTO boundary','plugin host lifecycle','cloud sync control','vault access facade','authentication boundary','authorization checks','safe config modeling','dependency injection graph','observability/logging facade','error isolation','sandbox policy','cross-language adapters','C++ native bridge facade','F# analytics bridge facade','Python AIHub bridge facade','workflow status display','dashboard host','theme/state shell','device control facade','audio control facade','system tray shell','settings management','profile selection','remote console host','API compatibility boundary','database migration owner','EF/SQLite state store','test harness owner','mock provider host','agent command router','LLM provider access policy','failure recovery orchestration','background service host','extension registry','marketplace shell','telemetry DTOs','release gates','security audit surface','secret-safe settings','Azure control facade','GitHub control facade','branch repair UI surface','knowledge report viewer','runner command launcher','Codespaces UX shell','documentation link hub','typed integration contracts','long-term maintainability anchor']
 },
 'cpp-native-performance': {
  'shape':'native C++ performance, memory, rendering, SIMD, vector/SQL acceleration, hot-path algorithm engine',
  'tests':['cmake -S src/native/HELIOS.Native.Performance -B .build/native','python3 scripts/build_graph/build_graph.py run --tag native --max-workers 2'],
  'capabilities':['hot-path algorithm kernels','manual memory layout','SIMD/vectorization','GPU interop','rendering kernels','particle/effect updates','frame timing analysis','native audio DSP','low-latency device paths','XCore bridge runtime','native model inference','memory pooling','cache-aware transforms','zero-copy buffers','large-array scans','binary parsers','compression kernels','encryption primitives wrapper','benchmark baselines','profiling hooks','thread pools','lock-free queues','native graph algorithms','geometry transforms','image/OCR preprocessing','signal processing','hardware telemetry collectors','platform interop','driver-adjacent calls','native plugin ABI','fast serialization','diff/merge algorithms','dedupe hashing','similarity search kernels','ranking primitive accelerators','time-series compaction','batch matrix kernels','runtime memory guards','crash-isolated libraries','C ABI exports','C# P/Invoke target','native worker core','render graph experiments','performance regression tests','native fuzz targets','resource schedulers','allocator experiments','streaming pipelines','file watcher acceleration','large document shingling','line-level scoring acceleration','branch graph scoring acceleration']
 },

 'xcore-agent-engine': {
  'shape':'XCore local/cloud self-learning agent family, similar to Hermes/Fleet but focused on worker runtime, XP progression, local model routing, and native/F#/Python engine dispatch',
  'tests':['python3 scripts/agents/agent_specialization_matrix.py','python3 scripts/integrations/deep_agent_readiness.py'],
  'capabilities':['local XCore worker agents','cloud XCore worker agents','XP/progression bars','agent leveling records','worker persona selection','multi-model routing','local web model routing','cloud LLM routing','Hermes/Fleet coordination','C# command contract','Python launcher glue','F# scoring handoff','C++ hot-path handoff','vector memory hooks','SQL memory hooks','task natural selection','chaos experiment routing','outcome learning','speed/cost scoring','fleet composition','parallel agent lanes','agent rollback policy','safe dry-run operations','GitHub workflow handoff','Codespaces handoff','WSL2 launch guidance','Windows launch guidance','dashboard status cards','JRPG GUI presence','agent skill inventory','tool inventory','custom instruction bundles','sub-agent routing','model benchmark ledger','prompt packet memory','branch repair participation','module scoring participation','test selection participation','merge/prune suggestions','knowledge absorption loops','abstract idea routing','self-check reports','local capacity detection','cloud capacity detection','auth boundary respect','non-destructive planning','manual approval markers','observability events','failure recovery','provider fallback','report artifact emission','learning confidence grades','agent specialization matrix links']
 },
 'fsharp-analytics-engine': {
  'shape':'async queries, mathematical prediction, analysis, search, ranking, deep scoring, immutable pipelines',
  'tests':['dotnet test tests/analytics/HELIOS.Analytics.FSharp.Tests/HELIOS.Analytics.FSharp.Tests.fsproj'],
  'capabilities':['async query composition','immutable data pipelines','statistical modeling','prediction models','ranking algorithms','search scoring','Bayesian scoring','time-series analysis','feature extraction','data normalization','anomaly detection','forecasting','model evaluation','confidence intervals','probabilistic routing','semantic scoring','branch quality scoring','merge risk scoring','prune/keep classification','document idea scoring','JSTOR literature scoring','knowledge absorption ranking','test coverage scoring','module health scoring','submodule dependency scoring','language placement scoring','F# computation expressions','parallel map/reduce','type-safe domain modeling','discriminated unions for actions','pattern matching classifiers','pure transform tests','property-based test targets','graph scoring','agent assignment scoring','Hermes/Fleet prioritization','XCore workload scoring','Bicep risk scoring','YAML workflow risk scoring','security finding severity scoring','LLM provider weighting','multi-objective optimization','Pareto ranking','dedupe similarity scoring','abstract idea clustering','small-code synthesis ranking','learning feedback loops','cross-repo comparison scores','trend analysis','performance prediction','resource allocation models','query planners','async data ingestion','strong math DSLs','explainable scoring records','testable analytics kernels','deep learning orchestration facade','model metadata records','validation metrics aggregation','knowledge confidence grading']
 },
 'python-aihub-tooling': {
  'shape':'Linux tooling, AIHub/library glue, report generation, Hermes/Fleet ops, cross-LLM adapters',
  'tests':['python3 -m py_compile scripts/build_graph/build_graph.py scripts/analysis/document_code_absorption_ranker.py','python3 scripts/build_graph/build_graph.py run --profile quick --changed-only'],
  'capabilities':['repo scanning','report generation','GitHub CLI wrappers','Azure CLI wrappers','OpenAI SDK glue','Claude/Anthropic glue','Codex task packets','Hermes/Fleet readiness','branch fix packet generation','workflow summaries','Markdown generation','JSON report emission','JSTOR export ingestion placeholder','Linux setup scripts','tool bootstrap','dashboard static generation','build graph orchestration','secret preflight scanning','apply gate scanning','what-if wrappers','Codespaces helpers','wiki dry-run publishing','artifact summaries','commit window scans','knowledge absorption reports','language optimizer reports','module matrix reports','document shingling','line-level metrics','CI status parsing','test command routing','parallel worker orchestration','safe shell wrappers','provider config templates','environment validation','local port checks','YAML validation glue','Bicep wrapper glue','cross-LLM prompt bundles','AIHub data adapters','Hermes agent packets','XCore build launchers','branch fetch/report only','merge recommendation reports','prune candidate reports','abstract idea extraction','small-code synthesis planning','trend summaries','external corpus connectors','JSTOR planned connector','security allowlist tooling','dashboard action buttons','finish setup orchestration','non-mutating automation gates']
 },

 'bicep-cloud-infra': {
  'shape':'Azure Bicep super-cloud infrastructure as code with what-if, Key Vault, environment gates, and cloud shell plans',
  'tests':['python3 scripts/azure/azure_connection_pipeline.py --stage all','python3 scripts/azure/azure_what_if.py'],
  'capabilities':['Azure what-if planning','Key Vault module','storage module','network module','identity module','monitoring module','Bicep parameters','resource group gate','subscription gate','cloud shell setup','managed identity','secret references','least privilege roles','diagnostic settings','private endpoint plan','cost tags','environment tags','rollback notes','module outputs','dependency graph','deployment dry-run','policy check','security baseline','location strategy','naming strategy','tenant boundary','resource locks','template spec plan','deployment history','manual approval gate']
 },
 'json-config-ledger': {
  'shape':'machine-readable config, learning ledgers, provider profiles, XP bars, agent matrices, and report contracts',
  'tests':['python3 -m json.tool config/build-graph.json >/dev/null','python3 -m json.tool config/language-decision-variables.json >/dev/null'],
  'capabilities':['agent persona config','language score config','provider profile config','workflow inventory','module score cache','branch packet data','XP ledger','model cost table','vector collection manifest','SQL schema manifest','Bicep parameter plan','dashboard data','knowledge source map','allowlist','readiness summary','test matrix','merge/prune plan','abstract idea ledger','runner capability map','local/cloud capacity map','skill/tool registry','custom instruction bundle','sub-agent routing table','fleet composition','quality thresholds','selection variables','benchmark baselines','provider limits','auth state summary','report index']
 },
 'yaml-bicep-md-knowledge': {
  'shape':'workflow, cloud infrastructure, documentation, wiki, JSTOR/literature knowledge objects',
  'tests':['python3 -m json.tool config/build-graph.json >/dev/null','python3 scripts/analysis/document_code_absorption_ranker.py'],
  'capabilities':['GitHub workflow definitions','runner matrices','scheduled agents','manual dispatch forms','artifact upload plans','CI gates','quality jobs','Azure Bicep modules','Key Vault declarations','storage declarations','network declarations','observability declarations','what-if parameters','cloud shell docs','wiki pages','Codespaces guides','agent instructions','merge plans','prune plans','testing plans','language role docs','decision variables','JSTOR literature notes','research citations','abstract idea records','release notes','architecture guides','API references','dashboard docs','Hermes Swift reports','XCore plans','security checklists','vault policies','provider templates','prompt templates','learning reports','knowledge matrices','module maps','submodule maps','line-level findings','benchmark docs','optimization reports','branch packets','PR body templates','issue templates','workflow references','setup guides','troubleshooting guides','external corpus exports','absorb/keep/prune decisions','redundancy notes','unique insight records','graded idea summaries','future implementation stubs']
 }
}

def load_ranker():
 if not RANKER.exists(): return []
 try: return json.loads(RANKER.read_text()).get('topAbsorb', [])[:60]
 except Exception: return []

def main():
 ideas=[]
 for item in load_ranker():
  ideas.append({'source':item.get('path'),'score':item.get('score'),'ideas':item.get('ideas',[]),'targetEngines':targets(item.get('ideas',[]), item.get('path',''))})
 payload={'generatedUtc':datetime.now(timezone.utc).isoformat(),'engineCount':len(ENGINES),'engines':ENGINES,'abstractIdeas':ideas,'principle':'Every engine is self-describing; documents/code are absorbed into abstract ideas, graded, then routed to C# orchestration, C++/XCore performance, F# analytics/deep scoring, Python AIHub tooling, or YAML/Bicep/MD knowledge assets.'}
 OUT.parent.mkdir(parents=True,exist_ok=True); OUT.write_text(json.dumps(payload,indent=2)+'\n')
 lines=['# Language Engine Catalog and Synthesis','',f"Generated: `{payload['generatedUtc']}`",'',payload['principle'],'']
 for name,engine in ENGINES.items():
  lines += [f"## {name}",'',f"Shape: {engine['shape']}",'','### Tests']+[f"- `{t}`" for t in engine['tests']]
  lines += ['','### 50+ capabilities']+[f"- {c}" for c in engine['capabilities']]+['']
 lines += ['## Abstract ideas from absorption ranker','','| Score | Source | Target engines | Ideas |','| ---: | --- | --- | --- |']
 for idea in ideas:
  lines.append(f"| {idea['score']} | `{idea['source']}` | {', '.join(idea['targetEngines'])} | {', '.join(idea['ideas'])} |")
 MD.write_text('\n'.join(lines)+'\n')
 print(f"Wrote {OUT.relative_to(ROOT)}"); print(f"Wrote {MD.relative_to(ROOT)}")
 return 0

def targets(ideas,path):
 text=' '.join(ideas+[path]).lower(); out=[]
 if any(x in text for x in ['gui','security','vault','contract']): out.append('csharp-orchestrator')
 if any(x in text for x in ['xcore','performance','render','memory']): out.append('cpp-native-performance'); out.append('xcore-agent-engine') if 'xcore' in text else None
 if any(x in text for x in ['analytics','prediction','score','rank','learn','jstor']): out.append('fsharp-analytics-engine')
 if any(x in text for x in ['agent','hermes','fleet','openai','codex','llm','automation']): out.append('python-aihub-tooling')
 if 'bicep' in text or 'azure' in text: out.append('bicep-cloud-infra')
 if 'json' in text or 'config' in text: out.append('json-config-ledger')
 if any(x in text for x in ['wiki','codespace','markdown','yaml','cloud']): out.append('yaml-bicep-md-knowledge')
 return out or ['csharp-orchestrator']
if __name__=='__main__': raise SystemExit(main())
