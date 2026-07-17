param location string = resourceGroup().location
param environmentName string = 'dev'
param serviceName string = 'helios-connect'
param deployApiManagement bool = false
@description('Client ID of the Entra app registration that protects the connector API.')
param entraClientId string
@description('Entra tenant containing the connector app registration.')
param entraTenantId string = subscription().tenantId
@description('Object ID of the user or service principal allowed to call the connector.')
@minLength(1)
param allowedPrincipalObjectId string

var suffix = uniqueString(resourceGroup().id, environmentName)
var compactName = take(replace('${serviceName}${environmentName}${suffix}', '-', ''), 20)

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${serviceName}-${environmentName}-id'
  location: location
}

var readerRoleDefinitionId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'acdd72a7-3385-48ef-bd42-f606fba81ae7')
resource identityReader 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, identity.id, readerRoleDefinitionId)
  properties: {
    roleDefinitionId: readerRoleDefinitionId
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource logs 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${serviceName}-${environmentName}-law'
  location: location
  properties: {
    retentionInDays: 30
    features: { enableLogAccessUsingOnlyResourcePermissions: true }
  }
}

resource insights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${serviceName}-${environmentName}-appi'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logs.id
  }
}

resource registry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: '${compactName}acr'
  location: location
  sku: { name: 'Basic' }
  properties: { adminUserEnabled: false, publicNetworkAccess: 'Enabled' }
}

resource vault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: '${serviceName}-${environmentName}-kv'
  location: location
  properties: {
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
    sku: { family: 'A', name: 'standard' }
    publicNetworkAccess: 'Enabled'
    networkAcls: { bypass: 'AzureServices', defaultAction: 'Allow' }
  }
}

resource bus 'Microsoft.ServiceBus/namespaces@2023-01-01-preview' = {
  name: '${serviceName}-${environmentName}-sb'
  location: location
  sku: { name: 'Standard', tier: 'Standard' }
}

resource topic 'Microsoft.ServiceBus/namespaces/topics@2023-01-01-preview' = {
  parent: bus
  name: 'events'
  properties: { enablePartitioning: true, supportOrdering: true }
}

resource workers 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2022-10-01-preview' = {
  parent: topic
  name: 'connector-workers'
  properties: {
    lockDuration: 'PT1M'
    maxDeliveryCount: 10
    deadLetteringOnMessageExpiration: true
    defaultMessageTimeToLive: 'P14D'
    requiresSession: false
  }
}

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: take(replace('${serviceName}${environmentName}${uniqueString(resourceGroup().id)}', '-', ''), 24)
  location: location
  sku: { name: 'Standard_LRS' }
  kind: 'StorageV2'
  properties: {
    allowBlobPublicAccess: false
    isHnsEnabled: true
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storage
  name: 'default'
}

resource evidence 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: 'hermes-evidence'
  properties: { publicAccess: 'None' }
}

resource cosmos 'Microsoft.DocumentDB/databaseAccounts@2024-05-15' = {
  name: '${compactName}cosmos'
  location: location
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    disableLocalAuth: true
    publicNetworkAccess: 'Enabled'
    locations: [{ locationName: location, failoverPriority: 0, isZoneRedundant: false }]
    consistencyPolicy: { defaultConsistencyLevel: 'Session' }
  }
}

resource cosmosDb 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-05-15' = {
  parent: cosmos
  name: 'hermes-learning'
  properties: { resource: { id: 'hermes-learning' } }
}

resource candidates 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-05-15' = {
  parent: cosmosDb
  name: 'candidates'
  properties: {
    resource: {
      id: 'candidates'
      partitionKey: { paths: ['/agentId'], kind: 'Hash' }
    }
  }
}

resource search 'Microsoft.Search/searchServices@2023-11-01' = {
  name: '${compactName}-search'
  location: location
  sku: { name: 'basic' }
  properties: {
    disableLocalAuth: true
    hostingMode: 'default'
    publicNetworkAccess: 'enabled'
    replicaCount: 1
    partitionCount: 1
  }
}

resource environment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: '${serviceName}-${environmentName}-cae'
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logs.properties.customerId
        sharedKey: logs.listKeys().primarySharedKey
      }
    }
  }
}

resource api 'Microsoft.App/containerApps@2024-03-01' = {
  name: '${serviceName}-${environmentName}-api'
  location: location
  tags: { 'azd-service-name': 'api' }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: { '${identity.id}': {} }
  }
  properties: {
    managedEnvironmentId: environment.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: { external: true, targetPort: 8080, transport: 'auto', allowInsecure: false }
    }
    template: {
      containers: [{
        name: 'api'
        image: 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'
        env: [
          { name: 'HELIOS_EXECUTION_MODE', value: 'dry-run' }
          { name: 'HELIOS_REQUIRE_ENTRA_AUTH', value: 'true' }
          { name: 'AZURE_TENANT_ID', value: entraTenantId }
          { name: 'AZURE_SUBSCRIPTION_ID', value: subscription().subscriptionId }
          { name: 'AZURE_RESOURCE_GROUP', value: resourceGroup().name }
          { name: 'AZURE_CLIENT_ID', value: identity.properties.clientId }
          { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: insights.properties.ConnectionString }
        ]
        resources: { cpu: json('0.5'), memory: '1Gi' }
      }]
      scale: { minReplicas: 0, maxReplicas: 3 }
    }
  }
}

resource apiAuth 'Microsoft.App/containerApps/authConfigs@2024-03-01' = {
  parent: api
  name: 'current'
  properties: {
    platform: { enabled: true }
    globalValidation: {
      unauthenticatedClientAction: 'Return401'
      excludedPaths: [ '/health' ]
    }
    httpSettings: { requireHttps: true }
    identityProviders: {
      azureActiveDirectory: {
        enabled: true
        registration: {
          clientId: entraClientId
          openIdIssuer: 'https://login.microsoftonline.com/${entraTenantId}/v2.0'
        }
        validation: {
          allowedAudiences: [ entraClientId, 'api://${entraClientId}' ]
          defaultAuthorizationPolicy: {
            allowedPrincipals: { identities: [ allowedPrincipalObjectId ] }
          }
        }
      }
    }
  }
}

resource apim 'Microsoft.ApiManagement/service@2022-08-01' = if (deployApiManagement) {
  name: '${serviceName}-${environmentName}-apim'
  location: location
  sku: { name: 'Consumption', capacity: 0 }
  properties: {
    publisherEmail: 'admin@example.invalid'
    publisherName: 'Helios'
  }
}

output keyVaultName string = vault.name
output serviceBusNamespace string = bus.name
output auditStorageName string = storage.name
output applicationInsightsName string = insights.name
output containerRegistryName string = registry.name
output containerAppName string = api.name
output containerAppUrl string = 'https://${api.properties.configuration.ingress.fqdn}'
output cosmosAccountName string = cosmos.name
output searchServiceName string = search.name
output managedIdentityClientId string = identity.properties.clientId
output connectorMcpUrl string = 'https://${api.properties.configuration.ingress.fqdn}/mcp'
output connectorOpenApiUrl string = 'https://${api.properties.configuration.ingress.fqdn}/openapi/v1.json'
