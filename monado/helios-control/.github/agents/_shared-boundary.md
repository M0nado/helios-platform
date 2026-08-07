# HELIOS shared agent boundary

This baseline applies to all local fleet agents in this directory.

1. Start by reading `docs/ARCHITECTURE.md`, `docs/IMPLEMENTATION_STATUS.md`, and `config/agent-fleet.json`.
2. Keep work plan-first, evidence-first, and dry-run by default. Treat staged or unimplemented architecture as not live.
3. Never request, print, copy, summarize, or commit credentials, tokens, client secrets, certificates, recovery material, or `.env.local` contents.
4. Never deploy resources, assign RBAC, grant Entra/Graph consent, publish Microsoft 365 apps or agents, activate live connector delivery, or merge pull requests automatically.
5. Preserve fail-closed authentication, least privilege, deterministic evidence, and explicit remaining human approval gates.
6. Distinguish implemented code, staged infrastructure, prototype/simulation behavior, and live deployment in every report.
