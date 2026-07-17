param location string
param accountName string
param environment string
param enablePrivateNetworking bool
param privateEndpointSubnetId string
param vnetId string
param ingressPrincipalId string
param workerPrincipalId string
param tags object

var blobReaderRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '2a2b9908-6ea1-4ae2-8e65-a410df84e7d1')
var blobContributorRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')

resource account 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: accountName
  location: location
  tags: tags
  sku: {
    name: environment == 'prod' ? 'Standard_ZRS' : 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    allowSharedKeyAccess: false
    defaultToOAuthAuthentication: true
    minimumTlsVersion: 'TLS1_2'
    publicNetworkAccess: enablePrivateNetworking ? 'Disabled' : 'Enabled'
    supportsHttpsTrafficOnly: true
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: enablePrivateNetworking ? 'Deny' : 'Allow'
    }
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: account
  name: 'default'
  properties: {
    deleteRetentionPolicy: {
      enabled: true
      days: environment == 'prod' ? 90 : 14
    }
    containerDeleteRetentionPolicy: {
      enabled: true
      days: environment == 'prod' ? 90 : 14
    }
    changeFeed: {
      enabled: true
      retentionInDays: environment == 'prod' ? 365 : 30
    }
    isVersioningEnabled: true
  }
}

resource evidenceContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: 'automation-evidence'
  properties: {
    publicAccess: 'None'
    immutableStorageWithVersioning: {
      enabled: environment == 'prod'
    }
  }
}

resource quarantineContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: 'deadletter-quarantine'
  properties: {
    publicAccess: 'None'
  }
}


resource connectorStateContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: 'connector-state'
  properties: {
    publicAccess: 'None'
  }
}


resource ingressBlobReader 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(account.id, ingressPrincipalId, blobReaderRoleId)
  scope: account
  properties: {
    principalId: ingressPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: blobReaderRoleId
  }
}

resource workerBlobContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(account.id, workerPrincipalId, blobContributorRoleId)
  scope: account
  properties: {
    principalId: workerPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: blobContributorRoleId
  }
}

module privateEndpoint 'modules/private-endpoint.bicep' = if (enablePrivateNetworking) {
  name: 'storage-private-endpoint'
  params: {
    location: location
    endpointName: 'pep-${accountName}-blob'
    resourceId: account.id
    groupIds: ['blob']
    subnetId: privateEndpointSubnetId
    vnetId: vnetId
    dnsZoneName: 'privatelink.blob.core.windows.net'
    tags: tags
  }
}

output accountName string = account.name
output blobEndpoint string = account.properties.primaryEndpoints.blob
output evidenceContainerName string = evidenceContainer.name
