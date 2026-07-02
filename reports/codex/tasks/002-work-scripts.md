# Codex Task: scripts from work

- Priority: 76
- Task type: extract-ideas
- Allowed paths: `['scripts/analysis/hybrid_gap_analysis.py', 'scripts/analysis/repo_inventory.py', 'scripts/build_graph/build_graph.py', 'scripts/cloudshell/helios-cloudshell.sh', 'scripts/setup/helios-dev.sh', 'scripts/web/helios-web.py']`
- Blocked paths: `['.git', 'bin', 'obj']`
- Expected output: comparison notes, merge risk, tests to run, and idea extraction
- Network required: no, unless explicitly needed by the task
- Secrets allowed: no

## Suggested checks

```bash
python3 scripts/analysis/branch_intelligence.py
python3 scripts/control/helios-control.py
```
