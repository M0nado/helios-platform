targetScope = 'resourceGroup'

@allowed([
  'dev'
])
param environmentName string = 'dev'
param location string = resourceGroup().location
param resourcePrefix string = 'helios'

var suffix = uniqueString(subscription().subscriptionId, resourceGroup().id, environmentName)
var prefix = toLower('${resourcePrefix}-${environmentName}')
var tags = {
  system: 'HELIOS'
  environment: environmentName
  managedBy: 'Bicep'
  sourceRepository: 'M0nado/helios-platform'
  sourceSlice: 'azure-dev-live-connect'
}

resource logs 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${prefix}-logs-${suffix}'
  location: location
  tags: tags
  properties: {
    retentionInDays: 30
    sku: {
      name: 'PerGB2018'
    }
  }
}

resource insights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${prefix}-appinsights-${suffix}'
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logs.id
  }
}

resource runtimeIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${prefix}-runtime-${suffix}'
  location: location
  tags: tags
}

resource vault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: take(replace('${prefix}-kv-${suffix}', '-', ''), 24)
  location: location
  tags: tags
  properties: {
    tenantId: tenant().tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    enablePurgeProtection: false
    softDeleteRetentionInDays: 7
    publicNetworkAccess: 'Enabled'
    sku: {
      family: 'A'
      name: 'standard'
    }
  }
}

resource artifacts 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: take(replace('${prefix}artifacts${suffix}', '-', ''), 24)
  location: location
  tags: tags
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    allowBlobPublicAccess: false
    allowSharedKeyAccess: false
    defaultToOAuthAuthentication: true
    minimumTlsVersion: 'TLS1_2'
    publicNetworkAccess: 'Enabled'
    supportsHttpsTrafficOnly: true
  }
}

output environment string = environmentName
output location string = location
output logAnalyticsId string = logs.id
output applicationInsightsId string = insights.id
output runtimeIdentityClientId string = runtimeIdentity.properties.clientId
output runtimeIdentityPrincipalId string = runtimeIdentity.properties.principalId
output keyVaultId string = vault.id
output keyVaultUri string = vault.properties.vaultUri
output storageAccountId string = artifacts.id
output storageAccountName string = artifacts.name
