param location string = resourceGroup().location
@allowed([
  'dev'
  'test'
  'preview'
  'prod'
])
param environmentName string = 'dev'
param serviceName string = 'helios-connector'
@description('Immutable OCI image reference. Replace the all-zero preview placeholder with an approved registry/repository@sha256:digest value before deployment.')
@minLength(80)
param containerImage string = 'heliosplaceholderacr.azurecr.io/helios-connect@sha256:0000000000000000000000000000000000000000000000000000000000000000'
@description('Externally bootstrapped Azure Container Registry that contains containerImage. This template never creates, changes, or grants access to the registry; an operator must grant this deployment managed identity pull access before deployment.')
@minLength(5)
@maxLength(50)
param containerRegistryName string = 'heliosplaceholderacr'
@description('Allows the documented all-zero digest only for a non-mutating what-if preview. Never set this to true for a deployment.')
param allowPreviewPlaceholder bool = false
@description('Client ID of the single-tenant Entra app registration protecting the API.')
param entraClientId string
@description('Tenant containing the connector app registration.')
param entraTenantId string = subscription().tenantId
@description('Object ID of the user or service principal allowed to call the connector.')
@minLength(1)
param allowedPrincipalObjectId string
@description('Optional canonical HTTPS origin exposed to Edge, such as a reviewed Front Door or custom DNS hostname. Leave empty to use the Container Apps FQDN.')
param publicBaseUrl string = ''
@description('Additional organization tags. Reserved HELIOS governance tags cannot be overridden.')
param commonTags object = {}

var suffix = uniqueString(resourceGroup().id, environmentName, serviceName)
var governedTags = union(commonTags, {
  'helios-managed': 'true'
  'helios-service': serviceName
  'helios-environment': environmentName
  'helios-owner': 'platform-engineering'
  'helios-provisioner': 'bicep'
  'helios-repository': 'M0nado/helios-platform'
})
var globalNamePrefix = take(replace('${serviceName}${environmentName}', '-', ''), 9)
var cosmosNamePrefix = take(replace('${serviceName}${environmentName}', '-', ''), 20)
var containerRegistryServer = '${containerRegistryName}.azurecr.io'
var digestParts = split(toLower(containerImage), '@sha256:')
var containerImageDigest = length(digestParts) == 2 ? digestParts[1] : ''
var previewPlaceholderDigest = '0000000000000000000000000000000000000000000000000000000000000000'
var digestNonHexRemainder = replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(containerImageDigest, '0', ''), '1', ''), '2', ''), '3', ''), '4', ''), '5', ''), '6', ''), '7', ''), '8', ''), '9', ''), 'a', ''), 'b', ''), 'c', ''), 'd', ''), 'e', ''), 'f', '')
var containerImageRegistryMatches = startsWith(toLower(containerImage), '${containerRegistryServer}/')
var containerImageIsImmutable = length(digestParts) == 2 && length(containerImageDigest) == 64 && empty(digestNonHexRemainder) && (allowPreviewPlaceholder || containerImageDigest != previewPlaceholderDigest)
var validatedContainerImage = containerImageRegistryMatches && containerImageIsImmutable
  ? containerImage
  : fail('containerImage must use the configured Azure Container Registry and an approved immutable sha256 digest.')

// The operator bootstrap owns Azure RBAC grants. The only template-owned data
// role is the account-scoped Cosmos built-in contributor below; local keys are
// disabled and the routine GitHub OIDC principal remains resource-group Contributor.
resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${serviceName}-${environmentName}-id'
  location: location
  tags: governedTags
}

resource logs 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${serviceName}-${environmentName}-law'
  location: location
  tags: governedTags
  properties: {
    retentionInDays: 30
    features: { enableLogAccessUsingOnlyResourcePermissions: true }
  }
}

resource insights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${serviceName}-${environmentName}-appi'
  location: location
  tags: governedTags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logs.id
  }
}

resource vault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: take('${globalNamePrefix}${suffix}kv', 24)
  location: location
  tags: governedTags
  properties: {
    tenantId: entraTenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
    sku: { family: 'A', name: 'standard' }
    publicNetworkAccess: 'Enabled'
    networkAcls: { bypass: 'AzureServices', defaultAction: 'Deny' }
  }
}

resource cosmos 'Microsoft.DocumentDB/databaseAccounts@2024-05-15' = {
  name: take('${cosmosNamePrefix}${suffix}cosmos', 44)
  location: location
  tags: governedTags
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    disableLocalAuth: true
    publicNetworkAccess: 'Enabled'
    minimalTlsVersion: 'Tls12'
    consistencyPolicy: { defaultConsistencyLevel: 'Session' }
    locations: [{ locationName: location, failoverPriority: 0, isZoneRedundant: false }]
    capabilities: [{ name: 'EnableServerless' }]
  }
}

resource controlDatabase 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-05-15' = {
  parent: cosmos
  name: 'helios'
  properties: { resource: { id: 'helios' } }
}

