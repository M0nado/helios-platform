#!/usr/bin/env python3
from __future__ import annotations
import json
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
CONFIG=ROOT/'config/language-decision-variables.json'
OUT=ROOT/'reports/learning/language-decision-matrix.json'
MD=ROOT/'reports/learning/language-decision-matrix.md'
LANGS=['csharp','cpp','fsharp','python','yaml','bicep','html']
SCENARIOS={
 'csharp-orchestrator-core':['secureBoundary','guiShell','pluginHost','apiContracts','failureIsolation','sandboxing','crossLlmRouting'],
 'cpp-xcore-performance-engine':['latencyCritical','memoryOwnership','graphicsRendering','gpuInterop','xcoreBridge','parallelism','simdVectorization','animationHotPath'],
 'fsharp-analytics-learning-engine':['statisticalModeling','rankingSearch','deepScoring','stateMachine','dataNormalization','featureExtraction','largeDataTransform'],
 'python-aihub-hermes-tooling':['linuxAutomation','aiProviderSdk','libraryAvailability','branchRepair','hermesFleetAgent','reportGeneration','codexTaskGeneration'],
 'super-cloud-yaml-bicep':['cloudControl','bicepInfra','workflowCi','azureWhatIf','bicepModuleAuthoring','workflowMatrix','deploymentSafety'],
 'smart-prune-merge-absorb':['pruneMergeAnalysis','knowledgeAbsorption','generatedArtifacts','branchRepair','testability','observability','incrementalMigration']
}

def main():
 data=json.loads(CONFIG.read_text()); variables=data['variables']; by_name={v['name']:v for v in variables}
 scenario_rows=[]
 for name, vars_ in SCENARIOS.items():
  scores=Counter()
  details=[]
  for var in vars_:
   row=by_name[var]; details.append(row)
   for lang in LANGS: scores[lang]+=row.get(lang,0)
  ranked=scores.most_common()
  scenario_rows.append({'scenario':name,'variables':vars_,'scores':dict(scores),'ranked':ranked,'winner':ranked[0][0] if ranked else 'unknown'})
 payload={'generatedUtc':datetime.now(timezone.utc).isoformat(),'variableCount':len(variables),'scenarios':scenario_rows,'principle':'Use C# as secure orchestrator/access layer; choose C++/F#/Python/YAML/Bicep/HTML by scored job fit, not habit.'}
 OUT.parent.mkdir(parents=True,exist_ok=True); OUT.write_text(json.dumps(payload,indent=2)+'\n')
 lines=['# Language Decision Matrix','',f"Generated: `{payload['generatedUtc']}`",'',payload['principle'],'',f"Variables: **{payload['variableCount']}**",'','| Scenario | Winner | C# | C++ | F# | Python | YAML | Bicep | HTML |','| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |']
 for s in scenario_rows:
  sc=s['scores']; lines.append(f"| {s['scenario']} | {s['winner']} | {sc.get('csharp',0)} | {sc.get('cpp',0)} | {sc.get('fsharp',0)} | {sc.get('python',0)} | {sc.get('yaml',0)} | {sc.get('bicep',0)} | {sc.get('html',0)} |")
 lines += ['','## Variables','', '| Variable | C# | C++ | F# | Python | YAML | Bicep | HTML | Why |','| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- |']
 for v in variables:
  lines.append(f"| {v['name']} | {v.get('csharp',0)} | {v.get('cpp',0)} | {v.get('fsharp',0)} | {v.get('python',0)} | {v.get('yaml',0)} | {v.get('bicep',0)} | {v.get('html',0)} | {v['why']} |")
 MD.write_text('\n'.join(lines)+'\n')
 print(f"Wrote {OUT.relative_to(ROOT)}"); print(f"Wrote {MD.relative_to(ROOT)}")
 return 0
if __name__=='__main__': raise SystemExit(main())
