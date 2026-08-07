# Identity and token application setup

`config/identity-bindings.json` is the source contract. It stores names and
permissions, never credential values.

## Identity order

1. Developers authenticate interactively with `az login`, `azd auth login`, `gh auth login`, and `pac auth create`.
2. GitHub Actions uses Entra workload identity federation and `id-token: write`.
   Its credential matches the exact protected-environment subject; required
   reviewers and the exact deployment branch are verified before federation is
   configured. No client secret is created.
3. Azure Pipelines uses an Azure Resource Manager service connection with workload identity federation.
4. The implemented Container App uses a user-assigned managed identity. Functions,
   workers, and Foundry/Agent 365 identities are later administrator-controlled
   targets and do not exist in the current deployment definition.
5. Microsoft 365 user actions use OAuth on-behalf-of so the agent cannot exceed the person’s access.
6. The OpenAI provider can read `OPENAI_API_KEY` only in an explicitly configured
   deployment. The Azure deployment keeps this binding disabled by default and
   exposes an explicit `enableOpenAiApiKeyBinding` gate. Automation does not
   write secret values, so online OpenAI still fails closed until an
   administrator provisions `helios-openai-api-key`, enables the binding, and
   completes governed Key Vault RBAC readiness.

## Secret references

| Reference | Destination | Purpose |
| --- | --- | --- |
| `helios-openai-api-key` | Azure Key Vault secret reference wired to Container Apps `OPENAI_API_KEY` | OpenAI Responses provider; secret value must be provisioned by an administrator |
| `helios-github-webhook-secret` | Planned Azure Key Vault binding | GitHub signature verification; not created or granted by current automation |
| `helios-linear-webhook-secret` | Planned Azure Key Vault binding | Linear signature verification; not created or granted by current automation |
| `helios-slack-signing-secret` | Planned Azure Key Vault binding | Slack signature and replay verification; not created or granted by current automation |

Tenant IDs, subscription IDs, resource groups, environment IDs, team/channel IDs,
and SharePoint targets belong in GitHub environment variables, Azure App Configuration,
or private deployment parameters—not the public repository.

No automation creates tenant-wide consent, Conditional Access policy, production
credentials, or organization-wide Copilot publication without explicit approval.
The current Bicep creates the RBAC-enabled vault and supports reviewed Container
Apps secret references behind explicit parameter gating. An administrator must
choose the secret-ingestion path, complete Key Vault RBAC readiness for the
runtime identity, and populate secret values before live provider calls can
succeed. Plaintext
secrets must never be supplied as Bicep, CLI, GitHub, or checked-in environment-file
values.
