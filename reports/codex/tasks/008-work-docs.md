# Codex Task: docs from work

- Priority: 79
- Task type: compare-selectively
- Allowed paths: `['docs/architecture/AZURE_HYBRID_ARCHITECTURE.md', 'docs/architecture/DASHBOARD_PUBLISHING_STRATEGY.md', 'docs/architecture/FSHARP_RANKING_BRIDGE.md', 'docs/microsoft-365/COPILOT_INTEGRATION.md', 'docs/security/CONTROL_PLANE_PERMISSIONS.md']`
- Blocked paths: `['.git', 'bin', 'obj']`
- Expected output: comparison notes, merge risk, tests to run, and idea extraction
- Network required: no, unless explicitly needed by the task
- Secrets allowed: no

## Suggested checks

```bash
python3 scripts/analysis/branch_intelligence.py
python3 scripts/control/helios-control.py
```
