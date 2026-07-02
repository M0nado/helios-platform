# Codex Task: reports from work

- Priority: 60
- Task type: extract-ideas
- Allowed paths: `['reports/branch-intelligence/agent-work-queue.json', 'reports/branch-intelligence/agent-work-queue.md', 'reports/branch-intelligence/analytics-metrics.json', 'reports/branch-intelligence/branch-ranking.json', 'reports/branch-intelligence/branch-ranking.md', 'reports/branch-intelligence/connectivity.json', 'reports/branch-intelligence/dashboard.md', 'reports/branch-intelligence/graphs.md', 'reports/branch-intelligence/merge-prune-recommendations.json', 'reports/branch-intelligence/merge-prune-recommendations.md']`
- Blocked paths: `['.git', 'bin', 'obj']`
- Expected output: comparison notes, merge risk, tests to run, and idea extraction
- Network required: no, unless explicitly needed by the task
- Secrets allowed: no

## Suggested checks

```bash
python3 scripts/analysis/branch_intelligence.py
python3 scripts/control/helios-control.py
```
