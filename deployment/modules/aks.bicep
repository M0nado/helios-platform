param location string
param clusterName string
param logAnalyticsWorkspaceId string
param image string
param appInsightsConnectionString string
param serviceBusNamespace string
param tags object = {}

resource aksCluster 'Microsoft.ContainerService/managedClusters@2024-03-01' = {
  name: clusterName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  tags: union(tags, {
    component: 'aks'
  })
  sku: {
    name: 'Base'
    tier: 'Standard'
  }
  properties: {
    dnsPrefix: take(clusterName, 54)
    kubernetesVersion: ''
    oidcIssuerProfile: {
      enabled: true
    }
    securityProfile: {
      workloadIdentity: {
        enabled: true
      }
    }
    agentPoolProfiles: [
      {
        name: 'system'
        count: 3
        vmSize: 'Standard_D4ds_v5'
        mode: 'System'
        osDiskSizeGB: 128
        osType: 'Linux'
        osSKU: 'Ubuntu'
        type: 'VirtualMachineScaleSets'
        availabilityZones: [
          '1'
          '2'
          '3'
        ]
        enableAutoScaling: true
        minCount: 3
        maxCount: 6
        maxPods: 110
      }
    ]
    addonProfiles: {
      omsagent: {
        enabled: true
        config: {
          logAnalyticsWorkspaceResourceID: logAnalyticsWorkspaceId
        }
      }
    }
    networkProfile: {
      networkPlugin: 'azure'
      loadBalancerSku: 'standard'
      outboundType: 'loadBalancer'
      networkPolicy: 'azure'
    }
    autoUpgradeProfile: {
      upgradeChannel: 'stable'
    }
    apiServerAccessProfile: {
      enablePrivateCluster: false
    }
  }
}

output clusterName string = aksCluster.name
output kubernetesManifestHint string = 'Apply deployment/manifests/hubspot-sync-cronjob.yaml after cluster bootstrap. Image=${image}; AppInsights=${appInsightsConnectionString}; ServiceBus=${serviceBusNamespace}'
