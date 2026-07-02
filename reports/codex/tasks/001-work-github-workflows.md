# Codex Task: .github/workflows from work

- Priority: 87
- Task type: extract-ideas
- Allowed paths: `['.github/workflows/helios-control-plane.yml']`
- Blocked paths: `['.git', 'bin', 'obj']`
- Expected output: comparison notes, merge risk, tests to run, and idea extraction
- Network required: no, unless explicitly needed by the task
- Secrets allowed: no

## Suggested checks

```bash
python3 scripts/analysis/branch_intelligence.py
python3 scripts/control/helios-control.py
```
