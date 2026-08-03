# HELIOS Polyglot Runtime Contract v1

Status: proposed governance baseline  
Authority: GitHub Issues `M0nado/helios-platform#148` and `#149`  
Canonical contract owner: `M0nado/helios-platform`  
Applies to: C#/.NET 8, the Windows operator GUI, F# scoring, optional C++20 native assistance, and Python/OpenAI Agents SDK workers

## Purpose

HELIOS is a governed product system with multiple implementation languages, not a collection of mutually trusted runtimes. This contract defines the only supported boundaries between those runtimes so that code can be built, tested, promoted, observed, and rolled back without giving a language adapter broader authority than the policy engine.

GitHub owns source, schemas, CI, manifests, and releases. Azure executes approved desired state. Linear tracks work. SharePoint stores governed human-readable architecture and evidence. Slack and Teams are notification and interaction surfaces only. Every persistent engineering change returns to GitHub through a protected pull request.

## Runtime ownership

| Runtime | Owned responsibilities | Required isolation | Prohibited responsibilities |
| --- | --- | --- | --- |
| C# / .NET 8 | Canonical contracts, policy evaluation, broker/API host, connector orchestration, evidence and approval coordination | ASP.NET Core service or bounded worker; least-privilege managed identity | Raw secret readback, direct-to-main writes, tenant-wide Graph, unapproved production apply |
| Windows GUI / .NET 8 | Current WPF operator client and future browser-viewable status, plan preview, replay preview, approvals, audit and evidence exploration | Unelevated client of the governed gateway; typed generated client | Direct GitHub/Azure/Graph/SaaS access, connector secrets, local policy override, alternate deployment engine |
| F# / .NET 8 | Pure scoring, routing, evaluation, policy analysis, and deterministic decision support | Same-process signed assembly only for pure functions; otherwise a bounded worker | Side-effecting connectors, approvals, secret access, production mutation, opaque self-modification |
| C++20 native assist | Optional performance-sensitive parsing, hashing, compression, hardware inspection, or virtual-device test primitives | Prefer an out-of-process local service; a minimal C ABI is permitted only after native threat review | Business policy, networking, arbitrary process execution, cloud credentials, physical USB writes on hosted runners |
| Python 3.12 | Hermes/AIHub workers, OpenAI Agents SDK orchestration, evaluation, model/provider adapters, offline learning jobs | Pinned virtual environment or container; service/MCP boundary; no embedded interpreter in the production .NET host | Direct production apply, self-approval, silent training-data collection, raw tenant/repository access, unbounded tools |

No runtime becomes authoritative merely because it can invoke another runtime. The .NET policy result applies equally to the GUI, Codex, local MCP, Microsoft 365 Copilot, F# evaluators, C++ helpers, and Python agents.

## Current baseline and transition

Merged PR #144 is the mixed-runtime code baseline, not proof of production readiness. Its current authoritative starting points include:

- `src/core/HELIOS.Platform.Contracts/AIHubEngineContracts.cs` for cross-platform .NET 8 AIHub contracts;
- `src/analytics/HELIOS.Analytics.FSharp/AIHub/AiHubLearningEngine.fs` for F# scoring and learning functions;
- `src/native/HELIOS.Native.Performance/include/helios/aihub_native_engine.hpp` for the optional C++20 native-assist interface;
- `.github/workflows/aihub-self-learning-growth.yml` and the Python build/report tools for report-first learning automation;
- the current `net8.0-windows` WPF application as a Windows client, not a Linux-hosted service.

The first hardening increment must close these observed gaps rather than create another parallel implementation:

- select Python 3.12 as the governed target and remove the current 3.11/3.12 workflow inconsistency;
- add direct F# unit, property, deterministic golden-vector, and ranking-order tests;
- compile and test the C++ target instead of treating a header-only interface as verified;
- align the Windows test target with the Windows application target;
- include the actual Windows, F#, native, contracts, service, and test projects in the governed build graph;
- make any failed required build-graph node fail the workflow;
- keep learning/report workflows read-only by default and exclude generated reports from their own inputs.

The Windows WPF client and a future browser-viewable operator experience may share generated clients and view models, but neither may be retargeted into or substituted for the cross-platform service host.

## Canonical boundary formats

