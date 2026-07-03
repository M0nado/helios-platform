param location string
@minLength(1)
param namePrefix string
@minLength(1)
param environmentName string

var vnetName = take(toLower('${namePrefix}-${environmentName}-hybrid-vnet'), 64)

resource virtualNetwork 'Microsoft.Network/virtualNetworks@2023-11-01' = {
  name: vnetName
  location: location
  properties: {
    addressSpace: {
      addressPrefixes: [
        '10.42.0.0/16'
      ]
    }
    subnets: [
      {
        name: 'control-plane'
        properties: {
          addressPrefix: '10.42.1.0/24'
        }
      }
      {
        name: 'private-endpoints'
        properties: {
          addressPrefix: '10.42.2.0/24'
        }
      }
    ]
  }
}

output virtualNetworkName string = virtualNetwork.name
