# F# Ranking Bridge

The branch intelligence runner emits machine-readable metrics that can be consumed by `HELIOS.Analytics.FSharp` for percentile ranking, anomaly detection, prediction, and health scoring.

## Current bridge artifact

- `reports/branch-intelligence/analytics-metrics.json`

## Planned consumer

A future `HELIOS.RepositoryAnalytics` tool should reference:

- `src/core/HELIOS.Platform.Contracts/HELIOS.Platform.Contracts.csproj`
- `src/analytics/HELIOS.Analytics.FSharp/HELIOS.Analytics.FSharp.fsproj`

It should read branch/module scores, run the F# analytics facade, and write ranked health reports back to `reports/branch-intelligence/`.
