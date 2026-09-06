param location string
param namespaceName string
param environment string
param enablePrivateNetworking bool
param privateEndpointSubnetId string
param vnetId string
param ingressPrincipalId string
param workerPrincipalId string
param tags object

var senderRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '69a216fc-b8fb-44d8-bc22-1f3c2cd27a39')
var receiverRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4f6d3b9b-027b-4f4c-9142-0e5a2a2247e0')

resource serviceBus 'Microsoft.ServiceBus/namespaces@2024-01-01' = {
  name: namespaceName
  location: location
  tags: tags
  sku: enablePrivateNetworking ? {
    name: 'Premium'
    tier: 'Premium'
    capacity: 1
  } : {
    name: 'Standard'
    tier: 'Standard'
  }
  properties: {
    disableLocalAuth: true
    publicNetworkAccess: enablePrivateNetworking ? 'Disabled' : 'Enabled'
    minimumTlsVersion: '1.2'
    zoneRedundant: enablePrivateNetworking && environment == 'prod'
  }
}

resource topic 'Microsoft.ServiceBus/namespaces/topics@2024-01-01' = {
  parent: serviceBus
  name: 'helios-events'
  properties: {
    defaultMessageTimeToLive: 'P14D'
    duplicateDetectionHistoryTimeWindow: 'P1D'
    enableBatchedOperations: true
    enablePartitioning: !enablePrivateNetworking
    maxSizeInMegabytes: enablePrivateNetworking ? 5120 : 1024
    requiresDuplicateDetection: true
    supportOrdering: true
  }
}

resource workerSubscription 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2024-01-01' = {
  parent: topic
  name: 'delivery-worker'
  properties: {
    deadLetteringOnMessageExpiration: true
    defaultMessageTimeToLive: 'P14D'
    lockDuration: 'PT2M'
    maxDeliveryCount: 10
    requiresSession: false
  }
}

resource auditSubscription 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2024-01-01' = {
  parent: topic
  name: 'evidence-audit'
  properties: {
    deadLetteringOnMessageExpiration: true
    defaultMessageTimeToLive: 'P30D'
    lockDuration: 'PT1M'
    maxDeliveryCount: 10
  }
}

resource commandsQueue 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
  parent: serviceBus
  name: 'helios-commands'
  properties: {
    deadLetteringOnMessageExpiration: true
    defaultMessageTimeToLive: 'P1D'
    duplicateDetectionHistoryTimeWindow: 'P1D'
    lockDuration: 'PT2M'
    maxDeliveryCount: 5
    requiresDuplicateDetection: true
  }
}

resource ingressSender 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(topic.id, ingressPrincipalId, senderRoleId)
  scope: topic
  properties: {
    principalId: ingressPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: senderRoleId
  }
}

resource workerReceiver 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(serviceBus.id, workerPrincipalId, receiverRoleId)
  scope: serviceBus
  properties: {
    principalId: workerPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: receiverRoleId
  }
}

resource workerSender 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(serviceBus.id, workerPrincipalId, senderRoleId)
  scope: serviceBus
  properties: {
    principalId: workerPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: senderRoleId
  }
}

module privateEndpoint 'private-endpoint.bicep' = if (enablePrivateNetworking) {
  name: 'servicebus-private-endpoint'
  params: {
    location: location
    endpointName: 'pep-${namespaceName}'
    resourceId: serviceBus.id
    groupIds: ['namespace']
    subnetId: privateEndpointSubnetId
    vnetId: vnetId
    dnsZoneName: 'privatelink.servicebus.windows.net'
    tags: tags
  }
}

output namespaceName string = serviceBus.name
output fullyQualifiedNamespace string = '${serviceBus.name}.servicebus.windows.net'
output topicName string = topic.name
output workerSubscriptionName string = workerSubscription.name
