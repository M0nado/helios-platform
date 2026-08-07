@description('Azure region for HELIOS shared infrastructure.')
param location string = resourceGroup().location

@description('Environment name, e.g. dev, test, prod.')
param environmentName string = 'dev'

@description('Prefix used for globally named resources.')
param namePrefix string = 'helios'

@description('Enable Hermes/XCore runner baseline resources.')
param enableHermesXcoreRunner bool = false

@description('Enable Hermes/XCore manual Container Apps Jobs.')
param enableHermesXcoreRunnerJobs bool = false

@description('Allow all-zero preview image digests for runner jobs in planning mode.')
param allowRunnerPreviewPlaceholder bool = true

@description('Immutable image reference for Hermes runner jobs.')
@minLength(80)
param hermesRunnerImage string = 'heliosplaceholderacr.azurecr.io/hermes-runner@sha256:0000000000000000000000000000000000000000000000000000000000000000'

@description('Immutable image reference for XCore runner jobs.')
@minLength(80)
param xcoreRunnerImage string = 'heliosplaceholderacr.azurecr.io/xcore-runner@sha256:0000000000000000000000000000000000000000000000000000000000000000'

@description('Maximum concurrent executions for Hermes/XCore manual runner jobs.')
@minValue(1)
@maxValue(32)
param runnerMaxConcurrentExecutions int = 4

module storage 'modules/storage.bicep' = {
  name: 'storage-${environmentName}'
  params: {
    location: location
    namePrefix: namePrefix
    environmentName: environmentName
  }
}

module keyVault 'modules/keyvault.bicep' = {
  name: 'keyvault-${environmentName}'
  params: {
    location: location
    namePrefix: namePrefix
    environmentName: environmentName
  }
}


module network 'modules/network.bicep' = {
  name: 'network-${environmentName}'
  params: {
    location: location
    namePrefix: namePrefix
    environmentName: environmentName
  }
}

module observability 'modules/observability.bicep' = {
  name: 'observability-${environmentName}'
  params: {
    location: location
    namePrefix: namePrefix
    environmentName: environmentName
  }
}

module xcoreHermesRunner 'modules/xcore-hermes-runner.bicep' = if (enableHermesXcoreRunner) {
  name: 'xcore-hermes-runner-${environmentName}'
  params: {
    location: location
    namePrefix: namePrefix
    environmentName: environmentName
    enableRunner: enableHermesXcoreRunner
    enableRunnerJobs: enableHermesXcoreRunnerJobs
    allowPreviewPlaceholder: allowRunnerPreviewPlaceholder
    hermesRunnerImage: hermesRunnerImage
    xcoreRunnerImage: xcoreRunnerImage
    maxConcurrentExecutions: runnerMaxConcurrentExecutions
  }
}

output storageAccountName string = storage.outputs.storageAccountName
output logAnalyticsWorkspaceName string = observability.outputs.logAnalyticsWorkspaceName
output keyVaultName string = keyVault.outputs.keyVaultName
output keyVaultUri string = keyVault.outputs.keyVaultUri
output virtualNetworkName string = network.outputs.virtualNetworkName
output xcoreHermesRunnerServiceBusNamespace string = enableHermesXcoreRunner ? xcoreHermesRunner!.outputs.serviceBusNamespaceName : ''
output xcoreHermesRunnerHermesQueue string = enableHermesXcoreRunner ? xcoreHermesRunner!.outputs.hermesTaskQueueName : ''
output xcoreHermesRunnerXcoreQueue string = enableHermesXcoreRunner ? xcoreHermesRunner!.outputs.xcoreTaskQueueName : ''
output xcoreHermesRunnerDeadLetterQueue string = enableHermesXcoreRunner ? xcoreHermesRunner!.outputs.deadLetterQueueName : ''
output xcoreHermesRunnerStorageAccountName string = enableHermesXcoreRunner ? xcoreHermesRunner!.outputs.runnerStorageAccountName : ''
output xcoreHermesRunnerManagedIdentityClientId string = enableHermesXcoreRunner ? xcoreHermesRunner!.outputs.runnerManagedIdentityClientId : ''
