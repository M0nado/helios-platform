@description('Azure region for Hermes/XCore runner resources.')
param location string

@description('Shared naming prefix.')
param namePrefix string

@description('Environment name, for example dev, test, or prod.')
param environmentName string

@description('Create Hermes/XCore runner baseline resources (identity, Service Bus queues, storage).')
param enableRunner bool = false

@description('Create manual-trigger Container Apps Jobs for Hermes and XCore execution.')
param enableRunnerJobs bool = false

@description('Allows all-zero preview image digests only for non-production planning runs.')
param allowPreviewPlaceholder bool = true

@description('Immutable image reference for the Hermes runner job.')
@minLength(80)
param hermesRunnerImage string = 'heliosplaceholderacr.azurecr.io/hermes-runner@sha256:0000000000000000000000000000000000000000000000000000000000000000'

@description('Immutable image reference for the XCore runner job.')
@minLength(80)
param xcoreRunnerImage string = 'heliosplaceholderacr.azurecr.io/xcore-runner@sha256:0000000000000000000000000000000000000000000000000000000000000000'

@description('Maximum concurrent manual runner executions per job.')
@minValue(1)
@maxValue(32)
param maxConcurrentExecutions int = 4

var suffix = uniqueString(resourceGroup().id, environmentName, namePrefix)
var baseName = toLower(replace('${namePrefix}${environmentName}', '-', ''))
var serviceBusName = take('${baseName}${suffix}sb', 50)
var storageName = take('${baseName}${suffix}runner', 24)
var identityName = '${namePrefix}-${environmentName}-hxr-id'
var workspaceName = '${namePrefix}-${environmentName}-hxr-logs'
var containerEnvName = '${namePrefix}-${environmentName}-hxr-env'
var hermesQueueName = 'hermes-runner-requests'
var xcoreQueueName = 'xcore-evaluation-requests'
var deadLetterQueueName = 'xcore-hermes-deadletter'
var previewPlaceholderDigest = '0000000000000000000000000000000000000000000000000000000000000000'
var tags = {
  system: 'HELIOS'
  environment: environmentName
  managedBy: 'Bicep'
  workload: 'xcore-hermes-runner'
}

var hermesDigestParts = split(toLower(hermesRunnerImage), '@sha256:')
var xcoreDigestParts = split(toLower(xcoreRunnerImage), '@sha256:')
var hermesDigest = length(hermesDigestParts) == 2 ? hermesDigestParts[1] : ''
var xcoreDigest = length(xcoreDigestParts) == 2 ? xcoreDigestParts[1] : ''
var hermesDigestRemainder = replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(hermesDigest, '0', ''), '1', ''), '2', ''), '3', ''), '4', ''), '5', ''), '6', ''), '7', ''), '8', ''), '9', ''), 'a', ''), 'b', ''), 'c', ''), 'd', ''), 'e', ''), 'f', '')
var xcoreDigestRemainder = replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(xcoreDigest, '0', ''), '1', ''), '2', ''), '3', ''), '4', ''), '5', ''), '6', ''), '7', ''), '8', ''), '9', ''), 'a', ''), 'b', ''), 'c', ''), 'd', ''), 'e', ''), 'f', '')
var hermesImageValid = length(hermesDigestParts) == 2 && length(hermesDigest) == 64 && empty(hermesDigestRemainder) && (allowPreviewPlaceholder || hermesDigest != previewPlaceholderDigest)
var xcoreImageValid = length(xcoreDigestParts) == 2 && length(xcoreDigest) == 64 && empty(xcoreDigestRemainder) && (allowPreviewPlaceholder || xcoreDigest != previewPlaceholderDigest)
var validatedHermesRunnerImage = !enableRunnerJobs || hermesImageValid ? hermesRunnerImage : fail('hermesRunnerImage must be an immutable sha256 digest (preview placeholder allowed only when explicitly enabled).')
var validatedXcoreRunnerImage = !enableRunnerJobs || xcoreImageValid ? xcoreRunnerImage : fail('xcoreRunnerImage must be an immutable sha256 digest (preview placeholder allowed only when explicitly enabled).')

resource runnerIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = if (enableRunner) {
  name: identityName
  location: location
  tags: tags
}

resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2024-01-01' = if (enableRunner) {
  name: serviceBusName
  location: location
  tags: tags
  sku: {
    name: 'Standard'
    tier: 'Standard'
    capacity: 1
  }
  properties: {
    disableLocalAuth: true
    minimumTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

resource hermesQueue 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = if (enableRunner) {
  parent: serviceBusNamespace
  name: hermesQueueName
  properties: {
    lockDuration: 'PT1M'
    maxDeliveryCount: 10
    requiresDuplicateDetection: true
    duplicateDetectionHistoryTimeWindow: 'PT10M'
    deadLetteringOnMessageExpiration: true
  }
}

resource xcoreQueue 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = if (enableRunner) {
  parent: serviceBusNamespace
  name: xcoreQueueName
  properties: {
    lockDuration: 'PT1M'
    maxDeliveryCount: 10
    requiresDuplicateDetection: true
    duplicateDetectionHistoryTimeWindow: 'PT10M'
    deadLetteringOnMessageExpiration: true
  }
}

resource deadLetterQueue 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = if (enableRunner) {
  parent: serviceBusNamespace
  name: deadLetterQueueName
  properties: {
    lockDuration: 'PT1M'
    maxDeliveryCount: 20
    deadLetteringOnMessageExpiration: true
  }
}

resource runnerStorage 'Microsoft.Storage/storageAccounts@2023-05-01' = if (enableRunner) {
  name: storageName
  location: location
  tags: tags
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = if (enableRunner) {
  parent: runnerStorage
  name: 'default'
}

resource runnerArtifactsContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = if (enableRunner) {
  parent: blobService
  name: 'runner-artifacts'
  properties: {
    publicAccess: 'None'
  }
}

resource workspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = if (enableRunner && enableRunnerJobs) {
  name: workspaceName
  location: location
  tags: tags
  properties: {
    retentionInDays: 30
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
  }
}

resource containerEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' = if (enableRunner && enableRunnerJobs) {
  name: containerEnvName
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: workspace!.properties.customerId
        sharedKey: workspace!.listKeys().primarySharedKey
      }
    }
  }
}

resource hermesRunnerJob 'Microsoft.App/jobs@2024-03-01' = if (enableRunner && enableRunnerJobs) {
  name: '${namePrefix}-${environmentName}-hermes-runner'
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${runnerIdentity.id}': {}
    }
  }
  properties: {
    environmentId: containerEnvironment.id
    configuration: {
      triggerType: 'Manual'
      replicaTimeout: 1800
      replicaRetryLimit: 1
      manualTriggerConfig: {
        parallelism: maxConcurrentExecutions
        replicaCompletionCount: 1
      }
    }
    template: {
      containers: [
        {
          name: 'hermes-runner'
          image: validatedHermesRunnerImage
          env: [
            {
              name: 'HELIOS_EXECUTION_MODE'
              value: 'dry-run'
            }
            {
              name: 'HELIOS_RUNNER_ROLE'
              value: 'hermes-orchestrator'
            }
            {
              name: 'HELIOS_SERVICEBUS_NAMESPACE'
              value: serviceBusNamespace.name
            }
            {
              name: 'HELIOS_SERVICEBUS_QUEUE'
              value: hermesQueue.name
            }
          ]
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
        }
      ]
    }
  }
}

resource xcoreRunnerJob 'Microsoft.App/jobs@2024-03-01' = if (enableRunner && enableRunnerJobs) {
  name: '${namePrefix}-${environmentName}-xcore-runner'
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${runnerIdentity.id}': {}
    }
  }
  properties: {
    environmentId: containerEnvironment.id
    configuration: {
      triggerType: 'Manual'
      replicaTimeout: 1800
      replicaRetryLimit: 1
      manualTriggerConfig: {
        parallelism: maxConcurrentExecutions
        replicaCompletionCount: 1
      }
    }
    template: {
      containers: [
        {
          name: 'xcore-runner'
          image: validatedXcoreRunnerImage
          env: [
            {
              name: 'HELIOS_EXECUTION_MODE'
              value: 'dry-run'
            }
            {
              name: 'HELIOS_RUNNER_ROLE'
              value: 'xcore-evaluator'
            }
            {
              name: 'HELIOS_SERVICEBUS_NAMESPACE'
              value: serviceBusNamespace.name
            }
            {
              name: 'HELIOS_SERVICEBUS_QUEUE'
              value: xcoreQueue.name
            }
          ]
          resources: {
            cpu: json('1.0')
            memory: '2Gi'
          }
        }
      ]
    }
  }
}

output serviceBusNamespaceName string = enableRunner ? serviceBusNamespace.name : ''
output hermesTaskQueueName string = enableRunner ? hermesQueue.name : ''
output xcoreTaskQueueName string = enableRunner ? xcoreQueue.name : ''
output deadLetterQueueName string = enableRunner ? deadLetterQueue.name : ''
output runnerStorageAccountName string = enableRunner ? runnerStorage.name : ''
output runnerArtifactsContainerName string = enableRunner ? runnerArtifactsContainer.name : ''
output runnerManagedIdentityClientId string = enableRunner ? runnerIdentity!.properties.clientId : ''
output runnerEnvironmentName string = (enableRunner && enableRunnerJobs) ? containerEnvironment.name : ''
output hermesRunnerJobName string = (enableRunner && enableRunnerJobs) ? hermesRunnerJob.name : ''
output xcoreRunnerJobName string = (enableRunner && enableRunnerJobs) ? xcoreRunnerJob.name : ''
