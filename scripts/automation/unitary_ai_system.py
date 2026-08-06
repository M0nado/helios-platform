#!/usr/bin/env python3
"""Render the HELIOS unitary AI/provider learning architecture."""
from __future__ import annotations
import datetime as dt, json, os
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; CONFIG=ROOT/'config/helios-unitary-ai-system.json'; OUT=ROOT/'reports/ai-system'
SECRET_ENVS={'codex':['GITHUB_TOKEN','HELIOS_AUTOMATION_TOKEN'],'chatgpt-openai':['OPENAI_API_KEY'],'github-copilot':['GITHUB_TOKEN'],'microsoft-copilot-365-pro':['MICROSOFT_TENANT_ID','MICROSOFT_CLIENT_ID'],'azure-ai-foundry':['AZURE_SUBSCRIPTION_ID','AZURE_TENANT_ID','AZURE_OPENAI_ENDPOINT'],'hermes-xcore-local-cloud':['HERMES_ENDPOINT','XCORE_ENDPOINT']}
def main():
 data=json.loads(CONFIG.read_text(encoding='utf-8')); families=[]
 for fam in data.get('providerFamilies',[]):
  required=SECRET_ENVS.get(fam['id'],[]); present=[name for name in required if os.environ.get(name)]
  families.append({**fam,'requiredSecretNames':required,'presentSecretNames':present,'ready':len(present)==len(required)})
 payload={**data,'generatedUtc':dt.datetime.now(dt.timezone.utc).isoformat(),'providerFamilies':families}
 OUT.mkdir(parents=True,exist_ok=True); (OUT/'unitary-ai-system.json').write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n')
 lines=['# HELIOS Unitary AI System','',data['principle'],'','## Core DNA']+[f"- **{k}**: {v}" for k,v in data.get('coreDna',{}).items()]
 lines += ['','## Provider families','| Provider | Role | Ready | Required secret names |','| --- | --- | --- | --- |']
 for fam in families: lines.append(f"| {fam['id']} | {fam['role']} | {fam['ready']} | {', '.join(fam['requiredSecretNames']) or 'none'} |")
 lines += ['','## Shared learning signals']+[f"- {s}" for s in data.get('unitaryLearningSignals',[])]
 lines += ['','## Routing heuristics']+[f"- {s}" for s in data.get('routingHeuristics',[])]
 lines += ['','## Deep setup order']+[f"{i+1}. {s}" for i,s in enumerate(data.get('deepSetupOrder',[]))]
 (OUT/'unitary-ai-system.md').write_text('\n'.join(lines)+'\n')
 print(f"Wrote {(OUT/'unitary-ai-system.md').relative_to(ROOT)}")
if __name__=='__main__': main()
