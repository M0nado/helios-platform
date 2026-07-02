#!/usr/bin/env python3
from __future__ import annotations
import json, html
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'status-site/index.html'
def load(path, default):
    p=ROOT/path
    return json.loads(p.read_text()) if p.exists() else default
profiles=load(Path('reports/integrations/cross-access-profiles.json'), {'profiles':[]}).get('profiles',[])
summary=load(Path('reports/control-plane/control-summary.json'), {})
inv=load(Path('reports/project-inventory/repo-inventory.json'), {})
actions=[('Run all safe reports','./helios.sh all'),('Refresh local GUI','curl -X POST http://127.0.0.1:8787/rebuild'),('GitHub inventory','./helios.sh github'),('Azure inventory','./helios.sh azure'),('Cross-access profiles','python3 scripts/integrations/cross_access_profiles.py'),('Readiness score','./helios.sh readiness'),('App automation readiness','./helios.sh apps'),('Open local app shell','./helios.sh local-app'),('Doctor','./helios.sh doctor'),('PR body preview','./helios.sh pr-update --dry-run'),('Codex packets','./helios.sh codex'),('Build graph','./helios.sh build'),('Open dashboard','./helios.sh dashboard')]
cards=''.join(f"<article><h3>{html.escape(p['displayName'])}</h3><p>{html.escape(p['provider'])} / {html.escape(p['scope'])}</p><p>CLI: {'✅' if p.get('cliAvailable') else '⚠️'} Auth: {'✅' if p.get('authenticated') else '⚠️'}</p><code>{html.escape(p.get('inventory',''))}</code></article>" for p in profiles)
buttons=''.join(f"<button onclick=\"copyCmd('{html.escape(cmd)}')\">{html.escape(name)}</button>" for name,cmd in actions)
langs=''.join(f"<li>{html.escape(k)}: {v}</li>" for k,v in sorted(inv.get('languageCounts',{}).items()))
OUT.write_text(f'''<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>HELIOS Command Center</title>
<style>body{{font-family:Segoe UI,Arial,sans-serif;margin:0;background:#08111f;color:#eaf2ff}}header{{padding:24px;background:#10294a}}main{{padding:24px}}section{{margin:24px 0}}button{{margin:6px;padding:10px 14px;border:0;border-radius:8px;background:#4da3ff;color:#00172f;font-weight:700;cursor:pointer}}article{{border:1px solid #29496f;border-radius:12px;padding:16px;margin:10px;background:#0e1d33}}.grid{{display:grid;grid-template-columns:repeat(auto-fit,minmax(260px,1fr));gap:12px}}code{{color:#b8dcff}}a{{color:#8dccff}}</style>
<script>function copyCmd(c){{navigator.clipboard.writeText(c);document.getElementById('last').textContent=c}} async function rebuild(){{await fetch('/rebuild',{{method:'POST'}});location.reload()}}</script></head>
<body><header><h1>HELIOS Command Center GUI</h1><p>Safe local control for GitHub, Azure, Entra, Purview, Microsoft 365, OpenAI/ChatGPT, Claude, Codex, MAUI, Visual Studio, branches, reports, and hybrid planning.</p></header><main>
<section><h2>Instant safe actions</h2>{buttons}<button onclick="rebuild()">Rebuild reports now</button><p>Last copied: <code id="last"></code></p></section>
<section><h2>Cross-access profiles</h2><div class="grid">{cards}</div></section>
<section><h2>Whole-project inventory</h2><p>Files indexed: {inv.get('fileCount','unknown')}</p><ul>{langs}</ul></section>
<section><h2>Reports</h2><ul><li><a href="reports/cross-access-profiles.md">Cross-access profiles</a></li><li><a href="reports/readiness-score.md">Readiness score</a></li><li><a href="reports/app-automation-readiness.md">App automation readiness</a></li><li><a href="reports/doctor.md">Doctor</a></li><li><a href="reports/repo-inventory.md">Whole-project inventory</a></li><li><a href="reports/hybrid-gap-analysis.md">Hybrid gap analysis</a></li><li><a href="reports/control-summary.md">Control summary</a></li><li><a href="reports/build-graph.md">Build graph</a></li><li><a href="actions.md">Action commands</a></li></ul></section>
</main></body></html>''')
print(f'Wrote {OUT.relative_to(ROOT)}')
