# HELIOS Platform

HELIOS Platform is being reorganized into a modular, multi-language platform spanning C#, WinUI 3, C++, F#, Python, Hermes/XCore integrations, Azure infrastructure, security hardening, and generated wiki/project automation.

## Verified current state

The canonical status file is `docs/status/project-status.yaml`.

As of 2026-07-01:

- Current branch observed: `work`.
- No Git remotes are configured in this checkout.
- External sources such as `helios-control`, `hermes-fleet-production`, `hermes-core`, and `xcore-agent` still require remotes or local repository paths before merge work can begin.
- Canonical planning, architecture, Azure, security, docs, and test scaffolding has been added to guide the overhaul.

## Start here

- `docs/START_HERE.md`
- `docs/status/current-status.md`
- `docs/planning/helios-overhaul-plan.yaml`
- `docs/planning/branch-inventory.md`
- `docs/planning/merge-order.md`
- `docs/architecture/MODULAR_ARCHITECTURE.md`
- `docs/architecture/COMPONENT_MATRIX.md`
- `docs/architecture/BUILD_VARIANTS.md`

## Architecture boundaries

- WinUI 3 frontend: `src/frontend/HELIOS.Control.WinUI/`
- C# core/contracts/orchestration: `src/core/`
- C++ performance backend: `src/native/HELIOS.Performance/`
- F# analytics/prediction: `src/analytics/HELIOS.Analytics.FSharp/`
- Python AIHub/Hermes/XCore adapters: `src/python/`
- Azure IaC and validation: `infra/`
- Security guidance and scripts: `docs/security/`, `scripts/security/`

## Build and validation

Use `HELIOS.Platform.slnx` for verified .NET projects. Use the focused GitHub Actions workflows for .NET, native, Python, docs, security, Azure what-if, wiki generation, and release readiness.
