# HELIOS build separation

HELIOS uses separate build lanes for platform-neutral core code and Windows desktop shell code.

## Cross-platform Linux lane

The Linux final gate intentionally builds only the C# projects under `src/core/HELIOS.Platform.*`:

- `src/core/HELIOS.Platform.Contracts/HELIOS.Platform.Contracts.csproj` for shared contracts.
- `src/core/HELIOS.Platform.Minimal/HELIOS.Platform.csproj` for the minimal CLI smoke target.
- `src/core/HELIOS.Platform/HELIOS.Platform.csproj` for the orchestration/core CLI.

This keeps orchestration, contracts, and CLI validation portable for Ubuntu runners and avoids accidentally pulling Windows desktop SDK requirements into the cross-platform CI gate.

## Windows-only shell lane

The repository-root `HELIOS.Platform.csproj` remains a Windows desktop project with WPF enabled and `net8.0-windows` targeting. WPF, WinForms tray, shell-extension, installer, and sample WPF shell projects build in the dedicated Windows Shell Build workflow on `windows-latest`.

## `EnableWindowsTargeting` tradeoff

Do not enable `EnableWindowsTargeting` in the root Windows project by default. It can let a Linux runner compile a Windows-targeted project, but it also makes the Linux lane restore Windows desktop targeting packs and blurs the boundary between portable core code and Windows shell code.

If a temporary CI scenario must compile the root Windows project on Linux, set `EnableWindowsTargeting=true` only in that CI-specific command or configuration, for example:

```bash
dotnet build HELIOS.Platform.csproj -p:EnableWindowsTargeting=true
```

That override should stay out of the project file so local Windows builds and the Windows-only shell workflow remain the source of truth for desktop shell validation.
