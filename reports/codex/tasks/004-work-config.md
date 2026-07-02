# Codex Task: config from work

- Priority: 60
- Task type: extract-ideas
- Allowed paths: `['config/build-graph.json', 'config/hybrid-integration-roadmap.json']`
- Blocked paths: `['.git', 'bin', 'obj']`
- Expected output: comparison notes, merge risk, tests to run, and idea extraction
- Network required: no, unless explicitly needed by the task
- Secrets allowed: no

## Suggested checks

```bash
python3 scripts/analysis/branch_intelligence.py
python3 scripts/control/helios-control.py
```
