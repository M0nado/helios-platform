# Identity and token application setup

`config/identity-bindings.json` is the source contract. It stores names and
permissions, never credential values.

## Identity order

1. Developers authenticate interactively with `az login`, `azd auth login`, `gh auth login`, and `pac auth create`.
2. GitHub Actions uses Entra workload identity federation and `id-token: write`.
3. Azure Pipelines uses an Azure Resource Manager service connection with workload identity federation.
4. Container Apps, Functions, workers, and Foundry agents use separate managed or Agent 365 identities.
5. Microsoft 365 user actions use OAuth on-behalf-of so the agent cannot exceed the person’s access.
6. The OpenAI provider reads `OPENAI_API_KEY` locally and a Key Vault reference in Azure; it is never copied into App Configuration, logs, messages, or model memory.

## Secret references

| Reference | Destination | Purpose |
| --- | --- | --- |
| `helios-openai-api-key` | Azure Key Vault | OpenAI Responses provider |
| `helios-github-webhook-secret` | Azure Key Vault | GitHub signature verification |
| `helios-linear-webhook-secret` | Azure Key Vault | Linear signature verification |
| `helios-slack-signing-secret` | Azure Key Vault | Slack signature and replay verification |

Tenant IDs, subscription IDs, resource groups, environment IDs, team/channel IDs,
and SharePoint targets belong in GitHub environment variables, Azure App Configuration,
or private deployment parameters—not the public repository.

No automation creates tenant-wide consent, Conditional Access policy, production
credentials, or organization-wide Copilot publication without explicit approval.
