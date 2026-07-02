# HELIOS Codespace + Microsoft C# Environment Setup

This guide defines the target development environment for HELIOS Platform.

## Goal

Create one clean Codespace and one local Windows setup path for C#/.NET development, Microsoft/Azure integration, AIHub work, installer refactoring, and GUI planning.

## Required toolchain

- .NET 8 SDK
- PowerShell 7+
- GitHub CLI
- Azure CLI
- Docker
- Python 3.11 for legacy/reference validation only
- Node.js LTS for tooling/docs/WebView/dashboard prototypes
- VS Code extensions for C#, PowerShell, Azure, Docker, Python, YAML, Markdown

## Codespace behavior

The Codespace should:

1. Restore primary .NET projects.
2. Validate basic repo layout.
3. Expose ports for local dashboards and APIs.
4. Avoid running destructive Windows scripts.
5. Let Codex/Copilot safely modify docs, configs, and C# skeletons.

## Windows local behavior

Local Windows development should use:

- Visual Studio 2022 or VS Code + C# Dev Kit
- Windows App SDK / WinUI 3 for future shell work
- WPF preserved for current Monado animation assets
- Windows Terminal / PowerShell 7
- DevDrive for source/build/cache
- Optional WSL2 for AIHub local models and Linux dev tools

## Development paths

### Safe in Codespaces

- C# library development
- AIHub provider interfaces
- docs/config editing
- GitHub Actions workflows
- JSON manifest validation
- non-destructive script linting

### Windows-only local

- WinUI/WPF visual testing
- Razer Chroma integration
- driver optimizer testing
- BitLocker/VHDX vault work
- Defender/WDAC/AppLocker work
- USB/recovery creator testing
- WinRE/WinPE work

## Recommended branch flow

```text
main
  └─ rescue/consolidation-aihub-integration
       ├─ copilot/aihub-hermes-codex-foundry
       ├─ feature/helios-aihub-core
       ├─ feature/helios-installer-csharp
       ├─ feature/helios-gui-shell
       └─ feature/helios-dotnet-ci
```

## First validation commands

```powershell
dotnet --info
gh --version
az --version
pwsh --version
```

Then:

```powershell
dotnet restore src/core/HELIOS.Platform/HELIOS.Platform.csproj
dotnet build src/core/HELIOS.Platform/HELIOS.Platform.csproj --configuration Release
```

If full platform build is noisy, build smaller modules first as they are created:

```powershell
dotnet build src/HELIOS.AIHub/HELIOS.AIHub.csproj
dotnet build src/HELIOS.Installer/HELIOS.Installer.csproj
```

## Environment variables

Never commit real values. Use local secrets, GitHub Actions secrets, or Azure Key Vault.

Suggested names:

```text
HELIOS_ENV=development
HELIOS_PROFILE=dev
HELIOS_AIHUB_MODE=local-first
AZURE_TENANT_ID=<secret>
AZURE_SUBSCRIPTION_ID=<secret>
AZURE_CLIENT_ID=<secret>
AZURE_CLIENT_SECRET=<secret>
OPENAI_API_KEY=<secret>
AZURE_OPENAI_ENDPOINT=<secret>
AZURE_KEYVAULT_URI=<secret>
```

## Cleanup rules

- Keep exactly one active `.devcontainer/devcontainer.json`.
- Archive old Codespaces repos and docs as references only.
- Do not duplicate devcontainer variants unless they are named profiles with a clear reason.
- Keep Windows-destructive scripts outside Codespace auto-start commands.

## Next build goals

1. Create a root solution.
2. Add buildable AIHub core.
3. Add installer C# shell with no destructive logic.
4. Add CI for restore/build/test.
5. Add Windows runner workflow for GUI and installer tests.
