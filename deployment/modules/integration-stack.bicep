param location string
param serviceBusNamespaceName string
param logicAppName string
param actionGroupName string
@secure()
param slackWebhookUrl string
param slackChannel string
param hubspotBaseUrl string
param tags object = {}

var enableSlackForwarding = !empty(slackWebhookUrl)

resource serviceBus 'Microsoft.ServiceBus/namespaces@2024-01-01' = {
  name: serviceBusNamespaceName
  location: location
  sku: {
    name: 'Standard'
    tier: 'Standard'
    capacity: 1
  }
  tags: union(tags, {
    component: 'service-bus'
  })
  properties: {
    publicNetworkAccess: 'Enabled'
    minimumTlsVersion: '1.2'
  }
}

resource hubspotQueue 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
  parent: serviceBus
  name: 'hubspot-sync'
  properties: {
    requiresDuplicateDetection: true
    duplicateDetectionHistoryTimeWindow: 'PT10M'
    enablePartitioning: true
    defaultMessageTimeToLive: 'P7D'
    maxDeliveryCount: 10
    deadLetteringOnMessageExpiration: true
  }
}

resource githubEventsQueue 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
  parent: serviceBus
  name: 'github-control'
  properties: {
    enablePartitioning: true
    defaultMessageTimeToLive: 'P7D'
    maxDeliveryCount: 10
  }
}

resource slackAlertsQueue 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
  parent: serviceBus
  name: 'slack-alerts'
  properties: {
    enablePartitioning: true
    defaultMessageTimeToLive: 'P1D'
    maxDeliveryCount: 5
  }
}

resource slackForwarder 'Microsoft.Logic/workflows@2019-05-01' = if (enableSlackForwarding) {
  name: logicAppName
  location: location
  tags: union(tags, {
    component: 'logic-app'
    integration: 'slack'
    hubspotBaseUrl: hubspotBaseUrl
  })
  properties: {
    state: 'Enabled'
    definition: loadJsonContent('../logicapps/azure-monitor-to-slack.definition.json')
    parameters: {
      slackWebhookUrl: {
        value: slackWebhookUrl
      }
      slackChannel: {
        value: slackChannel
      }
      workflowName: {
        value: logicAppName
      }
    }
  }
}

resource monitorActionGroup 'Microsoft.Insights/actionGroups@2023-01-01' = if (enableSlackForwarding) {
  name: actionGroupName
  location: 'Global'
  tags: union(tags, {
    component: 'alerting'
  })
  properties: {
    enabled: true
    groupShortName: 'HELIOS'
    logicAppReceivers: [
      {
        name: 'slack-forwarder'
        resourceId: slackForwarder.id
        callbackUrl: listCallbackUrl('${slackForwarder.id}/triggers/manual', '2016-06-01').value
        useCommonAlertSchema: true
      }
    ]
  }
}

output serviceBusNamespace string = serviceBus.name
output hubspotQueueName string = hubspotQueue.name
output githubQueueName string = githubEventsQueue.name
output slackQueueName string = slackAlertsQueue.name
output actionGroupId string = enableSlackForwarding ? monitorActionGroup.id : ''
