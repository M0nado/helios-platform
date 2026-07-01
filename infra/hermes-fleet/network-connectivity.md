# Network and connectivity notes

## Intended traffic flow

- Users -> `portal` web app over HTTPS
- Portal -> `api` web app over HTTPS
- API -> Storage Account over Azure backbone network
- API -> Cosmos DB over Azure backbone network
- Operators -> optional jumpbox VM over RDP only when explicitly enabled

## Subnet responsibilities

### `app`
- Delegated to App Service regional VNet integration
- Hosts outbound integration from the API and portal apps
- Should not be reused for VMs or private endpoints

### `data`
- Allows scoped access to Storage and Cosmos DB
- Best place to add private endpoints later
- Keeps data-plane policies separate from user-facing apps

### `compute`
- Reserved for the management VM
- Good boundary for future bastion or admin-only tooling

## Recommended production controls

- Replace the open RDP rule in `compute.bicep` with a narrow corporate IP range or Azure Bastion.
- Add NSGs to the `data` subnet if you introduce private endpoints or other IaaS workloads.
- Keep App Service TLS minimum at 1.2 or above.
- Use managed identity instead of connection strings where SDKs allow it.
- Add health probes and a front-end gateway before exposing the platform publicly.

## Validation checklist

- Confirm the web apps report successful VNet integration.
- Confirm Storage and Cosmos DB deny public access from non-approved networks.
- Confirm the portal can reach the API over its configured base URL.
- Confirm the API can resolve and reach Storage and Cosmos DB endpoints.
- If the VM is enabled, confirm RDP is limited to approved operators only.
