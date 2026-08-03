#!/usr/bin/env python3
from __future__ import annotations
import json, os, shutil, subprocess
from datetime import datetime, timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; CFG=ROOT/'config/cross-access-profiles.example.json'; OUT=ROOT/'reports/integrations/cross-access-profiles.json'; MD=ROOT/'reports/integrations/cross-access-profiles.md'
def cli_ready(cli): return shutil.which(cli) is not None
def auth_ready(check):
    if '+' in check: return all(bool(os.environ.get(x)) for x in check.split('+'))
    if check.endswith('_API_KEY') or check in {'OPENAI_API_KEY','ANTHROPIC_API_KEY'}: return bool(os.environ.get(check))
    parts=check.split()
    if not parts or shutil.which(parts[0]) is None: return False
    try: return subprocess.run(parts,cwd=ROOT,capture_output=True,text=True,timeout=15).returncode==0
    except Exception: return False
cfg=json.loads(CFG.read_text()); profiles=[]
for p in cfg['profiles']:
    item=dict(p); item['cliAvailable']=cli_ready(p['cli']); item['authenticated']=auth_ready(p['authCheck']); item['ready']=item['cliAvailable'] and (item['authenticated'] or p['secretSource']=='none'); profiles.append(item)
payload={'generatedUtc':datetime.now(timezone.utc).isoformat(),'safeByDefault':True,'profiles':profiles,'notes':'No secrets are printed. Apply mode is disabled for org, enterprise, tenant, and subscription scopes.'}
OUT.parent.mkdir(parents=True,exist_ok=True); OUT.write_text(json.dumps(payload,indent=2,sort_keys=True)+'\n')
lines=['# Cross-Access Profiles','',f"Generated: `{payload['generatedUtc']}`",'','| Profile | Provider | Scope | CLI | Auth | Apply mode |','| --- | --- | --- | --- | --- | --- |']
for p in profiles: lines.append(f"| {p['displayName']} | {p['provider']} | {p['scope']} | {'✅' if p['cliAvailable'] else '⚠️'} | {'✅' if p['authenticated'] else '⚠️'} | {p['applyMode']} |")
MD.write_text('\n'.join(lines)+'\n'); print(f'Wrote {OUT.relative_to(ROOT)}'); print(f'Wrote {MD.relative_to(ROOT)}')
