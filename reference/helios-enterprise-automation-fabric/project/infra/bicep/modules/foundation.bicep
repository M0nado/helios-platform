param prefix string
param environment string
param location string
param uniqueSuffix string
param deployWorkloads bool
param enablePrivateNetworking bool
param publicWebhookIngress bool
param brokerImage string
param workerImage string
param deploymentPrincipalObjectId string
param tags object

var compactPrefix = toLower(replace(prefix, '-', ''))
var keyVaultName = take('${compactPrefix}-${environment}-${uniqueSuffix}-kv', 24)
var registryName = take('${compactPrefix}he${environment}${uniqueSuffix}acr', 50)
var storageName = take('${compactPrefix}${environment}${uniqueSuffix}st', 24)
var serviceBusName = take('${compactPrefix}-${environment}-${uniqueSuffix}-sb', 50)
var appConfigName = take('${compactPrefix}-${environment}-${uniqueSuffix}-appcs', 50)
var workspaceName = take('${prefix}-${environment}-${uniqueSuffix}-logs', 63)
var insightsName = take('${prefix}-${environment}-${uniqueSuffix}-appi', 260)
var environmentName = take('${prefix}-${environment}-${uniqueSuffix}-cae', 60)
var brokerName = take('${prefix}-${environment}-broker', 32)
var workerName = take('${prefix}-${environment}-worker', 32)
var vnetName = '${prefix}-${environment}-vnet'
var infrastructureSubnetName = 'container-apps-infrastructure'
var privateEndpointSubnetName = 'private-endpoints'

resource ingressIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${prefix}-${environment}-ingress-uami'
  location: location
  tags: tags
}

resource workerIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${prefix}-${environment}-worker-uami'
  location: location
  tags: tags
}

resource workspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: workspaceName
  location: location
  tags: tags
  properties: {
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
    publicNetworkAccessForIngestion: enablePrivateNetworking ? 'Disabled' : 'Enabled'
    publicNetworkAccessForQuery: enablePrivateNetworking ? 'Disabled' : 'Enabled'
    retentionInDays: environment == 'prod' ? 90 : 30
    sku: {
      name: 'PerGB2018'
    }
  }
}

resource insights 'Microsoft.Insights/components@2020-02-02' = {
  name: insightsName
  location: location
  kind: 'web'
  tags: tags
  properties: {
    Application_Type: 'web'
    DisableIpMasking: false
    DisableLocalAuth: true
    Flow_Type: 'Bluefield'
    IngestionMode: 'LogAnalytics'
    Request_Source: 'rest'
    RetentionInDays: environment == 'prod' ? 90 : 30
    WorkspaceResourceId: workspace.id
  }
}

resource registry 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' = {
  name: registryName
  location: location
  tags: tags
  sku: {
    name: enablePrivateNetworking ? 'Premium' : 'Standard'
  }
  properties: {
    adminUserEnabled: false
    anonymousPullEnabled: false
    dataEndpointEnabled: enablePrivateNetworking
    networkRuleBypassOptions: 'AzureServices'
    policies: {
      exportPolicy: {
        status: 'disabled'
      }
      quarantinePolicy: {
        status: 'disabled'
      }
      retentionPolicy: {
        days: environment == 'prod' ? 30 : 7
        status: 'enabled'
      }
      trustPolicy: {
        status: 'enabled'
        type: 'Notary'
      }
    }
    publicNetworkAccess: enablePrivateNetworking ? 'Disabled' : 'Enabled'
    zoneRedundancy: enablePrivateNetworking && environment == 'prod' ? 'Enabled' : 'Disabled'
  }
}

resource vault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  tags: tags
  properties: {
    tenantId: tenant().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enablePurgeProtection: environment == 'prod'
    enableRbacAuthorization: true
    enableSoftDelete: true
    publicNetworkAccess: enablePrivateNetworking ? 'Disabled' : 'Enabled'
    softDeleteRetentionInDays: environment == 'prod' ? 90 : 30
  }
}

resource appConfiguration 'Microsoft.AppConfiguration/configurationStores@2024-05-01' = {
  name: appConfigName
  location: location
  tags: tags
  sku: {
    name: 'standard'
  }
  properties: {
    createMode: 'Default'
    disableLocalAuth: true
    enablePurgeProtection: environment == 'prod'
    publicNetworkAccess: enablePrivateNetworking ? 'Disabled' : 'Enabled'
  }
}

resource vnet 'Microsoft.Network/virtualNetworks@2024-07-01' = if (enablePrivateNetworking) {
  name: vnetName
  location: location
  tags: tags
  properties: {
    addressSpace: {
      addressPrefixes: [
        '10.42.0.0/16'
      ]
    }
    subnets: [
      {
        name: infrastructureSubnetName
        properties: {
          addressPrefix: '10.42.0.0/23'
          delegations: [
            {
              name: 'Microsoft.App.environments'
              properties: {
                serviceName: 'Microsoft.App/environments'
              }
            }
          ]
        }
      }
      {
        name: privateEndpointSubnetName
        properties: {
          addressPrefix: '10.42.8.0/24'
          privateEndpointNetworkPolicies: 'Disabled'
        }
      }
    ]
  }
}

resource infrastructureSubnet 'Microsoft.Network/virtualNetworks/subnets@2024-07-01' existing = if (enablePrivateNetworking) {
  parent: vnet
  name: infrastructureSubnetName
}

resource privateEndpointSubnet 'Microsoft.Network/virtualNetworks/subnets@2024-07-01' existing = if (enablePrivateNetworking) {
  parent: vnet
  name: privateEndpointSubnetName
}

