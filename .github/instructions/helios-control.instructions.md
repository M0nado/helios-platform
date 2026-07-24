---
applyTo: "monado/helios-control/**,plugins/helios-control-fabric/**"
---

Preserve HELIOS plan-first and fail-closed behavior.

- Never add plaintext credentials, tokens, relay URLs with embedded secrets, or
  tenant secrets.
- Keep remote MCP read-only. Deployment, RBAC, consent, merge, and publication
  stay behind protected workflows and explicit approval.
- Keep GitHub as code authority, Linear as work authority, SharePoint as
  governed documentation, and Slack/Teams as operations surfaces.
- Pin executable dependencies and immutable container digests.
- Add focused tests for authentication, request bounds, idempotency, replay,
  tenant isolation, and no-apply guarantees.
- Do not represent simulated Hermes/XCore evaluation as production learning.
