# HELIOS OpenAI MCP server

This MCP server gives ChatGPT, Codex, and other approved MCP clients a bounded HELIOS tool surface. It delegates policy and audit behavior to `HELIOS.IntegrationBroker`; it never talks directly to Azure, GitHub, Slack, Linear, Microsoft Graph, Hermes, or AIHub.

## Exposed tools

- `helios_status`
- `helios_list_tools`
- `helios_search_events`
- `helios_preview_action`
- `helios_request_action`

`helios_request_action` records an approval-pending request only. It does not execute the connector action.

## Local HTTP mode

```powershell
$env:HELIOS_BROKER_URL = 'http://localhost:5080'
$env:HELIOS_BROKER_TOKEN = '<broker token from secure storage>'
$env:HELIOS_MCP_TOKEN = '<separate MCP client token from secure storage>'
npm install
npm start
```

The Streamable HTTP endpoint is `/mcp`; health is `/healthz`.

## Local stdio mode

```powershell
$env:HELIOS_BROKER_URL = 'http://localhost:5080'
$env:HELIOS_BROKER_TOKEN = '<broker token from secure storage>'
npm run stdio
```

Do not place tokens in MCP manifests, source files, workflow YAML, chat messages, or committed `.env` files.