module serviceBus '../servicebus.bicep' = {
  name: 'servicebus'
  params: {
    location: location
    namespaceName: serviceBusName
    environment: environment
    enablePrivateNetworking: enablePrivateNetworking
    privateEndpointSubnetId: enablePrivateNetworking ? privateEndpointSubnet.id : ''
    vnetId: enablePrivateNetworking ? vnet.id : ''
    ingressPrincipalId: ingressIdentity.properties.principalId
    workerPrincipalId: workerIdentity.properties.principalId
    tags: tags
  }
}

module storage '../storage.bicep' = {
  name: 'storage'
  params: {
    location: location
    accountName: storageName
    environment: environment
    enablePrivateNetworking: enablePrivateNetworking
    privateEndpointSubnetId: enablePrivateNetworking ? privateEndpointSubnet.id : ''
    vnetId: enablePrivateNetworking ? vnet.id : ''
    ingressPrincipalId: ingressIdentity.properties.principalId
    workerPrincipalId: workerIdentity.properties.principalId
    tags: tags
  }
}

var acrPullRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
var keyVaultSecretsUserRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
var appConfigReaderRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '516239f1-63e1-4d78-a4de-a74fb236a071')

resource ingressAcrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(registry.id, ingressIdentity.id, acrPullRoleId)
  scope: registry
  properties: {
    principalId: ingressIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: acrPullRoleId
  }
}

resource workerAcrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(registry.id, workerIdentity.id, acrPullRoleId)
  scope: registry
  properties: {
    principalId: workerIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: acrPullRoleId
  }
}

resource ingressVaultReader 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(vault.id, ingressIdentity.id, keyVaultSecretsUserRoleId)
  scope: vault
  properties: {
    principalId: ingressIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: keyVaultSecretsUserRoleId
  }
}

resource workerVaultReader 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(vault.id, workerIdentity.id, keyVaultSecretsUserRoleId)
  scope: vault
  properties: {
    principalId: workerIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: keyVaultSecretsUserRoleId
  }
}

resource ingressConfigReader 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(appConfiguration.id, ingressIdentity.id, appConfigReaderRoleId)
  scope: appConfiguration
  properties: {
    principalId: ingressIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: appConfigReaderRoleId
  }
}

resource workerConfigReader 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(appConfiguration.id, workerIdentity.id, appConfigReaderRoleId)
  scope: appConfiguration
  properties: {
    principalId: workerIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: appConfigReaderRoleId
  }
}

module registryPrivateEndpoint 'private-endpoint.bicep' = if (enablePrivateNetworking) {
  name: 'registry-private-endpoint'
  params: {
    location: location
    endpointName: 'pep-${registryName}'
    resourceId: registry.id
    groupIds: [
      'registry'
    ]
    subnetId: privateEndpointSubnet.id
    vnetId: vnet.id
    dnsZoneName: 'privatelink.azurecr.io'
    tags: tags
  }
}

module vaultPrivateEndpoint 'private-endpoint.bicep' = if (enablePrivateNetworking) {
  name: 'vault-private-endpoint'
  params: {
    location: location
    endpointName: 'pep-${keyVaultName}'
    resourceId: vault.id
    groupIds: [
      'vault'
    ]
    subnetId: privateEndpointSubnet.id
    vnetId: vnet.id
    dnsZoneName: 'privatelink.vaultcore.azure.net'
    tags: tags
  }
}

module appConfigPrivateEndpoint 'private-endpoint.bicep' = if (enablePrivateNetworking) {
  name: 'appconfig-private-endpoint'
  params: {
    location: location
    endpointName: 'pep-${appConfigName}'
    resourceId: appConfiguration.id
    groupIds: [
      'configurationStores'
    ]
    subnetId: privateEndpointSubnet.id
    vnetId: vnet.id
    dnsZoneName: 'privatelink.azconfig.io'
    tags: tags
  }
}

module workloads '../containerapps.bicep' = if (deployWorkloads) {
  name: 'container-apps'
  params: {
    location: location
    environmentName: environmentName
    infrastructureSubnetId: enablePrivateNetworking ? infrastructureSubnet.id : ''
    useVnet: enablePrivateNetworking
    internalEnvironment: enablePrivateNetworking && !publicWebhookIngress
    registryLoginServer: registry.properties.loginServer
    workspaceCustomerId: workspace.properties.customerId
    workspaceSharedKey: listKeys(workspace.id, workspace.apiVersion).primarySharedKey
    brokerName: brokerName
    workerName: workerName
    brokerImage: brokerImage
    workerImage: workerImage
    ingressIdentityId: ingressIdentity.id
    ingressIdentityClientId: ingressIdentity.properties.clientId
    workerIdentityId: workerIdentity.id
    workerIdentityClientId: workerIdentity.properties.clientId
    serviceBusNamespace: serviceBus.outputs.namespaceName
    keyVaultUri: vault.properties.vaultUri
    appConfigurationEndpoint: appConfiguration.properties.endpoint
    evidenceStorageAccount: storage.outputs.accountName
    applicationInsightsConnectionString: insights.properties.ConnectionString
    tags: tags
  }
  dependsOn: [
    ingressAcrPull
    workerAcrPull
    ingressVaultReader
    workerVaultReader
  ]
}

output brokerUrl string = deployWorkloads ? workloads!.outputs.brokerUrl : ''
output keyVaultName string = vault.name
output serviceBusNamespace string = serviceBus.outputs.namespaceName
output evidenceStorageAccount string = storage.outputs.accountName
output appConfigurationEndpoint string = appConfiguration.properties.endpoint
output workloadIdentityClientIds object = {
  ingress: ingressIdentity.properties.clientId
  worker: workerIdentity.properties.clientId
  deploymentPrincipalObjectId: deploymentPrincipalObjectId
}
