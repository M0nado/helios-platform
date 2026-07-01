targetScope = 'resourceGroup'

@description('Short workload name used as the prefix for Azure resources.')
param workloadName string = 'monadoblade'

@description('Deployment environment name such as dev, test, or prod.')
param environmentName string = 'dev'

@description('Azure region for all resources in this deployment.')
param location string = resourceGroup().location

@description('Address space for the platform virtual network.')
param addressSpace string = '10.40.0.0/16'

@description('Subnet used for App Service VNet integration.')
param appSubnetPrefix string = '10.40.1.0/24'

@description('Subnet used for private data services such as Storage and Cosmos DB.')
param dataSubnetPrefix string = '10.40.2.0/24'

@description('Subnet used for the management or jumpbox VM.')
param computeSubnetPrefix string = '10.40.3.0/24'

@description('Administrator username for the optional jumpbox VM.')
param vmAdminUsername string = 'azureuser'

@secure()
@description('Administrator password for the optional jumpbox VM.')
param vmAdminPassword string

@description('App Service Plan SKU name.')
param appServiceSkuName string = 'P1v3'

@description('Runtime stack for the Linux web apps.')
param linuxFxVersion string = 'DOTNETCORE|8.0'

@description('Cosmos DB consistency level.')
@allowed([
  'Session'
  'Strong'
  'BoundedStaleness'
  'Eventual'
  'ConsistentPrefix'
])
param cosmosDbConsistency string = 'Session'

@description('Whether to deploy the management jumpbox VM.')
param deployJumpboxVm bool = false

@description('Additional tags applied to all resources.')
param tags object = {}

var namePrefix = toLower('${workloadName}-${environmentName}')
var defaultTags = union({
  workload: workloadName
  environment: environmentName
  managedBy: 'Azure SRE Agent'
  architecture: 'separate-infra-folder'
}, tags)

module network './modules/network.bicep' = {
  name: 'network'
  params: {
    location: location
    namePrefix: namePrefix
    addressSpace: addressSpace
    appSubnetPrefix: appSubnetPrefix
    dataSubnetPrefix: dataSubnetPrefix
    computeSubnetPrefix: computeSubnetPrefix
    tags: defaultTags
  }
}

module data './modules/data-stack.bicep' = {
  name: 'data'
  params: {
    location: location
    namePrefix: namePrefix
    appSubnetId: network.outputs.appSubnetId
    dataSubnetId: network.outputs.dataSubnetId
    cosmosDbConsistency: cosmosDbConsistency
    tags: defaultTags
  }
}

module web './modules/web-stack.bicep' = {
  name: 'web'
  params: {
    location: location
    namePrefix: namePrefix
    appSubnetId: network.outputs.appSubnetId
    appServiceSkuName: appServiceSkuName
    linuxFxVersion: linuxFxVersion
    storageAccountName: data.outputs.storageAccountName
    cosmosEndpoint: data.outputs.cosmosEndpoint
    cosmosAccountName: data.outputs.cosmosAccountName
    cosmosDatabaseName: data.outputs.cosmosDatabaseName
    tags: defaultTags
  }
}

module compute './modules/compute.bicep' = if (deployJumpboxVm) {
  name: 'compute'
  params: {
    location: location
    namePrefix: namePrefix
    subnetId: network.outputs.computeSubnetId
    adminUsername: vmAdminUsername
    adminPassword: vmAdminPassword
    tags: defaultTags
  }
}

output virtualNetworkId string = network.outputs.virtualNetworkId
output appSubnetId string = network.outputs.appSubnetId
output dataSubnetId string = network.outputs.dataSubnetId
output computeSubnetId string = network.outputs.computeSubnetId
output storageAccountName string = data.outputs.storageAccountName
output cosmosAccountName string = data.outputs.cosmosAccountName
output cosmosEndpoint string = data.outputs.cosmosEndpoint
output cosmosDatabaseName string = data.outputs.cosmosDatabaseName
output appServicePlanName string = web.outputs.appServicePlanName
output apiWebAppName string = web.outputs.apiWebAppName
output portalWebAppName string = web.outputs.portalWebAppName
output apiHostname string = web.outputs.apiHostname
output portalHostname string = web.outputs.portalHostname
output jumpboxVmName string = deployJumpboxVm ? compute!.outputs.vmName : ''
