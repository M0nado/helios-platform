# Hermes/XCore9 specialization packs

The specialization registry is versioned in
`config/xcore9-specialization-packs.v1.json`.

## Registry contract

Each pack defines:

- role;
- input/output modalities;
- allowed tools and denied tools;
- required capability contracts;
- max parallelism and timeout;
- idempotency-key requirement.

Packs cannot self-expand capabilities and cannot execute undeclared tools.

## Parallel execution policy

Global policy enforces bounded fan-out/fan-in and coordinator timeout
cancellation. Partial failures resolve to review-required outcomes, not silent
success.

## Plugin/skill binding rules

- Undeclared plugin/skill use is rejected.
- Required capability contracts must be present before execution.
- Correlation ID and evidence links are required for every invocation.

## Multimodal routing

Supported lanes: `text`, `code`, `docs`, `telemetry`, `media`.

Each lane requires provenance metadata and emits normalized evidence metadata so
traceability is preserved across fan-out/fan-in operations.
