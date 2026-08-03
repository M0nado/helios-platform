@description('Azure region for HELIOS shared infrastructure.')
param location string = resourceGroup().location

@description('Environment name, e.g. dev, test, prod.')
param environmentName string = 'dev'

@description('Prefix used for globally named resources.')
param namePrefix string = 'helios'

@description('APIM publisher contact. Supply through protected deployment parameters.')
param publisherEmail string
@description('Internal Container Apps connector origin, for example https://app.internal.region.azurecontainerapps.io.')
param connectorBackendUrl string = ''

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
@description('Existing regional Network Watcher resource group.')
param networkWatcherResourceGroupName string = 'NetworkWatcherRG'
@description('Existing regional Network Watcher name.')
param networkWatcherName string = 'NetworkWatcher_${location}'

var firewallConfigurationComplete = egressMode != 'azureFirewall' || (!empty(azureFirewallPrivateIp) && !empty(hubVirtualNetworkId) && !empty(azureFirewallPolicyId))
var validatedEgressMode = firewallConfigurationComplete && (environmentName != 'prod' || egressMode == 'azureFirewall') ? egressMode : fail('Production and all azureFirewall deployments require the firewall IP, hub VNet ID, and Firewall Policy ID.')
var validatedConnectorBackendUrl = environmentName != 'prod' || !empty(connectorBackendUrl) ? connectorBackendUrl : fail('Production requires the internal connector backend URL.')

module storage 'modules/storage.bicep' = {
  name: 'storage-${environmentName}'
  params: {
    location: location
    namePrefix: namePrefix
    environmentName: environmentName
  }
}

module keyVault 'modules/keyvault.bicep' = {
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
    azureFirewallPolicyName: last(split(azureFirewallPolicyId, '/'))
    enabledEgressProfiles: enabledEgressProfiles
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
    keyVaultId: keyVault.outputs.keyVaultId
    storageAccountId: storage.outputs.storageAccountId
    cosmosAccountId: cosmosAccountId
    serviceBusNamespaceId: serviceBusNamespaceId
    searchServiceId: searchServiceId
    containerRegistryId: containerRegistryId
    aiServiceResourceIds: aiServiceResourceIds
    openAiServiceResourceIds: openAiServiceResourceIds
  }
}

module privateEdge 'modules/private-edge.bicep' = {
  name: 'private-edge-${environmentName}'
  params: {
    location: location
    namePrefix: namePrefix
    environmentName: environmentName
    apimSubnetId: network.outputs.apimSubnetId
    publisherEmail: publisherEmail
    connectorBackendUrl: validatedConnectorBackendUrl
  }
}

output storageAccountName string = storage.outputs.storageAccountName
output logAnalyticsWorkspaceName string = observability.outputs.logAnalyticsWorkspaceName
output keyVaultName string = keyVault.outputs.keyVaultName
output keyVaultUri string = keyVault.outputs.keyVaultUri
output virtualNetworkName string = network.outputs.virtualNetworkName
output frontDoorEndpointHostName string = privateEdge.outputs.frontDoorEndpointHostName
output directPublicIngress string = privateEdge.outputs.directPublicIngress
output egressDecision string = network.outputs.egressDecision
