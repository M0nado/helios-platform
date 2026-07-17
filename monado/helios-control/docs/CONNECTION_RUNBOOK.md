# Connection runbook

This is the activation runbook. The connector implements signed ingress,
local/cloud MCP inventory, Cosmos-leased Edge control runs, and an inline signed
relay dispatcher. Service Bus scale-out, provider-native workers, WAF/APIM policy, and a
dead-letter operator remain later milestones.

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

## Outbound control-run relays

Each enabled destination requires an HTTPS callback and a unique HMAC secret of
at least 32 bytes. Inject them as Container Apps environment values through
Key Vault references; never place them in Bicep parameter files or workflow
logs. Names follow `HELIOS_CONNECTOR_<NAME>_URL` and
`HELIOS_CONNECTOR_<NAME>_HMAC_SECRET`, where `<NAME>` is `GITHUB`, `LINEAR`,
`SLACK`, `SHAREPOINT`, `TEAMS`, or `COPILOT`. Each binding also requires an exact
comma-separated `HELIOS_CONNECTOR_<NAME>_ALLOWED_HOSTS`; optional
`HELIOS_CONNECTOR_<NAME>_HMAC_KEY_ID` defaults to `v1`.

Keep `HELIOS_CONNECTOR_DELIVERY_MODE=dry-run` until the receiving relay verifies
`X-Helios-Signature`, enforces freshness using the signed body and
`X-Helios-Timestamp`, selects the key through `X-Helios-Key-Id`, persists
`X-Helios-Idempotency-Key` before side effects, and returns success only after
its own provider receipt is durable. Promote one
destination at a time. The Edge connector-status endpoint reports binding and
mode without disclosing endpoints or secrets.

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
