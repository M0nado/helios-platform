# Connection runbook

This is the target activation runbook. The current connector implements signed
ingress plus local/cloud MCP inventory only. It has no Service Bus route,
connector worker, outbound SaaS writer, durable idempotency store, WAF/APIM
policy, or dead-letter queue.

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
and `/copilot`. The current Container Apps slice terminates TLS; supported
providers are HMAC-verified in the application and unsupported providers fail
closed. WAF/APIM and private-backend ingress remain a later gate.

## Local MCP

Run the API on loopback only for explicit development and configure Codex with
the HTTP MCP adapter. The compatibility endpoint currently exposes only
`hermes_get_status` and `hermes_list_routes`. Replay and notification tools do
not exist and must not be documented as available before durable routing lands.

## Verification

- Invalid signatures return 401 and create no event.
- Duplicate delivery IDs create one target operation.
- Disabled routes create an auditable skip.
- Secrets never appear in logs.
- After Service Bus and a governed replay tool are implemented, a dead-letter
  event can be inspected and replayed after correction. This is not a current
  verification criterion.
