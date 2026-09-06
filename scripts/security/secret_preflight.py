#!/usr/bin/env python3
from __future__ import annotations
import argparse,json,re,subprocess,sys
from datetime import datetime, timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'reports/security/secret-preflight.json'
MD=ROOT/'reports/security/secret-preflight.md'
ALLOWLIST=ROOT/'config/security-preflight-allowlist.json'
PATTERNS=[('private-key',re.compile(r'-----BEGIN (?:RSA |EC |OPENSSH |)PRIVATE KEY-----')),('github-token',re.compile(r'gh[pousr]_[A-Za-z0-9_]{20,}')),('openai-key',re.compile(r'sk-[A-Za-z0-9]{20,}')),('azure-secret',re.compile(r'(?i)(client_secret|azure_openai_api_key|api[_-]?key)\s*[:=]\s*["\']?[^"\'\s]{16,}'))]
SKIP_PARTS={'.git','.tools','node_modules','bin','obj','.build','reports'}
TEXT_SUFFIXES={'.py','.sh','.ps1','.json','.yml','.yaml','.md','.txt','.cs','.fs','.fsproj','.csproj','.cpp','.hpp','.h','.bicep','.env','.template'}

def load_allowlist():
    if not ALLOWLIST.exists(): return []
    try:
        return json.loads(ALLOWLIST.read_text()).get('secretPreflight',{}).get('pathPrefixes',[])
    except json.JSONDecodeError:
        return []

def allowed(path, prefixes):
    rel=str(path.relative_to(ROOT))
    return any(rel.startswith(prefix) for prefix in prefixes)

def tracked_files():
    p=subprocess.run(['git','ls-files'],cwd=ROOT,text=True,capture_output=True)
    if p.returncode==0:
        return [ROOT/line for line in p.stdout.splitlines() if line.strip()],'git'
    manifest=ROOT/'.helios-tracked-files'
    if manifest.is_file():
        files=[ROOT/line for line in manifest.read_text().splitlines() if line.strip() and (ROOT/line).resolve().is_relative_to(ROOT.resolve())]
        return files,'manifest'
    return [path for path in ROOT.rglob('*') if path.is_file() and should_scan(path)],'filesystem'

def should_scan(path):
    rel=path.relative_to(ROOT)
    if any(part in SKIP_PARTS for part in rel.parts): return False
    return path.suffix.lower() in TEXT_SUFFIXES or path.name.endswith('.env')

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--strict', action='store_true', help='return non-zero when findings are present')
    args=parser.parse_args()
    findings=[]
    prefixes=load_allowlist()
    files,discovery_source=tracked_files()
    scanned=0
    for path in files:
        if not path.exists() or not should_scan(path) or allowed(path,prefixes): continue
        try: lines=path.read_text(errors='ignore').splitlines(); scanned+=1
        except OSError: continue
        for no,line in enumerate(lines,1):
            if 'example' in path.name.lower() or 'template' in path.name.lower():
                continue
            placeholder=any(token in line.lower() for token in ['<', '>', 'your_', 'example', 'placeholder', 'xxx', '***', '$env:', 'getenvironmentvariable', 'convert.tobase64string', 'randomnumbergenerator'])
            if placeholder:
                continue
            for name,pattern in PATTERNS:
                if pattern.search(line):
                    findings.append({'path':str(path.relative_to(ROOT)),'line':no,'pattern':name})
    OUT.parent.mkdir(parents=True,exist_ok=True)
    scan_error=None if scanned else 'no eligible files were scanned'
    payload={'generatedUtc':datetime.now(timezone.utc).isoformat(),'ok':not findings and not scan_error,'discoverySource':discovery_source,'candidateFileCount':len(files),'scannedFileCount':scanned,'scanError':scan_error,'allowlistPathPrefixes':prefixes,'findings':findings}
    OUT.write_text(json.dumps(payload,indent=2)+'\n')
    lines=['# Secret Preflight','',f"Generated: `{payload['generatedUtc']}`",'',f"Status: {'PASS' if payload['ok'] else 'ERROR' if scan_error else 'FAIL'}",'',f"Discovery: `{discovery_source}`",f"Candidates: `{len(files)}`",f"Files scanned: `{scanned}`"]
    if scan_error: lines += ['',f"Error: {scan_error}"]
    if findings:
        lines += ['','| Path | Line | Pattern |','| --- | --- | --- |']+[f"| `{f['path']}` | {f['line']} | {f['pattern']} |" for f in findings]
    MD.write_text('\n'.join(lines)+'\n')
    print(f"Secret preflight: {'PASS' if payload['ok'] else 'ERROR' if scan_error else 'FAIL'} ({len(findings)} findings, {scanned} files scanned via {discovery_source})")
    return 1 if scan_error or (args.strict and findings) else 0
if __name__=='__main__': sys.exit(main())
