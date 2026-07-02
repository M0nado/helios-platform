# Codex Task: src/core from work

- Priority: 104
- Task type: compare-selectively
- Allowed paths: `['src/core/HELIOS.Platform.Contracts/AnalyticsContracts.cs', 'src/core/HELIOS.Platform.Contracts/HELIOS.Platform.Contracts.csproj']`
- Blocked paths: `['.git', 'bin', 'obj']`
- Expected output: comparison notes, merge risk, tests to run, and idea extraction
- Network required: no, unless explicitly needed by the task
- Secrets allowed: no

## Suggested checks

```bash
python3 scripts/analysis/branch_intelligence.py
python3 scripts/control/helios-control.py
```
