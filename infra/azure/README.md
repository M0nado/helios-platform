# HELIOS Azure Infrastructure

Bicep templates for the governed private platform network, private service access,
approved edge, dashboard/report storage, and observability.

## Network and edge policy

The platform uses dedicated ingress, APIM, Container Apps, Functions, VM/runner,
private-endpoint, and management subnets. Every subnet has an NSG, route table,
and a regional Network Watcher records a virtual-network flow log. Container Apps and Functions receive their required subnet
delegations. Private DNS zones and endpoints cover Key Vault, Storage blob/file,
Cosmos DB SQL, Service Bus, AI Search, ACR, and each enabled Cognitive Services
account. Azure OpenAI account IDs are supplied separately so they are linked to
`privatelink.openai.azure.com` rather than the general Cognitive Services zone.

Production must select `azureFirewall` and provide the private IP, hub VNet ID,
and Firewall Policy ID of an approved hub firewall. The deployment creates both
VNet peerings plus application/network firewall rules generated from
`monado/helios-control/config/network-paths.json`. `natGateway` is retained only
for isolated development profiles because NAT does not enforce an FQDN allowlist.
The firewall policy is default-deny and enables a destination group only when its
matching connector/model profile is enabled.

Each live `HELIOS_CONNECTOR_<NAME>_URL` relay must also be registered in
`connectorRelayDestinations` as an enabled profile plus the callback's bare FQDN.
The template rejects schemes, ports, paths, wildcards, empty hosts, and relay
profiles that are not enabled; accepted relay hosts become dedicated HTTPS
application rules rather than relying on the provider-site allowlist.

`environmentName` accepts only the canonical `dev`, `test`, and `prod` values so
production firewall and connector-backend guards cannot be bypassed with an
alias or case variation. When Azure Firewall routing is enabled, APIM receives a
more-specific `ApiManagement` service-tag return route to preserve its supported
control-plane path while workload default routes continue through the firewall.

Front Door Premium reaches internal APIM through Private Link, and APIM forwards
the approved API methods to `connectorBackendUrl`. The canonical connector deployment
must receive `containerAppsInfrastructureSubnetId`; production validation rejects an
omitted subnet and configures internal Container Apps ingress. APIM and the workloads
therefore have public network access disabled. Do not approve the Front Door
private-link connection or remove a migration-time public endpoint until health,
OAuth discovery/token, MCP, signed webhook, and rollback drills all pass. A failed
gate restores the prior edge configuration from the reviewed incremental what-if;
it never opens a workload directly to the Internet.

Key Vault private access uses two deployment stages. First deploy with
`keyVaultPrivateCutoverApproved=false`, validate the private endpoint and DNS from
an approved workload, and preserve rollback evidence. In a separately reviewed
deployment, set the gate to `true`; the cutover module runs only after the private
endpoint deployment succeeds and then disables the public data plane. Keep the
approved value on later deployments to avoid reopening the staging path. If
private-path verification fails, leave the gate false and roll back or repair the
endpoint without changing existing client access.

Control Center exposes the effective allow/deny matrix at authenticated endpoint
`GET /control/network-paths`. It intentionally returns host patterns and decisions,
not resolved secrets, private addresses, or firewall credentials.

## Validate locally

```bash
az bicep build --file infra/azure/main.bicep
az deployment group validate \
  --resource-group <resource-group> \
  --template-file infra/azure/main.bicep \
  --parameters infra/azure/parameters/dev.json
```

Deployment should be gated through `.github/workflows/azure-infra.yml`.
