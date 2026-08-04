# Reliability evidence and controlled failure contract

`scripts/reliability/reliability_harness.py` is the repository-local evidence
calculator for provider, worker, queue, storage, identity, network, and deployment
paths. It does not provision Azure resources or change production state.

## Required measurements

Each run records availability, successful service rate, latency, error rate, retry
rate, dead-letter count, queue age, cancellation success, restart success, mean time
to detect (MTTD), mean time to recover (MTTR), rollback success, restore success,
and cleanup completeness. `degradedRate` and `outageRate` are separate: a slow or
fallback response must not be reported as either fully healthy or totally down.

Use stable operation and correlation IDs in the surrounding normalized integration
event. Evidence artifacts should link back to the issue, workflow, deployment, and
telemetry query without containing credentials or raw fleet evidence.

## Controlled failures

The scenario manifest is disabled by default and declares one bounded development
failure for every required target. The injector rejects staging and production,
limits each scenario's injection count, and uses a deterministic seed. Run it only
against isolated development dependencies with an owner, expiry, and rollback plan.
Identity injection means an expired synthetic development credential; it must never
disable an account or rotate a real secret. Deployment injection means a development
health-check failure; it must never invoke a production rollout or rollback.

## Duplicate and orphan proof

Every retry/replay campaign must execute the same logical request at least twice and
capture its `durableEffectId`, whether that attempt set `effectCreated`, and the
resulting `orphanedResources`. `assess_repeated_failure_evidence` fails unless all
attempts resolve to exactly one durable effect, no more than one attempt created it,
and the orphan set is empty. Keep distinct attempt and correlation IDs in the
surrounding event evidence. Human review should compare the durable store, queue/DLQ,
leases, temporary resources, and deployment slots before accepting the proof.

## SLO burn-rate triage

Compute burn rate as observed bad-event rate divided by the SLO error budget. The
harness marks a **critical fast burn** when both 5-minute and 1-hour windows are at
least 14.4x, and a **high slow burn** when both 30-minute and 6-hour windows are at
least 6x. Reliability work should be ordered by critical, high, then normal priority;
raw incident count alone must not override sustained error-budget consumption.

## Example

```bash
python3 scripts/reliability/reliability_harness.py observations.json \
  --slo 0.999 --output reliability-report.json
python3 -m unittest discover -s scripts/reliability/tests -v
```

Generated reports are CI evidence and should be uploaded as workflow artifacts, not
committed. Any infrastructure experiment still requires Bicep validation/what-if,
an explicit approval gate, and a documented rollback.
