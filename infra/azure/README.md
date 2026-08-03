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
VNet peerings and application rules generated from
`monado/helios-control/config/network-paths.json`. `natGateway` is retained only
for isolated development profiles because NAT does not enforce an FQDN allowlist.
The firewall policy is default-deny and enables a destination group only when its
matching connector/model profile is enabled.

Front Door Premium reaches internal APIM through Private Link, and APIM forwards
the approved API methods to `connectorBackendUrl`. The canonical connector deployment
must receive `containerAppsInfrastructureSubnetId`; production validation rejects an
omitted subnet and configures internal Container Apps ingress. APIM and the workloads
therefore have public network access disabled. Do not approve the Front Door
private-link connection or remove a migration-time public endpoint until health,
OAuth discovery/token, MCP, signed webhook, and rollback drills all pass. A failed
gate restores the prior edge configuration from the reviewed incremental what-if;
it never opens a workload directly to the Internet.

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
