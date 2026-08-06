# Hermes/XCore Source Import Disposition — 2026-07-28

This record classifies the supplied integration pack and Python modules before any code is promoted into `M0nado/helios-platform`.

## Promote as rewritten, tested components

- Typed environment, capability, event, approval, incident, and evidence contracts.
- Deterministic configuration builders from the security, VM orchestration, and ML registry modules.
- Read-only transcript classification and conversion logic.
- Deterministic engine catalog and advisory recommendation logic.

Promotion requires repository-native naming, schemas, unit tests, explicit error handling, bounded inputs, and removal of host-specific paths.

## Rewrite behind governed adapters

- AIHub and Hermes integration clients.
- VM and accelerator orchestration.
- Model registry and training-job descriptions.
- Provider and enterprise-connector drivers.

Adapters must use dependency injection, workload identity, Key Vault references, timeouts, retry limits, idempotency, correlation IDs, dry-run support, and append-only evidence. A connector may expose only the authority granted in `system.contract.json`.

## Quarantine

- Process-local control servers that bind broadly or lack an authenticated authorization layer.
- Direct training-trigger endpoints.
- Self-registration and self-teaching loops that can mutate runtime state without reviewed artifacts.
- Host installers or scripts that change WSL, Hyper-V, Docker, package-manager, recovery, boot, or removable-media state.
- Any flow that transports provider keys through local HTTP headers, command lines, environment dumps, or logs.

Quarantined components are design evidence only. They are not copied into a runtime or CI image.

## Do not import

- Repair and master logs.
- Reboot sentinel files.
- Incomplete browser downloads.
- Generated outputs without reproducible source and provenance.

These artifacts may be referenced by an incident or recovery record when necessary, but they are never executable inputs.

## Release gate

No supplied source becomes deployable until a pull request proves:

1. the target capability and environment;
2. the human or protected-environment authority;
3. the exact immutable artifact;
4. dry-run or what-if evidence;
5. secret and identity handling;
6. rollback or recovery behavior;
7. tests, security checks, and an accountable owner.
