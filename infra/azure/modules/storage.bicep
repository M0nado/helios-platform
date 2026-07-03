param location string
@minLength(1)
param namePrefix string
@minLength(1)
param environmentName string

var safePrefix = empty(namePrefix) ? 'helios' : namePrefix
var safeEnvironment = empty(environmentName) ? 'dev' : environmentName
var safeName = toLower('st${uniqueString(resourceGroup().id)}${replace(safePrefix, '-', '')}${replace(safeEnvironment, '-', '')}reports')

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: take(safeName, 24)
  location: location
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

output storageAccountName string = storage.name
