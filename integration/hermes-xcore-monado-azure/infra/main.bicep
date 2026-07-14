targetScope = 'resourceGroup'

@allowed([
  'dev'
])
param environmentName string
param location string = resourceGroup().location
param deployControl bool = false
param image string = 'invalid.example/helios-control:deployment-disabled'

var suffix = uniqueString(subscription().id, resourceGroup().id, environmentName)
var prefix = toLower('helios-${environmentName}')
var infrastructureVnetName = '${prefix}-hybrid-vnet'
var infrastructureSubnetName = 'control-plane'
var tags = {
  system: 'HELIOS'
  environment: environmentName
  managedBy: 'Bicep'
  source: 'M0nado/helios-platform'
  slice: 'monado-control'
}

resource logs 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${prefix}-logs-${suffix}'
  location: location
  tags: tags
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
  }
}

resource insights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${prefix}-ai-${suffix}'
  location: location
  tags: tags
  kind: 'web'
  properties: { Application_Type: 'web', WorkspaceResourceId: logs.id }
}

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${prefix}-identity-${suffix}'
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
    enablePurgeProtection: true
    softDeleteRetentionInDays: 30
    publicNetworkAccess: 'Disabled'
    sku: { family: 'A', name: 'standard' }
  }
}

resource registry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: 'heliosacr${suffix}'
  location: location
  tags: tags
  sku: { name: 'Premium' }
  properties: { adminUserEnabled: false }
}

resource infrastructureSubnet 'Microsoft.Network/virtualNetworks/subnets@2023-11-01' existing = {
  name: '${infrastructureVnetName}/${infrastructureSubnetName}'
}

resource env 'Microsoft.App/managedEnvironments@2024-03-01' = if (deployControl) {
  name: '${prefix}-env-${suffix}'
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logs.properties.customerId
        sharedKey: logs.listKeys().primarySharedKey
      }
    }
    vnetConfiguration: {
      infrastructureSubnetId: infrastructureSubnet.id
      internal: true
    }
    workloadProfiles: [
      {
        name: 'Consumption'
        workloadProfileType: 'Consumption'
      }
    ]
    zoneRedundant: false
  }
}

resource control 'Microsoft.App/containerApps@2024-03-01' = if (deployControl) {
  name: '${prefix}-control'
  location: location
  tags: tags
  identity: { type: 'UserAssigned', userAssignedIdentities: { '${identity.id}': {} } }
  properties: {
    environmentId: env.id
    workloadProfileName: 'Consumption'
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: { external: false, targetPort: 8080, transport: 'auto', allowInsecure: false }
      registries: [{ server: registry.properties.loginServer, identity: identity.id }]
    }
    template: {
      containers: [{
        name: 'control'
        image: image
        env: [
          { name: 'HELIOS_ENVIRONMENT', value: environmentName }
          { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: insights.properties.ConnectionString }
          { name: 'KEY_VAULT_URI', value: vault.properties.vaultUri }
        ]
        resources: { cpu: json('0.5'), memory: '1Gi' }
        probes: [
          {
            type: 'Liveness'
            httpGet: { path: '/health/live', port: 8080, scheme: 'HTTP' }
            initialDelaySeconds: 5
            periodSeconds: 30
            timeoutSeconds: 3
            failureThreshold: 3
          }
          {
            type: 'Readiness'
            httpGet: { path: '/health/ready', port: 8080, scheme: 'HTTP' }
            initialDelaySeconds: 2
            periodSeconds: 10
            timeoutSeconds: 3
            failureThreshold: 3
          }
        ]
      }]
      scale: { minReplicas: 0, maxReplicas: 3, rules: [{ name: 'http', http: { metadata: { concurrentRequests: '50' } } }] }
    }
  }
}

output controlIdentityPrincipalId string = identity.properties.principalId
output controlName string = '${prefix}-control'
output infrastructureSubnetId string = infrastructureSubnet.id
output registryId string = registry.id
output registryName string = registry.name
output registryServer string = registry.properties.loginServer
