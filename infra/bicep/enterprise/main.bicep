targetScope = 'resourceGroup'

@description('HELIOS deployment environment')
@allowed([
  'dev'
  'test'
  'prod'
])
param environmentName string

@description('Azure region')
param location string = resourceGroup().location

@description('Resource name prefix')
param prefix string = 'helios'

var tags = {
  system: 'HELIOS'
  environment: environmentName
  managedBy: 'Bicep'
  controlPlane: 'M0nado/Helios-Control-Center'
}

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${prefix}-${environmentName}-law'
  location: location
  tags: tags
  properties: {
    retentionInDays: environmentName == 'prod' ? 90 : 30
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: take('${prefix}-${environmentName}-${uniqueString(resourceGroup().id)}', 24)
  location: location
  tags: tags
  properties: {
    tenantId: subscription().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
    enablePurgeProtection: environmentName == 'prod'
    softDeleteRetentionInDays: 90
    publicNetworkAccess: environmentName == 'prod' ? 'Disabled' : 'Enabled'
  }
}

output logAnalyticsWorkspaceId string = logAnalytics.id
output keyVaultName string = keyVault.name
