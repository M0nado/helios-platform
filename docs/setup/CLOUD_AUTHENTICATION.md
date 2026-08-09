# GitHub and Azure authentication

Install pinned tools with `scripts/setup/bootstrap-local-tools.sh`, then run:

```bash
scripts/setup/configure-cloud-auth.sh --interactive
scripts/setup/configure-cloud-auth.sh --status
```

The interactive mode delegates credentials to GitHub CLI and Azure CLI credential stores. It
never accepts a token argument, prints a token, or writes credentials into this repository.
Use `HELIOS_AZURE_SUBSCRIPTION` or `--subscription` to select an approved subscription.

GitHub Actions must not run interactive login. Azure jobs use environment-scoped variables,
`permissions: { contents: read, id-token: write }`, a protected environment, and a SHA-pinned
`azure/login` action. They may run `configure-cloud-auth.sh --ci-check` before OIDC login to
fail clearly when identity inputs are incomplete. Ordinary build jobs retain `contents: read`.

Cross-repository writes require a narrowly installed GitHub App. Microsoft Graph, SharePoint,
Purview, Fabric, and Copilot Studio use capability-specific broker identities and reviewed
permissions; they do not reuse a developer login or the workflow token.

## Unified local entry point

Use `scripts/setup/helios-auth --status` for a sanitized readiness report or
`scripts/setup/helios-auth --interactive` to start provider-owned authorization. Windows
developers can run `pwsh -File scripts/setup/Configure-HeliosCloudAuth.ps1 -Mode Interactive`.
Neither helper accepts, creates, prints, or persists access tokens.

Azure DevOps configuration is available through:

```bash
scripts/setup/helios-auth azure-devops configure \
  --organization https://dev.azure.com/ORGANIZATION \
  --project PROJECT
scripts/setup/helios-auth azure-devops status
```

An Azure DevOps administrator must approve a workload-identity federation service connection
and expose only its name as `HELIOS_AZDO_SERVICE_CONNECTION`; PATs are not pipeline inputs.
GitHub Actions uses `.github/workflows/reusable-azure-auth.yml`, Azure Pipelines uses the
federated connection in `azure-pipelines.yml`, and Azure workloads use managed identity.
Key Vault is reserved for durable provider material that cannot use federation; short-lived
GitHub and Azure access tokens are never copied into Key Vault.
