param location string
param environmentName string
param infrastructureSubnetId string
param useVnet bool
param internalEnvironment bool
param registryLoginServer string
param workspaceCustomerId string
@secure()
param workspaceSharedKey string
param brokerName string
param workerName string
param brokerImage string
param workerImage string
param ingressIdentityId string
param ingressIdentityClientId string
param workerIdentityId string
param workerIdentityClientId string
param serviceBusNamespace string
param keyVaultUri string
param appConfigurationEndpoint string
param evidenceStorageAccount string
param applicationInsightsConnectionString string
param tags object

var brokerTags = union(tags, {
  'azd-service-name': 'broker'
  'helios:component': 'ingress-broker'
})
var workerTags = union(tags, {
  'azd-service-name': 'worker'
  'helios:component': 'delivery-worker'
})

resource managedEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: environmentName
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: workspaceCustomerId
        sharedKey: workspaceSharedKey
      }
    }
    infrastructureResourceGroup: 'ME_${environmentName}'
    vnetConfiguration: useVnet ? {
      infrastructureSubnetId: infrastructureSubnetId
      internal: internalEnvironment
    } : null
    zoneRedundant: useVnet
  }
}

var commonEnvironmentVariables = [
  { name: 'HELIOS_SERVICEBUS_NAMESPACE', value: '${serviceBusNamespace}.servicebus.windows.net' }
  { name: 'HELIOS_SERVICEBUS_TOPIC', value: 'helios-events' }
  { name: 'HELIOS_KEYVAULT_URI', value: keyVaultUri }
  { name: 'HELIOS_APPCONFIG_ENDPOINT', value: appConfigurationEndpoint }
  { name: 'HELIOS_EVIDENCE_STORAGE_ACCOUNT', value: evidenceStorageAccount }
  { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: applicationInsightsConnectionString }
  { name: 'ASPNETCORE_FORWARDEDHEADERS_ENABLED', value: 'true' }
]

resource broker 'Microsoft.App/containerApps@2024-03-01' = {
  name: brokerName
  location: location
  tags: brokerTags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${ingressIdentityId}': {}
    }
  }
  properties: {
    environmentId: managedEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: !internalEnvironment
        allowInsecure: false
        targetPort: 8080
        transport: 'auto'
        traffic: [
          {
            latestRevision: true
            weight: 100
          }
        ]
      }
      registries: [
        {
          server: registryLoginServer
          identity: ingressIdentityId
        }
      ]
      secrets: []
    }
    template: {
      containers: [
        {
          name: 'broker'
          image: brokerImage
          env: concat(commonEnvironmentVariables, [
            { name: 'AZURE_CLIENT_ID', value: ingressIdentityClientId }
            { name: 'HELIOS_RUNTIME_ROLE', value: 'ingress' }
          ])
          probes: [
            {
              type: 'Liveness'
              httpGet: { path: '/health/live', port: 8080 }
              initialDelaySeconds: 10
              periodSeconds: 30
            }
            {
              type: 'Readiness'
              httpGet: { path: '/health/ready', port: 8080 }
              initialDelaySeconds: 10
              periodSeconds: 15
            }
          ]
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 5
        rules: [
          {
            name: 'http'
            http: {
              metadata: {
                concurrentRequests: '50'
              }
            }
          }
        ]
      }
    }
  }
}

resource worker 'Microsoft.App/containerApps@2024-03-01' = {
  name: workerName
  location: location
  tags: workerTags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${workerIdentityId}': {}
    }
  }
  properties: {
    environmentId: managedEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: null
      registries: [
        {
          server: registryLoginServer
          identity: workerIdentityId
        }
      ]
      secrets: []
    }
    template: {
      containers: [
        {
          name: 'worker'
          image: workerImage
          env: concat(commonEnvironmentVariables, [
            { name: 'AZURE_CLIENT_ID', value: workerIdentityClientId }
            { name: 'HELIOS_RUNTIME_ROLE', value: 'worker' }
            { name: 'HELIOS_SERVICEBUS_SUBSCRIPTION', value: 'delivery-worker' }
          ])
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 10
        rules: []
      }
    }
  }
}

output brokerUrl string = 'https://${broker.properties.configuration.ingress.fqdn}'
output brokerResourceId string = broker.id
output workerResourceId string = worker.id
output containerAppsEnvironmentId string = managedEnvironment.id
