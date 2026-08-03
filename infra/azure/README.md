# HELIOS Azure Infrastructure

Bicep templates for the governed private platform network, private service access,
approved edge, dashboard/report storage, and observability.

## Network and edge policy

The platform uses dedicated ingress, APIM, Container Apps, Functions, VM/runner,
private-endpoint, and management subnets. Every subnet has an NSG, route table,
and NSG flow logs. Container Apps and Functions receive their required subnet
delegations. Private DNS zones and endpoints cover Key Vault, Storage blob/file,
Cosmos DB SQL, Service Bus, AI Search, ACR, and each enabled Cognitive Services
account.

Production must select `azureFirewall` and provide the private IP of an approved
hub firewall whose application rules are generated from
`monado/helios-control/config/network-paths.json`. `natGateway` is retained only
for isolated development profiles because NAT does not enforce an FQDN allowlist.
The firewall policy is default-deny and enables a destination group only when its
matching connector/model profile is enabled.

Front Door Premium reaches internal APIM through Private Link. APIM and the
workloads have public network access disabled. Do not approve the Front Door
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
