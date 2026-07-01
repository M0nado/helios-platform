@description('Azure region for web resources.')
param location string

@description('Prefix used to build resource names.')
param namePrefix string

@description('Subnet used for App Service VNet integration.')
param appSubnetId string

@description('App Service Plan SKU name.')
param appServiceSkuName string = 'P1v3'

@description('Runtime stack for the Linux web apps.')
param linuxFxVersion string = 'DOTNETCORE|8.0'

@description('Storage account name exposed to the web tier through configuration.')
param storageAccountName string

@description('Cosmos DB endpoint exposed to the web tier through configuration.')
param cosmosEndpoint string

@description('Cosmos DB account name used for API managed identity data-plane RBAC.')
param cosmosAccountName string

@description('Cosmos DB database name exposed to the web tier through configuration.')
param cosmosDatabaseName string

@description('Tags applied to the web resources.')
param tags object = {}

var planName = '${namePrefix}-plan'
var workspaceName = '${namePrefix}-law'
var appInsightsName = '${namePrefix}-appi'
var apiSiteName = take(replace('${namePrefix}-api', '-', ''), 60)
var portalSiteName = take(replace('${namePrefix}-portal', '-', ''), 60)
var storageBlobDataContributorRoleDefinitionId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
var cosmosDbBuiltInDataContributorRoleDefinitionId = '${cosmos.id}/sqlRoleDefinitions/00000000-0000-0000-0000-000000000002'

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: storageAccountName
}

resource cosmos 'Microsoft.DocumentDB/databaseAccounts@2024-05-15' existing = {
  name: cosmosAccountName
}

resource workspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: workspaceName
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  tags: tags
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: workspace.id
  }
}

resource servicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: planName
  location: location
  tags: tags
  sku: {
    name: appServiceSkuName
    tier: contains(appServiceSkuName, 'P') ? 'PremiumV3' : 'Standard'
    size: appServiceSkuName
    capacity: 1
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

resource apiSite 'Microsoft.Web/sites@2023-12-01' = {
  name: apiSiteName
  location: location
  tags: union(tags, {
    role: 'api'
  })
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: servicePlan.id
    httpsOnly: true
    virtualNetworkSubnetId: appSubnetId
    siteConfig: {
      linuxFxVersion: linuxFxVersion
      alwaysOn: true
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      vnetRouteAllEnabled: true
      appSettings: [
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
        {
          name: 'MONADO_STORAGE_ACCOUNT'
          value: storageAccountName
        }
        {
          name: 'MONADO_COSMOS_ENDPOINT'
          value: cosmosEndpoint
        }
        {
          name: 'MONADO_COSMOS_DATABASE'
          value: cosmosDatabaseName
        }
        {
          name: 'MONADO_ROLE'
          value: 'api'
        }
      ]
    }
  }
}

resource apiStorageBlobDataContributorAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storage.id, apiSite.identity.principalId, storageBlobDataContributorRoleDefinitionId)
  scope: storage
  properties: {
    principalId: apiSite.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: storageBlobDataContributorRoleDefinitionId
  }
}

resource apiCosmosDataContributorAssignment 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2024-05-15' = {
  name: guid(cosmos.id, apiSite.identity.principalId, cosmosDbBuiltInDataContributorRoleDefinitionId)
  parent: cosmos
  properties: {
    principalId: apiSite.identity.principalId
    roleDefinitionId: cosmosDbBuiltInDataContributorRoleDefinitionId
    scope: cosmos.id
  }
}

resource portalSite 'Microsoft.Web/sites@2023-12-01' = {
  name: portalSiteName
  location: location
  tags: union(tags, {
    role: 'portal'
  })
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: servicePlan.id
    httpsOnly: true
    virtualNetworkSubnetId: appSubnetId
    siteConfig: {
      linuxFxVersion: linuxFxVersion
      alwaysOn: true
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      vnetRouteAllEnabled: true
      appSettings: [
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
        {
          name: 'MONADO_API_BASE_URL'
          value: 'https://${apiSite.properties.defaultHostName}'
        }
        {
          name: 'MONADO_ROLE'
          value: 'portal'
        }
      ]
    }
  }
}

output appServicePlanName string = servicePlan.name
output apiWebAppName string = apiSite.name
output portalWebAppName string = portalSite.name
output apiHostname string = apiSite.properties.defaultHostName
output portalHostname string = portalSite.properties.defaultHostName
