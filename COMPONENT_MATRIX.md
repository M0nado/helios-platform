# HELIOS Component Matrix

| Component area | Primary source | Language/runtime | CI lane |
| --- | --- | --- | --- |
| Platform core | helios-platform | C#/.NET 8 | .NET build/test |
| Front end/control | helios-control | C#/WinUI 3/XAML | Windows UI build |
| Native performance | helios-monado-blade | C++ | Native build/security scan |
| Fleet analytics | hermes-fleet-production | F# | .NET/F# analytics tests |
| AI Hub | helios-ai-hub | Python | Python lint/test |
| Automation/deploy | helios-build-agents | GitHub Actions/Azure CLI/PowerShell | Workflow + Azure smoke checks |
