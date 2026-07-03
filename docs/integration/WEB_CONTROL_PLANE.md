# HELIOS Web Control Plane

The web control plane is intentionally lightweight: it serves generated Markdown/JSON from `status-site/` and can regenerate reports locally through a single POST endpoint.

## Local GUI path

```bash
scripts/setup/helios-dev.sh --serve
```

Then open <http://127.0.0.1:8787/>.

## What it wires together

- `scripts/integrations/check-connections.py` for GitHub, Azure, .NET, Python, OpenAI/Azure OpenAI, Slack, and Microsoft 365 readiness.
- `scripts/analysis/branch_intelligence.py` for branch ranking, module impact, idea extraction, and agent work queues.
- `scripts/graphs/generate_graphs.py` for graph summaries.
- `scripts/github/update-wiki-from-reports.py` for wiki export pages.
- `scripts/ai/enrich-ideas.py` for safe offline AI-enrichment readiness metadata.

## Refresh endpoint

```bash
curl -X POST http://127.0.0.1:8787/rebuild
```

The endpoint does not merge branches, push changes, or call external AI APIs. It only regenerates local reports from the checked-out workspace.

## Cloud path

Run `.github/workflows/helios-control-plane.yml` from GitHub Actions to do the same work on GitHub-hosted runners and optionally publish GitHub Pages.

## Full control summary

Run the control summary directly when you want a terminal-first view of GitHub, Azure, AI, Codex, Slack, and Microsoft 365 readiness:

```bash
python3 scripts/control/helios-control.py
```

The generated `reports/control-plane/control-summary.md` is copied into the local dashboard by `scripts/web/helios-web.py` and into the GitHub Actions artifact by the `HELIOS Control Plane` workflow.
