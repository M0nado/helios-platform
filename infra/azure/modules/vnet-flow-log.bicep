param location string
param namePrefix string
param environmentName string
param networkWatcherName string
param virtualNetworkId string
param flowLogStorageId string
param logAnalyticsWorkspaceId string

resource networkWatcher 'Microsoft.Network/networkWatchers@2023-11-01' existing = {
  name: networkWatcherName
}

resource flowLog 'Microsoft.Network/networkWatchers/flowLogs@2023-11-01' = {
  parent: networkWatcher
  name: '${namePrefix}-${environmentName}-vnet-flow-log'
  location: location
  properties: {
    targetResourceId: virtualNetworkId
    storageId: flowLogStorageId
    enabled: true
    retentionPolicy: { days: 30, enabled: true }
    format: { type: 'JSON', version: 2 }
    flowAnalyticsConfiguration: {
      networkWatcherFlowAnalyticsConfiguration: {
        enabled: true
        workspaceResourceId: logAnalyticsWorkspaceId
        trafficAnalyticsInterval: 10
      }
    }
  }
}
