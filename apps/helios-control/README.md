# HELIOS Control App

One Streamable HTTP MCP server for ChatGPT Apps and Microsoft Copilot Studio, with a Monado control-center widget.

## Safety state

This first slice is read-only. It reports setup state, searches/fetches canonical integration records, validates the expected Azure workload-identity subject, and renders the dashboard. It cannot deploy, merge, message, or mutate external systems.

The canonical project is Linear **Helios Integration Fabric** (`JOH-35`). Slack engineering authority is `#helios-control-plane`; `#all-helios` is announcements only.

## Run locally

```bash
npm ci
npm run check
npm run build
npm start
curl http://127.0.0.1:8000/health
```

The MCP endpoint is `http://127.0.0.1:8000/mcp`.

## Easy Azure CLI plugin

The repository plugin at `plugins/helios-azure-cli` provides one guarded wizard for local Azure setup:

```bash
python3 plugins/helios-azure-cli/scripts/helios_azure.py doctor
python3 plugins/helios-azure-cli/scripts/helios_azure.py plan
python3 plugins/helios-azure-cli/scripts/helios_azure.py what-if --template path/to/main.bicep
```

Its OIDC and deployment commands require exact confirmation phrases and refuse to mutate Azure until PRs #174 and #176 are merged. It does not create or store client secrets.

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
