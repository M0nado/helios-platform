@description('Azure region for HELIOS shared infrastructure.')
param location string = resourceGroup().location

@description('Environment name, e.g. dev, test, prod.')
@minLength(1)
param environmentName string = 'dev'

@description('Prefix used for globally named resources.')
@minLength(1)
param namePrefix string = 'helios'

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

output storageAccountName string = storage.outputs.storageAccountName
output logAnalyticsWorkspaceName string = observability.outputs.logAnalyticsWorkspaceName
output keyVaultName string = keyVault.outputs.keyVaultName
output keyVaultUri string = keyVault.outputs.keyVaultUri
output virtualNetworkName string = network.outputs.virtualNetworkName
