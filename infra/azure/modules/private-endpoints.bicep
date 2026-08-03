param location string
param namePrefix string
param environmentName string
param virtualNetworkId string
param privateEndpointSubnetId string
param keyVaultId string
param storageAccountId string
param cosmosAccountId string
param serviceBusNamespaceId string
param searchServiceId string
param containerRegistryId string

@description('Resource IDs for enabled Azure AI/Cognitive Services accounts that support private endpoints.')
param aiServiceResourceIds array = []
@description('Resource IDs for Azure OpenAI accounts. These require the OpenAI-specific private DNS zone.')
param openAiServiceResourceIds array = []

var tags = {
  system: 'HELIOS'
  environment: environmentName
  managedBy: 'Bicep'
  'helios-managed': 'true'
}
var dnsZones = [
  'privatelink.vaultcore.azure.net'
  'privatelink.blob.${environment().suffixes.storage}'
  'privatelink.file.${environment().suffixes.storage}'
  'privatelink.documents.azure.com'
  'privatelink.servicebus.windows.net'
  'privatelink.search.windows.net'
  'privatelink.azurecr.io'
  'privatelink.cognitiveservices.azure.com'
  'privatelink.openai.azure.com'
]
var baseEndpoints = [
  { name: 'key-vault', resourceId: keyVaultId, groupId: 'vault', zoneIndex: 0 }
  { name: 'storage-blob', resourceId: storageAccountId, groupId: 'blob', zoneIndex: 1 }
  { name: 'storage-file', resourceId: storageAccountId, groupId: 'file', zoneIndex: 2 }
  { name: 'cosmos-sql', resourceId: cosmosAccountId, groupId: 'Sql', zoneIndex: 3 }
  { name: 'service-bus', resourceId: serviceBusNamespaceId, groupId: 'namespace', zoneIndex: 4 }
  { name: 'ai-search', resourceId: searchServiceId, groupId: 'searchService', zoneIndex: 5 }
  { name: 'acr', resourceId: containerRegistryId, groupId: 'registry', zoneIndex: 6 }
]
var aiEndpoints = map(aiServiceResourceIds, (resourceId, i) => {
  name: 'ai-service-${i}'
  resourceId: resourceId
  groupId: 'account'
  zoneIndex: 7
})
var openAiEndpoints = map(openAiServiceResourceIds, (resourceId, i) => {
  name: 'openai-service-${i}'
  resourceId: resourceId
  groupId: 'account'
  zoneIndex: 8
})
var endpoints = concat(baseEndpoints, aiEndpoints, openAiEndpoints)

resource zones 'Microsoft.Network/privateDnsZones@2020-06-01' = [for zone in dnsZones: {
  name: zone
  location: 'global'
  tags: tags
}]

resource zoneLinks 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = [for (zone, i) in dnsZones: {
  parent: zones[i]
  name: '${namePrefix}-${environmentName}-link'
  location: 'global'
  tags: tags
  properties: {
    registrationEnabled: false
    virtualNetwork: { id: virtualNetworkId }
  }
}]

resource privateEndpoints 'Microsoft.Network/privateEndpoints@2023-11-01' = [for endpoint in endpoints: if (!empty(endpoint.resourceId)) {
  name: '${namePrefix}-${environmentName}-${endpoint.name}-pe'
  location: location
  tags: tags
  properties: {
    subnet: { id: privateEndpointSubnetId }
    privateLinkServiceConnections: [{
      name: '${endpoint.name}-connection'
      properties: {
        privateLinkServiceId: endpoint.resourceId
        groupIds: [endpoint.groupId]
        requestMessage: 'HELIOS governed private endpoint'
      }
    }]
  }
}]

resource dnsZoneGroups 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2023-11-01' = [for (endpoint, i) in endpoints: if (!empty(endpoint.resourceId)) {
  parent: privateEndpoints[i]
  name: 'default'
  properties: {
    privateDnsZoneConfigs: [{
      name: 'zone'
      properties: { privateDnsZoneId: zones[endpoint.zoneIndex].id }
    }]
  }
}]

output privateDnsZoneNames array = dnsZones
