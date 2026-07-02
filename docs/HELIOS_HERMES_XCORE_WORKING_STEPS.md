# HELIOS Hermes XCore: best steps to get everything working

This checklist is the recommended path for bringing the HELIOS platform, Hermes XCore integration, Azure CLI tooling, and mixed-language components online without mixing unfinished branch work into the runnable baseline.

## 1. Stabilize Git before merging branches

1. Confirm the current branch and working tree are clean:
   ```bash
   git status --short --branch
   ```
2. Add the upstream repository if it is missing, then fetch every branch:
   ```bash
   git remote add upstream https://github.com/M0nado/helios-platform.git
   git fetch --all --prune
   ```
3. Create a single integration branch for the combined HELIOS/Hermes work:
   ```bash
   git switch -c integration/helios-hermes-xcore
   ```
4. Merge candidate branches one at a time. Resolve and test after each merge instead of batch-merging unrelated work:
   ```bash
   git merge --no-ff upstream/helios-control
   git merge --no-ff upstream/hermes-fleet-production
   ```
5. When a branch does not exist locally or remotely, record that fact in the merge notes and skip it rather than guessing at missing code.

## 2. Bring up the required developer toolchain

Install or verify these tools before restoring packages:

| Area | Required tooling | Purpose |
| --- | --- | --- |
| C# / Win UI / WPF shell | .NET 8 SDK, Visual Studio 2022 or Build Tools | Core platform, Windows UI, tray, installer, tests |
| C++ performance backend | CMake 3.20+, MSVC Build Tools or Ninja/Clang | Native performance modules under `src/native` |
| F# analytics | .NET 8 SDK with F# support | Prediction and analytics modules under `src/analytics` |
| Python AIHub integration | Python 3.11+, virtual environment tooling | AIHub automation, scripts, local integration probes |
| Azure integration | Azure CLI, PowerShell 7.4+ | Hermes XCore cloud resource setup and deployment defaults |

## 3. Configure Azure CLI for Hermes XCore

Use the repository bootstrap script instead of manually installing extensions. It configures Azure CLI defaults, installs the required extensions, optionally creates the integration resource group, and exports HELIOS session variables.

```powershell
./tools/azure/setup-helios-azure-cli.ps1 `
  -SubscriptionId '<subscription-id>' `
  -TenantId '<tenant-id>' `
  -Location 'eastus2' `
  -ResourceGroupName 'helios-hermes-xcore-rg' `
  -EnvironmentName 'integration' `
  -CreateResourceGroup
```

For CI or already-authenticated shells, use `-SkipLogin` and provide `AZURE_SUBSCRIPTION_ID`, `AZURE_TENANT_ID`, `HELIOS_RESOURCE_GROUP`, and `HELIOS_AZURE_LOCATION` through the environment.

## 4. Restore and build by layer

Run the stack from low-level dependencies toward the app shell so failures stay isolated:

```bash
# C# / Windows platform projects
dotnet restore HELIOS.Platform.csproj
dotnet build HELIOS.Platform.csproj --configuration Release --no-restore

# F# analytics
dotnet restore src/analytics/HELIOS.Analytics.FSharp/HELIOS.Analytics.FSharp.fsproj
dotnet build src/analytics/HELIOS.Analytics.FSharp/HELIOS.Analytics.FSharp.fsproj --configuration Release --no-restore

# Native C++ performance backend
cmake -S src/native/HELIOS.Native.Performance -B build/native-performance
cmake --build build/native-performance --config Release
```

If a layer fails, fix that layer before moving upward. Do not start UI, installer, or Azure deployment work while native or analytics builds are red.

## 5. Validate security, performance, and AIHub paths

1. Run .NET tests first:
   ```bash
   dotnet test src/tests/HELIOS.Platform.Tests.csproj --configuration Release
   dotnet test tests/analytics/HELIOS.Analytics.FSharp.Tests/HELIOS.Analytics.FSharp.Tests.fsproj --configuration Release
   ```
2. Run JavaScript adapter tests only after Node dependencies are present:
   ```bash
   npm test
   ```
3. Run PowerShell security and installer validation scripts from an elevated Windows PowerShell 7 session when they touch drivers, services, or OS policy.
4. Keep AIHub secrets out of the repository. Use environment variables or Azure Key Vault references for provider keys and endpoints.

## 6. Optimize in the right order

1. Security baseline first: credential handling, least privilege, signed scripts, audit logging, and dependency updates.
2. C++ backend second: profile hotspots, add benchmarks, and avoid changing public interop contracts until tests exist.
3. F# analytics third: add deterministic tests for prediction models before tuning parallelism.
4. C# frontend fourth: improve responsiveness with async boundaries, cancellation, and view-model isolation.
5. Python AIHub last: add retries, rate limits, and provider abstraction after core contracts are stable.

## 7. Final integration gate

Before pushing or opening the final PR, run this minimum gate and paste the results into the PR body:

```bash
git diff --check
dotnet restore HELIOS.Platform.csproj
dotnet build HELIOS.Platform.csproj --configuration Release --no-restore
dotnet test src/tests/HELIOS.Platform.Tests.csproj --configuration Release
```

Only push the integration branch after the working tree is clean and the required checks either pass or have documented environment limitations.

## 8. Single-shell automation order