1. JSON Schema 2020-12 is canonical for durable events, commands, plans, approvals, results, and evidence.
2. OpenAPI 3.1 describes HTTPS APIs. Generated clients are used where supported; manually duplicated DTOs are not authoritative.
3. Protocol Buffers may be used for a measured high-throughput internal boundary, but the `.proto` schema and its JSON mapping must be generated from or conformance-tested against the canonical contract.
4. In-process .NET boundaries use a signed, versioned contract package containing C# records and F#-friendly types. Domain behavior does not cross as untyped dictionaries or `dynamic` objects.
5. Python uses generated or conformance-tested Pydantic models. OpenAI tool parameters and structured outputs reference the same JSON Schemas.
6. C++ uses generated Protobuf types for an out-of-process boundary. A native library boundary is limited to a small C ABI with opaque handles, explicit buffer lengths, caller/callee ownership rules, and numeric error codes.

### Required event envelope

Every event contains:

- `schemaVersion`, `id`, `type`, `source`, and `subject`;
- `occurredAt` as UTC RFC 3339;
- `correlationId`, `causationId`, and W3C `traceParent` when available;
- `dataClassification` and `redactionProfile`;
- `producer.name`, `producer.version`, and `producer.commit`;
- `payload` validated against the registered schema;
- `extensions`, whose unknown fields are preserved but never treated as permissions.

Every command additionally contains the registered action ID, target environment, dry-run/apply mode, repository/ref/commit, manifest and artifact digests, typed parameters, authenticated subject, reason, idempotency key, deadline, and any required plan/approval IDs.

### Scalar rules

- Identifiers are UUIDs or registered stable string IDs; they are never display names.
- Time is UTC RFC 3339 with explicit offset. Durations use ISO 8601.
- Digests use `algorithm:lowercase-hex`, normally `sha256:<digest>`.
- Money and precision-sensitive decimal values are strings with an explicit unit or currency.
- Binary payloads are immutable artifact references with digest, size, media type, and expiry; they are not embedded in cross-runtime JSON.
- Enums reject unknown values for commands and preserve unknown values for read-only events.
- Secret values are never contract fields. Contracts carry only registered secret-reference names.

## Versioning and compatibility

- Contract packages and APIs use semantic versioning. Every durable message also carries `schemaVersion`.
- Additive optional fields are backward compatible. Renaming, removing, narrowing, changing meaning, or making an optional field required is breaking.
- A breaking change creates a new major schema and runs dual-read compatibility during a bounded migration window.
- Producers must pass current and previous-major consumer conformance tests before promotion.
- Consumers ignore permitted unknown extension fields but fail closed on unknown commands, capabilities, approval classes, or security-critical enums.
- The schema registry records owner, source repository/commit/path, license, compatibility window, deprecation date, and generated artifact hashes.

## Error contract

Cross-runtime failures return a typed result; language exceptions do not cross the boundary.

| Code family | Meaning | Retry rule |
| --- | --- | --- |
| `VALIDATION_*` | Schema, input, digest, or invariant failure | Never retry without changed input |
| `AUTHN_*` / `AUTHZ_*` | Identity or policy denial | Never retry automatically |
| `CONFLICT_*` | Idempotency, version, lease, or state conflict | Re-read state; retry only if policy permits |
| `DEPENDENCY_TRANSIENT_*` | Timeout, throttling, or temporary provider failure | Bounded exponential backoff with jitter |
| `DEPENDENCY_PERMANENT_*` | Unsupported or permanently rejected provider request | Dead-letter; operator action required |
| `CANCELLED` / `DEADLINE_EXCEEDED` | Caller cancellation or expired deadline | Do not resume side effects without a new plan |
| `INTERNAL_*` | Redacted implementation failure | Dead-letter after bounded retry; never expose secrets or stack traces externally |

An error includes `code`, safe `message`, `correlationId`, `retryable`, optional `retryAfter`, and an evidence reference. Internal details remain in access-controlled telemetry.

## Cancellation, deadlines, and idempotency

