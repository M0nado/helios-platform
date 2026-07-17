targetScope = 'resourceGroup'

@description('Azure region for the Helios connector resources.')
param location string = resourceGroup().location

@allowed([
  'dev'
  'test'
  'preview'
  'prod'
])
param environmentName string = 'dev'

param serviceName string = 'helios-connector'

@description('Additional organization tags. HELIOS ownership and environment tags are always enforced by the connector module.')
param commonTags object = {}

@description('Approved immutable image in the dedicated Helios Azure Container Registry.')
param containerImage string

@description('Existing dedicated Azure Container Registry name. Registry creation and role grants are operator-owned.')
@minLength(5)
@maxLength(50)
param containerRegistryName string

@description('Preview-only escape hatch for the documented all-zero image digest. The protected deployment workflow never enables this for apply.')
param allowPreviewPlaceholder bool = false

@description('Client ID of the single-tenant Entra app registration protecting the API.')
param entraClientId string

@description('Tenant containing the connector app registration.')
param entraTenantId string = subscription().tenantId

@description('Object ID of the user or service principal allowed to call the connector.')
@minLength(1)
param allowedPrincipalObjectId string

// azure.yaml/azd resolves this reviewed entry point. The hardened connector
// module remains the single implementation of Container Apps, managed identity,
// Key Vault, monitoring, immutable image binding, and Entra authentication.
// No registry or role assignment is created by this template.
module connector './connector.bicep' = {
  name: 'helios-connector-${environmentName}'
  params: {
    location: location
    environmentName: environmentName
    serviceName: serviceName
    containerImage: containerImage
    containerRegistryName: containerRegistryName
    allowPreviewPlaceholder: allowPreviewPlaceholder
    entraClientId: entraClientId
    entraTenantId: entraTenantId
    allowedPrincipalObjectId: allowedPrincipalObjectId
    commonTags: commonTags
  }
}

output keyVaultName string = connector.outputs.keyVaultName
output containerRegistryName string = connector.outputs.containerRegistryName
output containerRegistryServer string = connector.outputs.containerRegistryServer
output containerAppName string = connector.outputs.containerAppName
output containerAppUrl string = connector.outputs.connectorUrl
output connectorUrl string = connector.outputs.connectorUrl
output connectorMcpUrl string = connector.outputs.connectorMcpUrl
output connectorEntraClientId string = connector.outputs.connectorEntraClientId
output connectorEntraTenantId string = connector.outputs.connectorEntraTenantId
output managedIdentityClientId string = connector.outputs.managedIdentityClientId
