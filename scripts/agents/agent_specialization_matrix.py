#!/usr/bin/env python3
from __future__ import annotations
import json
from datetime import datetime, timezone
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
CONFIG = ROOT / 'config/agent-specializations.json'
OUT = ROOT / 'reports/branch-agents/agent-specialization-matrix.json'
MD = ROOT / 'reports/branch-agents/agent-specialization-matrix.md'

def main() -> int:
    data = json.loads(CONFIG.read_text())
    specs = data.get('specializations', {})
    rows = []
    for key, spec in sorted(specs.items()):
        present = [p for p in spec.get('paths', []) if (ROOT / p).exists()]
        rows.append({**spec, 'id': key, 'pathsPresent': present, 'pathsMissing': [p for p in spec.get('paths', []) if p not in present]})
    payload = {'generatedUtc': datetime.now(timezone.utc).isoformat(), 'ok': all(not r['pathsMissing'] for r in rows), 'specializationCount': len(rows), 'specializations': rows}
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(json.dumps(payload, indent=2) + '\n')
    lines = ['# Agent Specialization Matrix', '', f"Generated: `{payload['generatedUtc']}`", '', f"Specializations: **{payload['specializationCount']}**", '', '| Specialist | Focus | Checks | Present paths | Missing paths |', '| --- | --- | --- | --- | --- |']
    for row in rows:
        focus = '<br>'.join(row.get('focus', []))
        checks = '<br>'.join(f"`{c}`" for c in row.get('checks', []))
        lines.append(f"| {row['title']} | {focus} | {checks} | `{', '.join(row['pathsPresent'])}` | `{', '.join(row['pathsMissing'])}` |")
    MD.write_text('\n'.join(lines) + '\n')
    print(f"Wrote {OUT.relative_to(ROOT)}")
    print(f"Wrote {MD.relative_to(ROOT)}")
    return 0
if __name__ == '__main__':
    raise SystemExit(main())
