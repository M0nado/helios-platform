param location string
param endpointName string
param resourceId string
param groupIds array
param subnetId string
param vnetId string
param dnsZoneName string
param tags object

resource privateDnsZone 'Microsoft.Network/privateDnsZones@2024-06-01' = {
  name: dnsZoneName
  location: 'global'
  tags: tags
}

resource vnetLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2024-06-01' = {
  parent: privateDnsZone
  name: 'link-${uniqueString(vnetId, dnsZoneName)}'
  location: 'global'
  tags: tags
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: vnetId
    }
  }
}

resource endpoint 'Microsoft.Network/privateEndpoints@2024-07-01' = {
  name: endpointName
  location: location
  tags: tags
  properties: {
    subnet: {
      id: subnetId
    }
    privateLinkServiceConnections: [
      {
        name: 'connection'
        properties: {
          privateLinkServiceId: resourceId
          groupIds: groupIds
          privateLinkServiceConnectionState: {
            status: 'Approved'
            description: 'HELIOS managed private endpoint'
            actionsRequired: 'None'
          }
        }
      }
    ]
  }
}

resource dnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2024-07-01' = {
  parent: endpoint
  name: 'default'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'zone'
        properties: {
          privateDnsZoneId: privateDnsZone.id
        }
      }
    ]
  }
}

output privateEndpointId string = endpoint.id
output privateDnsZoneId string = privateDnsZone.id

