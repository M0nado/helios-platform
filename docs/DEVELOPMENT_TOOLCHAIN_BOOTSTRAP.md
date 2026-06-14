# HELIOS Local Toolchain Bootstrap

Use `scripts/setup/bootstrap-local-tools.sh` when a local runner or container does not already provide the .NET SDK or Azure CLI.

## What it installs

- .NET SDK channel `8.0` by default, using `dotnet-install.sh` when present or downloading it on demand.
- Azure CLI into an isolated Python virtual environment with `pip install azure-cli`.
- A sourceable environment file at `.tools/env.sh` unless `HELIOS_TOOLS_DIR` overrides the location.

The installed binaries live under `.tools/`, which is ignored by git, so verification runs do not dirty `git status --short`.

## Quick start

```bash
scripts/setup/bootstrap-local-tools.sh
source .tools/env.sh
az --version
dotnet --info
dotnet test tests/HELIOS.Platform.Tests/HELIOS.Platform.Tests.csproj --configuration Release --verbosity minimal
```

## Useful overrides

```bash
HELIOS_TOOLS_DIR=/tmp/helios-tools scripts/setup/bootstrap-local-tools.sh
HELIOS_DOTNET_CHANNEL=8.0 scripts/setup/bootstrap-local-tools.sh
HELIOS_INSTALL_AZURE_CLI=0 scripts/setup/bootstrap-local-tools.sh
```

For authenticated Azure operations, run `az login` or configure a service principal/managed identity after the CLI is installed. The bootstrap intentionally installs tools only; it does not write credentials or tenant secrets.
