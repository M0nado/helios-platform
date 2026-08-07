@description('Azure region for HELIOS shared infrastructure.')
param location string = resourceGroup().location

@description('Environment name, e.g. dev, test, prod.')
param environmentName string = 'dev'

@description('Prefix used for globally named resources.')
param namePrefix string = 'helios'

@description('Deploy a Linux VM in the control-plane subnet.')
param deployVm bool = false

@description('Admin username for VM provisioning.')
param vmAdminUsername string = 'heliosadmin'

@secure()
@description('SSH public key for VM provisioning.')
param vmAdminPublicKey string = ''

@description('Azure VM size for the optional VM deployment.')
param vmSize string = 'Standard_B2s'

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

module vm 'modules/vm.bicep' = if (deployVm && !empty(vmAdminPublicKey)) {
  name: 'vm-${environmentName}'
  params: {
    location: location
    namePrefix: namePrefix
    environmentName: environmentName
    virtualNetworkName: network.outputs.virtualNetworkName
    subnetName: 'control-plane'
    adminUsername: vmAdminUsername
    adminPublicKey: vmAdminPublicKey
    vmSize: vmSize
  }
}

output storageAccountName string = storage.outputs.storageAccountName
output logAnalyticsWorkspaceName string = observability.outputs.logAnalyticsWorkspaceName
output keyVaultName string = keyVault.outputs.keyVaultName
output keyVaultUri string = keyVault.outputs.keyVaultUri
output virtualNetworkName string = network.outputs.virtualNetworkName
output vmName string = deployVm && !empty(vmAdminPublicKey) ? vm.outputs.vmName : ''
