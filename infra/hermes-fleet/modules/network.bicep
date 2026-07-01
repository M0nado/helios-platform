@description('Azure region for all networking resources.')
param location string

@description('Prefix used to build resource names.')
param namePrefix string

@description('Address space for the platform virtual network.')
param addressSpace string

@description('Subnet used for App Service VNet integration.')
param appSubnetPrefix string

@description('Subnet used for Storage and Cosmos DB private access patterns.')
param dataSubnetPrefix string

@description('Subnet used for the jumpbox or management VM.')
param computeSubnetPrefix string

@description('Tags applied to networking resources.')
param tags object = {}

resource vnet 'Microsoft.Network/virtualNetworks@2023-11-01' = {
  name: '${namePrefix}-vnet'
  location: location
  tags: tags
  properties: {
    addressSpace: {
      addressPrefixes: [
        addressSpace
      ]
    }
    subnets: [
      {
        name: 'app'
        properties: {
          addressPrefix: appSubnetPrefix
          serviceEndpoints: [
            {
              service: 'Microsoft.Storage'
            }
            {
              service: 'Microsoft.AzureCosmosDB'
            }
          ]
          delegations: [
            {
              name: 'appservice'
              properties: {
                serviceName: 'Microsoft.Web/serverFarms'
              }
            }
          ]
        }
      }
      {
        name: 'data'
        properties: {
          addressPrefix: dataSubnetPrefix
          serviceEndpoints: [
            {
              service: 'Microsoft.Storage'
            }
            {
              service: 'Microsoft.AzureCosmosDB'
            }
          ]
        }
      }
      {
        name: 'compute'
        properties: {
          addressPrefix: computeSubnetPrefix
        }
      }
    ]
  }
}

output virtualNetworkId string = vnet.id
output appSubnetId string = resourceId('Microsoft.Network/virtualNetworks/subnets', vnet.name, 'app')
output dataSubnetId string = resourceId('Microsoft.Network/virtualNetworks/subnets', vnet.name, 'data')
output computeSubnetId string = resourceId('Microsoft.Network/virtualNetworks/subnets', vnet.name, 'compute')
