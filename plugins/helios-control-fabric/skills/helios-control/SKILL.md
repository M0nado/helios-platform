---
name: helios-control
description: Operate, diagnose, plan, and govern the HELIOS Monado/Hermes/XCore integration fabric across GitHub, Linear, Slack, Teams, SharePoint, Azure, Azure DevOps, Foundry, ChatGPT, Codex, and Microsoft Copilot.
---

# HELIOS control fabric

Use this skill for HELIOS platform status, setup, integration, release planning,
Azure readiness, agent orchestration, and cross-system synchronization.

## Authority map

- GitHub `M0nado/helios-platform` owns code, CI, images, releases, and deployment
  manifests.
- Linear project `Helios Integration Fabric` owns work, acceptance, and release
  gates.
- Slack `#helios-control-plane` is the detailed engineering operations thread.
  Private `#helios-ops` receives concise operational summaries. Use
  `#all-helios` only for stable availability announcements.
- Teams team `Helios`, channel `Helios Ops`, is the Microsoft operations surface.
- SharePoint owns human-readable governance and evidence. The current setup
  record is owner-only staging until a team-owned HELIOS site exists.
- Azure hosts the approved runtime. Microsoft Foundry Agent Service hosts
  governed cloud agents. Microsoft 365 Copilot, Copilot Studio, and Teams are
  distribution surfaces.

## Runtime roles

- Monado is the operator control plane.
- Hermes proposes bounded tasks and orchestration candidates.
- XCore evaluates sandbox evidence and proposes improvements.
- Foundry publishes only reviewed, versioned agent releases.

Treat the attached Hermes/XCore Python training loop as prototype simulation,
not production training. Never promote synthetic scores as learned model
evidence.

## Operating contract

1. Resolve live state from the authoritative system before planning a change.
2. Default to read-only inspection, deterministic plans, and dry-run connector
   delivery.
3. Agents may diagnose, plan, test, create task branches, and open draft pull
   requests when the user authorizes implementation.
4. Never write directly to `main`, auto-merge, deploy production, assign RBAC,
   grant tenant consent, publish a Microsoft 365 app, or enable live connector
   delivery without the corresponding explicit approval.
5. Store secrets only in Key Vault or an approved local secret store. Never
   write secrets to repositories, prompts, messages, logs, evidence, or model
   artifacts.
6. Require immutable image digests and retain compiled-template, parameter, and
   `what-if` hashes before Azure apply.
7. Keep deployment approval separate from plan and `what-if` approval.

## Easy setup

Run the plugin doctor, then generate a plan:

```bash
python plugins/helios-control-fabric/scripts/helios.py doctor
python plugins/helios-control-fabric/scripts/helios.py plan --environment azure-dev
python plugins/helios-control-fabric/scripts/helios.py oidc --environment azure-dev
python plugins/helios-control-fabric/scripts/helios.py fleet
python plugins/helios-control-fabric/scripts/helios.py edge --environment azure-dev
python plugins/helios-control-fabric/scripts/helios.py devops-sync
python plugins/helios-control-fabric/scripts/helios.py runners
```

The `oidc` command is a read-only live resolution and therefore requires an
authenticated GitHub CLI. It must use GitHub's effective default/immutable
subject policy and fail closed for custom templates, missing policy signals, or
missing canonical repository IDs. Never copy a static name-based subject from a
guide into Entra.

The `fleet` command reads the enterprise sub-agent registry and keeps production
provisioning blocked while issue #162 remains unresolved.

Continue with the repository wizard:

```powershell
pwsh ./monado/helios-control/scripts/Connect-HeliosAzureInteractive.ps1
```

The administrator must supply the tenant, subscription, resource group,
Foundry project/deployment, Entra application, Azure DevOps organization, and
publication targets. Do not guess those values.

The shared runtime must expose standard `search` and `fetch`, the Monado MCP
Apps resource, and accurate read-only annotations. Treat Azure Front Door
Premium with Container Apps Private Link as a separate reviewed cutover; do not
label the edge path live until the private endpoint is approved and health/MCP
handshakes pass.

## Cross-system synchronization

After a material validated milestone:

1. Update the GitHub pull request with the exact commit and check results.
2. Comment on the owning Linear issue.
3. Reply to the established `#helios-control-plane` Slack thread.
4. Reply to the established `Helios Ops` Teams thread.
5. Update the SharePoint setup record and re-read it to verify.

Use the same factual status everywhere. Do not announce Azure as live until a
deployment status and health check prove it.