- Deadlines and cancellation propagate through .NET `CancellationToken`, F# async/task cancellation, C++20 `std::stop_token`, Python `asyncio` cancellation, and all network calls.
- A consumer checks cancellation before acquiring a lease and before each external side effect.
- Cancellation after a side effect records the committed checkpoint; it never reports a completed mutation as if it were rolled back.
- Every write is idempotent on the registered event/command ID plus target. Providers also receive their supported idempotency token.
- Retries resume from durable checkpoints. Non-idempotent writes are never retried blindly.
- Expired plans and approvals cannot be refreshed implicitly; a new plan and approval are required.

## Native boundary rules

C++20 is optional. A managed implementation remains the reference until benchmarks justify native assistance.

- Prefer an out-of-process localhost service over in-process P/Invoke.
- If a C ABI is approved, exported functions are `noexcept`; no C++ exception, STL type, allocator, or ownership ambiguity crosses the ABI.
- .NET wraps native handles in `SafeHandle`; buffers always include length and ownership; UTF-8 is the interchange encoding.
- Native inputs are length-bounded and fuzz-tested. AddressSanitizer, UndefinedBehaviorSanitizer, and platform-appropriate control-flow protections run in CI.
- A native crash opens the circuit breaker and cannot crash or silently corrupt the policy host.
- Native components have no default network access and receive files only through digest-bound, read-only handles or brokered streams.

## Python and OpenAI Agents SDK rules

- Python dependencies are pinned with hashes and built into an immutable environment. Dynamic install-at-runtime is forbidden.
- Agent tools are schema-defined, allowlisted, capability-scoped, time-bounded, and deny-by-default.
- Handoffs preserve correlation, classification, deadline, principal, and approval context; a handoff cannot increase capability.
- Structured outputs are validated before they are interpreted as plans or tool arguments.
- Model output is untrusted input. It cannot authorize itself, select production credentials, execute arbitrary shell, or bypass policy.
- Provider credentials use `OPENAI_API_KEY` locally and a Key Vault reference in Azure. Keys never enter prompts, traces, memory, SharePoint, GitHub, Linear, Slack, or Teams.
- Approved sanitized sources, lineage, evaluation results, and rollback points are mandatory before a learning artifact can be proposed for promotion.
- Conversations are not silently harvested as training data.

## Build, test, and release matrix

Every pull request affecting a runtime boundary must run:

1. schema lint and generated-code drift detection;
2. C# and F# compile with .NET 8, analyzers, warnings-as-errors for governed projects, unit tests, and contract tests;
3. Windows GUI unit, bUnit/component, accessibility, and browser smoke tests on a supported Windows runner;
4. C++20 configure/build/test on declared architectures plus sanitizer and fuzz jobs when native code changes;
5. Python 3.12 lint, type check, unit tests, locked-dependency verification, tool-schema tests, and sandbox escape/negative tests;
6. cross-language golden-vector tests for serialization, validation, errors, cancellation, idempotency, time, decimal, enum, and digest handling;
7. secret scanning, dependency review, CodeQL or equivalent static analysis, license policy, and malicious-package checks;
8. CycloneDX or SPDX SBOM generation per artifact and a composed release SBOM;
9. provenance and GitHub OIDC-backed artifact attestation; release signatures and hashes verified before deployment;
10. install, upgrade, rollback, and previous-major compatibility tests.

Hosted runners may test disposable virtual disks only. Physical USB media requires an isolated self-hosted runner, protected-environment approval, and the USB Wizard security contract.

## Observability and evidence

- All runtimes emit OpenTelemetry traces, metrics, and structured redacted logs using the same correlation ID.
- Telemetry records runtime/package version, commit, schema version, route, outcome, latency, retry count, approval class, and evidence digest.
- Prompts, model responses, payload bodies, personal data, and source content are opt-in and redacted by classification policy.
- Release evidence binds repository, commit, workflow run, SBOM, attestations, signatures, test results, deployment plan, approval, environment, and rollback point.

## Promotion gates

A runtime or boundary cannot publish production artifacts until:

- its repository is accepted under Issue #149 and the canonical workflow is green;
- PR #170 restores the repository-wide gate;
- schemas and compatibility tests pass across all affected languages;
- generated artifacts, SBOMs, signatures, hashes, and provenance verify;
- Azure OIDC, protected environments, Key Vault references, observability, and rollback are proven;
- Issue #162 is closed through explicit review.

No part of this document authorizes a merge, production deployment, tenant consent, RBAC change, archive, destructive device action, or secret creation.
