targetScope = 'resourceGroup'

@description('Short lowercase platform prefix.')
@minLength(3)
@maxLength(12)
param prefix string = 'helios'

@allowed(['dev', 'test', 'stage', 'prod'])
param environment string

@description('Azure region for the HELIOS fabric.')
param location string = 'centralus'

@description('Owning team or person recorded in resource tags.')
param owner string = 'platform-engineering'

@description('Stable suffix for globally unique resources. Defaults from subscription and environment.')
param uniqueSuffix string = take(uniqueString(subscription().subscriptionId, resourceGroup().name, prefix, environment), 8)

@description('Deploy broker and worker Container Apps. Keep false until images and connector authorization are ready.')
param deployWorkloads bool = false

@description('Enable private VNet integration and private endpoints. Production should use true after DNS is reviewed.')
param enablePrivateNetworking bool = environment == 'prod'


@description('Keep the webhook broker externally reachable for GitHub, Slack, Linear, and Teams callbacks. Signature verification and rate limits remain mandatory.')
param publicWebhookIngress bool = true

@description('Container image for the ingress broker.')
param brokerImage string = 'ghcr.io/m0nado/helios-fabric-broker:2.0.0'

@description('Container image for the delivery worker.')
param workerImage string = 'ghcr.io/m0nado/helios-fabric-worker:2.0.0'

@description('Optional GitHub OIDC principal object ID granted deployment-time rights. Runtime rights use managed identities.')
param deploymentPrincipalObjectId string = ''

@description('Additional non-secret tags.')
param tags object = {}

var resourceGroupName = resourceGroup().name
var commonTags = union({
  'helios:system': 'enterprise-automation-fabric'
  'helios:environment': environment
  'helios:owner': owner
  'helios:managed-by': 'bicep'
  'helios:data-classification': 'confidential'
}, tags)

module foundation 'modules/foundation.bicep' = {
  name: 'helios-fabric-${environment}'
  params: {
    prefix: prefix
    environment: environment
    location: location
    uniqueSuffix: uniqueSuffix
    deployWorkloads: deployWorkloads
    enablePrivateNetworking: enablePrivateNetworking
    publicWebhookIngress: publicWebhookIngress
    brokerImage: brokerImage
    workerImage: workerImage
    deploymentPrincipalObjectId: deploymentPrincipalObjectId
    tags: commonTags
  }
}

output resourceGroupName string = resourceGroup().name
output resourceGroupId string = resourceGroup().id
output brokerUrl string = foundation.outputs.brokerUrl
output keyVaultName string = foundation.outputs.keyVaultName
output serviceBusNamespace string = foundation.outputs.serviceBusNamespace
output evidenceStorageAccount string = foundation.outputs.evidenceStorageAccount
output appConfigurationEndpoint string = foundation.outputs.appConfigurationEndpoint
output workloadIdentityClientIds object = foundation.outputs.workloadIdentityClientIds
