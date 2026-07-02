# Codex Task: status-site from work

- Priority: 78
- Task type: compare-selectively
- Allowed paths: `['status-site/actions.md', 'status-site/index.md', 'status-site/wiki-export/Agent-Work-Queue.md', 'status-site/wiki-export/Branch-Graphs.md', 'status-site/wiki-export/Branch-Intelligence.md', 'status-site/wiki-export/Idea-Impact.md', 'status-site/wiki-export/Module-Ranking.md']`
- Blocked paths: `['.git', 'bin', 'obj']`
- Expected output: comparison notes, merge risk, tests to run, and idea extraction
- Network required: no, unless explicitly needed by the task
- Secrets allowed: no

## Suggested checks

```bash
python3 scripts/analysis/branch_intelligence.py
python3 scripts/control/helios-control.py
```
