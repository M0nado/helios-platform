# Codex Task: docs from work

- Priority: 61
- Task type: extract-ideas
- Allowed paths: `['docs/architecture/HYBRID_INTEGRATION_EXPANSION_PLAN.md', 'docs/architecture/WHOLE_PROJECT_NEXT_STEPS.md']`
- Blocked paths: `['.git', 'bin', 'obj']`
- Expected output: comparison notes, merge risk, tests to run, and idea extraction
- Network required: no, unless explicitly needed by the task
- Secrets allowed: no

## Suggested checks

```bash
python3 scripts/analysis/branch_intelligence.py
python3 scripts/control/helios-control.py
```
