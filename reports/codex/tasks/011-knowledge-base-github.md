# Codex Task: .github from knowledge-base

- Priority: 23
- Task type: idea-review
- Allowed paths: `['.github/ISSUE_TEMPLATE/feature-request.md:21']`
- Blocked paths: `['.git', 'bin', 'obj']`
- Expected output: Creates a native performance path for C++/XCore acceleration without blocking managed code.
- Network required: no, unless explicitly needed by the task
- Secrets allowed: no

## Suggested checks

```bash
python3 scripts/analysis/branch_intelligence.py
python3 scripts/control/helios-control.py
```
