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
