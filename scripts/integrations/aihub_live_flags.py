#!/usr/bin/env python3
from __future__ import annotations
import json
from datetime import datetime, timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
CFG=ROOT/'config/aihub-live-flags.example.json'
OUT=ROOT/'reports/integrations/aihub-live-flags.json'
MD=ROOT/'reports/integrations/aihub-live-flags.md'
LEVELS=[
 {'mode':'plan-only','description':'reports, plans, GUI, no mutation'},
 {'mode':'auto-local','description':'local setup, local reports/tests, policy-safe local fixes'},
 {'mode':'repo-live','description':'GitHub push/wiki/pages/runner actions when flags and credentials are enabled'},
 {'mode':'cloud-live','description':'Azure/Bicep/Fabric/Foundry/Purview mutation when flags, credentials, what-if, and rollback are enabled'},
 {'mode':'full-live','description':'repo-live plus cloud-live; intended for explicitly configured automation only'}]
TRANSFER=['C# orchestrator records every result as typed contracts and GUI state','F# scores outcomes and redistributes weights across LLM/tool/agent combos','C++ accelerates repeated hot-path algorithms discovered by learning','Python connects APIs, local models, Hermes/XCore launches, and report ledgers','YAML/Bicep/JSON/MD carry setup, cloud, workflow, and knowledge forward','Hermes and XCore agents gain XP from every run and share prompt/tool/provider improvements']

def main():
 cfg=json.loads(CFG.read_text()) if CFG.exists() else {}
 payload={'generatedUtc':datetime.now(timezone.utc).isoformat(),'configPath':str(CFG.relative_to(ROOT)),'flags':cfg,'levels':LEVELS,'collectiveLearningTransfer':TRANSFER,'easyToggleCommands':['cp config/aihub-live-flags.example.json config/aihub-live-flags.local.json','python3 scripts/integrations/aihub_live_flags.py','scripts/setup/agent-runner-easy-setup.sh --agents 128 --mode hybrid-cloud --profile full'],'principle':'Live automation is explicit and easy to toggle by level. Learning always carries over: each provider, Hermes/XCore agent, tool, prompt, language engine, report, and check improves the shared AIHub ledger.'}
 OUT.parent.mkdir(parents=True,exist_ok=True); OUT.write_text(json.dumps(payload,indent=2)+'\n')
 lines=['# AIHub Live Flags','',f"Generated: `{payload['generatedUtc']}`",'',payload['principle'],'','## Current example flags']+[f"- `{k}`: `{v}`" for k,v in cfg.items()]
 lines += ['','## Automation levels']+[f"- **{l['mode']}**: {l['description']}" for l in LEVELS]
 lines += ['','## Collective learning transfer']+[f"- {x}" for x in TRANSFER]
 lines += ['','## Easy toggle commands']+[f"- `{c}`" for c in payload['easyToggleCommands']]
 MD.write_text('\n'.join(lines)+'\n')
 print(f'Wrote {OUT.relative_to(ROOT)}'); print(f'Wrote {MD.relative_to(ROOT)}')
 return 0
if __name__=='__main__': raise SystemExit(main())
