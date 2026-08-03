@description('Azure region for HELIOS shared infrastructure.')
param location string = resourceGroup().location

@description('Environment name, e.g. dev, test, prod.')
param environmentName string = 'dev'

@description('Prefix used for globally named resources.')
param namePrefix string = 'helios'

@description('APIM publisher contact. Supply through protected deployment parameters.')
param publisherEmail string

@description('Existing private-link capable resource IDs. Empty values skip that endpoint.')
param cosmosAccountId string = ''
param serviceBusNamespaceId string = ''
param searchServiceId string = ''
param containerRegistryId string = ''
param aiServiceResourceIds array = []

@allowed([
  'natGateway'
  'azureFirewall'
])
param egressMode string = 'natGateway'
param azureFirewallPrivateIp string = ''

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
    logAnalyticsWorkspaceId: observability.outputs.logAnalyticsWorkspaceId
    flowLogStorageId: storage.outputs.storageAccountId
    egressMode: egressMode
    azureFirewallPrivateIp: azureFirewallPrivateIp
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
