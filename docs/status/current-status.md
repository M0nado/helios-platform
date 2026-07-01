# HELIOS Platform Current Status

This document summarizes the verified repository state as of 2026-07-01.

## Verified state

- Current branch: `work`.
- Git remotes are configured for `helios-control` and `hermes-fleet-production`; `helios-control` still requires credentials before it can be fetched.
- One untracked file was observed: `dotnet-install.sh`.
- Existing .NET projects are present under `src/`, `HELIOS.Platform.Installer/`, and `HELIOS.Platform.ShellExtension/`.
- Canonical architecture, planning, Azure, merge, wiki, testing, and security scaffolding has been added for the overhaul.

## Integration status

Hermes fleet assets have been selectively imported from `hermes-fleet-production`; bulk-merging remains blocked by generated/historical content review. `helios-control` cannot be imported until credentials or a local path are available. `hermes-core` and `xcore-agent` still need remotes or local paths.

## Source of truth

Structured status is maintained in `docs/status/project-status.yaml`.

## Tooling restored

- .NET SDK 10 is required for `HELIOS.Platform.slnx` CLI support.
- PowerShell 7+ is required for repository automation scripts.
- `dotnet restore HELIOS.Platform.slnx` now restores successfully in this environment.
- `dotnet build HELIOS.Platform.slnx --configuration Release --no-restore` now builds the stable solution lane.
- `dotnet build HELIOS.Legacy.Quarantine.slnx --configuration Release --no-restore` now builds after quarantining unstable legacy Phase 10, API, advanced ML/optimization, plugin, server, Windows UI/device, theme, and test sources from the compile.
