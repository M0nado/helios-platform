# Component Matrix

| Component | Language | Path | Build target | Test target | Status |
|---|---|---|---|---|---|
| WinUI control frontend | C# / WinUI 3 | `src/frontend/HELIOS.Control.WinUI/` | `HELIOS.Control.WinUI.csproj` | `tests/e2e/` | Scaffolded |
| Platform contracts | C# | `src/core/HELIOS.Platform.Contracts/` | Future `.csproj` | `tests/unit/` | Boundary scaffolded |
| Platform orchestration | C# | `src/core/HELIOS.Platform.Orchestration/` | Future `.csproj` | `tests/integration/` | Boundary scaffolded |
| Existing platform core | C# | `src/core/HELIOS.Platform/` | `HELIOS.Platform.csproj` | `src/tests/` | Existing |
| Native performance | C++ | `src/native/HELIOS.Performance/` | CMake | `tests/native/` | Scaffolded |
| Analytics prediction | F# | `src/analytics/HELIOS.Analytics.FSharp/` | `.fsproj` | `tests/analytics/` | Scaffolded |
| AIHub adapter | Python | `src/python/helios_aihub/` | `pyproject.toml` | `tests/python/` | Scaffolded |
| Hermes/XCore adapter | Python | `src/python/hermes_xcore/` | `pyproject.toml` | `tests/python/` | Scaffolded |
| Azure IaC | Bicep/PowerShell | `infra/` | Bicep validation | Azure what-if | Scaffolded |
| Security | C# / docs / scripts | `src/security/`, `docs/security/`, `scripts/security/` | Security workflow | `tests/security/` | Scaffolded |
