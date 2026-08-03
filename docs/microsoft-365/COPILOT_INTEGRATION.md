# Microsoft 365 Copilot Integration Plan

HELIOS should treat Microsoft 365/Copilot as an integration layer for summaries, dashboards, and operator workflows rather than a required runtime dependency.

## Initial surfaces

- Export Branch Intelligence summaries to Markdown for SharePoint/Teams/Wiki reuse.
- Keep secrets in GitHub/Azure secrets and Key Vault, never in generated reports.
- Use Microsoft Graph/Copilot connectors only from gated workflows.
- Preserve C#/.NET as the connective service layer and F# as analytics/ranking logic.

## Readiness gates

1. Azure CLI authenticated.
2. Tenant/subscription selected.
3. Graph permissions reviewed.
4. Generated reports redacted.
5. Human approval before publishing to M365 surfaces.
