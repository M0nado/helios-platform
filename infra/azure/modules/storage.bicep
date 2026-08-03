param location string
param namePrefix string
param environmentName string

var safeName = toLower(replace('${namePrefix}${environmentName}reports', '-', ''))

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
