# HELIOS Control App

One Streamable HTTP MCP server for ChatGPT Apps and Microsoft Copilot Studio, with a Monado control-center widget.

## Safety state

This first slice is read-only. It reports setup state, searches/fetches canonical integration records, validates the expected Azure workload-identity subject, and renders the dashboard. It cannot deploy, merge, message, or mutate external systems.

## Run locally

```bash
npm ci
npm run check
npm run build
npm start
curl http://127.0.0.1:8000/health
```

The MCP endpoint is `http://127.0.0.1:8000/mcp`.

## Connect to ChatGPT

1. Host the server on stable HTTPS or expose port 8000 through an HTTPS development tunnel.
2. Enable Developer Mode in ChatGPT app settings.
3. Create an internal app using `https://<host>/mcp`.
4. Refresh the app after tool or metadata changes.

## Connect to Microsoft Copilot Studio

Preferred: Tools → Add a tool → New tool → Model Context Protocol, then enter the same `https://<host>/mcp` URL and configure OAuth 2.0.

Fallback: import `copilot/helios-mcp.openapi.yaml` as a Power Apps custom connector after replacing its host.

## Azure identity

Use GitHub workload identity federation with this exact subject:

```text
repo:M0nado/helios-platform:environment:azure-dev
```

The Windows/WPF and PowerShell repairs are present on canonical PR #174, but that PR still needs review, narrow tests, and two legacy red workflows cleared. Do not set `AZURE_DEPLOY_ENABLED=true` until #174 is merged, Azure PR #176 is rebased and hardened, and the protected environment has reviewers plus branch restrictions.
