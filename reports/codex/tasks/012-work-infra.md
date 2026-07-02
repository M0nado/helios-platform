# Codex Task: infra from work

- Priority: 78
- Task type: compare-selectively
- Allowed paths: `['infra/azure/README.md', 'infra/azure/main.bicep', 'infra/azure/modules/keyvault.bicep', 'infra/azure/modules/network.bicep', 'infra/azure/modules/observability.bicep', 'infra/azure/modules/storage.bicep', 'infra/azure/parameters/dev.json']`
- Blocked paths: `['.git', 'bin', 'obj']`
- Expected output: comparison notes, merge risk, tests to run, and idea extraction
- Network required: no, unless explicitly needed by the task
- Secrets allowed: no

## Suggested checks

```bash
python3 scripts/analysis/branch_intelligence.py
python3 scripts/control/helios-control.py
```
