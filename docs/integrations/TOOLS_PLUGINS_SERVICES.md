# HELIOS tools, plugins, and services

## Purpose

This layer turns the merged communication contract into executable, reviewable integration surfaces without granting agents direct administrative power.

## Components

### C# integration broker

`src/services/HELIOS.IntegrationBroker` is the authoritative policy and request boundary. It validates normalized events, publishes catalogs, previews policy, and records pending actions. The first version does not execute third-party writes.

### OpenAI MCP plugin

`plugins/openai/helios-mcp` exposes a small MCP surface over Streamable HTTP and stdio. It calls the broker rather than GitHub, Slack, Linear, Azure, or Microsoft Graph directly. This keeps authentication, policy, and audit behavior centralized.

### Copilot Studio connector

`plugins/copilot-studio/helios-openapi.yaml` exposes the same bounded broker operations for Copilot Studio. Tenant registration and admin consent remain separate authorization steps.

### Catalogs

- `tool-catalog.json` defines impact annotations, environments, input schemas, and approvals.
- `service-catalog.json` defines ownership, status, authentication, and secret sources.
- `plugin-catalog.json` maps AI and collaboration surfaces to the authoritative broker.

## Execution model

```text
ChatGPT / Codex / GitHub Copilot / Microsoft Copilot
                    |
                    v
        MCP or approved OpenAPI connector
                    |
                    v
          HELIOS Integration Broker
          | validate | preview | record
                    |
                    v
       protected connector workers/adapters
                    |
      GitHub | Azure | Slack | Linear | Graph
```

## Guardrails

1. No unrestricted command execution is exposed.
2. Read-only tools are labeled and separated from writes.
3. Write tools create pending requests; adapters execute only after approval.
4. Destructive tools are rejected by broker v1.
5. Production writes require dedicated production-approved definitions and protected environments.
6. Credentials live in local secure storage or Azure Key Vault, never in catalog files.
7. All operations carry correlation IDs and can feed deployment evidence.

## Activation sequence

1. Use the unified contract merged in PR #180.
2. Merge the clean broker implementation PR.
3. Run the dedicated workflow and broker smoke tests.
4. Configure the development broker token through a secure store.
5. Deploy the broker and MCP service through Azure OIDC and Bicep what-if.
6. Register Copilot Studio and OpenAI clients against the approved endpoint.
7. Enable Slack/Linear adapters only after Key Vault-backed credentials and idempotency tests pass.
