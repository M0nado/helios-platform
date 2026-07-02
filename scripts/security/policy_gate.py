#!/usr/bin/env python3
"""HELIOS safety/policy gate for automation commands and generated reports."""
from __future__ import annotations
import argparse, json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CONFIG = ROOT / 'config' / 'helios-policy.json'
OUT = ROOT / 'reports' / 'policy'

def final_gate_passed(path: Path) -> bool:
    if not path.exists(): return False
    data = json.loads(path.read_text(encoding='utf-8'))
    return not data.get('failed')

def scan_reports(patterns: list[str], paths: list[str]) -> list[dict]:
    hits=[]
    for rel in paths:
        base=ROOT/rel
        if not base.exists(): continue
        for file in base.rglob('*'):
            if not file.is_file() or file.suffix.lower() not in {'.json','.md','.txt','.log','.yml','.yaml'}: continue
            text=file.read_text(encoding='utf-8', errors='ignore')[:500000]
            for pat in patterns:
                if pat in text:
                    hits.append({'file':file.relative_to(ROOT).as_posix(),'pattern':pat})
    return hits

def main() -> int:
    parser=argparse.ArgumentParser(); parser.add_argument('--command', default=''); args=parser.parse_args()
    cfg=json.loads(CONFIG.read_text(encoding='utf-8'))
    violations=[]
    for rule in cfg.get('rules',[]):
        if rule.get('match') and rule['match'] in args.command:
            if rule.get('requires') and rule['requires'] not in args.command:
                violations.append({'rule':rule['id'],'reason':f"missing {rule['requires']}"})
            if rule.get('requiresReport') and not final_gate_passed(ROOT/rule['requiresReport']):
                violations.append({'rule':rule['id'],'reason':f"required passing report missing: {rule['requiresReport']}"})
        if rule.get('scanPaths'):
            for hit in scan_reports(cfg.get('denyPatterns',[]), rule['scanPaths']):
                violations.append({'rule':rule['id'], **hit})
    OUT.mkdir(parents=True, exist_ok=True)
    payload={'command':args.command,'violations':violations,'passed':not violations}
    (OUT/'policy-gate.json').write_text(json.dumps(payload, indent=2, sort_keys=True)+'\n', encoding='utf-8')
    (OUT/'policy-gate.md').write_text('# HELIOS Policy Gate\n\n' + ('Passed\n' if not violations else '\n'.join(f"- {v}" for v in violations)+'\n'), encoding='utf-8')
    print(f"Wrote {(OUT/'policy-gate.md').relative_to(ROOT)}")
    return 1 if violations else 0
if __name__=='__main__': raise SystemExit(main())
