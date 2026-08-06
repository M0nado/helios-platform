# HELIOS Deep GitHub, AI, Azure, and Workflow Automation

This runbook connects the existing HELIOS automation assets into one safe control plane for GitHub, Azure CLI, AI hub routing, branch consolidation, and multi-language CI.

## What is integrated

- **GitHub setup:** inventories workflows, branch state, remotes, PR gates, and Actions readiness.
- **AI hub setup:** validates committed AI configuration files and required secret environment variable names without printing secret values.
- **Azure CLI setup:** checks whether `az` is installed and whether a subscription context is authenticated.
- **Workflow automation:** generates machine-readable JSON and operator-readable Markdown reports for PRs, scheduled audits, and local runs.
- **Multi-language awareness:** counts C#, XAML/WinUI/WPF, C++, F#, Python, PowerShell, JavaScript/TypeScript, and GitHub Actions assets.
- **Branch consolidation guardrails:** detects whether focus branches/remotes such as `helios-control` and `hermes-fleet-production` are present before any merge plan proceeds.

## Local usage

```bash
python3 scripts/automation/deep_automation_orchestrator.py --mode full
```

Outputs are written to:

- `artifacts/automation/deep-automation-report.json`
- `artifacts/automation/deep-automation-report.md`

Use `--mode inventory`, `--mode ci`, `--mode ai`, or `--mode azure` to narrow the report scope for operator workflows. The script intentionally does not mutate branches, remotes, Azure resources, GitHub settings, or AI configuration.

## GitHub Actions usage

The `Deep AI Automation Orchestrator` workflow runs this same script on pull requests, pushes to core automation paths, nightly schedules, and manual dispatches. It uploads the generated reports as workflow artifacts and includes optional Azure login validation when the required secrets are present.

Recommended repository secrets for full cloud validation:

| Secret | Purpose |
| --- | --- |
| `AZURE_CLIENT_ID` | Federated identity or service principal client ID. |
| `AZURE_TENANT_ID` | Azure tenant ID. |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription used by HELIOS automation. |
| `OPENAI_API_KEY` / provider-specific keys | AI hub provider credentials consumed only by downstream approved workflows. |

## Branch consolidation workflow

1. Fetch all remotes with full history.
2. Confirm that `helios-control` and `hermes-fleet-production` exist locally, as remote branches, or as external remotes/submodules. Use `git for-each-ref --format='%(refname:short) %(objectname:short)' refs/heads refs/remotes` for portable local/remote ref inspection instead of `git show-ref --remotes`, which is not available in every Git build.
3. Run the deep automation orchestrator and inspect the branch section of the report.
4. Merge through pull requests only; require CI, security scans, and environment approvals.
5. Use GitHub environments for Azure deployment and AI-driven code changes so privileged automation remains gated.

## Safety boundaries

- Reports never include secret values.
- Azure checks are read-only (`az account show`) unless downstream deployment workflows are explicitly invoked.
- The orchestrator does not auto-merge branches because branch consolidation should remain auditable and protected by PR review.
