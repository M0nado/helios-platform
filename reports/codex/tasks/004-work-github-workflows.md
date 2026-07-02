# Codex Task: .github/workflows from work

- Priority: 96
- Task type: compare-selectively
- Allowed paths: `['.github/workflows/azure-infra.yml', '.github/workflows/branch-intelligence.yml', '.github/workflows/helios-control-plane.yml']`
- Blocked paths: `['.git', 'bin', 'obj']`
- Expected output: comparison notes, merge risk, tests to run, and idea extraction
- Network required: no, unless explicitly needed by the task
- Secrets allowed: no

## Suggested checks

```bash
python3 scripts/analysis/branch_intelligence.py
python3 scripts/control/helios-control.py
```
