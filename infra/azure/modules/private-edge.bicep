param location string
param namePrefix string
param environmentName string
param apimSubnetId string
param publisherEmail string
param publisherName string = 'HELIOS Platform'

var suffix = uniqueString(resourceGroup().id, environmentName)
var tags = {
  system: 'HELIOS'
  environment: environmentName
  managedBy: 'Bicep'
  'helios-managed': 'true'
  externalEdge: 'approved'
}

resource apim 'Microsoft.ApiManagement/service@2023-09-01-preview' = {
  name: take('${namePrefix}-${environmentName}-apim-${suffix}', 50)
  location: location
  tags: tags
  sku: {
    name: 'Premium'
    capacity: 1
  }
  properties: {
    publisherEmail: publisherEmail
    publisherName: publisherName
    publicNetworkAccess: 'Disabled'
    virtualNetworkType: 'Internal'
    virtualNetworkConfiguration: { subnetResourceId: apimSubnetId }
    disableGateway: false
  }
}

resource frontDoorProfile 'Microsoft.Cdn/profiles@2024-02-01' = {
  name: '${namePrefix}-${environmentName}-edge'
  location: 'global'
  tags: tags
  sku: { name: 'Premium_AzureFrontDoor' }
}

resource frontDoorEndpoint 'Microsoft.Cdn/profiles/afdEndpoints@2024-02-01' = {
  parent: frontDoorProfile
  name: '${namePrefix}-${environmentName}-edge'
  location: 'global'
  tags: tags
  properties: { enabledState: 'Enabled' }
}

resource originGroup 'Microsoft.Cdn/profiles/originGroups@2024-02-01' = {
  parent: frontDoorProfile
  name: 'apim-private-origin'
  properties: {
    healthProbeSettings: {
      probePath: '/health'
      probeRequestType: 'GET'
      probeProtocol: 'Https'
      probeIntervalInSeconds: 30
    }
    loadBalancingSettings: {
      sampleSize: 4
      successfulSamplesRequired: 3
      additionalLatencyInMilliseconds: 0
    }
  }
}

resource apimOrigin 'Microsoft.Cdn/profiles/originGroups/origins@2024-02-01' = {
  parent: originGroup
  name: 'apim'
  properties: {
    hostName: '${apim.name}.azure-api.net'
    httpPort: 80
    httpsPort: 443
    originHostHeader: '${apim.name}.azure-api.net'
    priority: 1
    weight: 1000
    enabledState: 'Enabled'
    enforceCertificateNameCheck: true
    sharedPrivateLinkResource: {
      privateLink: { id: apim.id }
      privateLinkLocation: location
      groupId: 'Gateway'
      requestMessage: 'HELIOS Front Door Premium approved private edge'
    }
  }
}

resource edgeRoute 'Microsoft.Cdn/profiles/afdEndpoints/routes@2024-02-01' = {
  parent: frontDoorEndpoint
  name: 'all-approved-traffic'
  properties: {
    originGroup: { id: originGroup.id }
    supportedProtocols: ['Https']
    patternsToMatch: ['/*']
    forwardingProtocol: 'HttpsOnly'
    linkToDefaultDomain: 'Enabled'
    httpsRedirect: 'Enabled'
    enabledState: 'Enabled'
  }
}

output frontDoorEndpointHostName string = frontDoorEndpoint.properties.hostName
output apimServiceId string = apim.id
output directPublicIngress string = 'Disabled'
