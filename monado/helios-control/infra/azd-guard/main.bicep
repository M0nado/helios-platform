targetScope = 'resourceGroup'

// Defense in depth for callers that suppress azure.yaml hooks. This template
// contains no resources and fails any direct azd apply. The protected GitHub
// workflow invokes ../main.bicep explicitly and never uses this module.
output directAzdProvisioningDisabled string = fail('Direct azd provisioning is disabled. Use the protected helios-cloud-deploy workflow.')
