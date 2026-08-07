# Monadoblade Delivery Fabric v2

## Scope

This document defines the governed six-profile Monadoblade delivery fabric contract.

- Active contract: `/config/profiles/monadoblade-profiles.v2.json`
- Legacy input retained: `/config/profiles/monadoblade-profiles.v1.json`
- Migration map: `/config/profiles/monadoblade-profiles.migration.v1-to-v2.json`
- Shell state machine: `/config/gui/monado-profile-shell.v2.json`
- USB wizard boundary: `/config/usb/monadoblade-usb-wizard.v1.json`
- ALVIS capability boundary: `/config/aihub/alvis-capabilities.v1.json`
- Integration projection contract: `/config/integrations/monadoblade-collaboration-projection.v1.json`

## Six-profile contract

1. Core `核`
2. Developer `創`
3. Studio `響`
4. Gamer `迅`
5. AI/Server `智`
6. local/offline Sysadmin `統`

## Safety boundaries

- Shell flow is post-auth presentation only: `safe-boot -> identity-verified -> wheel-ready -> profile-selected -> shell-ready`.
- Sysadmin remains local/offline with physical presence and two-factor local authorization.
- USB Wizard defaults to dry-run and read-only inventory.
- Recovery and Quarantine remain Sysadmin workflows, not user profiles.
- ALVIS is read-only or planning-boundary only (`search_*`, `fetch_*`, `plan_*`, `request_*`) and cannot execute tools directly.
- Only approved PR/evidence links are projected to Linear/Slack/Teams/SharePoint/Azure DevOps; projection targets never execute commands.

## Pages/wiki path

- Architecture entry: `/docs/architecture/MONADOBLADE_DELIVERY_FABRIC_V2.md`
- Wiki index entry: `/docs/WIKI/README.md`
