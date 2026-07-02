# Codex Task: docs/integration from work

- Priority: 77
- Task type: extract-ideas
- Allowed paths: `['docs/integration/CONTROL_PLANE_COMMANDS.md', 'docs/integration/WEB_CONTROL_PLANE.md']`
- Blocked paths: `['.git', 'bin', 'obj']`
- Expected output: comparison notes, merge risk, tests to run, and idea extraction
- Network required: no, unless explicitly needed by the task
- Secrets allowed: no

## Suggested checks

```bash
python3 scripts/analysis/branch_intelligence.py
python3 scripts/control/helios-control.py
```
