# HELIOS Connect

HELIOS Connect is the typed, zero-remote-mutation core shared by the CLI, GitHub summaries, and the Control Center. It records sanitized state only beneath ignored `.helios/connect/<session-id>/` paths.

## Safety boundary

- `auto` performs local discovery by default. Pass `--approve-read-only` to explicitly authorize the allowlisted GitHub and Azure identity checks; unavailable access is recorded rather than guessed.
- `operator` renders reviewed commands for a human-controlled terminal. It cannot execute them.
- `dashboard` reads sanitized session evidence locally.
- `verify` refreshes approved read-only evidence and classifies drift without reconciliation.
- Cloud Shell support is an inert handoff to `https://portal.azure.com/#cloudshell/`; it never owns a browser or terminal session.

Every mode reports `Commands executed by HELIOS: 0` and `Remote mutations performed by HELIOS: 0`.

## Build

```bash
dotnet build src/Helios.Connect/Helios.Connect.sln
dotnet run --project src/Helios.Connect/tests/Helios.Connect.ArchitectureTests
dotnet run --project src/Helios.Connect/src/Helios.Connect.Cli -- connect auto
dotnet run --project src/Helios.Connect/src/Helios.Connect.Cli -- connect auto --approve-read-only
dotnet run --project src/Helios.Connect/src/Helios.Connect.Cli -- connect operator
dotnet run --project src/Helios.Connect/src/Helios.Connect.Cli -- connect dashboard
dotnet run --project src/Helios.Connect/src/Helios.Connect.Cli -- connect verify --session <session-id>
```

Provider discovery implementations must implement `IReadOnlyDiscoveryAdapter`; the interface intentionally has no mutation operation. Exact repository commits and the canonical owner are derived from local Git metadata and `config/integrations/repositories.json`. Missing access is reported as unavailable rather than inferred.
