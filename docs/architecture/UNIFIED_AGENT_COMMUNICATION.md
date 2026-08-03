# HELIOS / Monado Unified Agent Communication

## System boundary

`M0nado/helios-platform` is the canonical product and execution platform. `Heli0s-Dynamics/adaptive-multibrain-bootstrap` governs cross-repository policy, integration contracts, GitHub/Azure bootstrap, and Hermes/XCore evaluation.

## Participants

- GitHub Copilot and Codex implement scoped repository work using the same `AGENTS.md` contract.
- Hermes routes bounded tasks and emits normalized events.
- XCore evaluates results, prunes derived learning state, detects regressions, and enforces policy.
- Microsoft Copilot and Copilot Studio are business-facing surfaces that call approved APIs through the Azure integration broker.
- Azure AI Foundry supplies approved model/agent deployments and evaluations.
- Control Center presents platform state and approved commands.
- AIHub owns model/provider adapters and agent runtime adapters.
- Monado Blade owns the engine and themed interaction contracts.

## Request path

```text
Human / Microsoft Copilot / Copilot Studio
                  |
                  v
      Azure API Management or broker API
                  |
        validation + authorization
                  |
         Service Bus command topic
                  |
                  v
          HELIOS platform handler
                  |
        result / status event envelope
                  |
        Event Grid / Service Bus event
                  |
       GitHub evidence + Teams/SharePoint
```

GitHub-originated engineering work follows:

```text
Issue -> Copilot/Codex/Hermes task -> branch -> tests -> pull request
      -> Actions event -> Azure broker -> platform/Teams/SharePoint telemetry
```

## Contract

The canonical repository map is `config/integrations/repositories.json`. The event envelope is `config/integrations/event-contract.schema.json`. Any platform service, workflow, connector, or agent producing integration traffic must include:

- event and correlation IDs;
- source, event type, and environment;
- data classification;
- actor identity;
- evidence links;
- a bounded payload without secrets.

## Microsoft integration

Microsoft Copilot and Copilot Studio must use governed connectors backed by Azure API Management, an Azure Function, or a Container App. Microsoft Graph permissions are split by capability and granted only with explicit tenant-admin consent:

- Teams: approved channel notifications and adaptive cards;
- SharePoint: release evidence, architecture decisions, and runbooks;
- Power Platform: approved command/request experiences;
- Fabric/Power BI: curated telemetry and evaluation datasets;
- Purview: classification, retention, and audit controls for published evidence.

Desktop scripts must not hold broad Microsoft Graph application permissions.

## Azure integration

GitHub Actions authenticates through workload identity federation. Azure resources use managed identities where supported. Key Vault stores connector secrets. Service Bus is the durable command/event path, Event Grid distributes notifications, and Application Insights plus Log Analytics provide correlated telemetry.

Every Azure infrastructure change starts with Bicep validation and what-if. Production deployment requires a protected GitHub environment and explicit approval.

## Privileged commands

The following cannot execute from a conversational request alone:

- disk partitioning or formatting;
- BitLocker/VHDX key changes;
- WDAC/AppLocker enforcement;
- firewall lockdown or process termination;
- Entra/RBAC or tenant permission changes;
- Intune/Purview policy enforcement;
- production deployment or secret rotation.

A privileged request must produce a reviewable GitHub issue or deployment request containing scope, target, what-if/dry-run evidence, approval, rollback, and correlation links.

## Initial end-to-end proof

The first integration proof should use development resources only:

1. Dispatch a sample event from GitHub Actions.
2. Validate it against the shared contract.
3. Send it through the Azure broker.
4. Have the platform acknowledge it without changing machine state.
5. Post a test notification to a dedicated Teams channel.
6. Store the evidence artifact in GitHub and the governed SharePoint test library.
7. Let XCore score completeness and open a follow-up issue for any missing evidence.
