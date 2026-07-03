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
build_graph=load(Path('reports/build-graph/latest.json'), {'counts':{},'nextFixes':[]})
hermes=load(Path('reports/hermes-fleet/latest.json'), {})
secret=load(Path('reports/security/secret-preflight.json'), {})
apply_gate=load(Path('reports/security/apply-gate-preflight.json'), {})
finish=load(Path('reports/integrations/finish-readiness.json'), {'score':'unknown','categoryScores':{}})
super_stack=load(Path('reports/integrations/super-stack-readiness.json'), {'ok':False})
actions=[('Run all safe reports','./helios.sh all'),('Refresh local GUI','curl -X POST http://127.0.0.1:8787/rebuild'),('GitHub inventory','./helios.sh github'),('Azure inventory','./helios.sh azure'),('Cross-access profiles','python3 scripts/integrations/cross_access_profiles.py'),('Readiness score','./helios.sh readiness'),('App automation readiness','./helios.sh apps'),('Open local app shell','./helios.sh local-app'),('Doctor','./helios.sh doctor'),('PR body preview','./helios.sh pr-update --dry-run'),('Codex packets','./helios.sh codex'),('Build graph','./helios.sh build'),('Open dashboard','./helios.sh dashboard'),('Finish apply plan','python3 scripts/apply/finish_readiness_apply.py'),('Apply safe finish steps','python3 scripts/apply/finish_readiness_apply.py --apply'),('Generate finish tasks','python3 scripts/apply/generate_finish_tasks.py'),('Easy setup','./finish.sh'),('Super stack readiness','python3 scripts/integrations/super_stack_readiness.py'),('Deep agent readiness','python3 scripts/integrations/deep_agent_readiness.py'),('Full integration matrix','python3 scripts/integrations/full_integration_matrix.py')]
cards=''.join(f"<article><h3>{html.escape(p['displayName'])}</h3><p>{html.escape(p['provider'])} / {html.escape(p['scope'])}</p><p>CLI: {'✅' if p.get('cliAvailable') else '⚠️'} Auth: {'✅' if p.get('authenticated') else '⚠️'}</p><code>{html.escape(p.get('inventory',''))}</code></article>" for p in profiles)
buttons=''.join(f"<button onclick=\"copyCmd('{html.escape(cmd)}')\">{html.escape(name)}</button>" for name,cmd in actions)
langs=''.join(f"<li>{html.escape(k)}: {v}</li>" for k,v in sorted(inv.get('languageCounts',{}).items()))
counts=''.join(f"<li>{html.escape(k)}: {v}</li>" for k,v in build_graph.get('counts',{}).items() if v)
fixes=''.join(f"<li><code>{html.escape(f.get('node',''))}</code>: <code>{html.escape(f.get('command',''))}</code> — {html.escape(f.get('reason',''))}</li>" for f in build_graph.get('nextFixes',[])[:8])
OUT.write_text(f'''<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>HELIOS Command Center</title>
<style>body{{font-family:Segoe UI,Arial,sans-serif;margin:0;background:#08111f;color:#eaf2ff}}header{{padding:24px;background:#10294a}}main{{padding:24px}}section{{margin:24px 0}}button{{margin:6px;padding:10px 14px;border:0;border-radius:8px;background:#4da3ff;color:#00172f;font-weight:700;cursor:pointer}}article{{border:1px solid #29496f;border-radius:12px;padding:16px;margin:10px;background:#0e1d33}}.grid{{display:grid;grid-template-columns:repeat(auto-fit,minmax(260px,1fr));gap:12px}}code{{color:#b8dcff}}a{{color:#8dccff}}</style>
<script>function copyCmd(c){{navigator.clipboard.writeText(c);document.getElementById('last').textContent=c}} async function rebuild(){{await fetch('/rebuild',{{method:'POST'}});location.reload()}}</script></head>
<body><header><h1>HELIOS Command Center GUI</h1><p>Safe local control for GitHub, Azure, Entra, Purview, Microsoft 365, OpenAI/ChatGPT, Claude, Codex, MAUI, Visual Studio, branches, reports, and hybrid planning.</p></header><main>
<section><h2>Instant safe actions</h2>{buttons}<button onclick="rebuild()">Rebuild reports now</button><p>Last copied: <code id="last"></code></p></section>
<section><h2>Cross-access profiles</h2><div class="grid">{cards}</div></section>
<section><h2>Whole-project inventory</h2><p>Files indexed: {inv.get('fileCount','unknown')}</p><ul>{langs}</ul></section>
<section><h2>Finish readiness</h2><p>Score: <strong>{finish.get('score','unknown')}%</strong></p><div class="grid"><article><h3>Build graph</h3><ul>{counts}</ul><p><a href="../reports/build-graph/latest.md">Latest build graph</a></p></article><article><h3>Security</h3><p>Secrets: {'✅' if secret.get('ok') else '⚠️'} Apply gates: {'✅' if apply_gate.get('ok') else '⚠️'}</p></article><article><h3>Hermes/Fleet</h3><p>{'✅' if hermes.get('ok') else '⚠️'} {html.escape(hermes.get('fleetName','not generated'))}</p></article></div><h3>Next fixes</h3><ul>{fixes}</ul></section>
<section><h2>Reports</h2><ul><li><a href="reports/cross-access-profiles.md">Cross-access profiles</a></li><li><a href="reports/readiness-score.md">Readiness score</a></li><li><a href="reports/app-automation-readiness.md">App automation readiness</a></li><li><a href="reports/doctor.md">Doctor</a></li><li><a href="reports/repo-inventory.md">Whole-project inventory</a></li><li><a href="reports/hybrid-gap-analysis.md">Hybrid gap analysis</a></li><li><a href="reports/control-summary.md">Control summary</a></li><li><a href="../reports/build-graph/latest.md">Build graph latest</a></li><li><a href="../reports/hermes-fleet/latest.md">Hermes/Fleet readiness</a></li><li><a href="../reports/security/secret-preflight.md">Secret preflight</a></li><li><a href="../reports/security/apply-gate-preflight.md">Apply gate preflight</a></li><li><a href="../reports/integrations/finish-readiness.md">Finish readiness</a></li><li><a href="../reports/integrations/super-stack-readiness.md">Super stack readiness</a></li><li><a href="../reports/integrations/deep-agent-readiness.md">Deep agent readiness</a></li><li><a href="../reports/integrations/full-integration-matrix.md">Full integration matrix</a></li><li><a href="../reports/apply/finish-readiness-apply.md">Finish apply plan</a></li><li><a href="../reports/apply/finish-tasks.md">Finish tasks</a></li><li><a href="actions.md">Action commands</a></li></ul></section>
</main></body></html>''')
print(f'Wrote {OUT.relative_to(ROOT)}')
