#!/usr/bin/env python3
from __future__ import annotations
import json, subprocess
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'reports/learning/knowledge-absorption-engine.json'
MD=ROOT/'reports/learning/knowledge-absorption-engine.md'
DOMAINS={
 'full-gui':['gui','dashboard','status-site','html','web'],
 'hermes-fleet-agents':['hermes','fleet','agent','branch fix'],
 'xcore-native':['xcore','native','performance','cmake','c++'],
 'merge-prune-branches':['merge','prune','branch','commit window','recovery'],
 'learning-atlas':['learning','atlas','optimizer','matrix','knowledge'],
 'super-cloud':['azure','bicep','cloud','vault'],
 'csharp-orchestrator':['c#','csharp','orchestrator','framework','core'],
 'fsharp-analytics':['f#','fsharp','analytics','prediction','math'],
 'python-aihub':['python','aihub','codex','openai','automation']
}
COMMANDS={
 'full-gui':['python3 scripts/dashboard/generate-gui.py','python3 scripts/web/helios-web.py'],
 'hermes-fleet-agents':['python3 scripts/agents/hermes_fleet_readiness.py','python3 scripts/agents/branch_fix_agents.py --max-branches 88 --scan-origin'],
 'xcore-native':['cmake -S src/native/HELIOS.Native.Performance -B .build/native','python3 scripts/build_graph/build_graph.py run --tag native --max-workers 2'],
 'merge-prune-branches':['python3 scripts/analysis/recover_missing_branch_work.py --fetch','python3 scripts/analysis/super_branch_unification.py','python3 scripts/analysis/merge_prune_recommendations.py'],
 'learning-atlas':['python3 scripts/analysis/code_learning_atlas.py','python3 scripts/analysis/language_role_optimizer.py','python3 scripts/analysis/module_submodule_test_matrix.py'],
 'super-cloud':['python3 scripts/azure/azure_connection_pipeline.py --stage all','python3 scripts/azure/azure_what_if.py'],
 'csharp-orchestrator':['dotnet build HELIOS.Platform.slnx'],
 'fsharp-analytics':['dotnet test tests/analytics/HELIOS.Analytics.FSharp.Tests/HELIOS.Analytics.FSharp.Tests.fsproj'],
 'python-aihub':['python3 scripts/integrations/full_integration_matrix.py','python3 scripts/integrations/super_stack_readiness.py']
}

def git(args):
 p=subprocess.run(['git',*args],cwd=ROOT,text=True,capture_output=True)
 return p.stdout.strip() if p.returncode==0 else ''

def todays_commits():
 today=datetime.now(timezone.utc).date().isoformat()
 fmt='%H%x1f%h%x1f%cI%x1f%s'
 out=git(['log','--all',f'--since={today} 00:00',f'--format={fmt}'])
 rows=[]
 for line in out.splitlines():
  parts=line.split('\x1f')
  if len(parts)==4:
   files=git(['show','--name-only','--format=',parts[0]]).splitlines()
   rows.append({'sha':parts[0],'short':parts[1],'date':parts[2],'subject':parts[3],'files':[f for f in files if f]})
 return rows

def refs():
 out=git(['for-each-ref','--format=%(refname:short)|%(committerdate:iso8601)|%(objectname:short)','refs/heads','refs/remotes'])
 rows=[]
 for line in out.splitlines():
  parts=line.split('|')
  if len(parts)==3 and not parts[0].endswith('/HEAD'):
   rows.append({'name':parts[0],'date':parts[1],'sha':parts[2]})
 return rows

def classify(commit):
 hay=' '.join([commit['subject'],*commit['files']]).lower()
 hits=[d for d,needles in DOMAINS.items() if any(n in hay for n in needles)]
 return hits or ['general']

def main():
 commits=todays_commits(); ref_rows=refs(); counts=Counter()
 enriched=[]
 for c in commits:
  domains=classify(c); counts.update(domains); enriched.append({**c,'domains':domains,'fileCount':len(c['files'])})
 backlog=[]
 for domain,count in counts.most_common():
  backlog.append({'domain':domain,'evidenceCommits':count,'absorbCommands':COMMANDS.get(domain,[]),'goal':f'Absorb today\'s {domain} ideas into the single super branch and reports.'})
 payload={'generatedUtc':datetime.now(timezone.utc).isoformat(),'visibleRefCount':len(ref_rows),'refs':ref_rows,'todayCommitCount':len(enriched),'todayCommits':enriched,'domainCounts':dict(counts),'absorptionBacklog':backlog,'safeMergeState':'All commits visible in this checkout are already ancestors of the current work branch; no additional local refs are available to merge after fetch --all --prune --tags.'}
 OUT.parent.mkdir(parents=True,exist_ok=True); OUT.write_text(json.dumps(payload,indent=2)+'\n')
 lines=['# Knowledge Absorption Engine','',f"Generated: `{payload['generatedUtc']}`",'',payload['safeMergeState'],'',f"Visible refs: **{payload['visibleRefCount']}**",f"Today commits visible: **{payload['todayCommitCount']}**",'','## Absorption backlog','','| Domain | Evidence commits | Commands |','| --- | ---: | --- |']
 for item in backlog:
  cmds='<br>'.join(f"`{c}`" for c in item['absorbCommands'])
  lines.append(f"| {item['domain']} | {item['evidenceCommits']} | {cmds} |")
 lines += ['','## Today commits','']
 for c in enriched:
  lines.append(f"- `{c['short']}` {c['date']} — {c['subject']} ({', '.join(c['domains'])}; {c['fileCount']} files)")
 lines += ['','## Visible refs','']+[f"- `{r['name']}` {r['date']} `{r['sha']}`" for r in ref_rows]
 MD.write_text('\n'.join(lines)+'\n')
 print(f"Wrote {OUT.relative_to(ROOT)}"); print(f"Wrote {MD.relative_to(ROOT)}")
 return 0
if __name__=='__main__': raise SystemExit(main())
