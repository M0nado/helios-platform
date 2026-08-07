@description('Azure region for HELIOS shared infrastructure.')
param location string = resourceGroup().location

@description('Environment name, e.g. dev, test, prod.')
@allowed([
  'dev'
  'test'
  'prod'
])
param environmentName string = 'dev'

@description('Prefix used for globally named resources.')
param namePrefix string = 'helios'

@description('APIM publisher contact. Supply through protected deployment parameters.')
param publisherEmail string
@description('Optional platform VNet address space for non-overlapping environment peering. Leave empty for canonical environment defaults.')
param platformAddressSpace string = ''
@description('Internal Container Apps connector origin, for example https://app.internal.region.azurecontainerapps.io.')
param connectorBackendUrl string = ''
@description('Provision network and private endpoints only. Use for reviewed production subnet bootstrap before connector cutover.')
param networkOnly bool = false
@description('Enable the Front Door public route after the connector has been redeployed with the reviewed public origin.')
param edgeRouteCutoverApproved bool = false

@description('Existing private-link capable resource IDs. Empty values skip that endpoint.')
param cosmosAccountId string = ''
param serviceBusNamespaceId string = ''
param searchServiceId string = ''
param containerRegistryId string = ''
param aiServiceResourceIds array = []
param openAiServiceResourceIds array = []

@allowed([
  'natGateway'
  'azureFirewall'
])
param egressMode string = 'natGateway'
param azureFirewallPrivateIp string = ''
@description('Resource ID of the hub VNet containing the approved Azure Firewall. Required with azureFirewall.')
param hubVirtualNetworkId string = ''
@description('Resource ID of the Firewall Policy whose HELIOS application rules are managed here. Required with azureFirewall.')
param azureFirewallPolicyId string = ''
@description('Integration profiles approved for outbound access. Each profile becomes an explicit Firewall Policy application rule.')
param enabledEgressProfiles array = []
@description('Validated HTTPS relay destinations. Each item has a profile and bare callback FQDN (no scheme, port, path, or wildcard).')
param connectorRelayDestinations array = []
@description('Approval gate for disabling the Key Vault public data plane after its private endpoint has deployed successfully.')
param keyVaultPrivateCutoverApproved bool = false
@description('Existing regional Network Watcher resource group.')
param networkWatcherResourceGroupName string = 'NetworkWatcherRG'
@description('Existing regional Network Watcher name.')
param networkWatcherName string = 'NetworkWatcher_${location}'

var firewallConfigurationComplete = egressMode != 'azureFirewall' || (!empty(azureFirewallPrivateIp) && !empty(hubVirtualNetworkId) && !empty(azureFirewallPolicyId))
var validatedEgressMode = firewallConfigurationComplete && (environmentName != 'prod' || egressMode == 'azureFirewall') ? egressMode : fail('Production and all azureFirewall deployments require the firewall IP, hub VNet ID, and Firewall Policy ID.')
var validatedConnectorBackendUrl = environmentName != 'prod' || networkOnly || !empty(connectorBackendUrl) ? connectorBackendUrl : fail('Production requires the internal connector backend URL unless networkOnly is enabled for reviewed subnet bootstrap.')
var keyVaultName = take(toLower(replace('${namePrefix}-${environmentName}-kv', '-', '')), 24)
var keyVaultId = resourceId('Microsoft.KeyVault/vaults', keyVaultName)

module storage 'modules/storage.bicep' = {
  name: 'storage-${environmentName}'
  params: {
    location: location
    namePrefix: namePrefix
    environmentName: environmentName
  }
}

// The normal stage creates or updates the vault without closing its current path.
// Once cutover is approved, only the post-private-endpoint update may manage it.
module keyVault 'modules/keyvault.bicep' = if (!keyVaultPrivateCutoverApproved) {
  name: 'keyvault-${environmentName}'
  params: {
    location: location
    namePrefix: namePrefix
    environmentName: environmentName
  }
}


