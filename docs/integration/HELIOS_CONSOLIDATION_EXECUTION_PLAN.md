# HELIOS Consolidation Execution Plan

This generated plan orders branch/repository consolidation by the source manifest.
Review conflicts before running commands with `--apply`.

## Ordered sources

0. **helios-platform** — mode `history`, branch `work`, path `.`, areas `integration_baseline, documentation`
1. **helios-control** — mode `subtree`, branch `main`, path `modules/helios-control`, areas `csharp_winui, control_plane, user_experience`
2. **hermes-fleet-production** — mode `subtree`, branch `main`, path `modules/hermes-fleet-production`, areas `fsharp_analytics, fleet_production, prediction_models, parallel_analytics`
3. **helios-monado-blade** — mode `submodule`, branch `main`, path `modules/helios-monado-blade`, areas `cpp_backend, performance, native_security, hermes_xcore`
4. **helios-security-setup** — mode `submodule`, branch `main`, path `modules/helios-security-setup`, areas `security, hardening, compliance`
5. **helios-ai-hub** — mode `submodule`, branch `main`, path `modules/helios-ai-hub`, areas `python_ai_hub, model_orchestration, ai_integration`
6. **helios-dev-ai-hub** — mode `submodule`, branch `main`, path `modules/helios-dev-ai-hub`, areas `python_ai_hub, development_ai_workflows, experimentation`
7. **helios-build-agents** — mode `submodule`, branch `main`, path `modules/helios-build-agents`, areas `azure_github_automation, azure_cli, github_actions, cicd`
8. **helios-gui-framework** — mode `submodule`, branch `main`, path `modules/helios-gui-framework`, areas `csharp_winui, gui_framework, shared_ui_components`
9. **helios-software-stack** — mode `submodule`, branch `main`, path `modules/helios-software-stack`, areas `toolchain, installer, azure_cli, developer_setup`

## Optimized build and test gates

- `dotnet restore HELIOS.Platform.csproj -p:Configuration=Release`
- `dotnet build HELIOS.Platform.csproj --no-restore -m --configuration Release`
- `dotnet test HELIOS.Platform.csproj --no-build --configuration Release --verbosity normal`
- `python3 -m unittest tests.automation.test_helios_consolidation -v`

## Safety gates

- Keep `helios-control` authoritative for C#/WinUI 3 shell and control-plane conflicts.
- Keep `hermes-fleet-production` authoritative for F# prediction, fleet analytics, and parallel math conflicts.
- Keep `helios-monado-blade` authoritative for C/C++ performance backend and native security conflicts.
- Keep `helios-ai-hub` authoritative for Python AI Hub integration conflicts.
- Keep `helios-build-agents` authoritative for Azure CLI, GitHub Actions, CI/CD, and cloud automation conflicts.
