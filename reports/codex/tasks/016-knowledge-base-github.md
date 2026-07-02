# Codex Task: .github from knowledge-base

- Priority: 23
- Task type: idea-review
- Allowed paths: `['.github/WORKFLOWS.md:210']`
- Blocked paths: `['.git', 'bin', 'obj']`
- Expected output: Uses existing F# math/prediction APIs to rank modules, branches, and fleet events.
- Network required: no, unless explicitly needed by the task
- Secrets allowed: no

## Suggested checks

```bash
python3 scripts/analysis/branch_intelligence.py
python3 scripts/control/helios-control.py
```
