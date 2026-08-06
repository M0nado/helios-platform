# Governed Slack and ZNC setup

Slack is the rapid operational notification surface. ZNC is optional and may
relay notifications to approved IRC channels; neither system is an execution
authority or source of truth. GitHub remains the engineering evidence store.

## Provisioning boundary

1. Copy `config/integrations/slack-znc.example.json` into the environment's
   external configuration store. Keep both connectors disabled initially.
2. Create a Slack app with only `chat:write` and the channel access needed for
   `helios-control-plane`. Do not grant workspace administration scopes.
3. Store the bot token and signing secret under the configured names in Azure
   Key Vault. Container Apps access them through managed identity.
4. If IRC is required, deploy ZNC on a private network with TLS, encrypted
   storage, a pinned container digest, and SASL credentials from Key Vault.
   Explicitly allowlist every network and channel.
5. Run `python3 scripts/integrations/validate_slack_znc_config.py <path>`.
6. Exercise development notifications with a correlation ID and evidence URL.
   Verify escaping, rate limiting, retry/dead-letter behavior, and that inbound
   Slack/IRC messages cannot create or approve an execution.
7. Enable production only through a protected environment review. Rollback is
   disabling the connector flags and revoking the app/network credentials.

Slack and ZNC secrets must never be committed, printed, placed in GitHub
workflow YAML, or copied into evidence artifacts. A Slack-to-IRC bridge must be
one-way and must mark relayed messages so they cannot form feedback loops.