module network 'modules/network.bicep' = {
  name: 'network-${environmentName}'
  params: {
    location: location
    namePrefix: namePrefix
    environmentName: environmentName
    platformAddressSpace: platformAddressSpace
    egressMode: validatedEgressMode
    azureFirewallPrivateIp: azureFirewallPrivateIp
    hubVirtualNetworkId: hubVirtualNetworkId
  }
}

module hubGovernance 'modules/hub-governance.bicep' = if (validatedEgressMode == 'azureFirewall') {
  name: 'hub-governance-${environmentName}'
  scope: resourceGroup(split(hubVirtualNetworkId, '/')[2], split(hubVirtualNetworkId, '/')[4])
  params: {
    namePrefix: namePrefix
    environmentName: environmentName
    hubVirtualNetworkName: last(split(hubVirtualNetworkId, '/'))
    platformVirtualNetworkId: network.outputs.virtualNetworkId
    platformAddressSpace: network.outputs.platformAddressSpace
    azureFirewallPolicyName: last(split(azureFirewallPolicyId, '/'))
    enabledEgressProfiles: enabledEgressProfiles
    connectorRelayDestinations: connectorRelayDestinations
  }
}

module virtualNetworkFlowLog 'modules/vnet-flow-log.bicep' = {
  name: 'vnet-flow-log-${environmentName}'
  scope: resourceGroup(networkWatcherResourceGroupName)
  params: {
    location: location
    namePrefix: namePrefix
    environmentName: environmentName
    networkWatcherName: networkWatcherName
    virtualNetworkId: network.outputs.virtualNetworkId
    flowLogStorageId: storage.outputs.storageAccountId
    logAnalyticsWorkspaceId: observability.outputs.logAnalyticsWorkspaceId
  }
}

module observability 'modules/observability.bicep' = {
  name: 'observability-${environmentName}'
  params: {
    location: location
    namePrefix: namePrefix
    environmentName: environmentName
  }
}

module privateEndpoints 'modules/private-endpoints.bicep' = {
  name: 'private-endpoints-${environmentName}'
  params: {
    location: location
    namePrefix: namePrefix
    environmentName: environmentName
    virtualNetworkId: network.outputs.virtualNetworkId
    privateEndpointSubnetId: network.outputs.privateEndpointSubnetId
    keyVaultId: keyVaultId
    storageAccountId: storage.outputs.storageAccountId
    cosmosAccountId: cosmosAccountId
    serviceBusNamespaceId: serviceBusNamespaceId
    searchServiceId: searchServiceId
    containerRegistryId: containerRegistryId
    aiServiceResourceIds: aiServiceResourceIds
    openAiServiceResourceIds: openAiServiceResourceIds
  }
}

module keyVaultPrivateCutover 'modules/keyvault-private-cutover.bicep' = if (keyVaultPrivateCutoverApproved) {
  name: 'keyvault-private-cutover-${environmentName}'
  params: {
    location: location
    namePrefix: namePrefix
    environmentName: environmentName
  }
  dependsOn: [
    privateEndpoints
  ]
}

module privateEdge 'modules/private-edge.bicep' = if (!networkOnly) {
  name: 'private-edge-${environmentName}'
  params: {
    location: location
    namePrefix: namePrefix
    environmentName: environmentName
    apimSubnetId: network.outputs.apimSubnetId
    publisherEmail: publisherEmail
    connectorBackendUrl: validatedConnectorBackendUrl
    edgeRouteEnabled: environmentName != 'prod' || edgeRouteCutoverApproved
  }
}

output storageAccountName string = storage.outputs.storageAccountName
output logAnalyticsWorkspaceName string = observability.outputs.logAnalyticsWorkspaceName
output keyVaultName string = keyVaultName
output keyVaultUri string = 'https://${keyVaultName}.${environment().suffixes.keyvaultDns}'
output virtualNetworkName string = network.outputs.virtualNetworkName
output containerAppsInfrastructureSubnetId string = network.outputs.containerAppsSubnetId
output frontDoorEndpointHostName string = networkOnly ? '' : privateEdge!.outputs.frontDoorEndpointHostName
output directPublicIngress string = networkOnly ? '' : privateEdge!.outputs.directPublicIngress
output egressDecision string = network.outputs.egressDecision
