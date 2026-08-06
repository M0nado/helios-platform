#!/usr/bin/env python3
"""Render a static HELIOS operator dashboard from generated JSON reports."""
from __future__ import annotations
import json, html, datetime as dt
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'reports/operator-dashboard'
REPORTS={
 'Final Gate':'reports/final-gate/final-gate.json','Mass Score':'reports/mass-integration/mass-integration-score.json','Conflict Forecast':'reports/mass-integration/conflict-forecast.json','Policy':'reports/policy/policy-gate.json','Autofix':'reports/autofix/autofix-plan.json','Auto Upgrade':'reports/auto-upgrade/auto-upgrade.json','LLM Router':'reports/llm-router/llm-router-plan.json','Specializations':'reports/specializations/specialization-matrix.json','Azure Outputs':'reports/azure/outputs.json','Vault':'reports/security/vault-readiness.json'}
COMMANDS=['./tools/helios.ps1 status','./tools/helios.ps1 setup verify','./tools/helios.ps1 github conflict-forecast','./tools/helios.ps1 policy check','./tools/helios.ps1 fix plan','./tools/helios.ps1 azure verify','./tools/helios.ps1 gate final','./tools/helios.ps1 start verify']
def load(rel):
 p=ROOT/rel
 if not p.exists(): return {'status':'missing','path':rel}
 try: return json.loads(p.read_text(encoding='utf-8'))
 except Exception as e: return {'status':'unreadable','path':rel,'error':str(e)}
def status(data):
 if data.get('failed'): return 'fail'
 if data.get('violations'): return 'fail'
 if data.get('status')=='missing': return 'missing'
 return 'ok'
def main():
 OUT.mkdir(parents=True,exist_ok=True); cards=[]
 for name,rel in REPORTS.items():
  data=load(rel); s=status(data); summary=html.escape(json.dumps(data,indent=2,sort_keys=True)[:2000])
  cards.append(f"<section class='card {s}'><h2>{html.escape(name)}</h2><p>{html.escape(rel)}</p><pre>{summary}</pre></section>")
 buttons=''.join(f"<button onclick=\"navigator.clipboard.writeText('{html.escape(c)}')\">{html.escape(c)}</button>" for c in COMMANDS)
 page=f"""<!doctype html><html><head><meta charset='utf-8'><title>HELIOS Operator Dashboard</title><style>body{{font-family:Segoe UI,Arial,sans-serif;background:#10131a;color:#eef;margin:0}}header{{padding:24px;background:#161b26}}.grid{{display:grid;grid-template-columns:repeat(auto-fit,minmax(320px,1fr));gap:16px;padding:16px}}.card{{border:1px solid #30384a;border-radius:12px;padding:16px;background:#1a2030}}.ok{{border-color:#278a46}}.fail{{border-color:#b83232}}.missing{{border-color:#8a7a27}}pre{{max-height:260px;overflow:auto;background:#0b0e14;padding:12px;border-radius:8px}}button{{display:block;margin:8px 0;padding:8px 12px}}</style></head><body><header><h1>HELIOS Operator Dashboard</h1><p>Generated {dt.datetime.now(dt.timezone.utc).isoformat()}</p><h2>Safe command buttons</h2>{buttons}</header><main class='grid'>{''.join(cards)}</main></body></html>"""
 (OUT/'index.html').write_text(page,encoding='utf-8'); (OUT/'dashboard.json').write_text(json.dumps({'reports':REPORTS,'commands':COMMANDS},indent=2)+'\n',encoding='utf-8')
 print(f"Wrote {(OUT/'index.html').relative_to(ROOT)}")
if __name__=='__main__': main()
