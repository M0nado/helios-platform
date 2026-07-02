# Codex Task: config from work

- Priority: 78
- Task type: compare-selectively
- Allowed paths: `['config/azure-control.example.json', 'config/build-graph.json', 'config/execution-order.json', 'config/github-control.example.json', 'config/integrations.example.json', 'config/secrets-map.example.json']`
- Blocked paths: `['.git', 'bin', 'obj']`
- Expected output: comparison notes, merge risk, tests to run, and idea extraction
- Network required: no, unless explicitly needed by the task
- Secrets allowed: no

## Suggested checks

```bash
python3 scripts/analysis/branch_intelligence.py
python3 scripts/control/helios-control.py
```
