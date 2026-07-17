# Connection runbook

## Safe order

1. Resolve the GitHub repository, SharePoint library, Microsoft Foundry project, and Entra Agent Registry.
2. Register a GitHub App with repository metadata, checks, issues, pull requests,
   and webhook read access; grant contents write only if release automation needs it.
3. Create Slack and Teams app identities. Subscribe only to required events.
4. Create a Linear OAuth application or scoped personal key for the `John` team.
5. Register a Microsoft Entra application for SharePoint/Teams; prefer selected-site
   permissions over tenant-wide Graph access.
6. Put credentials in Key Vault and deploy the Bicep stack.
7. Register webhook URLs and signing secrets.
8. Run contract tests in dry-run, then enable one route at a time.

## Webhook endpoints

`POST /webhooks/github`, `/linear`, `/slack`, `/teams`, `/sharepoint`, `/foundry`,
and `/copilot`. Public ingress should terminate TLS and pass through Azure WAF.

## Local MCP

Run the API on loopback and configure Codex with a stdio or HTTP MCP adapter.
Expose narrow tools (`get_status`, `list_routes`, `replay_event`,
`draft_notification`) before enabling any live mutation tool.

## Verification

- Invalid signatures return 401 and create no event.
- Duplicate delivery IDs create one target operation.
- Disabled routes create an auditable skip.
- Secrets never appear in logs.
- A dead-letter event can be inspected and replayed after correction.