resource controlRuns 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-05-15' = {
  parent: controlDatabase
  name: 'control-runs'
  properties: {
    resource: {
      id: 'control-runs'
      partitionKey: { paths: ['/partitionKey'], kind: 'Hash', version: 2 }
      defaultTtl: 2592000
      indexingPolicy: {
        indexingMode: 'consistent'
        automatic: true
        includedPaths: [{ path: '/*' }]
        excludedPaths: [{ path: '/"_etag"/?' }]
      }
    }
  }
}

var cosmosDataContributorRoleId = '00000000-0000-0000-0000-000000000002'
resource cosmosDataContributor 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2024-05-15' = {
  parent: cosmos
  name: guid(cosmos.id, identity.id, cosmosDataContributorRoleId)
  properties: {
    roleDefinitionId: '${cosmos.id}/sqlRoleDefinitions/${cosmosDataContributorRoleId}'
    principalId: identity.properties.principalId
    scope: '${cosmos.id}/dbs/${controlDatabase.name}/colls/${controlRuns.name}'
  }
}

resource environment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: '${serviceName}-${environmentName}-cae'
  location: location
  tags: governedTags
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
  tags: governedTags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: { '${identity.id}': {} }
  }
  properties: {
    managedEnvironmentId: environment.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
        allowInsecure: false
      }
      registries: [{
        server: containerRegistryServer
        identity: identity.id
      }]
    }
    template: {
      containers: [{
        name: 'api'
        image: validatedContainerImage
        env: [
          { name: 'HELIOS_EXECUTION_MODE', value: 'dry-run' }
          { name: 'HELIOS_REQUIRE_ENTRA_AUTH', value: 'true' }
          { name: 'HELIOS_CLOUD_RUNTIME_ONLY', value: 'true' }
          { name: 'HELIOS_LOCAL_RUNTIME_ALLOWED', value: 'false' }
          { name: 'HELIOS_ENTRA_CLIENT_ID', value: entraClientId }
          { name: 'HELIOS_PUBLIC_BASE_URL', value: empty(publicBaseUrl) ? 'https://${serviceName}-${environmentName}-api.${environment.properties.defaultDomain}' : (startsWith(toLower(publicBaseUrl), 'https://') ? publicBaseUrl : fail('publicBaseUrl must be an HTTPS origin.')) }
          { name: 'AZURE_TENANT_ID', value: entraTenantId }
          { name: 'AZURE_SUBSCRIPTION_ID', value: subscription().subscriptionId }
          { name: 'AZURE_RESOURCE_GROUP', value: resourceGroup().name }
          { name: 'AZURE_CLIENT_ID', value: identity.properties.clientId }
          { name: 'HELIOS_COSMOS_ENDPOINT', value: cosmos.properties.documentEndpoint }
          { name: 'HELIOS_COSMOS_DATABASE', value: controlDatabase.name }
          { name: 'HELIOS_COSMOS_CONTAINER', value: controlRuns.name }
          { name: 'HELIOS_CONNECTOR_DELIVERY_MODE', value: 'dry-run' }
          { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: insights.properties.ConnectionString }
        ]
        resources: { cpu: json('0.5'), memory: '1Gi' }
        probes: [
          {
            type: 'Startup'
            httpGet: { path: '/health/live', port: 8080, scheme: 'HTTP' }
            initialDelaySeconds: 1
            periodSeconds: 5
            timeoutSeconds: 3
            failureThreshold: 30
            successThreshold: 1
          }
          {
            type: 'Liveness'
            httpGet: { path: '/health/live', port: 8080, scheme: 'HTTP' }
            initialDelaySeconds: 10
            periodSeconds: 30
            timeoutSeconds: 5
            failureThreshold: 3
            successThreshold: 1
          }
          {
            type: 'Readiness'
            httpGet: { path: '/health/ready', port: 8080, scheme: 'HTTP' }
            initialDelaySeconds: 5
            periodSeconds: 10
            timeoutSeconds: 5
            failureThreshold: 3
            successThreshold: 1
          }
        ]
      }]
      scale: { minReplicas: environmentName == 'prod' ? 1 : 0, maxReplicas: 3 }
    }
  }
}

resource apiAuth 'Microsoft.App/containerApps/authConfigs@2024-03-01' = {
  parent: api
  name: 'current'
  properties: {
    platform: { enabled: true }
    globalValidation: {
      // Keep the auth sidecar in front of every request so it validates tokens
      // and sanitizes platform principal headers. The application then returns
      // route-specific 401 challenges while health/OAuth metadata stay public.
      unauthenticatedClientAction: 'AllowAnonymous'
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

output containerRegistryName string = containerRegistryName
output containerRegistryServer string = containerRegistryServer
output containerAppName string = api.name
output connectorUrl string = 'https://${api.properties.configuration.ingress.fqdn}'
output connectorMcpUrl string = 'https://${api.properties.configuration.ingress.fqdn}/mcp'
output connectorEntraClientId string = entraClientId
output connectorEntraTenantId string = entraTenantId
output keyVaultName string = vault.name
output managedIdentityClientId string = identity.properties.clientId
output controlRunStoreName string = cosmos.name
