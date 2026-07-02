# Codex Task: tests from work

- Priority: 98
- Task type: compare-selectively
- Allowed paths: `['tests/analytics/HELIOS.Analytics.FSharp.Tests/AnalyticsEngineTests.fs', 'tests/analytics/HELIOS.Analytics.FSharp.Tests/HELIOS.Analytics.FSharp.Tests.fsproj']`
- Blocked paths: `['.git', 'bin', 'obj']`
- Expected output: comparison notes, merge risk, tests to run, and idea extraction
- Network required: no, unless explicitly needed by the task
- Secrets allowed: no

## Suggested checks

```bash
python3 scripts/analysis/branch_intelligence.py
python3 scripts/control/helios-control.py
```
