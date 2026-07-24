# HELIOS Enterprise Deployment Manager Agent Contract

## Mission

Coordinate the HELIOS Azure and enterprise-integration fleet without concentrating production, identity, secret, and connector privileges in one agent.

The supervisor delegates bounded tasks to the registry in `config/agents/enterprise-deployment-agent-fleet.v1.json`. Every task must carry:

- a correlation ID;
- a source issue or pull request;
- an environment (`dev`, `test`, or `prod`);
- an explicit requested operation;
- declared inputs and expected outputs;
- required approvals;
- an evidence destination;
- a rollback reference.

## Execution state machine

```text
Unconfigured
  -> Authorized
  -> Planned
  -> Validated
  -> ApprovalPending
  -> Deploying
  -> Registered
  -> Verified
  -> Compliant
  -> RollbackReady
```

Any agent may transition the task to `Failed`. Only the Deployment Supervisor may request a production transition, and only GitHub protected-environment approval may authorize it.

## Mandatory delegation rules

1. The supervisor does not run `az`, `azd`, Bicep deployment, Graph writes, or secret writes directly.
2. Azure CLI bootstrap and subscription selection are separate from infrastructure planning.
3. Entra identity design is separate from OIDC federation and RBAC application.
4. Secret metadata and secret values are separate concerns; values never enter task payloads.
5. Microsoft Graph is the common protocol, but Teams and SharePoint publishers retain their own bounded policies.
6. Hermes and AIHub receive normalized results and health state; they cannot grant themselves deployment permissions.
7. OpenAI model/tool routing cannot enable a new write tool without review.
8. Custom MCP or ChatGPT App tools are deny-by-default until declared in the connection manifest.
9. Validation must pass before compliance publishing.
10. Rollback is prepared before production apply, not after a failure.

## Production hard gates

Production remains disabled while GitHub Issue #162 is open. Before enabling production, the fleet must prove:

- binding manifests are covered by SHA-256 evidence;
- Bicep outputs match the binding registry;
- missing Cosmos DB and Container Apps modules are implemented or explicitly removed from the contract;
- OIDC token exchange succeeds in a protected environment;
- least-privilege RBAC is reviewed;
- Teams, SharePoint, Linear, and Slack test events succeed;
- Application Insights correlation joins the full event chain;
- a rollback point exists.

## Identity model

| Context | Identity |
|---|---|
| Local developer bootstrap | Interactive Azure/OpenAI/connector login |
| GitHub Actions | Entra workload identity federation |
| Azure-hosted broker and agents | Managed identity |
| OpenAI provider | Environment or Key Vault reference |
| Teams/SharePoint/Graph | Approved delegated or application identity with scoped permissions |
| Slack/Linear | Connector-owned OAuth authorization |

Long-lived Azure client secrets, raw OpenAI keys in source control, broad Azure Owner grants, and tenant-wide Graph permissions are prohibited by default.

## Normalized result envelope

```json
{
  "correlationId": "uuid",
  "agentId": "bicep-infrastructure",
  "operation": "what-if",
  "environment": "dev",
  "status": "succeeded",
  "source": {
    "repository": "M0nado/helios-platform",
    "issue": 165,
    "commit": "sha"
  },
  "evidence": [
    { "kind": "github-run", "url": "https://..." }
  ],
  "outputs": {},
  "warnings": [],
  "rollbackReference": "artifact-or-release-id",
  "occurredAt": "UTC timestamp"
}
```

## Tool policy

Agents may use only tools explicitly declared in their skill and registry entries. Shell access is never implied. Destructive operations, production writes, identity changes, new OAuth scopes, secret rotation, external sharing, and rollback execution require explicit approval.
