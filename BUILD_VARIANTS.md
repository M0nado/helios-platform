# Build Variants

| Variant | Purpose | Projects |
| --- | --- | --- |
| Minimal | Fast core validation | `src/core/HELIOS.Platform.Minimal/HELIOS.Platform.csproj` |
| Core | Main platform services | `src/core/HELIOS.Platform/HELIOS.Platform.csproj` |
| Windows | Tray, installer, shell extension, and WPF/WinUI-adjacent surfaces | `HELIOS.Platform.sln` Windows projects |
| Security | Security validator and security tests | `src/Security/SecurityValidator.csproj`, `tests/SecurityValidationTests.csproj` |
| Consolidated | All fetched external modules after readiness checks pass | `modules/**` |
