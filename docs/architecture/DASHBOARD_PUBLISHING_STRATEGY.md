# Dashboard Publishing Strategy

Branch Intelligence currently publishes reports as GitHub Actions artifacts and writes `reports/branch-intelligence/dashboard.md` into the workflow summary. This keeps generated churn out of the default workflow while still preserving downloadable reports.

## Options

| Strategy | Use when | Tradeoff |
|---|---|---|
| Artifact only | Reports are noisy or experimental | Safest, no generated commits |
| Commit generated dashboard | The team wants a persistent repo-local dashboard | Requires `contents: write` and loop protection |
| GitHub Pages | The team wants a browsable operations portal | Requires Pages setup and artifact publishing |

## Recommended path

1. Keep artifact-only publishing until real remotes and CI scoring are enabled.
2. Promote to GitHub Pages once reports are stable.
3. Commit only curated summaries, not full machine JSON artifacts.
