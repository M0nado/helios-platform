# HELIOS / Monado agent operating contract

## Canonical ownership

`M0nado/helios-platform` is the canonical product and execution platform. `Heli0s-Dynamics/adaptive-multibrain-bootstrap` is the cross-repository control plane for shared policies, GitHub/Azure bootstrap, integration contracts, and Hermes/XCore evaluation.

Read these before cross-repository work:

- `.github/copilot-instructions.md`
- `config/integrations/repositories.json`
- `config/integrations/event-contract.schema.json`
- `docs/architecture/UNIFIED_AGENT_COMMUNICATION.md`

## Agent roles

- **GitHub Copilot:** repository-local implementation and review assistance.
- **Codex:** bounded implementation, refactoring, tests, and issue/PR work.
- **Hermes:** task routing, summaries, bounded learning experiments, and event production.
- **XCore:** evaluation, pruning, regression detection, contract validation, and policy enforcement.
- **Microsoft Copilot / Copilot Studio:** business-facing requests through approved Azure broker APIs and Microsoft Graph connectors.
- **Guardian:** rejects secret exposure, unsafe privileged Windows changes, unreviewed tenant changes, and unreviewed deployment.

## Required behavior

1. Use a scoped issue, feature branch, and pull request.
2. Keep changes in the owning repository; do not duplicate platform modules in the bootstrap repo.
3. Use the normalized integration event envelope for cross-system communication.
4. Build/test affected projects and validate integration contracts.
5. Never commit credentials, tokens, recovery keys, tenant secrets, or private endpoint credentials.
6. Keep disk formatting, BitLocker, WDAC/AppLocker, firewall lockdown, Entra/RBAC, Intune, Purview, secret rotation, and production deployment behind explicit approval and rollback gates.
7. Use Azure workload identity federation, Key Vault, protected GitHub environments, Bicep what-if, and least-privilege Microsoft Graph permissions.
8. Microsoft Copilot and Copilot Studio may request actions but cannot bypass GitHub/Azure approvals.
9. Keep generated logs, caches, model weights, VHDX files, local databases, and raw fleet evidence out of Git.
10. Preserve correlation IDs and evidence links across GitHub, Azure, Microsoft 365, and HELIOS services.