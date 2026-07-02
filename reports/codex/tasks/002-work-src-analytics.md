# Codex Task: src/analytics from work

- Priority: 102
- Task type: compare-selectively
- Allowed paths: `['src/analytics/HELIOS.Analytics.FSharp/HELIOS.Analytics.FSharp.fsproj', 'src/analytics/HELIOS.Analytics.FSharp/Models/AnalyticsModels.fs', 'src/analytics/HELIOS.Analytics.FSharp/ParallelWorkloads.fs', 'src/analytics/HELIOS.Analytics.FSharp/Prediction/PredictionWorkloads.fs', 'src/analytics/HELIOS.Analytics.FSharp/PublicApi.fs', 'src/analytics/HELIOS.Analytics.FSharp/Statistics/AnalyticsWorkloads.fs', 'src/analytics/HELIOS.Analytics.FSharp/Statistics/MathWorkloads.fs']`
- Blocked paths: `['.git', 'bin', 'obj']`
- Expected output: comparison notes, merge risk, tests to run, and idea extraction
- Network required: no, unless explicitly needed by the task
- Secrets allowed: no

## Suggested checks

```bash
python3 scripts/analysis/branch_intelligence.py
python3 scripts/control/helios-control.py
```
