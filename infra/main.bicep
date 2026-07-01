targetScope = 'resourceGroup'

param location string = resourceGroup().location
param workloadName string = 'helios'
param environmentName string = 'dev'

module identities 'modules/identities.bicep' = {
  name: 'identities'
  params: {
    location: location
    workloadName: workloadName
    environmentName: environmentName
  }
}

module keyVault 'modules/keyvault.bicep' = {
  name: 'keyvault'
  params: {
    location: location
    workloadName: workloadName
    environmentName: environmentName
  }
}

module storage 'modules/storage.bicep' = {
  name: 'storage'
  params: {
    location: location
    workloadName: workloadName
    environmentName: environmentName
  }
}

module monitoring 'modules/monitoring.bicep' = {
  name: 'monitoring'
  params: {
    location: location
    workloadName: workloadName
    environmentName: environmentName
  }
}
