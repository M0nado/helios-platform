# HELIOS Deep GitHub, AI, Azure, and Workflow Automation Setup

This runbook wires the repository into a single governed automation lane for HELIOS Control and Hermes Fleet Production work. It is designed for mixed C#, WinUI 3, C++, F#, and Python contributions while keeping AI-assisted code review, Azure CLI readiness, and GitHub workflow governance visible in every pull request.

## What this adds

- **HELIOS Control lane** for C# orchestration, WinUI 3 front-end work, release dashboards, and GitHub project automation.
- **Hermes Fleet Production lane** for C++ performance back ends, F# math and analytics, Python AIHub integrations, and Hermes XCore specialist setup.
- **Branch inventory** so local and remote work can be merged deliberately instead of blindly combining every branch.
- **AI governance gates** for generated code, prompt/config changes, and automation changes.
- **Azure CLI readiness checks** for OIDC-based deployments without embedding secrets in source control.

## One-time local bootstrap

Run the bootstrap in dry-run mode first:

```powershell
pwsh ./scripts/setup/Initialize-HeliosDeepAutomation.ps1
```

After `gh auth login` and `az login`, apply repository variables and labels:

```powershell
pwsh ./scripts/setup/Initialize-HeliosDeepAutomation.ps1 -Apply
```

The script writes `build/automation/helios-deep-automation-manifest.json` with CLI readiness, branch inventory, integration lanes, required secrets, repository variables, and required checks.

## Required repository secrets and variables

Configure these in GitHub repository settings or through environment-level deployment protection:

| Name | Type | Purpose |
| --- | --- | --- |
| `AZURE_CLIENT_ID` | Secret | Azure federated identity client ID for `azure/login`. |
| `AZURE_TENANT_ID` | Secret | Tenant ID for Azure CLI and deployment workflow authentication. |
| `AZURE_SUBSCRIPTION_ID` | Secret | Subscription selected by automation jobs. |
| `HELIOS_AI_ROUTER_KEY` | Secret | AIHub routing key for guarded model calls. |
| `HERMES_FLEET_SIGNING_KEY` | Secret | Signing material for Hermes fleet artifacts. |
| `HELIOS_AUTOMATION_MODE` | Variable | Set to `deep` to enable the full workflow profile. |
| `HELIOS_AI_GOVERNANCE` | Variable | Set to `enabled` to keep generated code checks active. |
| `HELIOS_CONTROL_LANE` | Variable | Defaults to `helios-control`. |
| `HERMES_FLEET_LANE` | Variable | Defaults to `hermes-fleet-production`. |
| `AZURE_LOCATION` | Variable | Default Azure deployment region. |

## Branch integration plan

Do not merge every branch directly into production. Use this order instead:

1. Fetch and list all local and remote branches.
2. Generate the automation manifest to capture branch heads and timestamps.
3. Create an integration branch from the current production baseline.
4. Merge HELIOS Control work first and run C# and WinUI validation.
5. Merge Hermes Fleet work second and run C++, F#, Python, and AIHub validation.
6. Resolve conflicts with the component owner recorded in the manifest.
7. Open a pull request and require all deep automation workflow jobs before merge.

Recommended commands:

```bash
git fetch --all --prune
git switch -c integration/helios-control-hermes-fleet
git merge --no-ff origin/helios-control
git merge --no-ff origin/hermes-fleet-production
pwsh ./scripts/setup/Initialize-HeliosDeepAutomation.ps1
```

## Azure CLI setup

Use federated credentials for GitHub Actions. Locally, validate with:

```bash
az login
az account set --subscription "$AZURE_SUBSCRIPTION_ID"
az account show --query '{name:name,id:id,tenantId:tenantId}'
```

The workflow only checks readiness unless deployment credentials are present. Cloud deployment steps should remain in protected environments with required reviewers.

## AI and automation governance

Every pull request should include:

- The generated automation manifest as an artifact.
- A changed-file summary grouped by C#, C++, F#, Python, PowerShell, workflow, and docs.
- Security scan output for scripts and AI configuration.
- Reviewer labels for `helios-control`, `hermes-fleet`, `ai-governance`, and `azure-automation` when matching files change.

AI-generated or AI-modified files must include human review for security, performance, licensing, and data handling before merge.
