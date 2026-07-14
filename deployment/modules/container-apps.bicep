param location string
param environmentName string
param containerEnvironmentName string
param controlPlaneAppName string
param hubspotWorkerAppName string
param logAnalyticsWorkspaceId string
param appInsightsConnectionString string
param controlPlaneImage string
param hubspotSyncImage string
param serviceBusNamespace string
param hubspotBaseUrl string
@secure()
param hubspotToken string = ''
param tags object = {}

var hubspotQueueName = 'hubspot-sync'
var hubspotWorkerServiceBusSecretName = 'hubspot-service-bus-connection'
var hubspotTokenSecretName = 'hubspot-token'
var hubspotWorkerBaseEnv = [
  {
    name: 'APPINSIGHTS_CONNECTION_STRING'
    value: appInsightsConnectionString
  }
  {
    name: 'SERVICEBUS_NAMESPACE'
    value: serviceBusNamespace
  }
  {
    name: 'SERVICEBUS_CONNECTION_STRING'
    secretRef: hubspotWorkerServiceBusSecretName
  }
  {
    name: 'HUBSPOT_BASE_URL'
    value: hubspotBaseUrl
  }
  {
    name: 'HUBSPOT_SYNC_QUEUE'
    value: hubspotQueueName
  }
]
var hubspotWorkerEnv = concat(hubspotWorkerBaseEnv, !empty(hubspotToken) ? [
  {
    name: 'HUBSPOT_TOKEN'
    secretRef: hubspotTokenSecretName
  }
] : [])
var hubspotWorkerSecrets = concat([
  {
    name: hubspotWorkerServiceBusSecretName
    value: listKeys(hubspotWorkerAuthRule.id, '2024-01-01').primaryConnectionString
  }
], !empty(hubspotToken) ? [
  {
    name: hubspotTokenSecretName
    value: hubspotToken
  }
] : [])

resource serviceBus 'Microsoft.ServiceBus/namespaces@2024-01-01' existing = {
  name: serviceBusNamespace
}

resource hubspotQueue 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' existing = {
  parent: serviceBus
  name: hubspotQueueName
}

resource hubspotWorkerAuthRule 'Microsoft.ServiceBus/namespaces/queues/authorizationRules@2024-01-01' existing = {
  parent: hubspotQueue
  name: 'hubspot-sync-worker'
}

resource managedEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: containerEnvironmentName
  location: location
  tags: union(tags, {
    component: 'container-apps'
  })
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: reference(logAnalyticsWorkspaceId, '2023-09-01').customerId
        sharedKey: listKeys(logAnalyticsWorkspaceId, '2023-09-01').primarySharedKey
      }
    }
    zoneRedundant: false
  }
}

resource controlPlaneApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: controlPlaneAppName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  tags: union(tags, {
    component: 'control-plane'
    hosting: 'aca'
  })
  properties: {
    managedEnvironmentId: managedEnvironment.id
    workloadProfileName: 'Consumption'
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
        allowInsecure: false
      }
      activeRevisionsMode: 'Single'
    }
    template: {
      containers: [
        {
          name: 'control-plane'
          image: controlPlaneImage
          env: [
            {
              name: 'APPINSIGHTS_CONNECTION_STRING'
              value: appInsightsConnectionString
            }
            {
              name: 'SERVICEBUS_NAMESPACE'
              value: serviceBusNamespace
            }
            {
              name: 'HELIOS_ENVIRONMENT'
              value: environmentName
            }
            {
              name: 'HUBSPOT_SYNC_QUEUE'
              value: hubspotQueueName
            }
          ]
          resources: {
            cpu: json('1.0')
            memory: '2Gi'
          }
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 4
      }
    }
  }
}

resource hubspotWorkerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: hubspotWorkerAppName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  tags: union(tags, {
    component: 'hubspot-sync'
    hosting: 'aca'
  })
  properties: {
    managedEnvironmentId: managedEnvironment.id
    workloadProfileName: 'Consumption'
    configuration: {
      activeRevisionsMode: 'Single'
      secrets: hubspotWorkerSecrets
    }
    template: {
      containers: [
        {
          name: 'hubspot-sync'
          image: hubspotSyncImage
          env: hubspotWorkerEnv
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 3
      }
    }
  }
}

output managedEnvironmentId string = managedEnvironment.id
output controlPlaneUrl string = 'https://${controlPlaneApp.properties.configuration.ingress.fqdn}'
output hubspotWorkerName string = hubspotWorkerApp.name
