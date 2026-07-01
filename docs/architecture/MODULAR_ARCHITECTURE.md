# HELIOS Modular Architecture

HELIOS is organized around explicit language and responsibility boundaries.

## Frontend

`src/frontend/HELIOS.Control.WinUI/` owns the WinUI 3 shell, pages, view models, and UI-facing services. It should call platform contracts and orchestration services instead of directly owning Azure, Hermes, XCore, Python, or native performance logic.

## C# platform core

- `src/core/HELIOS.Platform.Contracts/` owns public interfaces, DTOs, result types, and integration contracts.
- `src/core/HELIOS.Platform.Orchestration/` owns workflow coordination and subsystem routing.
- `src/core/HELIOS.Platform/` owns shared application services and composition.

## Native performance

`src/native/HELIOS.Performance/` owns C++ performance-sensitive workloads and exposes an explicit interop boundary.

## Analytics

`src/analytics/HELIOS.Analytics.FSharp/` owns F# math, statistics, prediction, analytics, and parallel processing APIs.

## Python adapters

- `src/python/helios_aihub/` owns AIHub integration.
- `src/python/hermes_xcore/` owns Hermes/XCore specialist adapters.

## Cloud and security

- `infra/` owns Azure Bicep infrastructure.
- `src/security/HELIOS.Security/` owns platform security services.
- `docs/security/` owns threat model, secrets, and hardening guidance.
