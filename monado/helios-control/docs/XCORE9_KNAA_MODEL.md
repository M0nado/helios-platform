# XCore9 KNAA value model

The versioned KNAA contract is `config/xcore9-knaa-model.v1.json`.

## KNAA semantics

`KNAA` means:

- **K**nowledge
- **N**ovelty
- **A**ctionability
- **A**lignment

Each dimension is normalized to `[0,1]` using clamp-to-unit-interval behavior.
Missing dimensions are preserved as `unknown` and reduce confidence.

## Scoring and uncertainty

- Aggregation method: weighted mean
- Minimum known dimensions: 2/4
- Confidence formula: `knownDimensionCount / 4`
- Insufficient evidence state: `unknown`

Threshold defaults:

- `blockBelow`: `0.35`
- `warnBelow`: `0.55`
- `reviewBelow`: `0.75`

## Policy gates

- KNAA output is **advisory by default**.
- Conservative auto-block may be enabled explicitly for tighter policies.
- KNAA has no direct promotion/deployment authority.

## Audit payload requirements

Each evaluation payload must include:

- `modelVersion`
- `thresholds`
- `evidenceLinks`
- `recommendation`
- `confidence`
- `policyMode`

## Threshold rollback procedure

1. Revert threshold changes via pull request.
2. Re-run targeted KNAA tests and contract checks.
3. Record evidence links for the reverted threshold set.
4. Keep promotion gates advisory until reviewers approve new thresholds.
