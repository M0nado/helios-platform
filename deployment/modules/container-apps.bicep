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
param tags object = {}

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
              value: 'hubspot-sync'
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
    }
    template: {
      containers: [
        {
          name: 'hubspot-sync'
          image: hubspotSyncImage
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
              name: 'HUBSPOT_BASE_URL'
              value: hubspotBaseUrl
            }
            {
              name: 'HUBSPOT_SYNC_QUEUE'
              value: 'hubspot-sync'
            }
            {
              name: 'HUBSPOT_TOKEN_SECRET_NAME'
              value: 'hubspot-private-app-token'
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
        maxReplicas: 3
      }
    }
  }
}

output managedEnvironmentId string = managedEnvironment.id
output controlPlaneUrl string = 'https://${controlPlaneApp.properties.configuration.ingress.fqdn}'
output hubspotWorkerName string = hubspotWorkerApp.name
