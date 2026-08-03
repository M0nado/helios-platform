# HELIOS App Automation Readiness

`./helios.sh apps` creates a safe readiness report for the app layer around HELIOS: GitHub CLI, GitHub Actions token readiness, GitHub MCP host readiness, Azure CLI/Cloud Shell, Microsoft 365 Copilot, ChatGPT/OpenAI, Claude, Slack, Linear, and the local HELIOS GUI.

## What it does

- Checks whether official CLIs such as `gh` and `az` are installed/authenticated.
- Checks whether expected environment variables are present without printing values.
- Generates `reports/integrations/app-automation-readiness.json` and `.md` as ignored local/CI artifacts.
- Lists safe setup commands for each app so the local GUI can show the next action.

## What it intentionally does not do

- It does not read browser cookies, OS keychains, saved passwords, or authenticator-app private data.
- It does not commit API keys, Slack tokens, Linear keys, GitHub tokens, or Copilot/ChatGPT browser sessions.
- It does not force org/enterprise/tenant changes; apply-mode remains separate and review-gated.

## Browser and Copilot notes

Internet Explorer is retired. Use Microsoft Edge for modern Microsoft 365 Copilot and ChatGPT/Claude browser workflows. If a legacy site requires IE compatibility, configure Edge IE Mode by enterprise policy outside this repository, then use the HELIOS report as documentation/readiness only.

## Recommended setup order

```bash
./helios.sh setup
gh auth login
az login
./helios.sh apps
./helios.sh all
./helios.sh dashboard
```

For hosted automation, put values in GitHub Actions secrets, Codespaces secrets, or Azure Key Vault using the names in `config/secrets-map.example.json`.
