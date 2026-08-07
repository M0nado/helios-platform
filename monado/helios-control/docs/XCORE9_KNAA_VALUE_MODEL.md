# XCore9 KNAA value model (v1)

## Purpose

KNAA is the evaluator contract used by the HELIOS control-run pipeline to produce
traceable recommendation telemetry before protected approval.

The evaluator is recommendation-only and never grants deployment or promotion
authority.

## Canonical KNAA semantics

- **K — Knowledge grounding**: whether run evidence is present, valid, and tied
  to the configured boundary.
- **N — Normalization fidelity**: whether deterministic plan and run contract
  signals are complete and internally consistent.
- **A — Actionability signal**: whether the run has enough structured signal to
  produce a useful recommendation.
- **A — Assurance posture**: whether safety and approval-gate indicators support
  a governed recommendation.

## Contract

- Machine-readable schema: `config/knaa-value-model.v1.schema.json`
- Runtime object: `KnaaAssessment` in `src/Helios.Connect.Api/KnaaEvaluation.cs`
- Version constants:
  - `schemaVersion`: `helios.knaa.v1`
  - `modelVersion`: `xcore9-knaa-1.0.0` (override with `HELIOS_KNAA_MODEL_VERSION`)

## Scoring pipeline

1. Collect source signals from control-run state (context step, inventory count,
   plan presence, evidence digest, connector selection, approval-step contract).
2. Normalize all bounded values to `[0.0, 1.0]`.
3. Build KNAA vector components:
   - `knowledge`
   - `normalization`
   - `actionability`
   - `assurance`
4. Compute weighted score and confidence.
5. Evaluate policy thresholds to classify:
   - `block`
   - `warn`
   - `review-required`
   - `pass`
6. Emit recommendation with explicit non-authority flags:
   - `promotionRecommended` (advisory)
   - `deploymentAuthorized` (always `false`)

## Missing-data and uncertainty semantics

- `known`: signal/component has enough evidence.
- `unknown`: partial coverage; score can be advisory but confidence is reduced.
- `insufficient-evidence`: required signals are missing; score is `null` and the
  recommendation requires review or conservative block mode.

Required signals for a non-null score:

- context verification signal
- plan presence signal
- evidence digest signal

## Policy defaults and governance

Defaults:

- `HELIOS_KNAA_THRESHOLD_BLOCK=0.35`
- `HELIOS_KNAA_THRESHOLD_WARN=0.55`
- `HELIOS_KNAA_THRESHOLD_REVIEW_REQUIRED=0.75`
- `HELIOS_KNAA_CONSERVATIVE_AUTO_BLOCK=false`

Behavior:

- Default mode is advisory-only.
- Conservative auto-block mode only escalates `block` outcomes; it still does not
  bypass protected workflow approval.
- No KNAA outcome can directly apply infrastructure, approve deployment, merge a
  PR, or promote an agent.

## Audit payload requirements

Connector relay payloads include:

- `knaaModelVersion`
- `knaaThresholds`
- `knaaEvidenceLinks`
- `knaaOutcome`
- full `knaa` assessment object

Traceability chain:

`sourceSignals -> vector -> recommendation -> policy outcome`

## Threshold change and rollback procedure

1. Propose threshold changes by pull request with rationale and expected policy
   impact.
2. Update only the KNAA environment variables in the controlled runtime config.
3. Run `dotnet test monado/helios-control/tests/Helios.Connect.Tests/Helios.Connect.Tests.csproj`.
4. Deploy through the existing protected workflow gate.
5. Monitor run outcomes for unexpected block/warn escalation.

Rollback:

1. Revert to the previously approved threshold values.
2. Redeploy through the same protected workflow.
3. Verify that control-run KNAA outcomes return to prior distribution.
4. Attach correlation IDs and evidence links to the rollback record.
