# Build Variants

| Variant | Scope | Primary workflow | Notes |
|---|---|---|---|
| Local .NET development | C# projects | `ci-dotnet.yml` | Restore, build, and test included solution projects. |
| Windows desktop | WinUI 3 frontend | `ci-dotnet.yml` | Requires Windows runner and Windows App SDK dependencies. |
| Native performance | C++ backend | `ci-native.yml` | CMake configure/build/test. |
| Python adapters | AIHub/Hermes/XCore packages | `ci-python.yml` | Package install and pytest. |
| Azure validation | Bicep/IaC | `azure-what-if.yml` | Validate and what-if before deployment. |
| Security audit | Secrets/dependencies | `security-audit.yml` | Secret scan and dependency audit. |
| Docs/wiki | Documentation | `ci-docs.yml`, `wiki-sync.yml` | Link validation and generated wiki artifacts. |
