# AI Provider Profiles

HELIOS tracks AI readiness as safe profile metadata. It does not store browser sessions, personal cookies, or provider secret values in git.

## Covered providers

| Provider | Profile | Secret / auth hint | Notes |
| --- | --- | --- | --- |
| Microsoft 365 Copilot | Standard / Work | `M365_TENANT_ID`, `M365_CLIENT_ID` | Tenant/work readiness via Microsoft/Azure auth. |
| Microsoft 365 Copilot | Pro / Consumer | `M365_COPILOT_PRO_ACCOUNT` | Account hint only; do not store browser session cookies. |
| ChatGPT | Free / Plus / Pro / Team UI | `CHATGPT_WORKSPACE_ID` | Workspace/account hint only; API automation should use OpenAI API keys instead. |
| OpenAI | Responses API | `OPENAI_API_KEY`, optional `OPENAI_ORG_ID`, `OPENAI_PROJECT_ID` | Safe opt-in enrichment/provider routing. |
| OpenAI | Agents / Tools API readiness | `OPENAI_API_KEY` | Future provider-router lane. |
| Azure OpenAI | Azure OpenAI API | `AZURE_OPENAI_ENDPOINT`, `AZURE_OPENAI_API_KEY` | Prefer Key Vault-backed sync. |
| Anthropic | Claude Messages API | `ANTHROPIC_API_KEY` | Safe opt-in provider readiness. |
| Codex | Developer agent | local Codex auth | Generates task packets; does not require committing generated packets. |

## Commands

```bash
./helios.sh profiles
./helios.sh readiness
python3 scripts/integrations/cross_access_profiles.py
```

## Safety rules

- Never commit provider API keys.
- Never commit ChatGPT or Copilot browser cookies/sessions.
- Use `config/secrets-map.example.json` for names only.
- Use Azure Key Vault, GitHub Secrets, or Codespaces Secrets for real values.
- Keep apply mode disabled unless explicitly reviewed later.