The HELIOS command shell is the preferred automation surface once the repository is ready for integrated execution. Use it in this order so local runs, agents, and CI all share the same orchestration path:

1. `./tools/helios.ps1 help` confirms the single shell entry point.
2. `./tools/helios.ps1 status` checks tools, Git, expected files, Azure variables, and generated-report readiness.
3. `./tools/helios.ps1 azure setup` bootstraps Azure CLI through the existing Hermes XCore setup script; `./tools/helios.ps1 azure verify` validates CLI login state, required extensions, and HELIOS environment variables.
4. `./tools/helios.ps1 branches fetch`, `./tools/helios.ps1 branches list`, and `./tools/helios.ps1 branches integrate` fetch, inspect, and merge `helios-control` before `hermes-fleet-production`.
5. `./tools/helios.ps1 agents list`, `./tools/helios.ps1 agents validate`, and `./tools/helios.ps1 agents run <name>` drive the agent registry in `config/helios-agents.json`.
6. `./tools/helios.ps1 build contracts|csharp|fsharp|native|frontend|all` runs layered builds from stable contracts toward user-facing shells.
7. `./tools/helios.ps1 test csharp|security|fsharp|native|python-aihub|all` runs domain tests and readiness checks.
8. `./tools/helios.ps1 reports latest` prints the newest generated Markdown report from `reports/generated/helios-shell`.
9. `./tools/helios.ps1 gate final` runs the final integrated quality gate.
10. `.github/workflows/helios-shell.yml` reuses the same shell commands in CI so local automation and hosted validation stay aligned.

## 9. Mass GitHub integration automation

For fully automated GitHub runner integration, use the mass integration scorer and orchestrator. It fetches branches and submodules, scores HELIOS/Hermes candidates, prioritizes `helios-control` and `hermes-fleet-production`, creates an integration branch, opens a pull request, and can request GitHub auto-merge when the runner token has permission.

Recommended automation order:

1. `./tools/helios.ps1 github mass-score` writes `reports/mass-integration/mass-integration-score.json` and `.md`.
2. `./tools/helios.ps1 github mass-branch --apply` creates or resets the configured integration branch and merges scored candidates in order.
3. `./tools/helios.ps1 github mass-pr --apply` pushes the integration branch and opens the GitHub pull request.
4. `./tools/helios.ps1 github mass-merge --apply` requests GitHub auto-merge for the integration pull request.
5. `./tools/helios.ps1 github mass-all --apply` runs score, branch, pull request, and auto-merge as one runner command.

The mass integration defaults live in `config/helios-mass-integration.json`. The GitHub runner workflow is `.github/workflows/helios-mass-integration.yml`; run it with `apply=true` only when the repository token is allowed to push branches, open pull requests, and enable auto-merge.

## 10. Deep capability setup order

Use `./tools/helios.ps1 setup verify` to generate a readiness map for every deep integration capability before running mutating automation. Use `./tools/helios.ps1 setup all` only when the shell has the required local tools and non-secret environment variables for the declared setup commands.

The deep capability registry lives in `config/helios-capabilities.json` and orders setup across GitHub CLI automation, Azure CLI, Microsoft 365/Copilot readiness, Cloud Shell, OpenAI/Codex AIHub readiness, MCP server bridge checks, Hermes XCore, agent skills, and mass integration. Reports are written to `reports/capabilities/capability-readiness.json` and `.md`.

## 11. GitHub repository setup completion

Use `./tools/helios.ps1 github repo-verify` to verify labels, workflows, token environment, and repository setup readiness. Use `./tools/helios.ps1 github repo-setup --apply` or the `HELIOS Repository Setup` workflow with `apply=true` when the automation token has repository administration permission. This applies the GitHub-side setup needed by the no-review automation path: auto-merge-capable repository settings, integration labels, optional branch protection, and setup reports under `reports/github-setup`.

## 12. Deep auto-upgrade runners and GUI

Use `./tools/helios.ps1 upgrade plan` to generate the full upgrade map for automation runners, auto-fix checks, mass merge/PR, local/cloud setup, provider mesh, Hermes XCore GUI, and learning/token optimization loops. Use `./tools/helios.ps1 upgrade gui` to render a local/hybrid static dashboard at `reports/auto-upgrade/gui/index.html`. The scheduled/manual GitHub runner is `.github/workflows/helios-auto-upgrade.yml`, and the phase registry is `config/helios-auto-upgrade.json`.

## 13. Finish setup command

Use `./tools/helios.ps1 finish verify` for the complete final non-mutating setup verification stack: GitHub repository setup verify, deep capability verify, mass integration score, auto-upgrade GUI generation, JSON validation, Python compilation, and whitespace validation. Use `./tools/helios.ps1 finish apply` only when the runner has the required GitHub/Azure/provider permissions and you want the full mutating setup chain.

## 14. Super automation backlog

Use `./tools/helios.ps1 ideas super` to rank the next highest-impact additions: event-driven webhooks, agent GUI controls, multi-LLM routing/token optimization, safety vault/policy engine, fleet learning store, autofix patch pipeline, branch/submodule fusion, runner fleet bootstrap, MCP/plugin marketplace, and Hermes XCore digital twin simulation. The backlog is declared in `config/helios-super-automation-backlog.json` and rendered under `reports/super-automation`.
