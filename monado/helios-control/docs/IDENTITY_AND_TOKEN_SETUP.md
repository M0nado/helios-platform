# Identity and token application setup

`config/identity-bindings.json` is the source contract. It stores names and
permissions, never credential values.

## Identity order

1. Developers authenticate interactively with `az login`, `azd auth login`, `gh auth login`, and `pac auth create`.
2. GitHub Actions uses Entra workload identity federation and `id-token: write`.
3. Azure Pipelines uses an Azure Resource Manager service connection with workload identity federation.
4. The implemented Container App uses a user-assigned managed identity. Functions,
   workers, and Foundry/Agent 365 identities are later administrator-controlled
   targets and do not exist in the current deployment definition.
5. Microsoft 365 user actions use OAuth on-behalf-of so the agent cannot exceed the person’s access.
6. The OpenAI provider can read `OPENAI_API_KEY` only in an explicitly configured
   development process. The Azure deployment does not set that variable, grant
   Key Vault secret access, or create a Key Vault reference, so online OpenAI
   fails closed until an administrator implements and reviews that binding.

## Secret references

| Reference | Destination | Purpose |
| --- | --- | --- |
| `helios-openai-api-key` | Planned Azure Key Vault binding | OpenAI Responses provider; not created or granted by current automation |
| `helios-github-webhook-secret` | Planned Azure Key Vault binding | GitHub signature verification; not created or granted by current automation |
| `helios-linear-webhook-secret` | Planned Azure Key Vault binding | Linear signature verification; not created or granted by current automation |
| `helios-slack-signing-secret` | Planned Azure Key Vault binding | Slack signature and replay verification; not created or granted by current automation |

Tenant IDs, subscription IDs, resource groups, environment IDs, team/channel IDs,
and SharePoint targets belong in GitHub environment variables, Azure App Configuration,
or private deployment parameters—not the public repository.

No automation creates tenant-wide consent, Conditional Access policy, production
credentials, or organization-wide Copilot publication without explicit approval.
The current Bicep creates only the empty RBAC-enabled vault. An administrator must
choose the secret-ingestion path, grant the workload identity the narrow Key Vault
data-plane role, approve the Container Apps secret reference, and run a protected
what-if/deployment. Plaintext secrets must never be supplied as Bicep, CLI, GitHub,
or checked-in environment-file values.
