#!/usr/bin/env python3
from __future__ import annotations
import json,shutil,socket
from datetime import datetime, timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'reports/integrations/deep-agent-readiness.json'
MD=ROOT/'reports/integrations/deep-agent-readiness.md'
CHECKS=[
 ('super-gui','scripts/dashboard/generate-gui.py',['status-site/index.html','src/gui/']),
 ('local-web-port-8787','scripts/web/helios-web.py',[]),
 ('github-control-safe','scripts/github/github-inventory.py',['.github/workflows/']),
 ('codex-mcp-agent','scripts/codex/generate-codex-tasks.py',['config/github-mcp.example.json','ai-integration/codex-integration/']),
 ('hermes-fleet-agent','scripts/agents/hermes_fleet_readiness.py',['config/hermes-fleet.example.json']),
 ('server-core','src/core/HELIOS.Platform/Program.cs',['src/server/','src/core/']),
 ('vault-security','src/Security/SecurityValidator.cs',['installer/security/','src/core/HELIOS.Platform/Core/Security/']),
 ('azure-cloud-shell','scripts/azure/azure_connection_pipeline.py',['infra/azure/','cloud-integration/']),
]

def path_ok(path): return (ROOT/path).exists() or any((ROOT).glob(path+'**/*') if path.endswith('/') else [])
def port_free(port):
    with socket.socket(socket.AF_INET,socket.SOCK_STREAM) as s:
        return s.connect_ex(('127.0.0.1',port)) != 0

def main():
    rows=[]
    for name,primary,extras in CHECKS:
        present=path_ok(primary); extra_present=[p for p in extras if path_ok(p)]
        ok=present and (bool(extra_present) or not extras)
        rows.append({'name':name,'ok':ok,'primary':primary,'present':present,'extrasPresent':extra_present,'extrasMissing':[p for p in extras if p not in extra_present]})
    tools={tool:shutil.which(tool) is not None for tool in ['git','gh','az','dotnet','cmake','python3']}
    ports={'8787Free':port_free(8787)}
    fixes=[]
    for r in rows:
        if not r['ok']: fixes.append({'area':r['name'],'command':'./finish.sh --full','reason':'missing '+', '.join(([r['primary']] if not r['present'] else [])+r['extrasMissing'])})
    if not tools.get('gh'): fixes.append({'area':'github-control-safe','command':'scripts/setup/bootstrap-local-tools.sh','reason':'gh not found'})
    payload={'generatedUtc':datetime.now(timezone.utc).isoformat(),'ok':all(r['ok'] for r in rows),'checks':rows,'tools':tools,'ports':ports,'nextFixes':fixes,'safeGitHubMode':'inventory/report-only; no takeover/mutation without explicit human auth and workflow permissions'}
    OUT.parent.mkdir(parents=True,exist_ok=True); OUT.write_text(json.dumps(payload,indent=2)+'\n')
    lines=['# Deep Agent / GUI / GitHub Readiness','',f"Generated: `{payload['generatedUtc']}`",'',f"Status: {'PASS' if payload['ok'] else 'NEEDS SETUP'}",'',f"Safe GitHub mode: {payload['safeGitHubMode']}",'','| Area | Status | Primary | Present extras | Missing extras |','| --- | --- | --- | --- | --- |']
    for r in rows: lines.append(f"| {r['name']} | {'✅' if r['ok'] else '⚠️'} | `{r['primary']}` | `{', '.join(r['extrasPresent'])}` | `{', '.join(r['extrasMissing'])}` |")
    lines += ['','## Tools','']+[f"- {k}: {'✅' if v else '⚠️'}" for k,v in tools.items()]
    if fixes: lines += ['','## Next fixes','','| Area | Command | Reason |','| --- | --- | --- |']+[f"| {f['area']} | `{f['command']}` | {f['reason']} |" for f in fixes]
    MD.write_text('\n'.join(lines)+'\n')
    print(f"Wrote {OUT.relative_to(ROOT)}"); print(f"Wrote {MD.relative_to(ROOT)}")
if __name__=='__main__': main()
