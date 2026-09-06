#!/usr/bin/env python3
from __future__ import annotations
import argparse,json,re,subprocess,sys
from datetime import datetime, timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'reports/security/apply-gate-preflight.json'
MD=ROOT/'reports/security/apply-gate-preflight.md'
ALLOWLIST=ROOT/'config/security-preflight-allowlist.json'
MUTATING=re.compile(r'\b(az deployment (?:group|sub|mg|tenant) create|az group create|gh secret set|gh workflow run|kubectl apply|terraform apply)\b')
SAFE=re.compile(r'(--dry-run|what-if|--apply|workflow run helios-control-plane.yml|deploy ==|apply ==|confirm|whatif)', re.IGNORECASE)
SCAN_DIRS=['scripts','.github/workflows','infra']

def load_allowlist():
    if not ALLOWLIST.exists(): return []
    try:
        return json.loads(ALLOWLIST.read_text()).get('applyGatePreflight',{}).get('pathPrefixes',[])
    except json.JSONDecodeError:
        return []

def allowed(path, prefixes):
    rel=str(path.relative_to(ROOT))
    return any(rel.startswith(prefix) for prefix in prefixes)

def tracked_files():
    p=subprocess.run(['git','ls-files',*SCAN_DIRS],cwd=ROOT,text=True,capture_output=True)
    if p.returncode==0:
        return [ROOT/line for line in p.stdout.splitlines() if line.strip()],'git'
    manifest=ROOT/'.helios-tracked-files'
    if manifest.is_file():
        files=[ROOT/line for line in manifest.read_text().splitlines() if line.strip() and (ROOT/line).resolve().is_relative_to(ROOT.resolve())]
        return [path for path in files if any(path.is_relative_to(ROOT/directory) for directory in SCAN_DIRS)],'manifest'
    return [path for directory in SCAN_DIRS for path in (ROOT/directory).rglob('*') if path.is_file()],'filesystem'

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--strict', action='store_true', help='return non-zero when findings are present')
    args=parser.parse_args()
    findings=[]
    prefixes=load_allowlist()
    files,discovery_source=tracked_files()
    scanned=0
    for path in files:
        if not path.exists() or allowed(path,prefixes) or path.suffix.lower() not in {'.py','.sh','.ps1','.yml','.yaml','.md'}: continue
        try: lines=path.read_text(errors='ignore').splitlines(); scanned+=1
        except OSError: continue
        for no,line in enumerate(lines,1):
            context='\n'.join(lines[max(0,no-4):no+1])
            if MUTATING.search(line) and not SAFE.search(context):
                findings.append({'path':str(path.relative_to(ROOT)),'line':no,'pattern':'mutating-command-without-explicit-safety-gate'})
    OUT.parent.mkdir(parents=True,exist_ok=True)
    scan_error=None if scanned else 'no eligible files were scanned'
    payload={'generatedUtc':datetime.now(timezone.utc).isoformat(),'ok':not findings and not scan_error,'discoverySource':discovery_source,'candidateFileCount':len(files),'scannedFileCount':scanned,'scanError':scan_error,'allowlistPathPrefixes':prefixes,'findings':findings,'triage':'allowlisted prefixes are excluded before matching; run with a reduced allowlist and --strict to ratchet enforcement'}
    OUT.write_text(json.dumps(payload,indent=2)+'\n')
    lines=['# Apply Gate Preflight','',f"Generated: `{payload['generatedUtc']}`",'',f"Status: {'PASS' if payload['ok'] else 'ERROR' if scan_error else 'FAIL'}",'',f"Discovery: `{discovery_source}`",f"Candidates: `{len(files)}`",f"Files scanned: `{scanned}`"]
    if scan_error: lines += ['',f"Error: {scan_error}"]
    if findings:
        lines += ['','| Path | Line | Pattern |','| --- | --- | --- |']+[f"| `{f['path']}` | {f['line']} | {f['pattern']} |" for f in findings]
    MD.write_text('\n'.join(lines)+'\n')
    print(f"Apply gate preflight: {'PASS' if payload['ok'] else 'ERROR' if scan_error else 'FAIL'} ({len(findings)} findings, {scanned} files scanned via {discovery_source})")
    return 1 if scan_error or (args.strict and findings) else 0
if __name__=='__main__': sys.exit(main())
