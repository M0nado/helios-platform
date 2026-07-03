#!/usr/bin/env python3
from __future__ import annotations
import argparse,json,re,subprocess,sys
from datetime import datetime, timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'reports/security/apply-gate-preflight.json'
MD=ROOT/'reports/security/apply-gate-preflight.md'
MUTATING=re.compile(r'\b(az deployment (?:group|sub|mg|tenant) create|az group create|gh secret set|gh workflow run|kubectl apply|terraform apply)\b')
SAFE=re.compile(r'(--dry-run|what-if|--apply|workflow run helios-control-plane.yml)')
SCAN_DIRS=['scripts','.github/workflows','infra']

def tracked_files():
    p=subprocess.run(['git','ls-files',*SCAN_DIRS],cwd=ROOT,text=True,capture_output=True)
    return [ROOT/line for line in p.stdout.splitlines() if line.strip()] if p.returncode==0 else []

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--strict', action='store_true', help='return non-zero when findings are present')
    args=parser.parse_args()
    findings=[]
    for path in tracked_files():
        if not path.exists() or path.suffix.lower() not in {'.py','.sh','.ps1','.yml','.yaml','.md'}: continue
        for no,line in enumerate(path.read_text(errors='ignore').splitlines(),1):
            if MUTATING.search(line) and not SAFE.search(line):
                findings.append({'path':str(path.relative_to(ROOT)),'line':no,'pattern':'mutating-command-without-explicit-safety-gate'})
    OUT.parent.mkdir(parents=True,exist_ok=True)
    payload={'generatedUtc':datetime.now(timezone.utc).isoformat(),'ok':not findings,'findings':findings}
    OUT.write_text(json.dumps(payload,indent=2)+'\n')
    lines=['# Apply Gate Preflight','',f"Generated: `{payload['generatedUtc']}`",'',f"Status: {'PASS' if payload['ok'] else 'FAIL'}"]
    if findings:
        lines += ['','| Path | Line | Pattern |','| --- | --- | --- |']+[f"| `{f['path']}` | {f['line']} | {f['pattern']} |" for f in findings]
    MD.write_text('\n'.join(lines)+'\n')
    print(f"Apply gate preflight: {'PASS' if payload['ok'] else 'FAIL'} ({len(findings)} findings)")
    return 0 if payload['ok'] or not args.strict else 1
if __name__=='__main__': sys.exit(main())
